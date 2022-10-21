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
    [Trait("Unit tests", "Workflow criteria processor - Form Tests")]
    public class WorkflowProcFormTests : BaseTest
    {
        internal ICompanyContext context;
        public WorkflowProcFormTests()
        {
            this.context = GetCompany();
        }

        [Fact]
        public void SimpleBoolForm()
        {
            string condition = "<Conditions>" +
                "<Condition VersionStepID=\"1\" FormInputID=\"boolean1\" ValueType=\"B\" Operator=\"=\" Value=\"true\" />" +
                  "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, 1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void SimpleBoolFormFails()
        {
            string condition = "<Conditions>" +
                "<Condition VersionStepID=\"1\" FormInputID=\"boolean1\" ValueType=\"B\" Operator=\"=\" Value=\"False\" />" +
                  "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, 1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void MultiTypeForm()
        {
            string condition = "<Conditions>" +
                  "<Condition VersionStepID =\"1\" FormInputID=\"boolean1\" ValueType=\"B\" Value=\"true\" Operator=\"=\" Connector=\"AND\" />" +
                  "<Condition VersionStepID =\"1\" FormInputID=\"integer1\" ValueType=\"D\" Operator=\"=\" Value=\"45\" />" +
                  "<Condition VersionStepID =\"1\" FormInputID=\"text1\" ValueType=\"T\" Operator=\"=\" Value=\"TestText\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, 1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void MultiTypeForm_Fail()
        {
            string condition = "<Conditions>" +
                  "<Condition VersionStepID =\"1\" FormInputID=\"boolean1\" ValueType=\"B\" Value=\"true\" Operator=\"=\" Connector=\"AND\" />" +
                  "<Condition VersionStepID =\"1\" FormInputID=\"integer1\" ValueType=\"D\" Operator=\"=\" Value=\"45\" />" +
                  "<Condition VersionStepID =\"1\" FormInputID=\"text1\" ValueType=\"T\" Operator=\"=\" Value=\"TestText?\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, 1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void MultiTypeForm_InvalidVersionId()
        {
            string condition = "<Conditions>" +
                  "<Condition VersionStepID =\"789789\" FormInputID=\"boolean1\" ValueType=\"B\" Value=\"true\" Operator=\"=\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, 1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void MultiTypeForm_InvalidItemId()
        {
            string condition = "<Conditions>" +
                  "<Condition VersionStepID =\"1\" FormInputID=\"boolean1\" ValueType=\"B\" Value=\"true\" Operator=\"=\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }
    }
}
