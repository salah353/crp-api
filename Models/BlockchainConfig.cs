namespace alpha.Models
{
    public class BlockchainSettings
    {
        public string RpcUrl { get; set; } = "";
        public int? ChainId { get; set; }
        public List<TokenConfig> Tokens { get; set; } = new();
    }

    public class TokenConfig
    {
        public string Symbol { get; set; } = "";
        public string Address { get; set; } = "";
        public int Decimals { get; set; }
    }

    public class GlobalBlockchainConfig
    {
        public BlockchainSettings Bsc { get; set; } = new();
        public BlockchainSettings Solana { get; set; } = new();
        public BlockchainSettings Sui { get; set; } = new();
    }
}
