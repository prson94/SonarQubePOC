using d360.core.queue;
using d360.extensions.storage;
using System.Threading.Tasks;

namespace igx.jobs.scoreprocessor.ChangeTypes
{
    interface IScoreProcess
    {
        ScoreQueueInfo Info { get; set; }
        AzureStorageProvider Storage { get; set; }

        Task Run();
    }
}
