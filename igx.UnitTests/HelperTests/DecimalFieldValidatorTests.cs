using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using d360.model.helpers.filters.program;
using FluentAssertions;
using Xunit;

namespace igx.UnitTests.HelperTests
{
    public class DecimalFieldValidatorTests
    {
        [Theory, AutoData]
        public async Task InputDecimalValueIsValidTest(decimal value)
        {
            await Task.CompletedTask;

            // assign
            var validator = new DecimalFieldValidator();

            // act
            var actual = validator.CheckValue(value.ToString(CultureInfo.InvariantCulture), "testName", "=");

            //assert
            actual.Status.Should().Be(true);
            actual.UpdatedValue.Should().Be(value);
        }

        [Theory]
        [InlineData("500", 500)]
        [InlineData("50.0", 50)]
        [InlineData("1,050.25", 1050.25)]
        public async Task InputStringValueIsValidTest(string input, decimal expectedResult)
        {
            await Task.CompletedTask;

            // assign
            var validator = new DecimalFieldValidator();

            // act
            var actual = validator.CheckValue(input, "testName", "=");

            //assert
            actual.Status.Should().Be(true);
            actual.UpdatedValue.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("1+0.23")]
        public async Task InputValueIsInvalidTest(string input)
        {
            await Task.CompletedTask;

            // assign
            var validator = new DecimalFieldValidator();

            // act
            var actual = validator.CheckValue(input, "testProperty", "=");

            //assert
            actual.Status.Should().Be(false);
        }

        [Theory]
        [InlineData("testProperty")]
        [InlineData("test'Property")]
        [InlineData("test\"Property")]
        public async Task CheckProperValidationMessageTest(string propertyName)
        {
            await Task.CompletedTask;

            // assign
            var validator = new DecimalFieldValidator();

            // act
            var actual = validator.CheckValue("invalid", propertyName, "=");

            //assert
            actual.Status.Should().Be(false);
            actual.Message.Should().Be($"Invalid decimal value for field '{propertyName}'");
        }
    }
}
