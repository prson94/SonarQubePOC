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
    [Trait("Unit tests", "Workflow criteria processor - Date tests")]
    public class WorkflowProcDateTests : BaseTest
    {
        internal ICompanyContext context;
        public WorkflowProcDateTests()
        {
            this.context = GetCompany();
        }

        [Fact]
        public void DateConditionEqualTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"=\" Value=\"12.56\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"=\" Value=\"12.42\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionNotEqualTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"=\" Value=\"12.42\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionNotEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"=\" Value=\"12.56\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionGreaterThanTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\">\" Value=\"12.35\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionGreaterThanTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\">\" Value=\"12.58\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionLessThanTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"&lt;\" Value=\"12.62\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionLessThanTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"&lt;\" Value=\"12.12\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionLessOrEqThanTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"&lt;=\" Value=\"12.62\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"&lt;=\" Value=\"12.56\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionLessOrEqTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"&lt;=\" Value=\"12.12\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"&lt;=\" Value=\"12.56\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionMoreOrEqThanTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\">=\" Value=\"12.24\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\">=\" Value=\"12.56\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionMoreOrEqTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\">=\" Value=\"12.57\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\">=\" Value=\"12.56\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }
    }
}