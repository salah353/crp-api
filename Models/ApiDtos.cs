using System.ComponentModel.DataAnnotations;

namespace alpha.Models
{
    public class PrepareTransferRequest
    {
        [Required]
        public string Chain { get; set; } = "";
        [Required]
        public string Token { get; set; } = "";
        [Required]
        public string To { get; set; } = "";
        [Required]
        public string Amount { get; set; } = "";
        [Required]
        public string From { get; set; } = ""; // Required by spec/impl logic
        public string? ClientIdempotencyKey { get; set; } // Optional
    }

    public class PrepareTransferResponse
    {
        public string IntentId { get; set; } = "";
        public string ToSignPayload { get; set; } = "";
        public object Metadata { get; set; } = new { };
    }

    public class BroadcastTransferRequest
    {
        [Required]
        public string Chain { get; set; } = "";
        [Required]
        public string IntentId { get; set; } = "";
        [Required]
        public string SignedPayload { get; set; } = "";
    }

    public class BroadcastResponse
    {
        public string IntentId { get; set; } = "";
        public BroadcastOutcome BroadcastOutcome { get; set; }
        public string? TxHash { get; set; }
        public string? ErrorCode { get; set; }
        public string? LastError { get; set; }
        public RetryAction? RetryAction { get; set; }
    }

    public class GetStatusRequest
    {
        [Required]
        public string Chain { get; set; } = "";
        [Required]
        public string IntentId { get; set; } = "";
    }

    public class GetStatusResponse
    {
        public TransferStatus Status { get; set; }
        public string? TxHash { get; set; }
        public string? Error { get; set; }
        public string? ErrorCode { get; set; }
        public RetryAction? RetryAction { get; set; }
    }

    public class GasQuoteRequest
    {
        [Required]
        public string Chain { get; set; } = "";
        [Required]
        public string Token { get; set; } = "";
        public string To { get; set; } = ""; // Optional for some chains but usually needed
        public string Amount { get; set; } = "0";
        public string From { get; set; } = "";
    }

    public class BalanceRequest
    {
        [Required]
        public string Chain { get; set; } = "";
        [Required]
        public string Address { get; set; } = "";
        public string? TokenAddress { get; set; } // Optional, if null returns native
    }

    public class BalanceResponse
    {
        public string Chain { get; set; } = "";
        public string Address { get; set; } = "";
        public string Symbol { get; set; } = "";
        public string Balance { get; set; } = "0";
    }

    public class AssetResponse
    {
        public string Symbol { get; set; } = "";
        public string Name { get; set; } = "";
        public string Balance { get; set; } = "0";
        public string? TokenAddress { get; set; }
        public int Decimals { get; set; }
    }

    public class WalletAssetsResponse
    {
        public string Chain { get; set; } = "";
        public string Address { get; set; } = "";
        public List<AssetResponse> Assets { get; set; } = new();
    }
}
