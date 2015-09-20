using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ServiceBus.Messaging;
using System.Configuration;
using d360.core;

namespace d360.test.jobs
{
    [TestClass]
    public class QueueTesting
    {
        [TestMethod]
        public void ClearQueue_Success()
        {
            var queueClient = QueueClient.CreateFromConnectionString(constants.SERVICE_BUS_ACTIONS, "company-actions");

            while (queueClient.Peek() != null)
            {
                var m = queueClient.Receive();
                if (m != null) m.Complete();
            }

            queueClient = null;
        }
    }
}
