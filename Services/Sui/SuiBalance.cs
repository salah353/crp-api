using alpha.Models;
using alpha.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace alpha.Services.Sui
{
    public class SuiBalance : IChainBalanceService
    {
        private readonly HttpClient _http = new HttpClient();
        private readonly BlockchainSettings _settings;

        public SuiBalance(IOptions<GlobalBlockchainConfig> config)
        {
            _settings = config.Value.Sui;
        }

        public Chain Chain => Chain.Sui;

        private async Task<JsonDocument> CallRpcAsync(string method, params object[] @params)
        {
            var request = new { jsonrpc = "2.0", id = 1, method = method, @params = @params };
            var response = await _http.PostAsJsonAsync(_settings.RpcUrl, request);
            response.EnsureSuccessStatusCode();
            
            var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                throw new Exception($"Sui RPC Error: {error.GetRawText()}");
            }
            return doc;
        }

        public async Task<string> GetNativeBalanceAsync(string address)
        {
            var res = await CallRpcAsync("suix_getBalance", address, "0x2::sui::SUI");
            if (!res.RootElement.TryGetProperty("result", out var result)) return "0";
            return result.GetProperty("totalBalance").GetString() ?? "0";
        }

        public async Task<string> GetTokenBalanceAsync(string address, string tokenAddress)
        {
            var res = await CallRpcAsync("suix_getBalance", address, tokenAddress);
            if (!res.RootElement.TryGetProperty("result", out var result)) return "0";
            return result.GetProperty("totalBalance").GetString() ?? "0";
        }
    }
}
