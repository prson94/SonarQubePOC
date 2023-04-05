using Xunit;
using d360.model.helpers.filters;

namespace igx.UnitTests.RegexFilterExpressionParserTests.cs
{
	[Trait("Unit tests", "Filter expression parser with regex (for simple data catalog api)")]
	public class RegexFilterExpressionParserTests : BaseTest
	{

		[Theory]
		[InlineData("FieldType ct fieldValue", "FieldType", "ct", "fieldValue")]
		[InlineData("FieldType eq fieldValue", "FieldType", "eq", "fieldValue")]
		[InlineData("FieldType in fieldValue", "FieldType", "in", "fieldValue")]
		[InlineData("FieldType nct fieldValue", "FieldType", "nct", "fieldValue")]
		[InlineData("FieldType neq fieldValue", "FieldType", "neq", "fieldValue")]
		[InlineData("FieldType nin fieldValue", "FieldType", "nin", "fieldValue")]
		[InlineData("FieldType ne fieldValue", "FieldType", "ne", "fieldValue")]
		[InlineData("Field Type ne field Value", "Field Type", "ne", "field Value")]
		[InlineData("Field Type ne field Value > test 123", "Field Type", "ne", "field Value > test 123")]
		[InlineData("Field Type ct *value*", "Field Type", "ct", "*value*")]
		[InlineData("Field Type ct 1.2test", "Field Type", "ct", "1.2test")]
		[InlineData("Field Type ct 1!2test", "Field Type", "ct", "1!2test")]
		[InlineData("Field Type ct 1£2test", "Field Type", "ct", "1£2test")]
		[InlineData("Field Type ct 1$2test", "Field Type", "ct", "1$2test")]
		[InlineData("Field Type ct 1%2test", "Field Type", "ct", "1%2test")]
		[InlineData("Field Type ct 1^2test", "Field Type", "ct", "1^2test")]
		[InlineData("Field Type ct 1&2test", "Field Type", "ct", "1&2test")]
		[InlineData("Field Type ct 1*2test", "Field Type", "ct", "1*2test")]
		[InlineData("Field Type ct 1_2test", "Field Type", "ct", "1_2test")]
		[InlineData("Field Type ct 1-2test", "Field Type", "ct", "1-2test")]
		[InlineData("Field Type ct 1+2test", "Field Type", "ct", "1+2test")]
		[InlineData("Field Type ct 1=2test", "Field Type", "ct", "1=2test")]
		[InlineData("Field Type ct 1{2test", "Field Type", "ct", "1{2test")]
		[InlineData("Field Type ct 1}2test", "Field Type", "ct", "1}2test")]
		[InlineData("Field Type ct 1;2test", "Field Type", "ct", "1;2test")]
		[InlineData("Field Type ct 1:2test", "Field Type", "ct", "1:2test")]
		[InlineData("Field Type ct 1@2test", "Field Type", "ct", "1@2test")]
		[InlineData("Field Type ct 1#2test", "Field Type", "ct", "1#2test")]
		[InlineData("Field Type ct 1<2test", "Field Type", "ct", "1<2test")]
		[InlineData("Field Type ct 1>2test", "Field Type", "ct", "1>2test")]
		[InlineData("Field Type ct 1,2test", "Field Type", "ct", "1,2test")]
		[InlineData("Field Type ct 1/2test", "Field Type", "ct", "1/2test")]
		[InlineData("Field Type ct 1?2test", "Field Type", "ct", "1?2test")]
		[InlineData("Field Type ct test!\"£$% ^&*_ +}{~@:?><test", "Field Type", "ct", "test!\"£$% ^&*_ +}{~@:?><test")]
		[InlineData("Field Type ct \"£$% ^&*_ +}{~@:?><", "Field Type", "ct", "\"£$% ^&*_ +}{~@:?><")]
		[InlineData("Field Type ct ~`!@#$%^&*_-+={}[]|\\:;<,>.?/", "Field Type", "ct", "~`!@#$%^&*_-+={}[]|\\:;<,>.?/")]

		public void FullFilterExpressionSingle(string expression, string m1, string m2, string m3)
		{
			var matches = FilterExpressionRegexParser.ParseFullFilterExpression(expression);
			var filterMatch = FilterExpressionRegexParser.ParseSingleFilterExpression(matches[0]);
			if (filterMatch.Success && filterMatch.Groups.Count == 4)
			{
				Assert.Equal(m1, filterMatch.Groups[1].Value);
				Assert.Equal(m2, filterMatch.Groups[2].Value);
				Assert.Equal(m3, filterMatch.Groups[3].Value);
			}

			if (matches.Count != 1)
			{
				Assert.True(false, "Should return only 1 match");
			}
		}

		[Theory]
		[InlineData("(FieldType ct fieldValue) and (Field Type ne field value)", "FieldType ct fieldValue", "Field Type ne field value", "")]
		[InlineData("((FieldType ct fieldValue) and (FieldType ne field value)) or (data eq test)", "FieldType ct fieldValue", "FieldType ne field value", "data eq test")]
		public void FullFilterExpressionMultiple(string expression, string m1, string m2, string m3)
		{
			var matches = FilterExpressionRegexParser.ParseFullFilterExpression(expression);

			if (!string.IsNullOrEmpty(m1))
			{
				Assert.Equal(matches[0].Value, m1);
			}
			if (!string.IsNullOrEmpty(m2))
			{
				Assert.Equal(matches[1].Value, m2);
			}
			if (!string.IsNullOrEmpty(m3))
			{
				Assert.Equal(matches[2].Value, m3);
			}
		}
	}

}


