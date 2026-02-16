using alpha.Models;
using Microsoft.Extensions.Options;

namespace alpha.Services
{
    public interface IWalletService
    {
        Task<BalanceResponse> GetBalanceAsync(string chainStr, string address, string symbol);
        Task<WalletAssetsResponse> GetAssetsAsync(string chainStr, string address);
    }

    public class WalletService : IWalletService
    {
        private readonly IEnumerable<IChainBalanceService> _balanceServices;
        private readonly GlobalBlockchainConfig _config;

        public WalletService(IEnumerable<IChainBalanceService> balanceServices, IOptions<GlobalBlockchainConfig> config)
        {
            _balanceServices = balanceServices;
            _config = config.Value;
        }

        public async Task<BalanceResponse> GetBalanceAsync(string chainStr, string address, string symbol)
        {
            if (!Enum.TryParse<Chain>(chainStr, true, out var chain))
                throw new ArgumentException($"Invalid chain: {chainStr}");

            var service = _balanceServices.FirstOrDefault(s => s.Chain == chain);
            if (service == null) throw new NotSupportedException($"Chain {chain} not supported");

            var settings = GetSettings(chain);
            var token = settings.Tokens.FirstOrDefault(t => t.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
            if (token == null) throw new Exception($"Symbol {symbol} not found in configuration for {chain}");

            string balance;
            // Check if it's the native token (usually 0x0... or SOL/SUI special strings)
            // For simplicity, we compare symbol with native symbols or check if address is zero-ish
            bool isNative = (chain == Chain.Bsc && token.Symbol == "BNB") || 
                            (chain == Chain.Sol && token.Symbol == "SOL") ||
                            (chain == Chain.Sui && token.Symbol == "SUI");

            if (isNative)
            {
                balance = await service.GetNativeBalanceAsync(address);
            }
            else
            {
                balance = await service.GetTokenBalanceAsync(address, token.Address);
            }

            return new BalanceResponse
            {
                Chain = chainStr,
                Address = address,
                Symbol = token.Symbol,
                Balance = balance
            };
        }

        public async Task<WalletAssetsResponse> GetAssetsAsync(string chainStr, string address)
        {
            if (!Enum.TryParse<Chain>(chainStr, true, out var chain))
                throw new ArgumentException($"Invalid chain: {chainStr}");

            var service = _balanceServices.FirstOrDefault(s => s.Chain == chain);
            if (service == null) throw new NotSupportedException($"Chain {chain} not supported");

            var settings = GetSettings(chain);
            var results = new List<AssetResponse>();

            foreach (var token in settings.Tokens)
            {
                try 
                {
                    bool isNative = (chain == Chain.Bsc && token.Symbol == "BNB") || 
                                    (chain == Chain.Sol && token.Symbol == "SOL") ||
                                    (chain == Chain.Sui && token.Symbol == "SUI");

                    string balance = isNative 
                        ? await service.GetNativeBalanceAsync(address)
                        : await service.GetTokenBalanceAsync(address, token.Address);

                    results.Add(new AssetResponse
                    {
                        Symbol = token.Symbol,
                        Name = token.Symbol, // We don't have Name in config yet, use Symbol
                        Balance = balance,
                        TokenAddress = token.Address,
                        Decimals = token.Decimals
                    });
                }
                catch 
                {
                    // Skip failed lookups (e.g. RPC timeout)
                    continue;
                }
            }

            return new WalletAssetsResponse
            {
                Chain = chainStr,
                Address = address,
                Assets = results
            };
        }

        private BlockchainSettings GetSettings(Chain chain)
        {
            return chain switch
            {
                Chain.Bsc => _config.Bsc,
                Chain.Sol => _config.Solana,
                Chain.Sui => _config.Sui,
                _ => throw new Exception("Unsupported chain")
            };
        }
    }
}
