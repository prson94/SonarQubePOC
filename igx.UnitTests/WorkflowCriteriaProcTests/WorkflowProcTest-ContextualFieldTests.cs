using d360.model;
using d360.model.workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.WorkflowCriteriaProcTests
{
    [Trait("Unit tests", "Workflow criteria processor - Contextual fields tests")]
    public class WorkflowProcContextualTests : BaseTest
    {
        internal ICompanyContext context;
        public WorkflowProcContextualTests()
        {
            this.context = GetCompany();
        }

        [Fact]
        public void ContextualFieldIssueTest()
        {
            string condition = "<Conditions>" +
    "<Condition ValueType =\"T\" Value=\"ArtifactType\" Operator=\"=\" ContextualFieldID=\"IssueObject\" ValueLabel=\"ArtifactType\" Connector=\"AND\" />" +
    "<Condition ValueType =\"D\" Value=\"199\" Operator=\"=\" ContextualFieldID=\"IssueObjectID\" ValueLabel=\"199\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };

            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Issue", 1, condition, -1, changedFields, "ArtifactType", 199);
            Assert.True(res, "Invalid evaluation result!");

        }


        [Fact]
        public void ContextualFieldNameTest()
        {
            string condition = "<Conditions>" +
    "<Condition ValueType =\"T\" Value=\"ObjectName\" Operator=\"=\" ContextualFieldID=\"Name\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };

            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void ContextualFieldNameTest_Fails()
        {
            string condition = "<Conditions>" +
    "<Condition ValueType =\"T\" Value=\"ObjectName?\" Operator=\"=\" ContextualFieldID=\"Name\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };

            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void ContextualFieldDescriptionTest()
        {
            string condition = "<Conditions>" +
    "<Condition ValueType =\"T\" Value=\"ObjectDescription\" Operator=\"=\" ContextualFieldID=\"Description\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };

            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void ContextualFieldDescriptionTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition ValueType =\"T\" Value=\"ObjectDescription?\" Operator=\"=\" ContextualFieldID=\"Description\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };

            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }


        [Fact]
        public void ContextualFieldRequestedonTest()
        {
            var numDays = (new DateTime(2000, 01, 01) - DateTime.Now.Date).TotalDays;

            string condition = "<Conditions>" +
                $"<Condition ValueType =\"DT\" Value=\"{numDays}\" Operator=\"=\" ContextualFieldID=\"RequestedOn\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };

            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void ContextualFieldRequestedonTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition ValueType =\"DT\" Value=\"1\" Operator=\"=\" ContextualFieldID=\"RequestedOn\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };

            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }
    }
}