using d360.extensions.queue;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace d360.test.jobs
{
    [TestClass]
    public class WorkflowTests: BaseTest
    {
        //[TestMethod]
        //public void SendToServiceBus_OK()
        //{
        //    var src = new AzureQueueSource();

        //    src.CreateTopicMessage(new core.queue.EventInfo {
        //        Action = core.enums.Workflow.ChangeType.Add,
        //        CompanyID = 4,
        //        DomainPrefix = "demo.dev",
        //        Object = core.SystemObjects.Artifact,
        //        ObjectID = 4651,
        //        ObjectType = core.SystemObjects.ArtifactType,
        //        ObjectTypeID = 1,
        //        ResourceID = 1
        //    });
        //}


        [TestMethod]
        public void TestEntireWorkflowProcess()
        {

        }
    }
}
