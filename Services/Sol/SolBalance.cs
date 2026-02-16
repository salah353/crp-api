using alpha.Models;
using alpha.Services;
using Solnet.Rpc;
using Solnet.Wallet;
using Microsoft.Extensions.Options;

namespace alpha.Services.Sol
{
    public class SolBalance : IChainBalanceService
    {
        private readonly IRpcClient _rpcClient;
        private readonly BlockchainSettings _settings;

        public SolBalance(IOptions<GlobalBlockchainConfig> config)
        {
            _settings = config.Value.Solana;
            _rpcClient = ClientFactory.GetClient(_settings.RpcUrl);
        }

        public Chain Chain => Chain.Sol;

        public async Task<string> GetNativeBalanceAsync(string address)
        {
            var result = await _rpcClient.GetBalanceAsync(address);
            if (!result.WasSuccessful) throw new Exception($"Solana RPC error: {result.Reason}");
            return result.Result.Value.ToString();
        }

        public async Task<string> GetTokenBalanceAsync(string address, string tokenAddress)
        {
            // First derive the Associated Token Account (ATA)
            var owner = new PublicKey(address);
            var mint = new PublicKey(tokenAddress);
            var ata = Solnet.Programs.AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner, mint);

            var result = await _rpcClient.GetTokenAccountBalanceAsync(ata.Key);
            if (!result.WasSuccessful) 
            {
                // If account doesn't exist, balance is 0
                return "0";
            }
            
            return result.Result.Value.Amount;
        }
    }
}
