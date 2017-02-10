using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using Akka.Routing;

namespace AkkaNet.Poc.Core.Actor
{
    public class PORetrieverCoordinatorActor : ReceiveActor
    {
        private IActorRef _workers;

        public PORetrieverCoordinatorActor()
        {            
            ReceiveAsync<PORetrieverActor.GetPurchaseOrder>(HandleGetPurchaseOrder);            
        }

        protected override void PreStart()
        {
            _workers = Context.ActorOf(Context.DI().Props<PORetrieverActor>().WithRouter(new RoundRobinPool(10)),
                "PORetrieverActors");
            base.PreStart();
        }

        protected virtual Task HandleGetPurchaseOrder(PORetrieverActor.GetPurchaseOrder msg)
        {
            _workers.Tell(msg, Sender);
            return Task.FromResult<object>(null);
        }
    }
}