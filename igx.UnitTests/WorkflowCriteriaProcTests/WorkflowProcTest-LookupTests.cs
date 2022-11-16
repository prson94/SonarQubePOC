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
    [Trait("Unit tests", "Workflow criteria processor - Lookup tests")]
    public class WorkflowProcLookupTests : BaseTest
    {
        internal ICompanyContext context;
        public WorkflowProcLookupTests()
        {
            this.context = GetCompany();
        }

        [Fact]
        public void LookupConditionEqualTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"5\" ValueType=\"L\" Operator=\"=\" Value=\"1,2\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 4 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void LookupConditionEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"5\" ValueType=\"L\" Operator=\"=\" Value=\"1\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 4 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void LookupConditionNotEqualTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"5\" ValueType=\"L\" Operator=\"!=\" Value=\"1,2\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 4 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void LookupConditionNotEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"5\" ValueType=\"L\" Operator=\"!=\" Value=\"1\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 4 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }
    }
}