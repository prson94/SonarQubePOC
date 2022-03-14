namespace d360.core.queue
{
    public class IntegrationQueueModel : QueueObject
    {
        public int IntegrationSettingID { get; set; }

        public long ExecutionID { get; set; }

        public int SynchedAssetTypeID { get; set; }

        public string UrlPrefix { get; set; }
    }
}
