using alpha.Models;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Microsoft.Extensions.Options;

namespace alpha.Services.Sol
{
    public class SolTransfer : IChainTransferService
    {
        private readonly IRpcClient _rpcClient;
        private readonly BlockchainSettings _settings;

        public SolTransfer(IOptions<GlobalBlockchainConfig> config)
        {
            _settings = config.Value.Solana;
            _rpcClient = ClientFactory.GetClient(_settings.RpcUrl);
        }

        public Chain Chain => Chain.Sol;

        public async Task<(string ToSign, object Metadata)> PrepareTransferAsync(TransferIntent intent)
        {
            if (string.IsNullOrEmpty(intent.From))
                throw new ArgumentException("From address is required for Solana prepare");

            var fromPubkey = new PublicKey(intent.From);
            var toPubkey = new PublicKey(intent.To);

            var blockHash = await _rpcClient.GetLatestBlockHashAsync();
            if (!blockHash.WasSuccessful)
                throw new Exception("Failed to get blockhash");

            var nativeSol = _settings.Tokens.FirstOrDefault(t => t.Symbol == "SOL")?.Address ?? "So11111111111111111111111111111111111111112";
            bool isSplToken = !string.IsNullOrEmpty(intent.Token) && 
                              intent.Token != nativeSol;

            TransactionBuilder txBuilder = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromPubkey);

            if (isSplToken)
            {
                // Validate token
                var token = _settings.Tokens.FirstOrDefault(t => t.Address == intent.Token);
                if (token == null) throw new Exception($"Token {intent.Token} is not allowed on Solana");

                var tokenMint = new PublicKey(intent.Token!);
                var amount = ulong.Parse(intent.Amount);
                var fromTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(fromPubkey, tokenMint);
                var toTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(toPubkey, tokenMint);

                var toAccountInfo = await _rpcClient.GetAccountInfoAsync(toTokenAccount.Key);
                if (toAccountInfo.Result?.Value == null)
                {
                    txBuilder.AddInstruction(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(fromPubkey, toPubkey, tokenMint));
                }

                txBuilder.AddInstruction(TokenProgram.Transfer(fromTokenAccount, toTokenAccount, amount, fromPubkey));
            }
            else
            {
                var lamports = ulong.Parse(intent.Amount);
                txBuilder.AddInstruction(SystemProgram.Transfer(fromPubkey, toPubkey, lamports));
            }

            var message = txBuilder.CompileMessage();
            return (Convert.ToBase64String(message), new { blockhash = blockHash.Result.Value.Blockhash, isSplToken });
        }

        public async Task<string> BroadcastAsync(string signedPayload, object? metadata = null)
        {
            var bytes = Convert.FromBase64String(signedPayload);
            var result = await _rpcClient.SendTransactionAsync(bytes);
            if (!result.WasSuccessful) throw new Exception($"Broadcast failed: {result.Reason}");
            return result.Result;
        }

        public async Task<(bool IsFinalized, bool IsFailed, string? TxHash)> GetStatusAsync(string txHash)
        {
            var result = await _rpcClient.GetTransactionAsync(txHash);
            if (!result.WasSuccessful) return (false, false, txHash);
            return (result.Result.Meta.Error == null, result.Result.Meta.Error != null, txHash);
        }

        public async Task<object> GetGasQuoteAsync(string to, string amount, string from)
        {
             var blockHash = await _rpcClient.GetLatestBlockHashAsync();
             if (!blockHash.WasSuccessful) throw new Exception("Network error: " + blockHash.Reason);
             return new { feeLamports = "5000", blockhash = blockHash.Result.Value.Blockhash };
        }
    }
}
