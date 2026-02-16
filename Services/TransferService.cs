using alpha.Models;
using alpha.Repositories;
using alpha.Services.Bsc;
using alpha.Services.Sol;
using alpha.Services.Sui;

namespace alpha.Services
{
    public interface ITransferService
    {
        Task<PrepareTransferResponse> PrepareAsync(PrepareTransferRequest request);
        Task<BroadcastResponse> BroadcastAsync(BroadcastTransferRequest request);
        Task<GetStatusResponse> GetStatusAsync(GetStatusRequest request);
        Task<object> GetGasQuoteAsync(GasQuoteRequest request);
    }

    public class TransferService : ITransferService
    {
        private readonly iRepo _repo;
        private readonly IEnumerable<IChainTransferService> _chainServices;

        public TransferService(iRepo repo, IEnumerable<IChainTransferService> chainServices)
        {
            _repo = repo;
            _chainServices = chainServices;
        }

        private IChainTransferService GetChainService(string chainStr)
        {
            if (!Enum.TryParse<Chain>(chainStr, true, out var chain))
                throw new ArgumentException($"Invalid chain: {chainStr}");

            var service = _chainServices.FirstOrDefault(s => s.Chain == chain);
            return service ?? throw new NotSupportedException($"Chain {chain} not supported");
        }

        public async Task<PrepareTransferResponse> PrepareAsync(PrepareTransferRequest request)
        {
            // Idempotency: check if already exists
            if (!string.IsNullOrEmpty(request.ClientIdempotencyKey))
            {
                var existing = _repo.GetIntentByClientKey(request.ClientIdempotencyKey);
                if (existing != null)
                {
                    return new PrepareTransferResponse
                    {
                        IntentId = existing.IntentId,
                        ToSignPayload = existing.UnsignedPayload ?? "",
                        Metadata = new { note = "Idempotent response" }
                    };
                }
            }

            var chainService = GetChainService(request.Chain);

            var intent = new TransferIntent
            {
                Chain = chainService.Chain,
                Token = request.Token,
                To = request.To,
                Amount = request.Amount,
                From = request.From,
                ClientIdempotencyKey = request.ClientIdempotencyKey,
                Status = TransferStatus.Pending
            };

            var (toSign, metadata) = await chainService.PrepareTransferAsync(intent);
            intent.UnsignedPayload = toSign;
            _repo.CreateIntent(intent);

            return new PrepareTransferResponse
            {
                IntentId = intent.IntentId,
                ToSignPayload = toSign,
                Metadata = metadata
            };
        }

        public async Task<BroadcastResponse> BroadcastAsync(BroadcastTransferRequest request)
        {
            var intent = _repo.GetIntent(request.IntentId);
            if (intent == null) throw new Exception("Intent not found");

            // Idempotency: if already broadcast/finalized
            if (intent.BroadcastOutcome == BroadcastOutcome.Submitted && !string.IsNullOrEmpty(intent.TxHash))
            {
                return new BroadcastResponse
                {
                    IntentId = intent.IntentId,
                    BroadcastOutcome = BroadcastOutcome.Submitted,
                    TxHash = intent.TxHash
                };
            }

            try
            {
                var chainService = GetChainService(request.Chain);
                var txHash = await chainService.BroadcastAsync(request.SignedPayload, intent.UnsignedPayload);

                intent.Status = TransferStatus.Pending;
                intent.BroadcastOutcome = BroadcastOutcome.Submitted;
                intent.TxHash = txHash;
                intent.RetryAction = RetryAction.PollOnly;
                _repo.UpdateIntent(intent);

                return new BroadcastResponse
                {
                    IntentId = intent.IntentId,
                    BroadcastOutcome = BroadcastOutcome.Submitted,
                    TxHash = txHash,
                    RetryAction = RetryAction.PollOnly
                };
            }
            catch (Exception ex)
            {
                intent.BroadcastOutcome = BroadcastOutcome.Rejected;
                intent.LastError = ex.Message;
                intent.RetryAction = RetryAction.FailFinal;
                _repo.UpdateIntent(intent);

                return new BroadcastResponse
                {
                    IntentId = intent.IntentId,
                    BroadcastOutcome = BroadcastOutcome.Rejected,
                    ErrorCode = "RPC_ERROR",
                    LastError = ex.Message,
                    RetryAction = RetryAction.FailFinal
                };
            }
        }

        public async Task<GetStatusResponse> GetStatusAsync(GetStatusRequest request)
        {
            var intent = _repo.GetIntent(request.IntentId);
            if (intent == null) throw new Exception("Intent not found");

            if (intent.Status == TransferStatus.Finalized || intent.Status == TransferStatus.Failed)
            {
                return new GetStatusResponse
                {
                    Status = intent.Status,
                    TxHash = intent.TxHash,
                    RetryAction = RetryAction.FailFinal
                };
            }

            if (!string.IsNullOrEmpty(intent.TxHash))
            {
                try
                {
                    var chainService = GetChainService(request.Chain);
                    var (isFinal, isFailed, hash) = await chainService.GetStatusAsync(intent.TxHash);

                    if (isFinal) intent.Status = TransferStatus.Finalized;
                    else if (isFailed)
                    {
                        intent.Status = TransferStatus.Failed;
                        intent.RetryAction = RetryAction.FailFinal;
                    }
                    _repo.UpdateIntent(intent);
                }
                catch { /* RPC error, keep polling */ }
            }

            return new GetStatusResponse
            {
                Status = intent.Status,
                TxHash = intent.TxHash,
                RetryAction = intent.RetryAction ?? RetryAction.PollOnly
            };
        }

        public async Task<object> GetGasQuoteAsync(GasQuoteRequest request)
        {
            var chainService = GetChainService(request.Chain);
            return await chainService.GetGasQuoteAsync(request.To, request.Amount, request.From);
        }
    }
}
