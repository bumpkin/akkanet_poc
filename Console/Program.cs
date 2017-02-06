using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.AutoFac;
using AkkaNet.Poc.Core.Actor;
using AkkaNet.Poc.Core.Placeholder;
using Autofac;

namespace Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<EventSourceActor>();
            builder.RegisterType<PORetrieverActor>();            

            builder.RegisterType<PurchaseOrderModelRetrieverTest>().As<IPurchaseOrderModelRetriever>();
            var container = builder.Build();

            var system = ActorSystem.Create("MySystem");
            var propsResolver = new AutoFacDependencyResolver(container, system);

            var actor = system.ActorOf(Props.Create(() => new PORequestedActor()));
            actor.Tell(new PORequestedActor.RequestPurchaseOrder("PO-N10002"));

            system.WhenTerminated.Wait();
        }
    }

    internal class PurchaseOrderModelRetrieverTest : IPurchaseOrderModelRetriever
    {
        private int count = 0;

        public Task<PurchaseOrderModel> GetPurchaseOrder(string poNumber)
        {
            if (count < 2)
            {
                ++count;
                throw new ApplicationException("Transient");
            }
            return Task.FromResult(new PurchaseOrderModel
            {
                PONumber = poNumber
            });
        }
    }
}
