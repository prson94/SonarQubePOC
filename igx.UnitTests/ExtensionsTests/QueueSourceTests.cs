using d360.core.queue;
using d360.extensions.queue;
using System;
using Xunit;

namespace igx.UnitTests.ExtensionsTests
{
    [Trait("Unit tests", "Queue Source - Tests logic and methods for topic messages")]
    public class QueueSourceTests : BaseTest
    {

        public QueueSourceTests()
        {

        }


        [Fact]
        public void CheckNullValuePassed()
        {
            var queueSource = new AzureQueueSource();
            Assert.Throws<ArgumentNullException>(() => queueSource.GetMessageIdFromEventInfo(null));
        }

        [Fact]
        public void CheckAppendsObjectInfo()
        {
            var queueSource = new AzureQueueSource();
            var eventInfoNoObject = new EventInfo()
            {
                CompanyID = 1,
                Action = d360.core.enums.Workflow.ChangeType.Add,
                DomainPrefix = "int",
                ItemStepID = 5,
                ResourceID = 10,
                VersionStepTransitionID = 15,
                WorkflowItemID = 20,
                Object = null
            };

            var eventInfoObject = new EventInfo()
            {
                CompanyID = 1,
                Action = d360.core.enums.Workflow.ChangeType.Add,
                DomainPrefix = "int",
                ItemStepID = 5,
                ResourceID = 10,
                VersionStepTransitionID = 15,
                WorkflowItemID = 20,
                Object = new EventObjectInfo()
                {
                    Object = d360.core.SystemObjects.Artifact,
                    ObjectID = 25
                }
            };


            var messageIdNoObject = queueSource.GetMessageIdFromEventInfo(eventInfoNoObject);
            var messageIdObject = queueSource.GetMessageIdFromEventInfo(eventInfoObject);

            Assert.False(messageIdNoObject.ToLowerInvariant() == messageIdObject.ToLowerInvariant(), "Message Id generated does not have appended object values");

        }

        [Fact]
        public void CheckUniqueSameWorkflowItem()
        {
            var queueSource = new AzureQueueSource();
            var eventInfo1 = new EventInfo()
            {
                CompanyID = 1,
                Action = d360.core.enums.Workflow.ChangeType.Add,
                DomainPrefix = "int",
                ItemStepID = 5,
                ResourceID = 10,
                VersionStepTransitionID = 15,
                WorkflowItemID = 20,
                Object = null
            };

            var eventInfo2 = new EventInfo()
            {
                CompanyID = 1,
                Action = d360.core.enums.Workflow.ChangeType.Add,
                DomainPrefix = "int",
                ItemStepID = 6,
                ResourceID = 10,
                VersionStepTransitionID = 16,
                WorkflowItemID = 20,
                Object = null
            };


            var messageId1 = queueSource.GetMessageIdFromEventInfo(eventInfo1);
            var messageId2 = queueSource.GetMessageIdFromEventInfo(eventInfo2);

            Assert.False(messageId1.ToLowerInvariant() == messageId2.ToLowerInvariant(), "Message Id generated is not unique when different steps are called within the same workflow instance");

        }

        [Fact]
        public void CheckUniqueSameIdsDifferentCompany()
        {
            var queueSource = new AzureQueueSource();
            var eventInfo1 = new EventInfo()
            {
                CompanyID = 1,
                Action = d360.core.enums.Workflow.ChangeType.Add,
                DomainPrefix = "int",
                ItemStepID = 5,
                ResourceID = 10,
                VersionStepTransitionID = 15,
                WorkflowItemID = 20,
                Object = null
            };

            var eventInfo2 = new EventInfo()
            {
                CompanyID = 2,
                Action = d360.core.enums.Workflow.ChangeType.Add,
                DomainPrefix = "int2",
                ItemStepID = 5,
                ResourceID = 10,
                VersionStepTransitionID = 15,
                WorkflowItemID = 20,
                Object = null
            };


            var messageId1 = queueSource.GetMessageIdFromEventInfo(eventInfo1);
            var messageId2 = queueSource.GetMessageIdFromEventInfo(eventInfo2);

            Assert.False(messageId1.ToLowerInvariant() == messageId2.ToLowerInvariant(), "Message Id is not unique when generated for different companies");

        }

        [Fact]
        public void CheckUniqueDifferentActions()
        {
            var queueSource = new AzureQueueSource();
            var eventInfo1 = new EventInfo()
            {
                CompanyID = 1,
                Action = d360.core.enums.Workflow.ChangeType.Add,
                DomainPrefix = "int",
                ItemStepID = 5,
                ResourceID = 10,
                VersionStepTransitionID = 15,
                WorkflowItemID = 20,
                Object = null
            };

            var eventInfo2 = new EventInfo()
            {
                CompanyID = 1,
                Action = d360.core.enums.Workflow.ChangeType.Update,
                DomainPrefix = "int",
                ItemStepID = 5,
                ResourceID = 10,
                VersionStepTransitionID = 15,
                WorkflowItemID = 20,
                Object = null
            };


            var messageId1 = queueSource.GetMessageIdFromEventInfo(eventInfo1);
            var messageId2 = queueSource.GetMessageIdFromEventInfo(eventInfo2);

            Assert.False(messageId1.ToLowerInvariant() == messageId2.ToLowerInvariant(), "Message Id is not unique when generated for different actions");

        }

        [Fact]
        public void CheckUniqueDifferentObjects()
        {
            var queueSource = new AzureQueueSource();
            var eventInfo1 = new EventInfo()
            {
                CompanyID = 1,
                Action = d360.core.enums.Workflow.ChangeType.Add,
                DomainPrefix = "int",
                ItemStepID = 5,
                ResourceID = 10,
                VersionStepTransitionID = 15,
                WorkflowItemID = 20,
                Object = new EventObjectInfo()
                { 
                    Object = d360.core.SystemObjects.Artifact,
                    ObjectID = 25
                }
            };

            var eventInfo2 = new EventInfo()
            {
                CompanyID = 1,
                Action = d360.core.enums.Workflow.ChangeType.Update,
                DomainPrefix = "int",
                ItemStepID = 5,
                ResourceID = 10,
                VersionStepTransitionID = 15,
                WorkflowItemID = 20,
                Object = new EventObjectInfo()
                {
                    Object = d360.core.SystemObjects.Artifact,
                    ObjectID = 26
                }
            };


            var messageId1 = queueSource.GetMessageIdFromEventInfo(eventInfo1);
            var messageId2 = queueSource.GetMessageIdFromEventInfo(eventInfo2);

            Assert.False(messageId1.ToLowerInvariant() == messageId2.ToLowerInvariant(), "Message Id is not unique when generated for the same workflow and step with different objects");

        }

    }
}