using d360.core;
using d360.core.enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace igx.UnitTests.ScoringTests
{
    [Trait("Unit tests", "Scoring - Operators For DateTime Comparison")]
    public class OperatorTestsDateTimeCompares : BaseTest
    {
        string dataType = DataType.DateTime.ToString();
        private readonly string valueToCompare = DateTime.Now.Date.ToShortDateStringInvariantCulture();
        private readonly List<string> pastValues = new List<string>() { DateTime.Now.AddDays(-1).Date.ToShortDateStringInvariantCulture() };
        private readonly List<string> currentValues = new List<string>() { DateTime.Now.Date.ToShortDateStringInvariantCulture() };
        private readonly List<string> futureValues = new List<string>() { DateTime.Now.AddDays(1).Date.ToShortDateStringInvariantCulture() };
        string falseFormatStatement = "The [DateTime] result of the [{0}] (with {1}) comparison is true, but it should be false.";
        string trueFormatStatement = "The [DateTime] result of the [{0}] (with {1}) comparison is false, but it should be true.";

        [Fact]
        public void AfterOperator_Past()
        {
            Operator op = Operator.After;
            var result = op.TestTwoValues(dataType, false, pastValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "past date"));
        }

        [Fact]
        public void AfterOperator_Current()
        {
            Operator op = Operator.After;
            var result = op.TestTwoValues(dataType, false, currentValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "equal date"));
        }

        [Fact]
        public void AfterOperator_Future()
        {
            Operator op = Operator.After;
            var result = op.TestTwoValues(dataType, false, futureValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "future date"));
        }


        [Fact]
        public void BeforeOperator_Past()
        {
            Operator op = Operator.Before;
            var result = op.TestTwoValues(dataType, false, pastValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "past date"));
        }

        [Fact]
        public void BeforeOperator_Current()
        {
            Operator op = Operator.Before;
            var result = op.TestTwoValues(dataType, false, currentValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "equal date"));
        }

        [Fact]
        public void BeforeOperator_Future()
        {
            Operator op = Operator.Before;
            var result = op.TestTwoValues(dataType, false, futureValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "future date"));
        }


        [Fact]
        public void BetweenOperator_CurrentFuture()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(currentValues);
            values.AddRange(futureValues);
            var result = op.TestTwoValues(dataType, false, values, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "current/future dates"));
        }

        [Fact]
        public void BetweenOperator_FuturePast()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(futureValues);
            values.AddRange(pastValues);
            var result = op.TestTwoValues(dataType, false, values, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "future/past dates"));
        }

        [Fact]
        public void BetweenOperator_PastCurrent()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(pastValues);
            values.AddRange(currentValues);
            var result = op.TestTwoValues(dataType, false, values, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "past/current dates"));
        }

        [Fact]
        public void BetweenOperator_PastFuture()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(pastValues);
            values.AddRange(futureValues);
            var result = op.TestTwoValues(dataType, false, values, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "past/future dates"));
        }


        [Fact]
        public void EqualsOperator_Past()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(dataType, false, pastValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "past date"));
        }

        [Fact]
        public void EqualsOperator_Current()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(dataType, false, currentValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "equal date"));
        }

        [Fact]
        public void EqualsOperator_Future()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(dataType, false, futureValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "future date"));
        }


        [Fact]
        public void NotEqualsOperator_Past()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(dataType, false, pastValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "past date"));
        }

        [Fact]
        public void NotEqualsOperator_Current()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(dataType, false, currentValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "equal date"));
        }

        [Fact]
        public void NotEqualsOperator_Future()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(dataType, false, futureValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "future date"));
        }
    }
}