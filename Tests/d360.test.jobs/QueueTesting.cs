using d360.core;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace d360.test.jobs
{
    [TestClass]
    public class QueueTesting
    {
        [TestMethod]
        public void ClearQueue_Success()
        {
            //var queueClient = QueueClient.CreateFromConnectionString(constants.SERVICE_BUS_ACTIONS, "company-actions");

            //while (queueClient.Peek() != null)
            //{
            //    var m = queueClient.Receive();
            //    if (m != null) m.Complete();
            //}

            //queueClient = null;
        }
    }
}
