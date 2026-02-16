using Nethereum.Web3;
using alpha.Models;
using alpha.Services;
using Microsoft.Extensions.Options;

namespace alpha.Services.Bsc
{
    public class BscBalance : IChainBalanceService
    {
        private readonly Web3 _web3;
        private readonly BlockchainSettings _settings;

        public BscBalance(IOptions<GlobalBlockchainConfig> config)
        {
            _settings = config.Value.Bsc;
            _web3 = new Web3(_settings.RpcUrl);
        }

        public Chain Chain => Chain.Bsc;

        public async Task<string> GetNativeBalanceAsync(string address)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);
            return balance.Value.ToString();
        }

        public async Task<string> GetTokenBalanceAsync(string address, string tokenAddress)
        {
            // Simple ERC20 balanceOf call
            // Selector for balanceOf(address) is 0x70a08231
            var data = "0x70a08231" + address.Replace("0x", "").PadLeft(64, '0');
            var result = await _web3.Eth.Transactions.Call.SendRequestAsync(new Nethereum.RPC.Eth.DTOs.CallInput
            {
                To = tokenAddress,
                Data = data
            });
            
            if (string.IsNullOrEmpty(result) || result == "0x") return "0";
            
            return new Nethereum.Hex.HexTypes.HexBigInteger(result).Value.ToString();
        }
    }
}
