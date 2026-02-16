using alpha.Models;

namespace alpha.Services
{
    public interface IChainBalanceService
    {
        Chain Chain { get; }
        Task<string> GetNativeBalanceAsync(string address);
        Task<string> GetTokenBalanceAsync(string address, string tokenAddress);
    }
}
