using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using d360.model.validators;
using d360.core.entities;
using igx.UnitTests.Core;

namespace igx.UnitTests.ValidatorTests
{
    [Trait("Unit tests", "Survey Api Model Validator")]
    public class SurveyApiModelValidatorTests : BaseTest
    {
        SurveyApiModelValidator surveyApiModelValidator;
        public SurveyApiModelValidatorTests()
        {
            this.surveyApiModelValidator = new SurveyApiModelValidator(GetAssetRepository(), GetResourceRepository(), GetSurveyRepository());
        }

        [Fact]
        public void IsValidResource()
        {
            List<KeyValuePair<string, string>> keyValuePairs = new List<KeyValuePair<string, string>>();

            var result = surveyApiModelValidator.IsValidResource(keyValuePairs);

            //if no resource set request is valid
            Assert.True(result);

            //invalid uid should return false
            keyValuePairs.Add(new KeyValuePair<string, string>("resourceuid", DataConstants.InvalidGUID));
            result = surveyApiModelValidator.IsValidResource(keyValuePairs);
            Assert.False(result);

            //valid uid should return true
            keyValuePairs.Clear();
            keyValuePairs.Add(new KeyValuePair<string, string>("resourceuid", DataConstants.ValidGUID));
            result = surveyApiModelValidator.IsValidResource(keyValuePairs);
            Assert.True(result);
        }

        [Fact]
        public void IsValidAsset()
        {
            List<KeyValuePair<string, string>> keyValuePairs = new List<KeyValuePair<string, string>>();

            var result = surveyApiModelValidator.IsValidAsset(keyValuePairs);

            //if no resource set request is valid
            Assert.True(result);

            //invalid uid should return false
            keyValuePairs.Add(new KeyValuePair<string, string>("assetuid", DataConstants.InvalidGUID));
            result = surveyApiModelValidator.IsValidAsset(keyValuePairs);
            Assert.False(result);

            //valid uid should return true
            keyValuePairs.Clear();
            keyValuePairs.Add(new KeyValuePair<string, string>("assetuid", DataConstants.ValidGUID));
            result = surveyApiModelValidator.IsValidAsset(keyValuePairs);
            Assert.True(result);
        }

        [Fact]
        public void IsValidDate()
        {
            List<KeyValuePair<string, string>> keyValuePairs = new List<KeyValuePair<string, string>>();

            var result = surveyApiModelValidator.IsValidDate(keyValuePairs, "date");

            //if no date set request is valid
            Assert.True(result);

            //invalid date should return false
            keyValuePairs.Add(new KeyValuePair<string, string>("date", "invalid date"));
            result = surveyApiModelValidator.IsValidDate(keyValuePairs, "date");
            Assert.False(result);

            //valid date should return true
            keyValuePairs.Clear();
            keyValuePairs.Add(new KeyValuePair<string, string>("date", "10/10/2020"));
            result = surveyApiModelValidator.IsValidDate(keyValuePairs, "date");
            Assert.True(result);
        }

        [Fact]
        public void IsValidSurveyType()
        {
            List<KeyValuePair<string, string>> keyValuePairs = new List<KeyValuePair<string, string>>();

            var result = surveyApiModelValidator.IsValidSurveyType(keyValuePairs);

            //if no survey uid set request is valid
            Assert.True(result);

            //invalid uid should return false
            keyValuePairs.Add(new KeyValuePair<string, string>("surveytypeuid", DataConstants.InvalidGUID));
            result = surveyApiModelValidator.IsValidSurveyType(keyValuePairs);
            Assert.False(result);

            //valid uid should return true
            keyValuePairs.Clear();
            keyValuePairs.Add(new KeyValuePair<string, string>("surveytypeuid", DataConstants.ValidGUID));
            result = surveyApiModelValidator.IsValidSurveyType(keyValuePairs);
            Assert.True(result);
        }

        [Fact]
        public void IsRequiredGuidExistForDeleteSurveyResult()
        {
            List<KeyValuePair<string, string>> keyValuePairs = new List<KeyValuePair<string, string>>();

            var result = surveyApiModelValidator.IsRequiredGuidExistForDeleteSurveyResult(keyValuePairs);

            //if no params set request is valid
            Assert.False(result);

            //if any param set
            keyValuePairs.Add(new KeyValuePair<string, string>("surveytypeuid", DataConstants.ValidGUID));
            result = surveyApiModelValidator.IsRequiredGuidExistForDeleteSurveyResult(keyValuePairs);
            Assert.True(result);

            keyValuePairs.Clear();
            keyValuePairs.Add(new KeyValuePair<string, string>("resourceuid", DataConstants.ValidGUID));
            result = surveyApiModelValidator.IsRequiredGuidExistForDeleteSurveyResult(keyValuePairs);
            Assert.True(result);

            keyValuePairs.Add(new KeyValuePair<string, string>("assetuid", DataConstants.ValidGUID));
            result = surveyApiModelValidator.IsRequiredGuidExistForDeleteSurveyResult(keyValuePairs);
            Assert.True(result);

            //if any random param should return false
            keyValuePairs.Clear();
            keyValuePairs.Add(new KeyValuePair<string, string>("random_param", DataConstants.ValidGUID));
            result = surveyApiModelValidator.IsRequiredGuidExistForDeleteSurveyResult(keyValuePairs);
            Assert.False(result);
        }

        [Theory]
        [InlineData("assettypeuid", "random_text", false)]
        [InlineData("surveytypeuid", "random_text", false)]
        [InlineData("_pagesize", "random_text", false)]
        [InlineData("_pagenum", "random_text", false)]
        [InlineData("_order", "random_text", false)]
        [InlineData("assettypeuid", "f8bf1431-0d7b-4381-9cec-dd32c05e0158", true)]
        [InlineData("surveytypeuid", "f8bf1431-0d7b-4381-9cec-dd32c05e0158", true)]
        [InlineData("_pagesize", "10", true)]
        [InlineData("_pagenum", "10", true)]
        [InlineData("_order", "name", true)]
        [InlineData("_order", "validfordays", true)]
        [InlineData("_order", "createdon", true)]
        [InlineData("_order", "updatedon", true)]
        [InlineData("_order", "numberofresponses", true)]
        public void ValidateGetSurveyTypesRequest(string paramName, string paramValue, bool isFailStatus)
        {
            List<KeyValuePair<string, string>> keyValuePairs = new List<KeyValuePair<string, string>>();
            keyValuePairs.Add(new KeyValuePair<string, string>(paramName, paramValue));
            var result = surveyApiModelValidator.ValidateGetSurveyTypesRequest(keyValuePairs);
            if (!isFailStatus)
            {
                Assert.True(result.StatusCode == System.Net.HttpStatusCode.BadRequest);
            }
            else
            {
                Assert.True(result == null);
            }
        }
    }

}


