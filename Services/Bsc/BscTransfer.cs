using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using alpha.Models;
using System.Numerics;
using Microsoft.Extensions.Options;

namespace alpha.Services.Bsc
{
    public class BscTransfer : IChainTransferService
    {
        private readonly Web3 _web3;
        private readonly BlockchainSettings _settings;

        public BscTransfer(IOptions<GlobalBlockchainConfig> config)
        {
            _settings = config.Value.Bsc;
            _web3 = new Web3(_settings.RpcUrl);
        }

        public Chain Chain => Chain.Bsc;

        public async Task<(string ToSign, object Metadata)> PrepareTransferAsync(TransferIntent intent)
        {
            if (string.IsNullOrEmpty(intent.From))
                throw new ArgumentException("From address is required for EVM prepare");

            var fromAddress = intent.From;
            var nonce = await _web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(fromAddress, BlockParameter.CreatePending());
            var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
            
            // Native BNB is represented by 0x0... address in our config
            var nativeBnb = _settings.Tokens.FirstOrDefault(t => t.Symbol == "BNB")?.Address ?? "0x0000000000000000000000000000000000000000";
            bool isErc20Transfer = !string.IsNullOrEmpty(intent.Token) && 
                                   intent.Token.ToLower() != nativeBnb.ToLower();

            string toAddress;
            string valueHex;
            string dataHex;
            HexBigInteger gasLimit;

            if (isErc20Transfer)
            {
                // Validate if token is allowed
                var token = _settings.Tokens.FirstOrDefault(t => t.Address.ToLower() == intent.Token.ToLower());
                if (token == null) throw new Exception($"Token {intent.Token} is not allowed on BSC");

                toAddress = intent.Token!;
                valueHex = "0x0";
                gasLimit = new HexBigInteger(65000);
                
                var recipientAddress = intent.To.StartsWith("0x") ? intent.To.Substring(2) : intent.To;
                recipientAddress = recipientAddress.PadLeft(64, '0');
                
                var amountWei = BigInteger.Parse(intent.Amount);
                var amountHex = amountWei.ToString("X").PadLeft(64, '0');
                
                dataHex = $"0xa9059cbb{recipientAddress}{amountHex}";
            }
            else
            {
                toAddress = intent.To;
                var amountWei = BigInteger.Parse(intent.Amount);
                valueHex = $"0x{amountWei.ToString("X")}";
                dataHex = "0x";
                gasLimit = new HexBigInteger(21000);
            }

            var payload = System.Text.Json.JsonSerializer.Serialize(new 
            {
                to = toAddress,
                value = valueHex,
                gas = gasLimit.HexValue,
                gasPrice = gasPrice.HexValue,
                nonce = nonce.HexValue,
                data = dataHex,
                chainId = _settings.ChainId ?? 56
            });

            return (payload, new { nonce = nonce.Value.ToString(), isErc20 = isErc20Transfer });
        }

        public async Task<string> BroadcastAsync(string signedPayload, object? metadata = null)
        {
            var txHash = await _web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(signedPayload);
            return txHash;
        }

        public async Task<(bool IsFinalized, bool IsFailed, string? TxHash)> GetStatusAsync(string txHash)
        {
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            if (receipt == null) return (false, false, txHash);
            return (receipt.Status.Value == 1, receipt.Status.Value == 0, txHash);
        }

        public async Task<object> GetGasQuoteAsync(string to, string amount, string from)
        {
            var gasPrice = await _web3.Eth.GasPrice.SendRequestAsync();
            var input = new CallInput
            {
                From = from,
                To = to,
                Value = new HexBigInteger(BigInteger.Parse(amount))
            };
            var estimatedGas = await _web3.Eth.Transactions.EstimateGas.SendRequestAsync(input);

            return new
            {
                gasPrice = gasPrice.Value.ToString(),
                estimatedGas = estimatedGas.Value.ToString(),
                totalFeeWei = (gasPrice.Value * estimatedGas.Value).ToString(),
                chainId = _settings.ChainId ?? 56
            };
        }
    }
}
