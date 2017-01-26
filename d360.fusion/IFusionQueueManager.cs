using System.Threading.Tasks;
using d360.core.entities;

namespace d360.fusion
{
    public interface IFusionQueueManager
    {
        Task ProcessMessagesAsync(int messageReservationTime = 1800,
                                                int bulkTimeout = 180,
                                                int readTimeout = 180,
                                                int executionTimeout = 180,
                                                int maxRetries = 3,
                                                int mergeSize = 50000);
        Task SendMessageAsync(FusionProcessingData fusion);
    }
}