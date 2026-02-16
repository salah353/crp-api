using alpha.Models;
using System.Collections.Concurrent;

namespace alpha.Repositories
{
    public class Repo : iRepo
    {
        private static readonly ConcurrentDictionary<string, TransferIntent> _intents = new();
        // Index for idempotency key
        private static readonly ConcurrentDictionary<string, string> _clientKeyIndex = new();

        public TransferIntent CreateIntent(TransferIntent intent)
        {
            if (string.IsNullOrEmpty(intent.IntentId))
                intent.IntentId = Guid.NewGuid().ToString();

            _intents[intent.IntentId] = intent;

            if (!string.IsNullOrEmpty(intent.ClientIdempotencyKey))
            {
                _clientKeyIndex[intent.ClientIdempotencyKey] = intent.IntentId;
            }

            return intent;
        }

        public TransferIntent? GetIntent(string intentId)
        {
            if (_intents.TryGetValue(intentId, out var intent))
                return intent;
            return null;
        }

        public TransferIntent? GetIntentByClientKey(string clientKey)
        {
            if (_clientKeyIndex.TryGetValue(clientKey, out var intentId))
            {
                return GetIntent(intentId);
            }
            return null;
        }

        public void UpdateIntent(TransferIntent intent)
        {
            if (_intents.ContainsKey(intent.IntentId))
            {
                intent.UpdatedAt = DateTime.UtcNow;
                _intents[intent.IntentId] = intent;
            }
        }

        public IEnumerable<TransferIntent> GetAll()
        {
            return _intents.Values;
        }
    }
}
