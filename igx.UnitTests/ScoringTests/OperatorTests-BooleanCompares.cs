using d360.core;
using d360.core.enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace igx.UnitTests.ScoringTests
{
    [Trait("Unit tests", "Scoring - Operators For Boolean Comparison")]
    public class OperatorTestsBooleanCompares : BaseTest
    {
        string dataType = DataType.Boolean.ToString();
        string falseFormatStatement = "The [Boolean] result of the [{0}] (with {1}) comparison is true, but it should be false.";
        string trueFormatStatement = "The [Boolean] result of the [{0}] (with {1}) comparison is false, but it should be true.";

        [Fact]
        public void IsFalseOperator_FalseValueCase()
        {
            Operator op = Operator.IsFalse;
            var result = op.TestTwoValues(dataType, false, null, "false");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "false value"));
        }

        [Fact]
        public void IsFalseOperator_TrueValueCase()
        {
            Operator op = Operator.IsFalse;
            var result = op.TestTwoValues(dataType, false, null, "true");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "true value"));
        }


        [Fact]
        public void IsTrueOperator_FalseValueCase()
        {
            Operator op = Operator.IsTrue;
            var result = op.TestTwoValues(dataType, false, null, "false");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "false value"));
        }

        [Fact]
        public void IsTrueOperator_TrueValueCase()
        {
            Operator op = Operator.IsTrue;
            var result = op.TestTwoValues(dataType, false, null, "true");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "true value"));
        }


        [Fact]
        public void NotPopulatedOperator_PassCase()
        {
            Operator op = Operator.NotPopulated;
            var result = op.TestTwoValues(dataType, false, null, null);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "null value"));
        }

        [Fact]
        public void NotPopulatedOperator_TrueValueFailCase()
        {
            Operator op = Operator.NotPopulated;
            var result = op.TestTwoValues(dataType, false, null, "true");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "true value"));
        }

        [Fact]
        public void NotPopulatedOperator_FalseValueFailCase()
        {
            Operator op = Operator.NotPopulated;
            var result = op.TestTwoValues(dataType, false, null, "false");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "false value"));
        }

        [Fact]
        public void PopulatedOperator_TrueValuePassCase()
        {
            Operator op = Operator.Populated;
            var result = op.TestTwoValues(dataType, false, null, "true");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "true value"));
        }

        [Fact]
        public void PopulatedOperator_FalseValuePassCase()
        {
            Operator op = Operator.Populated;
            var result = op.TestTwoValues(dataType, false, null, "false");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "false value"));
        }

        [Fact]
        public void PopulatedOperator_FailCase()
        {
            Operator op = Operator.Populated;
            var result = op.TestTwoValues(dataType, false, null, null);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "null value"));
        }

    }
}