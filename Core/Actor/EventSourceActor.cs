using System;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Newtonsoft.Json;

namespace AkkaNet.Poc.Core.Actor
{
    public class EventSourceActor : ReceiveActor
    {
        #region Messages

        public class SendPurchaseOrderEvent
        {
            public SendPurchaseOrderEvent(string key, string @event, string status, string description = null, Exception exception = null)
            {
                Entity = "PurchaseOrder";
                EventDateTime = DateTime.Now;

                Key = key;
                Event = @event;
                Status = status;
                Description = description;
                Exception = exception;
            }

            public DateTime EventDateTime { get; }
            public string Entity { get;  }
            public string Key { get; }
            public string Event { get; private set; }
            public string Status { get; private set; }
            public string Description { get; private set; }
            public Exception Exception { get; }
        }
        #endregion

        private string _path = @"C:\Temp\akkanetpoc.log";

        public EventSourceActor()
        {
            ReceiveAsync<SendPurchaseOrderEvent>(Handler);
        }

        private Task Handler(SendPurchaseOrderEvent sendPurchaseOrderEvent)
        {
            var message = JsonConvert.SerializeObject(sendPurchaseOrderEvent, Formatting.Indented);
            File.AppendAllText(_path, message + Environment.NewLine);
            return Task.FromResult<object>(null);
        }
    }
}