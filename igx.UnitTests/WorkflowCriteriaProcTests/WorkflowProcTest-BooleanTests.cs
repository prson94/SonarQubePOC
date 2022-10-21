using d360.model;
using d360.model.DataAccessLayer;
using d360.model.workflow;
using igx.UnitTests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.WorkflowCriteriaProcTests
{
    [Trait("Unit tests", "Workflow criteria processor - Boolean tests")]
    public class WorkflowProcBoolTests : BaseTest
    {
        internal ICompanyContext context;
		//internal IAssetRepository assetsRepo;
		public WorkflowProcBoolTests()
        {
            context = GetCompany();
			//assetsRepo = GetAssetRepository();
			//context.Assets = new System.Data.Entity.DbSet<d360.core.entities.Asset>();
			//context.Set<d360.core.entities.Asset>().Add(new d360.core.entities.Asset{ ID = 1, Object = "Artifact", ObjectID = 1 });
			//var assetType = assetsRepo.GetAssetType(new List<KeyValuePair<string, string>>(), null, assetTypeUid: Guid.Parse(DataConstants.ValidGUID));
			//context.Assets = assetsRepo.GetAssets(assetType, new List<KeyValuePair<string, string>>());
		}

        [Fact]
        public void BoolConditionEqualTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"4\" ValueType=\"B\" Operator=\"=\" Value=\"True\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 4 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }

        [Fact]
        public void BoolConditionEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"4\" ValueType=\"B\" Operator=\"=\" Value=\"False\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 4 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void BoolConditionNotEqualTest()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"4\" ValueType=\"B\" Operator=\"!=\" Value=\"True\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 4 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.False(res, "Invalid evaluation result!");
        }

        [Fact]
        public void BoolConditionNotEqualTest_Fail()
        {
            string condition = "<Conditions>" +
                "<Condition FieldTypeID=\"4\" ValueType=\"B\" Operator=\"!=\" Value=\"False\" Connector =\"AND\" />" +
                "</Conditions>";
            bool? res = null;
            List<int> changedFields = new List<int> { 4 };
            res = WorkflowRegistrationCriteriaProcessor.Evaluate(context, "Artifact", 1, condition, -1, changedFields);
            Assert.True(res, "Invalid evaluation result!");
        }
    }
}