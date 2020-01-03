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
    [Trait("Unit tests", "Workflow criteria processor - Double tests")]
    public class WorkflowProcDoubleTests : BaseTest
    {
        internal ICompanyContext context;
        public WorkflowProcDoubleTests()
        {
            this.context = GetCompany();
        }

        [Fact]
        public void DoubleConditionEqualTest()
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
        public void DoubleConditionEqualTest_Fail()
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
        public void DoubleConditionNotEqualTest()
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
        public void DoubleConditionNotEqualTest_Fail()
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
        public void DoubleConditionGreaterThanTest()
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
        public void DoubleConditionGreaterThanTest_Fail()
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
        public void DoubleConditionLessThanTest()
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
        public void DoubleConditionLessThanTest_Fail()
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
        public void DoubleConditionLessOrEqThanTest()
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
        public void DoubleConditionLessOrEqTest_Fail()
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
        public void DoubleConditionMoreOrEqThanTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\">=\" Value=\"12.24\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"&lt;=\" Value=\"12.56\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DoubleConditionMoreOrEqTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\">=\" Value=\"12.89\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"&lt;=\" Value=\"12.56\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }
    }
}