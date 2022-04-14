using d360.core.entities;
using d360.core.exceptions;
using d360.model.validators;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace igx.UnitTests.ValidatorTests
{
    [Trait("Unit tests", "Semantic Type - Validation")]
    public class SemanticValidationTests
    {
        static PostSemantic createCommonPostSemantic() {
            return new PostSemantic
            {
                Name = "Test",
                Qualifier = "PostTest",
                Priority = 1
            };
        }

        [Fact(DisplayName = "Base Type Not A Number So Maximum Not Allowed")]
        public static void BaseType_NotNum_Maximum_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.LocalTime;
            model.Maximum = 1;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Maximum must not contain a value."), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Base Type Is Long Decimal-based Maximum Is Invalid")]
        public static void BaseType_Long_Maximum_Invalid()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.Long;
            model.Maximum = 1.5M;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Maximum must be a whole number value"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Base Type Not A Number So Minimum Not Allowed")]
        public static void BaseType_NotNum_Minimum_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.LocalTime;
            model.Minimum = 1;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Minimum must not contain a value."), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Base Type Is Long Decimal-based Minimum Is Invalid")]
        public static void BaseType_Long_Minimum_Invalid()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.Long;
            model.Minimum = 1.5M;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Minimum must be a whole number value"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Header Filter Confidence is required")]
        public static void HeaderFilterConfidence_Required()
        {
            var model = createCommonPostSemantic();
            model.HeaderFilter = "/test/";            

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest && 
                ex.StatusDescription.Contains("HeaderFilterConfidence must be populated"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Header Filter Confidence is invalid")]
        public static void HeaderFilterConfidence_Invalid()
        {
            var model = createCommonPostSemantic();
            model.HeaderFilter = "/test/";            
            model.HeaderFilterConfidence = 0;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("HeaderFilterConfidence must be a whole number"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Advanced, JsonPayload is required")]
        public static void MatchType_Is_Advanced_Json_Is_Required()
        {
            var model = createCommonPostSemantic();
            model.MatchType = d360.core.enums.SemanticMatchType.Advanced;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Advanced, JsonPayload must not be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Advanced, Header Filter not allowed")]
        public static void MatchType_Is_Advanced_HeaderFilter_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.MatchType = d360.core.enums.SemanticMatchType.Advanced;
            model.JsonPayloadStructured = JObject.Parse("{ test: true }");
            model.HeaderFilter = "/test/";

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Advanced, HeaderFilter must be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Advanced, Invalid Values not allowed")]
        public static void MatchType_Is_Advanced_InvalidValues_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.MatchType = d360.core.enums.SemanticMatchType.Advanced;
            model.JsonPayloadStructured = JObject.Parse("{ test: true }");
            model.InvalidValuesStructured = new List<string> { "test" };

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Advanced, InvalidValues must be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Advanced, MinSamples not allowed")]
        public static void MatchType_Is_Advanced_MinSamples_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.MatchType = d360.core.enums.SemanticMatchType.Advanced;
            model.JsonPayloadStructured = JObject.Parse("{ test: true }");
            model.MinimumSamples = 1;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Advanced, MinimumSamples must be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Advanced, Regex not allowed")]
        public static void MatchType_Is_Advanced_Regex_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.MatchType = d360.core.enums.SemanticMatchType.Advanced;
            model.JsonPayloadStructured = JObject.Parse("{ test: true }");
            model.RegularExpression = "test";

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Advanced, RegularExpression must be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Advanced, Valid Values not allowed")]
        public static void MatchType_Is_Advanced_ValidValues_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.MatchType = d360.core.enums.SemanticMatchType.Advanced;
            model.JsonPayloadStructured = JObject.Parse("{ test: true }");
            model.ValidValuesStructured = new List<string> { "test" };

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Advanced, ValidValues must be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = List, Json not allowed")]
        public static void MatchType_Is_List_Json_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.MatchType = d360.core.enums.SemanticMatchType.List;
            model.JsonPayloadStructured = JObject.Parse("{ test: true }");

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("List, JsonPayload must be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = List, Regex not allowed")]
        public static void MatchType_Is_List_Regex_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.MatchType = d360.core.enums.SemanticMatchType.List;
            model.RegularExpression = "test";

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("List, RegularExpression must be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Number, HeaderFilter is required")]
        public static void MatchType_Is_Number_HeaderFilter_Required()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.Long;
            model.MatchType = d360.core.enums.SemanticMatchType.Number;
            model.Threshold = 99;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Number, HeaderFilter must not be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Number, Json not allowed")]
        public static void MatchType_Is_Number_Json_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.Long;
            model.MatchType = d360.core.enums.SemanticMatchType.Number;
            model.Threshold = 99;
            model.JsonPayloadStructured = JObject.Parse("{ test: true }");

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Number, JsonPayload must be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Pattern, Json not allowed")]
        public static void MatchType_Is_Pattern_Json_NotAllowed()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.String;
            model.MatchType = d360.core.enums.SemanticMatchType.Pattern;
            model.RegularExpression = "test";
            model.JsonPayloadStructured = JObject.Parse("{ test: true }");

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Pattern, JsonPayload must be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Match Type = Pattern, Regex is required")]
        public static void MatchType_Is_Pattern_Regex_Required()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.String;
            model.MatchType = d360.core.enums.SemanticMatchType.Pattern;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Pattern, RegularExpression must not be empty"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Minimum must be less than Maximum")]
        public static void Minimum_Not_Less_Than_Maximum()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.Long;
            model.Minimum = 2.5M;
            model.Maximum = 1.5M;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Minimum must not be greater than Maximum"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "MinMaxPresent is not allowed")]
        public static void MinMaxPresent_Not_Allowed()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.Long;
            model.MinMaxPresent = true;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("must contain values for MinMaxPresent to be used"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Priority is invalid")]
        public static void Priority_Invalid()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.Long;
            model.Priority = -1;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Priority must contain a value of"), XMsg.BadResponseMessage);
        }

        [Fact(DisplayName = "Threshold is invalid")]
        public static void Threshold_Invalid()
        {
            var model = createCommonPostSemantic();
            model.BaseType = d360.core.enums.SemanticBaseType.Long;
            model.Threshold = -1;

            var repo = model.ToRepositoryModel(0);

            Action act = () => repo.Validate();
            var ex = Assert.Throws<GenericException>(act);

            Assert.True(
                ex.StatusCode == System.Net.HttpStatusCode.BadRequest &&
                ex.StatusDescription.Contains("Threshold must contain a whole number value"), XMsg.BadResponseMessage);
        }
    }

}


