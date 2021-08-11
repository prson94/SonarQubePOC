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
        [InlineData("05/12/2021","" )]
        public void DateFieldValidator(object value, bool expectedValue)
        {
            var validator = new DateFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(test.Status);
            Assert.True(test.UpdatedValue.ToString().ToLower() == expectedValue.ToString().ToLower());
        }

        [Theory]
        [InlineData("text")]
        [InlineData("15.57")]
        [InlineData("'15'")]
        [InlineData("trueee")]
        public void DateFieldValidatorInvalids(object value)
        {
            var validator = new DateFieldValidator();
            var test = validator.CheckValue(value, "", "");
            Assert.True(!test.Status);
            Assert.True(test.UpdatedValue == null);
        }
    }

}


