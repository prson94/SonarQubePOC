using System;

namespace d360.core.entities.api
{
    public class FusionConfigurationScheduleRequestModel
    {
        public FusionConfigurationScheduleRequestModel()
        {
            IsComplete = false;
        }

        public Guid ID { get; set; }
        
        public string MachineQueuedOn { get; set; }

        public bool IsComplete { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
    }
}
