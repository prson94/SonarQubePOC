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
    [Trait("Unit tests", "Workflow criteria processor - String tests")]
    public class WorkflowProcStringTests : BaseTest
    {
        internal ICompanyContext context;
        public WorkflowProcStringTests()
        {
            this.context = GetCompany();
        }

        [Fact]
        public void StringConditionEqualTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"=\" Value=\"TestStringValue\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 1 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void StringConditionEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"=\" Value=\"asdasdas\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 1 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void StringConditionNotEqualTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"!=\" Value=\"TestStringValue\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 1 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void StringConditionNotEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"!=\" Value=\"asdasdas\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 1 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

    }
}