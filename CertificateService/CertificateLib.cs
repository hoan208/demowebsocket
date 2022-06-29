using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using System.Text;
using System.Xml;
using System.Windows.Interop;
using System.Windows;
using System.Web;

namespace CertificateService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Feed1" in both code and config file together.
    public class CertificateLib : ICertificateLib
    {
        public string GetData(string value)
        {
            return string.Format("You entered: {0}", value);
        }

        /// <summary>
        /// Lấy ra thông tin chữ ký số dưới dạng xml
        /// </summary>
        /// <returns></returns>
        public string GetCertificate()
        {         
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("SD");
            xmlDoc.AppendChild(rootNode);

            try
            {             
                X509Certificate2Collection keyStore = new X509Certificate2Collection();
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                keyStore.AddRange(store.Certificates);
                store.Close();

                X509Certificate2 cert = null;

                try
                {
                    //IntPtr windowHandle = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                    //cert = X509Certificate2UI.SelectFromCollection(keyStore, 
                    //    "Chứng thư số ký", "Chọn chứng thư số ký", 
                    //    X509SelectionFlag.SingleSelection, windowHandle)[0];
                    cert = X509Certificate2UI.SelectFromCollection(keyStore,
                        "Chứng thư số ký", "Chọn chứng thư số ký",
                        X509SelectionFlag.SingleSelection)[0];
                }
                catch
                {                               
                    return "Error";
                }

                if (cert != null)
                {
                    //Thông tin chứng chỉ số
                    string Subject = cert.SubjectName.Name.ToLower();     

                    if (Subject.Contains("công ty cp chứng khoán fpt") == false)
                    {
                        if (Subject.Contains("công ty cổ phần chứng khoán fpt") == false)
                        {
                            return "check company name";
                        }                           
                    }                    

                    //Ngày hết hạn hết hiệu lực
                    if (cert.NotAfter < DateTime.Now)
                    {
                        //return cert.NotAfter.ToShortDateString();
                        return "chain certificate is expired";
                    }

                    //Ngày hiệu lực sớm hơn ngày hiện tại
                    if (cert.NotBefore > DateTime.Now)
                    {
                        //return cert.NotAfter.ToShortDateString();
                        return "valid date greater than date";
                    }
                    //signedElement.Prefix = "ds";
                    //signedElement.FirstChild.Prefix = "ds";
                    //signedElement.FirstChild.FirstChild.Prefix = "ds";

                    SignedXml signedXml = new SignedXml(xmlDoc);
                    signedXml.SigningKey = cert.PrivateKey;

                    // Create a reference to be signed.
                    Reference reference = new Reference();
                    reference.Uri = "";

                    // Add an enveloped transformation to the reference.            
                    XmlDsigEnvelopedSignatureTransform env =
                       new XmlDsigEnvelopedSignatureTransform(true);
                    reference.AddTransform(env);

                    //canonicalize
                    XmlDsigC14NTransform c14t = new XmlDsigC14NTransform();
                    reference.AddTransform(c14t);

                    KeyInfo keyInfo = new KeyInfo();
                    KeyInfoX509Data keyInfoData = new KeyInfoX509Data(cert);

                    keyInfoData.AddSubjectName(cert.SubjectName.Name);

                    KeyInfoName kin = new KeyInfoName();

                    kin.Value = "Public key of certificate";
                    RSACryptoServiceProvider rsaprovider = (RSACryptoServiceProvider)cert.PublicKey.Key;

                    //RSACng rsaprovider = (RSACng)cert.GetRSAPublicKey();
                    RSAKeyValue rkv = new RSAKeyValue(rsaprovider);

                    keyInfo.AddClause(kin);
                    keyInfo.AddClause(rkv);
                    keyInfo.AddClause(keyInfoData);

                    signedXml.KeyInfo = keyInfo;

                    // Add the reference to the SignedXml object.
                    signedXml.AddReference(reference);

                    // Compute the signature.
                    signedXml.ComputeSignature();

                    // Get the XML representation of the signature and save 
                    // it to an XmlElement object.
                    XmlElement xmlDigitalSignature = signedXml.GetXml();

                    xmlDoc.DocumentElement.AppendChild(
                    xmlDoc.ImportNode(xmlDigitalSignature, true));                    
                }
                else
                {
                    return "Error";
                }
            }
            catch (Exception)
            {
                return "Error";
            }

            return xmlDoc.OuterXml;
        }

        /// <summary>
        /// Lấy ra thông tin chứng thư số trong nghiệp vụ đăng ký sử dụng HĐĐT
        /// </summary>
        /// <returns></returns>
        public ChungThuSo GetCertificate2()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("SD");
            xmlDoc.AppendChild(rootNode);

            try
            {
                X509Certificate2Collection keyStore = new X509Certificate2Collection();
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                keyStore.AddRange(store.Certificates);
                store.Close();

                X509Certificate2 cert = null;

                cert = X509Certificate2UI.SelectFromCollection(keyStore,
                        "Chứng thư số ký", "Chọn chứng thư số ký",
                        X509SelectionFlag.SingleSelection)[0];
                
                return new ChungThuSo { STT = "1", 
                                        Seri = cert.GetSerialNumberString(), 
                                        TTChuc = cert.Issuer, 
                                        TNgay = cert.NotBefore.ToString("yyyy-MM-ddTHH:mm:ss"), 
                                        DNgay = cert.NotAfter.ToString("yyyy-MM-ddTHH:mm:ss") 
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Thêm vùng bảo vệ vào chữ ký số cho file xml đăng ký sử dụng và hóa đơn sai sót
        /// </summary>
        /// <param name="filePath">link file hoặc data</param>
        /// <param name="uri1">vùng bảo vệ 1 </param>
        /// <param name="uri2">vùng bảo vệ 2</param>
        /// <returns></returns>
        public string GetXMLFile(string xmlData, string uri1, string uri2)
        {
            //var ee = HttpContext.Current.Request.;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            //if (filePath.Contains("C:/InvoiceDataXML/")) xmlDoc.Load(filePath);
            xmlDoc.LoadXml(xmlData);
            try
            {
                X509Certificate2Collection keyStore = new X509Certificate2Collection();
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                keyStore.AddRange(store.Certificates);
                store.Close();

                X509Certificate2 cert = null;

                cert = X509Certificate2UI.SelectFromCollection(keyStore,
                        "Chứng thư số ký", "Chọn chứng thư số ký",
                        X509SelectionFlag.SingleSelection)[0];

                if (cert != null)
                {
                    //Thông tin chứng chỉ số
                    string Subject = cert.SubjectName.Name.ToLower();

                    if (Subject.Contains("công ty cp chứng khoán fpt") == false)
                    {
                        if (Subject.Contains("công ty cổ phần chứng khoán fpt") == false)
                        {
                            return "check company name";
                        }
                    }

                    //Ngày hết hạn hết hiệu lực
                    if (cert.NotAfter < DateTime.Now)
                    {
                        return "chain certificate is expired";
                    }

                    //Ngày hiệu lực sớm hơn ngày hiện tại
                    if (cert.NotBefore > DateTime.Now)
                    {
                        //return cert.NotAfter.ToShortDateString();
                        return "valid date greater than date";
                    }

                    string signatureCanonicalizationMethod = "http://www.w3.org/2001/10/xml-exc-c14n#";
                    string signatureMethod = @"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                    string digestMethod = @"http://www.w3.org/2001/04/xmlenc#sha256";

                    string signatureReferenceURI = "#" + uri1;
                    string signatureReferenceURI2 = "#" + uri2;

                    SignedXml signer = new SignedXml(xmlDoc);
                    KeyInfo keyInfo = new KeyInfo();
                    KeyInfoX509Data keyInfoData = new KeyInfoX509Data(cert);

                    keyInfoData.AddSubjectName(cert.SubjectName.Name);
                    keyInfo.AddClause(keyInfoData);

                    signer.SigningKey = cert.PrivateKey;
                    // signer.KeyInfo = new KeyInfo();
                    signer.KeyInfo = keyInfo;


                    signer.SignedInfo.CanonicalizationMethod = signatureCanonicalizationMethod;
                    signer.SignedInfo.SignatureMethod = signatureMethod;

                    //XmlDsigEnvelopedSignatureTransform envelopeTransform = new XmlDsigEnvelopedSignatureTransform();
                    XmlDsigExcC14NTransform cn14Transform = new XmlDsigExcC14NTransform();

                    Reference signatureReference = new Reference("#FATCA");
                    signatureReference.Uri = signatureReferenceURI;
                    //signatureReference.AddTransform(envelopeTransform);
                    signatureReference.AddTransform(cn14Transform);
                    signatureReference.DigestMethod = digestMethod;

                    signer.AddReference(signatureReference);

                    Reference signatureReference2 = new Reference("#FATCA");
                    signatureReference2.Uri = signatureReferenceURI2;
                    //signatureReference2.AddTransform(envelopeTransform);
                    signatureReference2.AddTransform(cn14Transform);
                    signatureReference2.DigestMethod = digestMethod;

                    signer.AddReference(signatureReference2);

                    signer.ComputeSignature();

                    XmlElement xmlDigitalSignature = signer.GetXml();
                    xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));
                }
                else
                {
                    return "Error";
                }
            }
            catch (Exception)
            {
                return "Error";
            }
            return xmlDoc.OuterXml;
        }

        public string GetXMLFileBTH(string filePath, string uri1, string uri2)
        {
            //var ee = HttpContext.Current.Request.;
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.PreserveWhitespace = true;
            //if (filePath.Contains("C:/InvoiceDataXML/")) xmlDoc.Load(filePath);
            xmlDoc.Load(filePath);
            try
            {
                X509Certificate2Collection keyStore = new X509Certificate2Collection();
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                keyStore.AddRange(store.Certificates);
                store.Close();

                X509Certificate2 cert = null;

                cert = X509Certificate2UI.SelectFromCollection(keyStore,
                        "Chứng thư số ký", "Chọn chứng thư số ký",
                        X509SelectionFlag.SingleSelection)[0];

                if (cert != null)
                {
                    //Thông tin chứng chỉ số
                    string Subject = cert.SubjectName.Name.ToLower();

                    if (Subject.Contains("công ty cp chứng khoán fpt") == false)
                    {
                        if (Subject.Contains("công ty cổ phần chứng khoán fpt") == false)
                        {
                            return "check company name";
                        }
                    }

                    //Ngày hết hạn hết hiệu lực
                    if (cert.NotAfter < DateTime.Now)
                    {
                        return "chain certificate is expired";
                    }

                    //Ngày hiệu lực sớm hơn ngày hiện tại
                    if (cert.NotBefore > DateTime.Now)
                    {
                        //return cert.NotAfter.ToShortDateString();
                        return "valid date greater than date";
                    }

                    string signatureCanonicalizationMethod = "http://www.w3.org/2001/10/xml-exc-c14n#";
                    string signatureMethod = @"http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                    string digestMethod = @"http://www.w3.org/2001/04/xmlenc#sha256";

                    string signatureReferenceURI = "#" + uri1;
                    string signatureReferenceURI2 = "#" + uri2;

                    SignedXml signer = new SignedXml(xmlDoc);
                    KeyInfo keyInfo = new KeyInfo();
                    KeyInfoX509Data keyInfoData = new KeyInfoX509Data(cert);

                    keyInfoData.AddSubjectName(cert.SubjectName.Name);
                    keyInfo.AddClause(keyInfoData);

                    signer.SigningKey = cert.PrivateKey;
                    // signer.KeyInfo = new KeyInfo();
                    signer.KeyInfo = keyInfo;


                    signer.SignedInfo.CanonicalizationMethod = signatureCanonicalizationMethod;
                    signer.SignedInfo.SignatureMethod = signatureMethod;

                    //XmlDsigEnvelopedSignatureTransform envelopeTransform = new XmlDsigEnvelopedSignatureTransform();
                    XmlDsigExcC14NTransform cn14Transform = new XmlDsigExcC14NTransform();

                    Reference signatureReference = new Reference("#FATCA");
                    signatureReference.Uri = signatureReferenceURI;
                    //signatureReference.AddTransform(envelopeTransform);
                    signatureReference.AddTransform(cn14Transform);
                    signatureReference.DigestMethod = digestMethod;

                    signer.AddReference(signatureReference);

                    Reference signatureReference2 = new Reference("#FATCA");
                    signatureReference2.Uri = signatureReferenceURI2;
                    //signatureReference2.AddTransform(envelopeTransform);
                    signatureReference2.AddTransform(cn14Transform);
                    signatureReference2.DigestMethod = digestMethod;

                    signer.AddReference(signatureReference2);

                    signer.ComputeSignature();

                    XmlElement xmlDigitalSignature = signer.GetXml();
                    xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xmlDigitalSignature, true));
                }
                else
                {
                    return "Error";
                }
            }
            catch (Exception)
            {
                return "Error";
            }
            return xmlDoc.OuterXml;
        }
    }
}
