using d360.core;
using d360.core.enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace igx.UnitTests.ScoringTests
{
    [Trait("Unit tests", "Scoring - Operators For Text Comparison")]
    public class OperatorTestsTextCompares : BaseTest
    {
        string dataType = DataType.Text.ToString();

        string falseFormatStatement = "The [Text] result of the [{0}] comparison is true, but it should be false.";
        string trueFormatStatement = "The [Text] result of the [{0}] comparison is false, but it should be true.";


        [Fact]
        public void ContainsOperator_PassCase()
        {
            Operator op = Operator.Contains;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "Gov" }, "hello there Govern");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void ContainsOperator_FailCase()
        {
            Operator op = Operator.Contains;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "jojo" }, "hello there Govern");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }


        [Fact]
        public void EndsWithOperator_PassCase()
        {
            Operator op = Operator.EndsWith;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "ern" }, "hello there Govern");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void EndsWithOperator_FailCase()
        {
            Operator op = Operator.EndsWith;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "jojo" }, "hello there Govern");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }


        [Fact]
        public void EqualsOperator_PassCase()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "hello there Govern" }, "hello there Govern");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void EqualsOperator_FailCase()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "jojo" }, "hello there Govern");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }


        [Fact]
        public void InOperator_PassCase()
        {
            Operator op = Operator.In;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "blue", "green" }, "blue,yellow,red");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void InOperator_FailCase()
        {
            Operator op = Operator.In;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "orange", "green" }, "blue,yellow,red");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }


        [Fact]
        public void NotContainsOperator_PassCase()
        {
            Operator op = Operator.NotContains;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "zuzu" }, "hello there Govern");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void NotContainsOperator_FailCase()
        {
            Operator op = Operator.NotContains;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "Govern" }, "hello there Govern");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }


        [Fact]
        public void NotEqualsOperator_PassCase()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "jojo" }, "hello there Govern");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void NotEqualsOperator_FailCase()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "hello there Govern" }, "hello there Govern");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }


        [Fact]
        public void NotInOperator_PassCase()
        {
            Operator op = Operator.NotIn;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "orange", "green" }, "blue,yellow,red");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void NotInOperator_FailCase()
        {
            Operator op = Operator.NotIn;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "blue", "green" }, "blue,yellow,red");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }


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


        [Fact]
        public void StartsWithOperator_PassCase()
        {
            Operator op = Operator.StartsWith;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "hello" }, "hello there Govern");
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName()));
        }

        [Fact]
        public void StartsWithOperator_FailCase()
        {
            Operator op = Operator.StartsWith;
            var result = op.TestTwoValues(dataType, false, new List<string>() { "ello" }, "hello there Govern");
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName()));
        }
    }
}