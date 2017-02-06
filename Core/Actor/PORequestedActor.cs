using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using AkkaNet.Poc.Core.Entity;

namespace AkkaNet.Poc.Core.Actor
{
    /// <summary>
    /// Process a Purchase Order Request
    /// </summary>
    public class PORequestedActor : ReceiveActor
    {        
        #region Messages

        public class RequestPurchaseOrder
        {
            public RequestPurchaseOrder(string poNumber)
            {
                PoNumber = poNumber;
            }

            public string PoNumber { get; }
        }

        public class ReceivePurchaseOrderEntity
        {
            public ReceivePurchaseOrderEntity(PurchaseOrderEntity purchaseOrder)
            {
                PurchaseOrder = purchaseOrder;
            }

            public PurchaseOrderEntity PurchaseOrder { get; private set; }
        }
        #endregion

        private IActorRef _eventsource;
        private IActorRef _retriever;

        public PORequestedActor()
        {
            ReceiveAsync<RequestPurchaseOrder>(HandleRequestPurchaseOrder);
            ReceiveAsync<ReceivePurchaseOrderEntity>(HandlePurchaseOrderEntity);
        }

        protected override void PreStart()
        {
            _eventsource = Context.ActorOf(Context.DI().Props<EventSourceActor>(), "eventsource");
            _retriever = Context.ActorOf(Context.DI().Props<PORetrieverActor>(), "retriever");
        }

        private Task HandleRequestPurchaseOrder(RequestPurchaseOrder req)
        {           
            _retriever.Tell(new PORetrieverActor.GetPurchaseOrder(req.PoNumber));
            return Task.FromResult<object>(null);
        }

        private Task HandlePurchaseOrderEntity(ReceivePurchaseOrderEntity entity)
        {
            _eventsource.Tell(new EventSourceActor.SendPurchaseOrderEvent(entity.PurchaseOrder.PONumber, "PO Retrieved", "Success"));
            
            // todo: validate purchase order

            // todo: determine if dependency is needed and get those dependency

            // todo: send PO to FC

            return Task.FromResult<object>(null);
        }
    }
}
