using System;
using Xunit;
using d360.model.helpers.filters.program;

namespace igx.UnitTests.FilterExpressionTests
{
    [Trait("Unit tests", "Filter validators test")]
    public class FieldValidatorsTests : BaseTest
    {

        [Theory]
        [InlineData(1, 1)]
        [InlineData(-800, -800)]
        [InlineData("500", 500)]
        public void NumberFieldValidatorValids(object value, int expectedValue)
        {
            var validator = new NumberFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(test.Status);
            Assert.True(test.UpdatedValue.ToString() == expectedValue.ToString());
        }


        [Theory]
        [InlineData(-800.2)]
        [InlineData("text")]
        [InlineData("15.57")]
        public void NumberFieldValidatorValidsInvalids(object value)
        {
            var validator = new NumberFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(!test.Status);
            Assert.True(test.UpdatedValue == null);
        }

        [Theory]
        [InlineData("text")]
        [InlineData("'15'")]
        public void DecimalFieldValidatorInvalids(object value)
        {
            var validator = new DecimalFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(!test.Status);
            Assert.True(test.UpdatedValue == null);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData(true, true)]
        public void BooleanFieldValidator(object value, bool expectedValue)
        {
            var validator = new BooleanFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(test.Status);
            Assert.True(test.UpdatedValue.ToString().ToLower() == expectedValue.ToString().ToLower());
        }

        [Theory]
        [InlineData("text")]
        [InlineData("15.57")]
        [InlineData("'15'")]
        [InlineData("trueee")]
        public void BooleanFieldValidatorInvalids(object value)
        {
            var validator = new BooleanFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(!test.Status);
            Assert.True(test.UpdatedValue == null);
        }

        [Theory]
        [InlineData("2021", "ct", "2021")]
        public void DateFieldValidator(object value, string @op, string expectedValue)
        {
            var validator = new DateFieldValidator();
            var test = validator.CheckValue(value, "", @op);
            Assert.True(test.Status);
        }

        [Fact]
        public void DateFieldV2Validator()
        {
            var dateAsString = new DateTime(2020, 3, 24).ToShortDateString();
            var validator = new DateFieldValidator();
            var test = validator.CheckValue(dateAsString, "", "");
            Assert.True(test.Status);
        }

        [Theory]
        [InlineData("text", "", "")]
        [InlineData("15.57", "", "")]
        [InlineData("'15'", "", "")]
        [InlineData("13-32-2002", "", "")]
        [InlineData("2021", "", "")]
        public void DateFieldValidatorInvalid(object value, string @op, string expectedValue)
        {
            var validator = new DateFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(!test.Status);
            Assert.True(test.UpdatedValue == null);
        }

        [Theory]
        [InlineData("2021", "ct", "2021")]
        public void DateSystemFieldValidator(object value, string @op, string expectedValue)
        {
            var validator = new SystemDateFieldValidator();
            var test = validator.CheckValue(value, "", @op);
            Assert.True(test.Status);
            Assert.True(test.UpdatedValue.ToString() == expectedValue);
        }

        [Fact]
        public void DateSystemFieldV2Validator()
        {
            var dateAsString = new DateTime(2020, 3, 24).ToShortDateString();
            var validator = new SystemDateFieldValidator();
            var test = validator.CheckValue(dateAsString, "", "");
            Assert.True(test.Status);
        }

        [Theory]
        [InlineData("text", "", "")]
        [InlineData("15.57", "", "")]
        [InlineData("'15'", "", "")]
        [InlineData("13-32-2002", "", "")]
        [InlineData("2021", "", "")]
        public void DateSystemFieldValidatorInvalid(object value, string @op, string expectedValue)
        {
            var validator = new SystemDateFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(!test.Status);
            Assert.True(test.UpdatedValue == null);
        }

        [Theory]
        [InlineData("test", "test")]
        [InlineData("'test'", "test")]
        public void TextFieldValidator(object value, string expectedValue)
        {
            var validator = new TextFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(test.Status);
            Assert.True(test.UpdatedValue.ToString().ToLower() == expectedValue.ToString().ToLower());
        }
    }

}


