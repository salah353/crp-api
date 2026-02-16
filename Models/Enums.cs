using System.Text.Json.Serialization;

namespace alpha.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Chain
    {
        Bsc,
        Sol,
        Sui
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TransferStatus
    {
        Pending,
        Finalized,
        Failed
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BroadcastOutcome
    {
        Submitted,
        Rejected,
        Unknown
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RetryAction
    {
        PollOnly,
        ReprepareAndResign,
        FailFinal
    }
}
