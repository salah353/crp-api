using crp_api.Models;

namespace crp_api.Repositories
{
    public interface iRepo
    {
        TransferIntent CreateIntent(TransferIntent intent);
        TransferIntent? GetIntent(string intentId);
        TransferIntent? GetIntentByClientKey(string clientKey);
        void UpdateIntent(TransferIntent intent);
        IEnumerable<TransferIntent> GetAll();
    }
}
