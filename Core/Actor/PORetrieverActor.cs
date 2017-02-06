using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using AkkaNet.Poc.Core.Entity;
using AkkaNet.Poc.Core.Placeholder;

namespace AkkaNet.Poc.Core.Actor
{
    /// <summary>
    /// Retrieves a purchase order.
    /// 
    /// Uses IPurchaseOrderModelRetriever to retieve a purchase order. Transforms the result
    /// into an entity and tells the Sender the result.
    /// 
    /// We can cache the purchase order so don't hammer IPurchaseOrderModelRetriever.
    /// 
    /// We can model IPurchaseOrderModelRetriever being up or down using an FSM 
    /// </summary>
    public class PORetrieverActor : ReceiveActor
    {
        public class Setting
        {
            public Setting(int maxRetry, TimeSpan retryTimeSpan)
            {
                MaxRetry = maxRetry;
                RetryTimeSpan = retryTimeSpan;
            }

            public int MaxRetry { get; }
            public TimeSpan RetryTimeSpan { get; }
        }

        #region Messages

        public class GetPurchaseOrder
        {
            public GetPurchaseOrder(string poNumber, int retryCount = 0)
            {
                PoNumber = poNumber;
                RetryCount = retryCount;
            }

            public string PoNumber { get; }

            public int RetryCount { get; }
        }
        #endregion
       
        private readonly Setting _setting;
        private readonly Func<IPOModelRetriever> _poRetrieverGenerator;
        private IPOModelRetriever _poRetriever;
        private readonly IExceptionTyper _exceptionTyper;
        private readonly ICancelable _cancelPublishing;
        private IActorRef _eventsource;

        public PORetrieverActor(Func<IPOModelRetriever> poRetrieverGenerator, IExceptionTyper exceptionTyper)
            : this(new Setting(3, TimeSpan.FromSeconds(5)), poRetrieverGenerator, exceptionTyper)
        {
        }

        public PORetrieverActor(Setting setting, Func<IPOModelRetriever> poRetrieverGenerator, IExceptionTyper exceptionTyper)
        {
            _setting = setting;
            _poRetrieverGenerator = poRetrieverGenerator;
            _exceptionTyper = exceptionTyper;
            _cancelPublishing = new Cancelable(Context.System.Scheduler);

            ReceiveAsync<GetPurchaseOrder>(HandleGetPurchaseOrder);
        }

        protected override void PreStart()
        {
           _eventsource = Context.ActorOf(Context.DI().Props<EventSourceActor>(), "eventsource");
           _poRetriever = _poRetrieverGenerator();
        }

        protected override void PostStop()
        {
            _cancelPublishing.Cancel(false);
        }

        private async Task HandleGetPurchaseOrder(GetPurchaseOrder getPurchaseOrder)
        {
            var model = await GetPurchaseOrderModel(getPurchaseOrder);
            if (model == null)
            {                
                return; // Nothing to do
            }

            var entity = Transform(model);
            if (entity == null)
            {
                return; // Nothing to do
            }

            Sender.Tell(new PORequestedActor.ReceivePurchaseOrderEntity(entity));
        }

        private async Task<PurchaseOrderModel> GetPurchaseOrderModel(GetPurchaseOrder getPurchaseOrder)
        {
            PurchaseOrderModel model = null;
            try
            {
                model = await _poRetriever.GetPurchaseOrder(getPurchaseOrder.PoNumber);
            }
            catch (Exception ex) when (IsTransientException(ex) && MaxRetryReached(getPurchaseOrder))
            {
                Sender.Tell(new Failure {Exception = new ApplicationException("POModelRetriever - Max retry reached", ex)}, Self);
            }
            catch (Exception ex) when (IsTransientException(ex))
            {
                ScheduleForRetry(getPurchaseOrder);
                TellEventSource(getPurchaseOrder, ex);
            }
            catch (Exception ex)
            {
                Sender.Tell(new Failure { Exception = new ApplicationException("Exception on POModelRetriever", ex) }, Self);
            }
            return model;
        }

        private bool MaxRetryReached(GetPurchaseOrder getPurchaseOrder)
        {
            return getPurchaseOrder.RetryCount >= _setting.MaxRetry;
        }
  
        private bool IsTransientException(Exception ex)
        {
            return _exceptionTyper.IsTransientException(ex);
        }

        private void ScheduleForRetry(GetPurchaseOrder getPurchaseOrder)
        {
            Context.System.Scheduler.ScheduleTellOnce(
                _setting.RetryTimeSpan, 
                Self,
                new GetPurchaseOrder(getPurchaseOrder.PoNumber, getPurchaseOrder.RetryCount + 1), 
                Sender,
                _cancelPublishing);
        }

        private void TellEventSource(GetPurchaseOrder getPurchaseOrder, Exception ex)
        {
            var message = $"Retry {getPurchaseOrder.RetryCount + 1}";
            _eventsource.Tell(new EventSourceActor.SendPurchaseOrderEvent(
                getPurchaseOrder.PoNumber, "Retrieve PO", "Retry", message, ex));
        }

        private static PurchaseOrderEntity Transform(PurchaseOrderModel model)
        {
            return new PurchaseOrderEntity
            {
                PONumber = model.PONumber
            };
        }
    }
}