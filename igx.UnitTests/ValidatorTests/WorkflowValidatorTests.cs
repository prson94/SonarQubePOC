using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.model.validators;
using igx.UnitTests.Core;
using Xunit;

namespace igx.UnitTests.ValidatorTests
{
    [Trait("Unit tests", "Workflow Validator")]

    public class WorkflowValidatorTests : BaseTest
    {
        private IWorkflowApiModelValidator validator;
        public WorkflowValidatorTests()
        {
            validator = this.GetWorkflowApiModelValidator();
        }

        delegate bool CurrentTestingMethod(IEnumerable<KeyValuePair<string, string>> query);
        [Fact]
        public void IsValidGuidCountForWorkflowGetTypeModelTest()
        {
            bool result;
            var queryParams = new List<KeyValuePair<string, string>>();
            CurrentTestingMethod testMethod = new CurrentTestingMethod(validator.IsValidGuidCountForWorkflowGetTypeModel);

            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Add(new KeyValuePair<string, string>("actiontypeuid",DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Add(new KeyValuePair<string, string>("assettypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("assettypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("relationshiptypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);
        }

        [Fact]
        public void IsValidGuidForWorkflowGetTypeModelTest()
        {
            bool result;
            var queryParams = new List<KeyValuePair<string, string>>();
            CurrentTestingMethod testMethod = new CurrentTestingMethod(validator.IsValidGuidForWorkflowGetTypeModel);

            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Add(new KeyValuePair<string, string>("actiontypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("assettypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("relationshiptypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("actiontypeuid", ""));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("assettypeuid", ""));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);


            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("relationshiptypeuid", ""));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);
        }


        [Fact]
        public void IsValidGuidCountForWorkflowGetVersionModelTest()
        {
            bool result;
            var queryParams = new List<KeyValuePair<string, string>>();
            CurrentTestingMethod testMethod = new CurrentTestingMethod(validator.IsValidGuidCountForWorkflowGetVersionModel);

            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Add(new KeyValuePair<string, string>("actiontypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Add(new KeyValuePair<string, string>("assettypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("assettypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("relationshiptypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);
        }

        [Fact]
        public void IsValidGuidForWorkflowGetVersionModel()
        {
            bool result;
            var queryParams = new List<KeyValuePair<string, string>>();
            CurrentTestingMethod testMethod = new CurrentTestingMethod(validator.IsValidGuidForWorkflowGetVersionModel);

            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Add(new KeyValuePair<string, string>("actiontypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("assettypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("relationshiptypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("workflowtypeuid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("actiontypeuid", ""));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("assettypeuid", ""));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);


            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("relationshiptypeuid", ""));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("workflowtypeuid", ""));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);
        }

        [Fact]
        public void IsValidGuidCountForGetWorkflowModel()
        {
            bool result;
            var queryParams = new List<KeyValuePair<string, string>>();
            CurrentTestingMethod testMethod = new CurrentTestingMethod(validator.IsValidGuidCountForGetWorkflowModel);

            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Add(new KeyValuePair<string, string>("ActionUid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Add(new KeyValuePair<string, string>("AssetUid", DataConstants.ValidGUID2));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("AssetUid", DataConstants.ValidGUID2));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("RelationshipUid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

        }

        [Fact]
        public void IsValidAsset()
        {
            bool result;
            CurrentTestingMethod testingMethod = new CurrentTestingMethod(validator.IsValidAsset);
            var queryParams = new List<KeyValuePair<string, string>>();

            queryParams.Add(new KeyValuePair<string, string>("AssetUid", DataConstants.ValidGUID));
            result = testingMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("AssetUid", DataConstants.InvalidGUID));
            result = testingMethod.Invoke(queryParams);
            Assert.False(result);

        }

       [Fact]
        public void IsValidAction()
        {
            bool result;
            CurrentTestingMethod testingMethod = new CurrentTestingMethod(validator.IsValidAction);
            var queryParams = new List<KeyValuePair<string, string>>();

            queryParams.Add(new KeyValuePair<string, string>("ActionUid", DataConstants.ValidGUID));
            result = testingMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("ActionUid", DataConstants.InvalidGUID));
            result = testingMethod.Invoke(queryParams);
            Assert.False(result);

        }

        [Fact]
        public void IsValidRelationship()
        {
            bool result;
            CurrentTestingMethod testingMethod = new CurrentTestingMethod(validator.IsValidRelationship);
            var queryParams = new List<KeyValuePair<string, string>>();

            queryParams.Add(new KeyValuePair<string, string>("RelationshipUid", DataConstants.ValidGUID));
            result = testingMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("RelationshipUid", DataConstants.InvalidGUID));
            result = testingMethod.Invoke(queryParams);
            Assert.False(result);
        }

       [Fact]
        public void IsValidGuidForGetWorkflowModel()
        {
            bool result;
            var queryParams = new List<KeyValuePair<string, string>>();
            CurrentTestingMethod testMethod = new CurrentTestingMethod(validator.IsValidGuidForGetWorkflowModel);

            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Add(new KeyValuePair<string, string>("ActionUid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("ActionUid", DataConstants.WrongFormatGUID));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("AssetUid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("AssetUid", DataConstants.WrongFormatGUID));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("RelationshipUid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("RelationshipUid", DataConstants.WrongFormatGUID));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("WorkflowTypeUid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("WorkflowTypeUid", DataConstants.WrongFormatGUID));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("VersionUid", DataConstants.ValidGUID));
            result = testMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("VersionUid", DataConstants.WrongFormatGUID));
            result = testMethod.Invoke(queryParams);
            Assert.False(result);

        }

        [Fact]
        public void IsValidWorkflowVersion()
        {
            bool result;
            CurrentTestingMethod testingMethod = new CurrentTestingMethod(validator.IsValidWorkflowVersion);
            var queryParams = new List<KeyValuePair<string, string>>();

            queryParams.Add(new KeyValuePair<string, string>("VersionUid", DataConstants.ValidGUID));
            result = testingMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("VersionUid", DataConstants.InvalidGUID));
            result = testingMethod.Invoke(queryParams);
            Assert.False(result);
        }

        [Fact]
        public void IsValidOrderByFieldForGetWorkflowModel()
        {
            bool result;
            CurrentTestingMethod testingMethod = new CurrentTestingMethod(validator.IsValidOrderByFieldForGetWorkflowModel);
            var queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add(new KeyValuePair<string, string>("_order", "StartedOn"));
            result = testingMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("_order", "StartedDate"));
            result = testingMethod.Invoke(queryParams);
            Assert.False(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("_order", "CompletedOn"));
            result = testingMethod.Invoke(queryParams);
            Assert.True(result);

            queryParams.Clear();
            queryParams.Add(new KeyValuePair<string, string>("_order", "version"));
            result = testingMethod.Invoke(queryParams);
            Assert.False(result);
        }
    }
}
