using d360.core;
using d360.core.enums;
using System;
using System.Collections.Generic;
using Xunit;

namespace igx.UnitTests.ScoringTests
{
    [Trait("Unit tests", "Scoring - Operators For Decimal Comparison")]
    public class OperatorTestsDecimalCompares : BaseTest
    {
        string dataType = DataType.Decimal.ToString();
        string valueToCompare = "22.75";
        List<string> lesserValues = new List<string>() { "21.34" };
        List<string> equalValues = new List<string>() { "22.75" };
        List<string> greaterValues = new List<string>() { "23.17" };
        string falseFormatStatement = "The [Decimal] result of the [{0}] (with {1}) comparison is true, but it should be false.";
        string trueFormatStatement = "The [Decimal] result of the [{0}] (with {1}) comparison is false, but it should be true.";


        [Fact]
        public void BetweenOperator_EqualGreater()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(equalValues);
            values.AddRange(greaterValues);
            var result = op.TestTwoValues(dataType, false, values, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "equal/greater values"));
        }

        [Fact]
        public void BetweenOperator_GreaterLesser()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(greaterValues);
            values.AddRange(lesserValues);
            var result = op.TestTwoValues(dataType, false, values, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "greater/lesser values"));
        }

        [Fact]
        public void BetweenOperator_LesserEqual()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(lesserValues);
            values.AddRange(equalValues);
            var result = op.TestTwoValues(dataType, false, values, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "lesser/equal values"));
        }

        [Fact]
        public void BetweenOperator_LesserGreater()
        {
            Operator op = Operator.Between;
            var values = new List<string>();
            values.AddRange(lesserValues);
            values.AddRange(greaterValues);
            var result = op.TestTwoValues(dataType, false, values, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "lesser/greater values"));
        }


        [Fact]
        public void EqualsOperator_Lesser()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(dataType, false, lesserValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "lesser value"));
        }

        [Fact]
        public void EqualsOperator_Equals()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(dataType, false, equalValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "equal value"));
        }

        [Fact]
        public void EqualsOperator_Greater()
        {
            Operator op = Operator.Equals;
            var result = op.TestTwoValues(dataType, false, greaterValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "greater value"));
        }


        [Fact]
        public void GreaterThanOperator_Lesser()
        {
            Operator op = Operator.GreaterThan;
            var result = op.TestTwoValues(dataType, false, lesserValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "lesser value"));
        }

        [Fact]
        public void GreaterThanOperator_Equals()
        {
            Operator op = Operator.GreaterThan;
            var result = op.TestTwoValues(dataType, false, equalValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "equal value"));
        }

        [Fact]
        public void GreaterThanOperator_Greater()
        {
            Operator op = Operator.GreaterThan;
            var result = op.TestTwoValues(dataType, false, greaterValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "greater value"));
        }


        [Fact]
        public void GreaterThanOrEqualsOperator_Lesser()
        {
            Operator op = Operator.GreaterThanOrEquals;
            var result = op.TestTwoValues(dataType, false, lesserValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "lesser value"));
        }

        [Fact]
        public void GreaterThanOrEqualsOperator_Equals()
        {
            Operator op = Operator.GreaterThanOrEquals;
            var result = op.TestTwoValues(dataType, false, equalValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "equal value"));
        }

        [Fact]
        public void GreaterThanOrEqualsOperator_Greater()
        {
            Operator op = Operator.GreaterThanOrEquals;
            var result = op.TestTwoValues(dataType, false, greaterValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "greater value"));
        }


        [Fact]
        public void LessThanOperator_Lesser()
        {
            Operator op = Operator.LessThan;
            var result = op.TestTwoValues(dataType, false, lesserValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "lesser value"));
        }

        [Fact]
        public void LessThanOperator_Equals()
        {
            Operator op = Operator.LessThan;
            var result = op.TestTwoValues(dataType, false, equalValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "equal value"));
        }

        [Fact]
        public void LessThanOperator_Greater()
        {
            Operator op = Operator.LessThan;
            var result = op.TestTwoValues(dataType, false, greaterValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "greater value"));
        }


        [Fact]
        public void LessThanOrEqualsOperator_Lesser()
        {
            Operator op = Operator.LessThanOrEquals;
            var result = op.TestTwoValues(dataType, false, lesserValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "lesser value"));
        }

        [Fact]
        public void LessThanOrEqualsOperator_Equals()
        {
            Operator op = Operator.LessThanOrEquals;
            var result = op.TestTwoValues(dataType, false, equalValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "equal value"));
        }

        [Fact]
        public void LessThanOrEqualsOperator_Greater()
        {
            Operator op = Operator.LessThanOrEquals;
            var result = op.TestTwoValues(dataType, false, greaterValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "greater value"));
        }


        [Fact]
        public void NotEqualsOperator_Lesser()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(dataType, false, lesserValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "lesser value"));
        }

        [Fact]
        public void NotEqualsOperator_Equal()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(dataType, false, equalValues, valueToCompare);
            Assert.False(result, string.Format(falseFormatStatement, op.GetDisplayName(), "equal value"));
        }

        [Fact]
        public void NotEqualsOperator_Greater()
        {
            Operator op = Operator.NotEquals;
            var result = op.TestTwoValues(dataType, false, greaterValues, valueToCompare);
            Assert.True(result, string.Format(trueFormatStatement, op.GetDisplayName(), "greater value"));
        }
    }
}