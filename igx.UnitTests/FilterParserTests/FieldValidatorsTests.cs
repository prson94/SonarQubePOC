using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using d360.model.validators;
using d360.core.entities;
using d360.model.helpers;
using d360.model.helpers.filters;
using d360.model.helpers.filters.program;

namespace igx.UnitTests.FilterExpressionTests
{
    [Trait("Unit tests", "Filter validators test")]
    public class FieldValidatorsTests : BaseTest
    {

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1.124, 1124)]
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
        [InlineData("'15'")]
        public void NumberFieldValidatorValidsInvalids(object value)
        {
            var validator = new NumberFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(!test.Status);
            Assert.True(test.UpdatedValue == null);
        }


        [Theory]
        [InlineData(1, 1)]
        [InlineData(1.124, 1.124)]
        [InlineData(-800, -800)]
        [InlineData("500", 500)]
        [InlineData(-15.5, -15.5)]
        public void DecimalFieldValidator(object value, double expectedValue)
        {
            var validator = new DecimalFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(test.Status);
            Assert.True(test.UpdatedValue.ToString() == expectedValue.ToString());
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
        [InlineData("24/03/2021", "", "24/03/2021 00:00:00")]
        [InlineData("24-03-2021", "", "24/03/2021 00:00:00")]
        [InlineData("2021", "ct", "2021", false)]
        public void DateFieldValidator(object value, string @op, string expectedValue, bool isDateComparison = true)
        {
            var validator = new DateFieldValidator();
            var test = validator.CheckValue(value, "", @op);
            Assert.True(test.Status);
            if (isDateComparison)
            {
                var comparisonResult = DateTime.Compare(DateTime.Parse(test.UpdatedValue.ToString()), DateTime.Parse(expectedValue));
                Assert.True(comparisonResult == 0);
            }
            else
            {
                Assert.True(test.UpdatedValue.ToString() == expectedValue);
            }

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
        [InlineData("24/03/2021", "", "24/03/2021 00:00:00")]
        [InlineData("24-03-2021", "", "24/03/2021 00:00:00")]
        [InlineData("2021", "ct", "2021", false)]
        [InlineData("24/03/2021", "le", "24/03/2021 23:59:59")]
        public void DateSystemFieldValidator(object value, string @op, string expectedValue, bool isDateComparison = true)
        {
            var validator = new SystemDateFieldValidator();
            var test = validator.CheckValue(value, "", @op);
            Assert.True(test.Status);
            if (isDateComparison)
            {
                var comparisonResult = DateTime.Compare(DateTime.Parse(test.UpdatedValue.ToString()), DateTime.Parse(expectedValue));
                Assert.True(comparisonResult == 0);
            }
            else
            {
                Assert.True(test.UpdatedValue.ToString() == expectedValue);
            }
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


