using d360.core.queue;

namespace d360.core.entities.Contracts
{
    public interface IEventTrackedEntity
    {
        EventObjectInfo GetEventObjectInfo();
    }
}
