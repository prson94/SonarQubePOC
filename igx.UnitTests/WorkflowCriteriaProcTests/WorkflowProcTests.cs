using d360.model;
using d360.model.workflow;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.WorkflowCriteriaProcTests
{
    [Trait("Unit tests", "Workflow criteria processor - General Tests")]
    public class WorkflowProcTests : BaseTest
    {
        internal ICompanyContext context;
        public WorkflowProcTests()
        {
            this.context = GetCompany();
        }
        [Fact]
        public void EmptyCriteria()
        {
            bool procResult;
            procResult = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "", 0, "");
            Assert.True(procResult, "Empty critera should return true");
        }

        [Fact]
        public void InvalidObjectId()
        {
            bool didThrowError = false;
            try
            {
                WorkflowRegistrationCriteriaProcessor.Evaluate(context, "", 0, "<Conditions />");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("OBJECT ID MUST BE GREATER THAN 0"))
                    didThrowError = true;
            }
            Assert.True(didThrowError, "Invalid object id should throw error");
        }

        [Fact]
        public void InvalidObject()
        {
            bool didThrowError = false;
            try
            {
                WorkflowRegistrationCriteriaProcessor.Evaluate(context, "", 21, "<Conditions />");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("OBJECT ID MUST BE GREATER THAN 0"))
                    didThrowError = true;
            }
            Assert.True(didThrowError, "Invalid object should throw error");
        }

        [Fact]
        public void EmptyCondition()
        {
            bool? res = null;
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, "<Conditions />");
            Assert.True(res, "No conditions should return true");
        }

        [Fact]
        public void CaseWithoutChangedFields()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"C\" Connector=\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"L\" Operator=\"C\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Conditions with change conditions needs to have changed fields");
        }

        [Fact]
        public void SatisfyAllTestCase()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"C\" Connector=\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"L\" Operator=\"C\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 1, 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void SatisfyAllFailCase()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"C\" Connector=\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"L\" Operator=\"C\" Connector=\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 1 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }


        [Fact]
        public void SatisfyAllTestCaseWithAllFields()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"=\" Value=\"TestStringValue\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"=\" Value=\"12.56\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"=\" Value=\"5\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"4\" ValueType=\"B\" Operator=\"=\" Value=\"True\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"5\" ValueType=\"L\" Operator=\"=\" Value=\"1,2\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 1, 2, 3, 4, 5 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(5).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void SatisfyAllTestCaseWithAllFieldsFail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"=\" Value=\"TestStringValue\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"D\" Operator=\"=\" Value=\"12.56\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"=\" Value=\"7\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"4\" ValueType=\"B\" Operator=\"=\" Value=\"True\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"5\" ValueType=\"L\" Operator=\"=\" Value=\"1,2\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 1, 2, 3, 4, 5 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(5).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void SatisfyAnyTestCase()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"C\" Connector=\"OR\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"L\" Operator=\"C\" Connector=\"OR\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 1 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }


        [Fact]
        public void SatisfyAnyTestCase2()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"C\" Connector=\"OR\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"L\" Operator=\"C\" Connector=\"OR\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 2 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void SatisfyAnyTestCaseFail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"1\" ValueType=\"T\" Operator=\"C\" Connector=\"OR\" />" +
                "<Condition FieldTypeID=\"2\" ValueType=\"L\" Operator=\"C\" Connector=\"OR\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void NonExistantFieldCase()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"78\" ValueType=\"T\" Operator=\"=\" Value=\"TestStringValue\" Connector =\"AND\" />" +
                  "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { };
            try
            {
                res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            }
            catch
            {
                Assert.True(false, "Should not throw error if there is no field defined!");
            }
            Assert.False(res, "Invalid evaluation result!");
        }
    }
}
