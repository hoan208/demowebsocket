using CertificateService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ConsoleService
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebSocket ws = new WebSocket("ws://127.0.0.1:7890/Echo"))
            {
                ws.OnMessage += Ws_OnMessage;

                ws.Connect();
                ws.Send("Hello from PCamp!");
                Console.ReadKey();
            }    



            //WebSocketServer wssv = new WebSocketServer("ws://127.0.0.1:7890");
            //wssv.Start();
            //Console.WriteLine("Ws server start on ws://127.0.0.1:7890");
            //Console.ReadKey();
            //wssv.Stop();



            //try
            //{
            //    ServiceHost host = new ServiceHost(typeof(CertificateLib), new Uri("http://localhost:5000"));
            //    // foreach (ServiceEndpoint EP in host.Description.Endpoints) EP.Behaviors.Add(new BehaviorAttribute()); /* nếu bị lỗi CORS block thì mở commnet này */
            //    host.Open();
            //    //tạo folder lưu file xml bảng tổng hợp
            //    string root = @"C:\InvoiceDataXML";
            //    if (!Directory.Exists(root))
            //        Directory.CreateDirectory(root);
            //}
            //catch (Exception)
            //{
            //    ServiceHost host = new ServiceHost(typeof(CertificateLib), new Uri("http://localhost:5001"));
            //    // foreach (ServiceEndpoint EP in host.Description.Endpoints) EP.Behaviors.Add(new BehaviorAttribute()); /* nếu bị lỗi CORS block thì mở commnet này */
            //    host.Open();
            //}

            //Console.WriteLine("Service SignalDigital Open ...");
            //Console.ReadLine();
        }
        private static void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            Console.WriteLine("Received from the server: " + e.Data);
        }
    }

    public class CustomHeaderMessageInspector : IDispatchMessageInspector
    {
        Dictionary<string, string> requiredHeaders;
        public CustomHeaderMessageInspector(Dictionary<string, string> headers)
        {
            requiredHeaders = headers ?? new Dictionary<string, string>();
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            string displayText = $"Server has received the following message:\n{request}\n";
            Console.WriteLine(displayText);
            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var httpHeader = reply.Properties["httpResponse"] as HttpResponseMessageProperty;
            foreach (var item in requiredHeaders)
            {
                httpHeader.Headers.Add(item.Key, item.Value);
            }

            string displayText = $"Server has replied the following message:\n{reply}\n";
            Console.WriteLine(displayText);
        }
    }

    public class CustomContractBehaviorAttribute : BehaviorExtensionElement, IEndpointBehavior
    {
        public override Type BehaviorType => typeof(CustomContractBehaviorAttribute);

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        { }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            var requiredHeaders = new Dictionary<string, string>();

            requiredHeaders.Add("Access-Control-Allow-Origin", "*");
            requiredHeaders.Add("Access-Control-Request-Method", "POST,GET,PUT,DELETE,OPTIONS");
            requiredHeaders.Add("Access-Control-Allow-Headers", "X-Requested-With,Content-Type");

            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new CustomHeaderMessageInspector(requiredHeaders));
        }

        public void Validate(ServiceEndpoint endpoint)
        { }

        protected override object CreateBehavior()
        {
            return new CustomContractBehaviorAttribute();
        }
    }

    #region code disable cors (cross site)
    public class MessageInspector : IDispatchMessageInspector
    {
        private ServiceEndpoint _serviceEndpoint;

        public MessageInspector(ServiceEndpoint serviceEndpoint)
        {
            _serviceEndpoint = serviceEndpoint;
        }

        /// <summary>
        /// Called when an inbound message been received
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="channel">The incoming channel.</param>
        /// <param name="instanceContext">The current service instance.</param>
        /// <returns>
        /// The object used to correlate stateMsg. 
        /// This object is passed back in the method.
        /// </returns>
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            StateMessage stateMsg = null;
            HttpRequestMessageProperty requestProperty = null;
            if (request.Properties.ContainsKey(HttpRequestMessageProperty.Name))
            {
                requestProperty = request.Properties[HttpRequestMessageProperty.Name]
                                  as HttpRequestMessageProperty;
            }

            if (requestProperty != null)
            {
                var origin = requestProperty.Headers["Origin"];
                if (!string.IsNullOrEmpty(origin))
                {
                    stateMsg = new StateMessage();
                    // if a cors options request (preflight) is detected, 
                    // we create our own reply message and don't invoke any 
                    // operation at all.
                    if (requestProperty.Method == "OPTIONS")
                    {
                        stateMsg.Message = Message.CreateMessage(request.Version, null);
                    }
                    request.Properties.Add("CrossOriginResourceSharingState", stateMsg);
                }
            }

            return stateMsg;
        }

        /// <summary>
        /// Called after the operation has returned but before the reply message
        /// is sent.
        /// </summary>
        /// <param name="reply">The reply message. This value is null if the 
        /// operation is one way.</param>
        /// <param name="correlationState">The correlation object returned from
        ///  the method.</param>
        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var stateMsg = correlationState as StateMessage;

            if (stateMsg != null)
            {
                if (stateMsg.Message != null)
                {
                    reply = stateMsg.Message;
                }

                HttpResponseMessageProperty responseProperty = null;

                if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
                {
                    responseProperty = reply.Properties[HttpResponseMessageProperty.Name]
                                       as HttpResponseMessageProperty;
                }

                if (responseProperty == null)
                {
                    responseProperty = new HttpResponseMessageProperty();
                    reply.Properties.Add(HttpResponseMessageProperty.Name,
                                         responseProperty);
                }

                // Access-Control-Allow-Origin should be added for all cors responses
                responseProperty.Headers.Set("Access-Control-Allow-Origin", "*");

                if (stateMsg.Message != null)
                {
                    // the following headers should only be added for OPTIONS requests
                    responseProperty.Headers.Set("Access-Control-Allow-Methods", "POST, GET, PUT, DELETE, OPTIONS");
                    responseProperty.Headers.Set("Access-Control-Allow-Headers", "Content-Type, Accept, Authorization, x-requested-with");
                }
            }
        }
    }

    class StateMessage
    {
        public Message Message;
    }

    public class BehaviorAttribute : Attribute, IEndpointBehavior, IOperationBehavior
    {
        public void Validate(ServiceEndpoint endpoint) { }

        public void AddBindingParameters(ServiceEndpoint endpoint,
                                 BindingParameterCollection bindingParameters)
        { }

        /// <summary>
        /// This service modify or extend the service across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to be
        /// modified or extended.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint,
                                          EndpointDispatcher endpointDispatcher)
        {
            // add inspector which detects cross origin requests
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(
                                                   new MessageInspector(endpoint));
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint,
                                        ClientRuntime clientRuntime)
        { }

        public void Validate(OperationDescription operationDescription) { }

        public void ApplyDispatchBehavior(OperationDescription operationDescription,
                                          DispatchOperation dispatchOperation)
        { }

        public void ApplyClientBehavior(OperationDescription operationDescription,
                                        ClientOperation clientOperation)
        { }

        public void AddBindingParameters(OperationDescription operationDescription,
                                  BindingParameterCollection bindingParameters)
        { }

    }
    #endregion
}
