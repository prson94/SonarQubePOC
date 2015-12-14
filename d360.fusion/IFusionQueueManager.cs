using System.Threading.Tasks;
using d360.core.entities;

namespace d360.workers.FusionWorkerRole
{
    public interface IFusionQueueManager
    {
        Task ProcessMessagesAsync();
        Task SendMessageAsync(FusionProcessingData fusion);
    }
}