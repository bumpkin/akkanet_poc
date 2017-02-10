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
                PONumber = poNumber;
            }

            public string PONumber { get; }
        }

        public class RequestPurchaseOrderReceived
        {
            public RequestPurchaseOrderReceived(string poNumber, string state)
            {
                PONumber = poNumber;
                State = state;
            }

            public string PONumber { get; }

            public string State { get; }
        }

        public class ReceivePurchaseOrderEntity
        {
            public ReceivePurchaseOrderEntity(PurchaseOrderEntity purchaseOrder)
            {
                PurchaseOrder = purchaseOrder;
            }

            public PurchaseOrderEntity PurchaseOrder { get; }
        }
        
        #endregion

        private IActorRef _eventsource;
        private IActorRef _retrieverCoordinator;
        private string _poNumber;

        public PORequestedActor()
        {
            WaitingForRequest();            
        }

        protected override void PreStart()
        {
            _eventsource = Context.ActorOf(Context.DI().Props<EventSourceActor>(), "EventSource");            
            _retrieverCoordinator = Context.ActorOf(Context.DI().Props<PORetrieverCoordinatorActor>(), "PORetrieverCoordinator");
            base.PreStart();
        }

        private void WaitingForRequest()
        {
            ReceiveAsync<RequestPurchaseOrder>(req =>
            {
                _poNumber = req.PONumber;
                _eventsource.Tell(new EventSourceActor.SendPurchaseOrderEvent(_poNumber, "RequestPurchaseOrder Received", "Success"));
                Sender.Tell(new RequestPurchaseOrderReceived(_poNumber, "Processing"));
                _retrieverCoordinator.Tell(new PORetrieverActor.GetPurchaseOrder(_poNumber));
                Become(RequestReceived);                
                return Task.FromResult<object>(null);
            });
        }

        private void RequestReceived()
        {
            ReceiveAsync<ReceivePurchaseOrderEntity>(entity =>
            {
                _eventsource.Tell(new EventSourceActor.SendPurchaseOrderEvent(entity.PurchaseOrder.PONumber, "PO Retrieved", "Success"));

                // todo: validate purchase order

                // todo: determine if dependency is needed and get those dependency

                // todo: send PO to FC

                return Task.FromResult<object>(null);
            });

            ReceiveAsync<Failure>(entity =>
            {
                _eventsource.Tell(new EventSourceActor.SendPurchaseOrderEvent(_poNumber, "PO Retrieved", "Failed", exception:entity.Exception));
                return Task.FromResult<object>(null);
            });          
        }
        
    }
}
