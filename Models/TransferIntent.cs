using System.ComponentModel.DataAnnotations;

namespace alpha.Models
{
    public class TransferIntent
    {
        public string IntentId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public Chain Chain { get; set; }

        [Required]
        public string Token { get; set; } = "";

        [Required]
        public string To { get; set; } = "";

        [Required]
        public string Amount { get; set; } = "0";

        public string From { get; set; } = ""; // Added for backend tracking

        public TransferStatus Status { get; set; } = TransferStatus.Pending;

        public string? TxHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastCheckedAt { get; set; }

        public int RetryCount { get; set; } = 0;
        public string? LastError { get; set; }
        public string? ClientIdempotencyKey { get; set; }

        public BroadcastOutcome? BroadcastOutcome { get; set; }
        public RetryAction? RetryAction { get; set; }
        public bool TimedOut { get; set; } = false;
        
        // Metadata to store chain specific signed payloads or unsigned payloads if needed
        public string? UnsignedPayload { get; set; }
    }
}
