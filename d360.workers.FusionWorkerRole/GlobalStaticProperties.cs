using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.workers.FusionWorkerRole
{
    internal static class GlobalStaticProperties
    {
        private static int _QueueMessageVisibilityTime { get; set; }
        /// <summary>
        /// This is the amount of time the message remains invisible after being
        /// read from the queue, before it becomes visible again (unless it is deleted)
        /// </summary>
        internal static int QueueMessageVisibilityTime
        {
            get
            {
                if (_QueueMessageVisibilityTime <= 0)
                {
                    //hasn't been loaded yet, so load it 
                    string VisTime =
                      RoleEnvironment.GetConfigurationSettingValue("QueueMessageVisibilityTime");
                    int intTest = 0;
                    bool success = int.TryParse(VisTime, out intTest);
                    if (!success || intTest <= 0)
                    {
                        _QueueMessageVisibilityTime = 120;
                    }
                    else
                    {
                        _QueueMessageVisibilityTime = intTest;
                    }
                    Trace.TraceInformation("[d360.workers.FusionWorkerRole.GlobalStaticProperties] "
                      + "Setting QueueMessageVisibilityTime to {0}", _QueueMessageVisibilityTime);
                }
                return _QueueMessageVisibilityTime;
            }
        }
    }
}
