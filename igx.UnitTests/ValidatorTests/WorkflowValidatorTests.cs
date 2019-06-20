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
    }
}
