using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Syndication;
using System.ServiceModel.Web;
using System.Text;

namespace CertificateService
{
    [ServiceContract]
    public interface ICertificateLib
    {
        [OperationContract]
        [WebInvoke(Method = "GET", UriTemplate = "/GetData/{value}", ResponseFormat = WebMessageFormat.Json)]
        string GetData(string value);

        [OperationContract]
        [WebGet(UriTemplate = "/GetCertificate", ResponseFormat = WebMessageFormat.Json)]
        string GetCertificate();

        [OperationContract]
        [WebGet(UriTemplate = "/GetCertificate2", ResponseFormat = WebMessageFormat.Json)]
        ChungThuSo GetCertificate2();

        //[OperationContract]
        //[WebInvoke(Method = "POST", UriTemplate = "/GetXMLFile?xmlData={xmlData}&uri1={uri1}&uri2={uri2}", ResponseFormat = WebMessageFormat.Json)]
        //string GetXMLFile(string xmlData, string uri1, string uri2);

        [OperationContract]
        [WebGet(UriTemplate = "/GetXMLFile?xmlData={xmlData}&uri1={uri1}&uri2={uri2}", ResponseFormat = WebMessageFormat.Json)]
        string GetXMLFile(string xmlData, string uri1, string uri2);

        [OperationContract]
        [WebGet(UriTemplate = "/GetXMLFileBTH?filePath={filePath}&uri1={uri1}&uri2={uri2}", ResponseFormat = WebMessageFormat.Json)]
        string GetXMLFileBTH(string filePath, string uri1, string uri2);
    }
}

