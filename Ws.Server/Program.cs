using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Ws.Server
{
    //public class Data
    //{
    //    public int id { set; get; }
    //    public string name { set; get; }
    //}
    public class Echo : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            ////Console.WriteLine("Received message from client: " + e.Data);
            ////var data = JsonConvert.DeserializeObject<List<Data>>(e.Data);

            X509Certificate2Collection keyStore = new X509Certificate2Collection();
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            keyStore.AddRange(store.Certificates);
            store.Close();

            X509Certificate2 cert = null;

            cert = X509Certificate2UI.SelectFromCollection(keyStore,
                    "Chứng thư số ký", "Chọn chứng thư số ký",
                    X509SelectionFlag.SingleSelection)[0];
            string Subject = cert.SubjectName.Name.ToLower();
            Console.WriteLine("Received message from client: " + Subject);
            Send(Subject);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //tạo server Ws
            WebSocketServer wssv = new WebSocketServer("ws://127.0.0.1:7890");
            wssv.AddWebSocketService<Echo>("/Echo");
            wssv.Start();
            Console.WriteLine("Đã start server ws://127.0.0.1:7890/Echo");

            Console.ReadKey();
            wssv.Stop();
        }
    }
}
