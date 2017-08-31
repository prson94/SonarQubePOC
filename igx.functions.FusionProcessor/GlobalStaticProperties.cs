using System.Diagnostics;

namespace igx.function.FusionProcessor
{
    internal static class GlobalStaticProperties
    {
        private static int _QueueMessageVisibilityTime { get; set; }
        private static int _DBBulkCopyTimeout { get; set; }
        private static int _DBReadQueryTimeout { get; set; }
        private static int _DBExecuteQueryTimeout { get; set; }
        private static int _MaximumRetries { get; set; }
        private static string _QueueName { get; set; }
        private static int _QueueCheckFrequency { get; set; }

        private static int _MergeChunkSize { get; set; }
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
                    string VisTime = RoleEnvironment.GetConfigurationSettingValue("QueueMessageVisibilityTime");
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

        
        internal static int MaximumRetries
        {
            get
            {
                if (_MaximumRetries <= 0)
                {
                    //hasn't been loaded yet, so load it 
                    string VisTime =
                      RoleEnvironment.GetConfigurationSettingValue("MaximumRetries");
                    int intTest = 0;
                    bool success = int.TryParse(VisTime, out intTest);
                    if (!success || intTest <= 0)
                    {
                        _MaximumRetries = 3;
                    }
                    else
                    {
                        _MaximumRetries = intTest;
                    }
                    Trace.TraceInformation("[d360.workers.FusionWorkerRole.GlobalStaticProperties] "
                      + "Setting MaximumRetries to {0}", _MaximumRetries);
                }
                return _MaximumRetries;
            }
        }

        internal static string QueueName
        {
            get
            {
                if (string.IsNullOrEmpty(_QueueName))
                {
                    //hasn't been loaded yet, so load it 
                    _QueueName =
                      RoleEnvironment.GetConfigurationSettingValue("QueueName");
                    
                    Trace.TraceInformation("[d360.workers.FusionWorkerRole.GlobalStaticProperties] "
                      + "Setting QueueName to {0}", _QueueName);
                }
                return _QueueName;
            }
        }

        internal static int QueueCheckFrequency
        {
            get
            {
                if (_QueueCheckFrequency <= 0)
                {
                    //hasn't been loaded yet, so load it 
                    string queueCheckFrequency =
                      RoleEnvironment.GetConfigurationSettingValue("QueueCheckFrequency");
                    int intTest = 0;
                    bool success = int.TryParse(queueCheckFrequency, out intTest);
                    if (!success || intTest <= 0)
                    {
                        _QueueCheckFrequency = 60000;
                    }
                    else
                    {
                        _QueueCheckFrequency = intTest;
                    }
                    Trace.TraceInformation("[d360.workers.FusionWorkerRole.GlobalStaticProperties] "
                      + "Setting QueueCheckFrequency to {0}", _QueueCheckFrequency);
                }
                return _QueueCheckFrequency;
            }
        }


        //Size of chunks for merge into the fields table
        internal static int MergeChunkSize
        {
            get
            {
                if (_MergeChunkSize <= 0)
                {
                    //hasn't been loaded yet, so load it 
                    string mergeChunkSize =
                      RoleEnvironment.GetConfigurationSettingValue("MergeChunkSize");
                    int intTest = 0;
                    bool success = int.TryParse(mergeChunkSize, out intTest);
                    if (!success || intTest <= 0)
                    {
                        _MergeChunkSize = 50000;
                    }
                    else
                    {
                        _MergeChunkSize = intTest;
                    }
                    Trace.TraceInformation("[d360.workers.FusionWorkerRole.GlobalStaticProperties] "
                      + "Setting MergeChunkSize to {0}", _MergeChunkSize);
                }
                return _MergeChunkSize;
            }
        }

    }
}
