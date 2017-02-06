using System.Threading.Tasks;

namespace AkkaNet.Poc.Core.Placeholder
{
    /// <summary>
    /// Purchase order model retriever. Should be in a common project. Placeholder 
    /// </summary>
    public interface IPurchaseOrderModelRetriever
    {
        Task<PurchaseOrderModel> GetPurchaseOrder(string poNumber);
    }
}