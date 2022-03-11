namespace d360.core.queue
{
    public interface IFilteredServiceBusMessage
    {
        string EventType { get; set; }
    }
}
