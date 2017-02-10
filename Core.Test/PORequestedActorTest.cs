using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.AutoFac;
using Akka.TestKit.VsTest;
using AkkaNet.Poc.Core.Actor;
using AkkaNet.Poc.Core.Entity;
using AkkaNet.Poc.Core.Placeholder;
using Autofac;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ploeh.AutoFixture;

namespace Core.Test
{
    [TestClass]
    public class PORequestedActorTest : TestKit
    {
        private Fixture _fixture;
        private AutoFacDependencyResolver _autoFacDependencyResolver;

        private IActorRef _poRequestedActor;

        private string _poNumber;

        [TestMethod]
        [TestCategory("Unit")]
        public void Should_StartProcessing_WhenRequestPurchaseOrderMessageReceived()
        {            
            _poRequestedActor.Tell(new PORequestedActor.RequestPurchaseOrder(_poNumber));
            var state = ExpectMsg<PORequestedActor.RequestPurchaseOrderReceived>().State;
            state.Should().Be("Processing");

            Thread.Sleep(1000);
            var eventMessage = EventSourceActorSpy.ReceivedMessages.Last();
            eventMessage.Event.Should().Be("RequestPurchaseOrder Received");
            eventMessage.Status.Should().Be("Success");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Should_SendEventPORetrievedSuccess_WhenReceivePurchaseOrderEntityReceived()
        {
            _poRequestedActor.Tell(new PORequestedActor.RequestPurchaseOrder(_poNumber));

            _poRequestedActor.Tell(
                   new PORequestedActor.ReceivePurchaseOrderEntity(new PurchaseOrderEntity
                   {
                       PONumber = _poNumber
                   }));

            Thread.Sleep(2000);
            var eventMessage = EventSourceActorSpy.ReceivedMessages.Last();
            eventMessage.Event.Should().Be("PO Retrieved");
            eventMessage.Status.Should().Be("Success");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Should_SendEventPORetrievedFailed_WhenReceivePurchaseOrderEntityReceived()
        {
            _poRequestedActor.Tell(new PORequestedActor.RequestPurchaseOrder(_poNumber));

            _poRequestedActor.Tell(
                   new Failure
                   {
                       Exception = new ApplicationException("Failed")
                   });

            Thread.Sleep(2000);
            var eventMessage = EventSourceActorSpy.ReceivedMessages.Last();
            eventMessage.Event.Should().Be("PO Retrieved");
            eventMessage.Status.Should().Be("Failed");
        }

        [TestInitialize]
        public void Initialize()
        {
            _poNumber = "PO-N0023";

            _fixture = new Fixture();

            var builder = new ContainerBuilder();
            builder.RegisterType<EventSourceActorSpy>().As<EventSourceActor>();
            builder.RegisterType<PORetrieverCoordinatorActorSpy>().As<PORetrieverCoordinatorActor>();
            var container = builder.Build();
            
            _autoFacDependencyResolver = new AutoFacDependencyResolver(container, Sys);
           
            _poRequestedActor =
                Sys.ActorOf(Props.Create(() => new PORequestedActor()));
        }

        public class EventSourceActorSpy : EventSourceActor
        {
            public static List<SendPurchaseOrderEvent> ReceivedMessages = new List<SendPurchaseOrderEvent>();

            protected override Task Handler(SendPurchaseOrderEvent sendPurchaseOrderEvent)
            {
                ReceivedMessages.Add(sendPurchaseOrderEvent);
                return Task.FromResult<object>(null);
            }
        }

        public class PORetrieverCoordinatorActorSpy : PORetrieverCoordinatorActor
        {
            public static List<object> ReceivedMessages = new List<object>();
            
            protected override Task HandleGetPurchaseOrder(PORetrieverActor.GetPurchaseOrder getPurchaseOrder)
            {
                ReceivedMessages.Add(getPurchaseOrder);
                return Task.FromResult<object>(null);
            }
        }
    }
}
