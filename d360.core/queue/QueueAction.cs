namespace d360.core.queue
{
    public enum QueueAction
    {
        AddToIndex = 1,
        UpdateInIndex = 2,
        RemoveFromIndex = 3,
        AddVersion = 4,
        BulkLoad = 5,
        Cache = 6,
        Event = 7,
        Integration = 8,
        Scoring = 9
    }
}
