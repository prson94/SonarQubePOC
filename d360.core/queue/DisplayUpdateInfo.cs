namespace d360.core.queue
{
    public class DisplayUpdateInfo : QueueObject
    {
        public int AssetTypeID { get; set; }
        public long AssetID { get; set; }

        public string ObjectType { get; set; }

        public int ObjectTypeID { get; set; }

        public bool RebuildAll { get; set; }
    }
}
