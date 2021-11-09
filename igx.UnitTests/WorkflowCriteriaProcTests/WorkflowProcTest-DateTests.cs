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
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"=\" Value=\"5\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(5).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"=\" Value=\"5\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(4).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionNotEqualTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"!=\" Value=\"5\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(5).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionNotEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"!=\" Value=\"5\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(4).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionGreaterThanTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\">\" Value=\"5\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(7).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionGreaterThanTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\">\" Value=\"5\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(3).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionLessThanTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"&lt;\" Value=\"5\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(3).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionLessThanTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"&lt;\" Value=\"5\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(8).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionLessOrEqThanTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"&lt;=\" Value=\"5\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"&lt;=\" Value=\"6\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(5).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionLessOrEqTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"&lt;=\" Value=\"5\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\"&lt;=\" Value=\"2\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(5).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionMoreOrEqThanTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\">=\" Value=\"5\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\">=\" Value=\"4\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(5).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void DateConditionMoreOrEqTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\">=\" Value=\"5\" Connector =\"AND\" />" +
                "<Condition FieldTypeID=\"3\" ValueType=\"DT\" Operator=\">=\" Value=\"7\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 3 };
            var dateField = context.Fields.FirstOrDefault(x => x.FieldTypeID == 3);
            dateField.FormattedValue = DateTime.Now.AddDays(5).ToString(CultureInfo.InvariantCulture);
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "ArtifactType", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }
    }
}