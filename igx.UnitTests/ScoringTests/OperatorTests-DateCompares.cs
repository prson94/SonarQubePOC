using d360.core;
using d360.core.enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace igx.UnitTests.ScoringTests
{
    [Trait("Unit tests", "Scoring - Operators For Date Comparison")]
    public class OperatorTestsDateCompares : BaseTest
    {
        private readonly string _dataType = DataType.Date.ToString();
        private readonly string _valueToCompare = DateTime.Now.Date.ToShortDateStringInvariantCulture();
        private readonly List<string> _pastValues = new List<string>() { DateTime.Now.AddDays(-1).Date.ToShortDateStringInvariantCulture() };
        private readonly List<string> _currentValues = new List<string>() { DateTime.Now.Date.ToShortDateStringInvariantCulture() };
        private readonly List<string> _futureValues = new List<string>() { DateTime.Now.AddDays(1).Date.ToShortDateStringInvariantCulture() };
        private const string FalseFormatStatement = "The [Date] result of the [{0}] (with {1}) comparison is true, but it should be false.";
        private const string TrueFormatStatement = "The [Date] result of the [{0}] (with {1}) comparison is false, but it should be true.";

        [Fact]
        public void AfterOperator_Past()
        {
            Operator op = Operator.After;
            var result = op.TestTwoValues(_dataType, false, _pastValues, _valueToCompare);
            Assert.True(result, string.Format(TrueFormatStatement, op.GetDisplayName(), "past date"));
        }

        [Fact]
        public void AfterOperator_Current()
        {
            Operator op = Operator.After;
            var result = op.TestTwoValues(_dataType, false, _currentValues, _valueToCompare);
            Assert.False(result, string.Format(FalseFormatStatement, op.GetDisplayName(), "equal date"));
        }

        [Fact]
        public void AfterOperator_Future()
        {
            Operator op = Operator.After;
            var result = op.TestTwoValues(_dataType, false, _futureValues, _valueToCompare);
            Assert.False(result, string.Format(FalseFormatStatement, op.GetDisplayName(), "future date"));
        }


        [Fact]
        public void BeforeOperator_Past()
        {
            Operator op = Operator.Before;
            var result = op.TestTwoValues(_dataType, false, _pastValues, _valueToCompare);
            Assert.False(result, string.Format(FalseFormatStatement, op.GetDisplayName(), "past date"));
        }

        [Fact]
        public void BeforeOperator_Current()
        {
            Operator op = Operator.Before;
            var result = op.TestTwoValues(_dataType, false, _currentValues, _valueToCompare);
            Assert.False(result, string.Format(FalseFormatStatement, op.GetDisplayName(), "equal date"));
        }

        [Fact]
        public void BeforeOperator_Future()
        {
            Operator op = Operator.Before;
            var result = op.TestTwoValues(_dataType, false, _futureValues, _valueToCompare);
            Assert.True(result, string.Format(TrueFormatStatement, op.GetDisplayName(), "future date"));
        }


        [Fact]
        public void BetweenOperator_CurrentFuture()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(_currentValues);
            values.AddRange(_futureValues);
            var result = op.TestTwoValues(_dataType, false, values, _valueToCompare);
            Assert.True(result, string.Format(TrueFormatStatement, op.GetDisplayName(), "current/future dates"));
        }

        [Fact]
        public void BetweenOperator_FuturePast()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(_futureValues);
            values.AddRange(_pastValues);
            var result = op.TestTwoValues(_dataType, false, values, _valueToCompare);
            Assert.False(result, string.Format(FalseFormatStatement, op.GetDisplayName(), "future/past dates"));
        }

        [Fact]
        public void BetweenOperator_PastCurrent()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(_pastValues);
            values.AddRange(_currentValues);
            var result = op.TestTwoValues(_dataType, false, values, _valueToCompare);
            Assert.True(result, string.Format(TrueFormatStatement, op.GetDisplayName(), "past/current dates"));
        }

        [Fact]
        public void BetweenOperator_PastFuture()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(_pastValues);
            values.AddRange(_futureValues);
            var result = op.TestTwoValues(_dataType, false, values, _valueToCompare);
            Assert.True(result, string.Format(TrueFormatStatement, op.GetDisplayName(), "past/future dates"));
        }


        [Fact]
        public void EqualsOperator_Past()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(_dataType, false, _pastValues, _valueToCompare);
            Assert.False(result, string.Format(FalseFormatStatement, op.GetDisplayName(), "past date"));
        }

        [Fact]
        public void EqualsOperator_Current()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(_dataType, false, _currentValues, _valueToCompare);
            Assert.True(result, string.Format(TrueFormatStatement, op.GetDisplayName(), "equal date"));
        }

        [Fact]
        public void EqualsOperator_Future()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(_dataType, false, _futureValues, _valueToCompare);
            Assert.False(result, string.Format(FalseFormatStatement, op.GetDisplayName(), "future date"));
        }


        [Fact]
        public void NotEqualsOperator_Past()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(_dataType, false, _pastValues, _valueToCompare);
            Assert.True(result, string.Format(TrueFormatStatement, op.GetDisplayName(), "past date"));
        }

        [Fact]
        public void NotEqualsOperator_Current()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(_dataType, false, _currentValues, _valueToCompare);
            Assert.False(result, string.Format(FalseFormatStatement, op.GetDisplayName(), "equal date"));
        }

        [Fact]
        public void NotEqualsOperator_Future()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(_dataType, false, _futureValues, _valueToCompare);
            Assert.True(result, string.Format(TrueFormatStatement, op.GetDisplayName(), "future date"));
        }
    }
}