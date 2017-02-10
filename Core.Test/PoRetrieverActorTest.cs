using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.AutoFac;
using Akka.TestKit.VsTest;
using AkkaNet.Poc.Core.Actor;
using AkkaNet.Poc.Core.Placeholder;
using Autofac;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Ploeh.AutoFixture;

namespace Core.Test
{
    [TestClass]
    public class PORetrieverActorTest : TestKit
    {
        private AutoFacDependencyResolver _autoFacDependencyResolver;

        private Mock<IExceptionTyper> _exceptionTyper;
        private Mock<IPOModelRetriever> _retriever;
        private IActorRef _retrieverActor;
        private Fixture _fixture;
        
        private PurchaseOrderModel _model;
        private string _poNumber;
        
        [TestMethod]
        [TestCategory("Unit")]
        public void Should_GetPurchaseOrder()
        {
            //Given  
            GivenPurchaseOrderModel();
            _retriever.Setup(x => x.GetPurchaseOrder(_poNumber))
                .Returns(Task.FromResult(_model));

            WhenGetPurchaseOrderIsSent();            

            // Then
            var purchaseOrder = ExpectMsg<PORequestedActor.ReceivePurchaseOrderEntity>().PurchaseOrder;
            purchaseOrder.PONumber.Should().Be(_poNumber);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Should_ReceiveHardException()
        {
            //Given             
            var exception = new ApplicationException("Hard Exception");
            _retriever.Setup(x => x.GetPurchaseOrder(_poNumber)).Throws(exception);
            _exceptionTyper.Setup(x => x.IsTransientException(exception)).Returns(false);

            WhenGetPurchaseOrderIsSent();

            // Then
            var failure = ExpectMsg<Failure>();
            failure.Exception.Should().BeOfType<ApplicationException>();
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Should_ReceiveTransientException_ThenSuccess()
        {
            // Given  
            GivenPurchaseOrderModel();

            var exception = new ApplicationException("Transient Exception");
            
            _retriever.SetupSequence(x => x.GetPurchaseOrder(_poNumber))
                .Throws(exception)
                .Returns(Task.FromResult(_model));
                
            _exceptionTyper.Setup(x => x.IsTransientException(exception)).Returns(true);

            WhenGetPurchaseOrderIsSent();

            // Then
            var purchaseOrder = ExpectMsg<PORequestedActor.ReceivePurchaseOrderEntity>(TimeSpan.FromSeconds(5)).PurchaseOrder;
            purchaseOrder.PONumber.Should().Be(_poNumber);
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void Should_ReceiveTransientExceptionTillRetryMaxedOut_ThenException()
        {
            //Given  
            GivenPurchaseOrderModel();

            var exception = new ApplicationException("Transient Exception");

            _retriever.Setup(x => x.GetPurchaseOrder(_poNumber))
                .Throws(exception);

            _exceptionTyper.Setup(x => x.IsTransientException(exception)).Returns(true);

            WhenGetPurchaseOrderIsSent();

            // Then
            var failure = ExpectMsg<Failure>(TimeSpan.FromSeconds(5));
            failure.Exception.Should().BeOfType<ApplicationException>();
        }

        private void GivenPurchaseOrderModel()
        {
            _model = _fixture.Create<PurchaseOrderModel>();
            _model.PONumber = _poNumber;
        }

        private void WhenGetPurchaseOrderIsSent()
        {
            _retrieverActor.Tell(new PORetrieverActor.GetPurchaseOrder(_poNumber));
        }

        [TestInitialize]
        public void Initialize()
        {
            _poNumber = "PO-N10039";

            _fixture = new Fixture();  

            var builder = new ContainerBuilder();
            builder.RegisterType<EventSourceActorSpy>().As<EventSourceActor>();
            var container = builder.Build();
            
            _autoFacDependencyResolver = new AutoFacDependencyResolver(container, Sys);

            _retriever = new Mock<IPOModelRetriever>();
            _exceptionTyper = new Mock<IExceptionTyper>();
            var setting = new PORetrieverActor.Setting(1,TimeSpan.FromMilliseconds(500));
            _retrieverActor =
                Sys.ActorOf(Props.Create(() => new PORetrieverActor(setting, () => _retriever.Object, _exceptionTyper.Object)));
        }

        public class EventSourceActorSpy : EventSourceActor
        {
            public static List<object> ReceivedMessages = new List<object>();

            protected override Task Handler(SendPurchaseOrderEvent sendPurchaseOrderEvent)
            {
                ReceivedMessages.Add(sendPurchaseOrderEvent);
                return Task.FromResult<object>(null);
            }            
        }
    }
}
