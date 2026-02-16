using alpha.Models;
using Microsoft.Extensions.Options;

namespace alpha.Services.Sui
{
    public class SuiTransfer : IChainTransferService
    {
        private readonly HttpClient _http = new HttpClient();
        private readonly BlockchainSettings _settings;

        public SuiTransfer(IOptions<GlobalBlockchainConfig> config)
        {
            _settings = config.Value.Sui;
        }

        public Chain Chain => Chain.Sui;

        private async Task<System.Text.Json.JsonDocument> CallRpcAsync(string method, params object[] @params)
        {
            var request = new { jsonrpc = "2.0", id = 1, method = method, @params = @params };
            
            Console.WriteLine($"[SUI RPC] Calling {method}");
            Console.WriteLine($"[SUI RPC] Request: {System.Text.Json.JsonSerializer.Serialize(request)}");
            
            var response = await _http.PostAsJsonAsync(_settings.RpcUrl, request);
            response.EnsureSuccessStatusCode();
            
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[SUI RPC] Response: {responseBody}");
            
            var doc = await System.Text.Json.JsonDocument.ParseAsync(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseBody)));
            
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var errorText = error.GetRawText();
                if (errorText.Contains("-32601")) return doc; 
                throw new Exception($"Sui RPC error in {method}: {errorText}");
            }
            return doc;
        }

        public async Task<(string ToSign, object Metadata)> PrepareTransferAsync(TransferIntent intent)
        {
            if (string.IsNullOrEmpty(intent.From)) throw new ArgumentException("From address is required for Sui prepare");

            var nativeSui = _settings.Tokens.FirstOrDefault(t => t.Symbol == "SUI")?.Address ?? "0x2::sui::SUI";

            var resolvedToken = _settings.Tokens.FirstOrDefault(t => t.Symbol.Equals(intent.Token, StringComparison.OrdinalIgnoreCase) || t.Address == intent.Token);
            var coinType = resolvedToken?.Address ?? intent.Token;

            if (string.IsNullOrEmpty(coinType) || coinType.Equals("SUI", StringComparison.OrdinalIgnoreCase)) coinType = nativeSui;

            // Validate token
            if (coinType != nativeSui)
            {
                var token = _settings.Tokens.FirstOrDefault(t => t.Address == coinType);
                if (token == null) throw new Exception($"Token {intent.Token} is not allowed on Sui");
            }

            var coinsRes = await CallRpcAsync("suix_getCoins", intent.From, coinType);
            var result = coinsRes.RootElement.GetProperty("result");
            var coins = result.GetProperty("data").EnumerateArray();
            
            var inputCoins = new List<string>();
            long totalAmount = 0;
            long targetAmount = long.Parse(intent.Amount);

            foreach (var coin in coins)
            {
                inputCoins.Add(coin.GetProperty("coinObjectId").GetString()!);
                totalAmount += long.Parse(coin.GetProperty("balance").GetString()!);
                if (totalAmount >= targetAmount) break;
            }

            if (totalAmount < targetAmount) throw new Exception($"Insufficient balance. Have {totalAmount}, need {targetAmount} of {coinType}");

            string[] payMethodNames = { "unsafe_pay", "sui_unsafe_pay", "suix_unsafe_pay" };
            System.Text.Json.JsonDocument? prepareRes = null;

            foreach (var methodName in payMethodNames)
            {
                var tryRes = await CallRpcAsync(methodName, intent.From, inputCoins, new[] { intent.To }, new[] { targetAmount.ToString() }, null, "20000000");
                if (!tryRes.RootElement.TryGetProperty("error", out _)) { prepareRes = tryRes; break; }
            }

            if (prepareRes == null) throw new Exception("Sui RPC error: All pay methods failed");

            var txBytes = prepareRes.RootElement.GetProperty("result").GetProperty("txBytes").GetString()!;
            return (txBytes, new { coinType, inputCoins, gasBudget = 20000000 });
        }

        public async Task<string> BroadcastAsync(string signedPayload, object? metadata = null)
        {
            // metadata is the txBytes (UnsignedPayload) from prepare
            var txBytes = metadata as string;
            if (string.IsNullOrEmpty(txBytes)) throw new ArgumentException("Sui broadcast requires txBytes in metadata");

            Console.WriteLine($"[SUI DEBUG] Broadcasting with txBytes length: {txBytes.Length}");
            Console.WriteLine($"[SUI DEBUG] Signature length: {signedPayload.Length}");
            Console.WriteLine($"[SUI DEBUG] First 100 chars of txBytes: {txBytes.Substring(0, Math.Min(100, txBytes.Length))}");
            Console.WriteLine($"[SUI DEBUG] First 100 chars of signature: {signedPayload.Substring(0, Math.Min(100, signedPayload.Length))}");

            var res = await CallRpcAsync("sui_executeTransactionBlock", txBytes, new[] { signedPayload }, new { showEffects = true }, "WaitForLocalExecution");
            return res.RootElement.GetProperty("result").GetProperty("digest").GetString()!;
        }

        public async Task<(bool IsFinalized, bool IsFailed, string? TxHash)> GetStatusAsync(string txHash)
        {
            var res = await CallRpcAsync("sui_getTransactionBlock", txHash, new { showEffects = true });
            if (res.RootElement.TryGetProperty("error", out _)) return (false, false, txHash);

            var effects = res.RootElement.GetProperty("result").GetProperty("effects");
            var status = effects.GetProperty("status").GetProperty("status").GetString();
            return (status == "success", status == "failure", txHash);
        }

        public async Task<object> GetGasQuoteAsync(string to, string amount, string from)
        {
            return new { estimatedGas = "20000000", gasPrice = "1000", totalFee = "20000000" };
        }
    }
}
