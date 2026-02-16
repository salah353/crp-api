using alpha.Models;

namespace alpha.Services
{
    public interface IChainTransferService
    {
        Chain Chain { get; }
        
        Task<(string ToSign, object Metadata)> PrepareTransferAsync(TransferIntent intent);
        Task<string> BroadcastAsync(string signedPayload, object? metadata = null);
        Task<(bool IsFinalized, bool IsFailed, string? TxHash)> GetStatusAsync(string txHash);
        Task<object> GetGasQuoteAsync(string to, string amount, string from);
    }
}
