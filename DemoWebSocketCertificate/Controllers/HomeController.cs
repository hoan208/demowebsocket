
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using System.Xml;
using System.Web.Configuration;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Net.WebSockets;
using WebSocketSharp.Server;

namespace DemoWebSocketCertificate.Controllers
{

    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }
        [HttpPost]
        public JsonResult GetCer()
        {

            //var ws = new WebSocket("wss://127.0.0.1:80/");
            //string _certificatePath = "\\cert\\public_privatekey.pfx";
            //X509Certificate2 x509 = new X509Certificate2(_certificatePath, "mypass");
            //X509CertificateCollection xcol = new X509CertificateCollection();
            //xcol.Add(x509);

            //ws.SslConfiguration = new WebSocketSharp.Net.ClientSslConfiguration("127.0.0.1", xcol, System.Security.Authentication.SslProtocols.Default, false);
            ////ws.SslConfiguration.ClientCertificates = new X509CertificateCollection();
            ////ws.SslConfiguration.ClientCertificates.Add(x509);

            //ws.OnOpen += Ws_OnOpen;
            //ws.OnMessage += Ws_OnMessage;
            //ws.OnError += Ws_OnError;
            //ws.OnClose += Ws_OnClose;
            //ws.Connect();

            //return null;


            try
            {
                //    X509Store store = new X509Store(StoreLocation.CurrentUser);
                //    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                //    X509Certificate2 cert = null;
                //    //manually chose the certificate in the store
                //    X509Certificate2Collection sel = X509Certificate2UI.SelectFromCollection(store.Certificates, null, null, X509SelectionFlag.SingleSelection);
                //    if (sel.Count > 0)
                //        cert = sel[0];
                //    else
                //    {
                //        //MessageBox.Show("Certificate not found");

                //    }
                //    store.Close();
                //    return null;
                //}
                //----------------------------------------------------------------------------
                X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
                X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(fcollection, "Test Certificate Select", "Select a certificate from the following list to get information on that certificate", X509SelectionFlag.MultiSelection);
                Console.WriteLine("Number of certificates: {0}{1}", scollection.Count, Environment.NewLine);


                foreach (X509Certificate2 x509 in scollection)
                {
                    try
                    {
                        byte[] rawdata = x509.RawData;
                        Console.WriteLine("Content Type: {0}{1}", X509Certificate2.GetCertContentType(rawdata), Environment.NewLine);
                        Console.WriteLine("Friendly Name: {0}{1}", x509.FriendlyName, Environment.NewLine);
                        Console.WriteLine("Certificate Verified?: {0}{1}", x509.Verify(), Environment.NewLine);
                        Console.WriteLine("Simple Name: {0}{1}", x509.GetNameInfo(X509NameType.SimpleName, true), Environment.NewLine);
                        Console.WriteLine("Signature Algorithm: {0}{1}", x509.SignatureAlgorithm.FriendlyName, Environment.NewLine);
                        Console.WriteLine("Public Key: {0}{1}", x509.PublicKey.Key.ToXmlString(false), Environment.NewLine);
                        Console.WriteLine("Certificate Archived?: {0}{1}", x509.Archived, Environment.NewLine);
                        Console.WriteLine("Length of Raw Data: {0}{1}", x509.RawData.Length, Environment.NewLine);
                        X509Certificate2UI.DisplayCertificate(x509);
                        x509.Reset();
                    }
                    catch (CryptographicException)
                    {
                        Console.WriteLine("Information could not be written out for this certificate.");
                    }
                }
                store.Close();
                return null;
            }

            catch (Exception)
            {
                return null;
            }

        }
    }
}
