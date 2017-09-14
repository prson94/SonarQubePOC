using System;

namespace d360.workers.ProcessDatabaseTasksWorkerRole
{
    public class QueueTask
    {
        public Guid ID { get; set; }
        public string Action { get; set; }
        public string Custom { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public DateTime Date { get; set; }
        public string MachineAssigned { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
        public int NumberOfRetries { get; set; }
        public short Priority { get; set; }
    }
}
