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
        private static int _DBBulkCopyTimeout { get; set; }
        private static int _DBReadQueryTimeout { get; set; }
        private static int _DBExecuteQueryTimeout { get; set; }
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

        /// <summary>
        /// Time in seconds before bulk copy operations fail due to timeout
        /// </summary>
        internal static int DBBulkCopyTimeout
        {
            get
            {
                if (_DBBulkCopyTimeout <= 0)
                {
                    //hasn't been loaded yet, so load it 
                    string VisTime =
                      RoleEnvironment.GetConfigurationSettingValue("DBBulkCopyTimeout");
                    int intTest = 0;
                    bool success = int.TryParse(VisTime, out intTest);
                    if (!success || intTest <= 0)
                    {
                        _DBBulkCopyTimeout = 120;
                    }
                    else
                    {
                        _DBBulkCopyTimeout = intTest;
                    }
                    Trace.TraceInformation("[d360.workers.FusionWorkerRole.GlobalStaticProperties] "
                      + "Setting DBBulkCopyTimeout to {0}", _DBBulkCopyTimeout);
                }
                return _DBBulkCopyTimeout;
            }
        }

        internal static int DBReadQueryTimeout
        {
            get
            {
                if (_DBReadQueryTimeout <= 0)
                {
                    //hasn't been loaded yet, so load it 
                    string VisTime =
                      RoleEnvironment.GetConfigurationSettingValue("DBReadQueryTimeout");
                    int intTest = 0;
                    bool success = int.TryParse(VisTime, out intTest);
                    if (!success || intTest <= 0)
                    {
                        _DBReadQueryTimeout = 120;
                    }
                    else
                    {
                        _DBReadQueryTimeout = intTest;
                    }
                    Trace.TraceInformation("[d360.workers.FusionWorkerRole.GlobalStaticProperties] "
                      + "Setting DBReadQueryTimeout to {0}", _DBReadQueryTimeout);
                }
                return _DBReadQueryTimeout;
            }
        }


        internal static int DBExecuteQueryTimeout
        {
            get
            {
                if (_DBExecuteQueryTimeout <= 0)
                {
                    //hasn't been loaded yet, so load it 
                    string VisTime =
                      RoleEnvironment.GetConfigurationSettingValue("DBExecuteQueryTimeout");
                    int intTest = 0;
                    bool success = int.TryParse(VisTime, out intTest);
                    if (!success || intTest <= 0)
                    {
                        _DBExecuteQueryTimeout = 120;
                    }
                    else
                    {
                        _DBExecuteQueryTimeout = intTest;
                    }
                    Trace.TraceInformation("[d360.workers.FusionWorkerRole.GlobalStaticProperties] "
                      + "Setting DBExecuteQueryTimeout to {0}", _DBExecuteQueryTimeout);
                }
                return _DBExecuteQueryTimeout;
            }
        }
    }
}
