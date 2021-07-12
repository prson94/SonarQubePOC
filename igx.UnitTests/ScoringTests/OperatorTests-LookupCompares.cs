using d360.core;
using d360.core.enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace igx.UnitTests.ScoringTests
{
    [Trait("Unit tests", "Scoring - Operators For Lookup Comparison")]
    public class OperatorTestsLookupCompares : BaseTest
    {
        string dataType = DataType.Lookup.ToString();

        string falseFormatStatement = "The [Lookup] result of the [{0}] comparison is true, but it should be false.";
        string trueFormatStatement = "The [Lookup] result of the [{0}] comparison is false, but it should be true.";


        [Fact]
        public void NotPopulatedOperator_EmptyValuePassCase()
        {
            Operator op = Operator.NotPopulated;
            var result = op.TestTwoValues(dataType, false, null, "");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void NotPopulatedOperator_NullValuePassCase()
        {
            Operator op = Operator.NotPopulated;
            var result = op.TestTwoValues(dataType, false, null, null);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void NotPopulatedOperator_FailCase()
        {
            Operator op = Operator.NotPopulated;
            var result = op.TestTwoValues(dataType, false, null, "a");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }


        [Fact]
        public void PopulatedOperator_PassCase()
        {
            Operator op = Operator.Populated;
            var result = op.TestTwoValues(dataType, false, null, "a");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void PopulatedOperator_NullValueFailCase()
        {
            Operator op = Operator.Populated;
            var result = op.TestTwoValues(dataType, false, null, null);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void PopulatedOperator_EmptyValueFailCase()
        {
            Operator op = Operator.Populated;
            var result = op.TestTwoValues(dataType, false, null, "");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }
    }
}