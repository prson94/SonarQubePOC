using Xunit;
using d360.model.helpers.filters;
using System.Linq;

namespace igx.UnitTests.RegexFilterExpressionParserTests.cs
{
	[Trait("Unit tests", "Filter expression parser with regex (for simple data catalog api)")]
	public class RegexFilterExpressionParserTests : BaseTest
	{
		[Theory]
		[InlineData("FieldType ct fieldValue", "##expressionMatch1")]
		[InlineData("(displayValue ct \\(banana\\)anana)", "(##expressionMatch1)")]
		[InlineData("displayValue ct \\(banana\\)anana", "##expressionMatch1")]
		[InlineData("f1 ct f2 or f3 ct f4", "##expressionMatch1 or ##expressionMatch2")]
		[InlineData("(f1 ct f2) or f3 ct f4", "(##expressionMatch1) or ##expressionMatch2")]
		[InlineData("f1 ct f2 or (f3 ct f4)", "##expressionMatch1 or (##expressionMatch2)")]
		[InlineData("(f1 ct f2) or (f3 ct f4)", "(##expressionMatch1) or (##expressionMatch2)")]
		[InlineData("((f1 ct f2) or (f3 ct f4))", "((##expressionMatch1) or (##expressionMatch2))")]
		[InlineData("((f1 ct f2) or (f3 ct f4 and f5 ct f6))", "((##expressionMatch1) or (##expressionMatch2 and ##expressionMatch3))")]
		[InlineData("((f1 ct f2) or ((f3 ct f4) and f5 ct f6))", "((##expressionMatch1) or ((##expressionMatch2) and ##expressionMatch3))")]
		[InlineData("((f1 ct f2) or ((f3 ct f4) and (f5 ct f6)))", "((##expressionMatch1) or ((##expressionMatch2) and (##expressionMatch3)))")]
		[InlineData("((f1 ct f2) or (((f3 ct f4) and (f5 ct f6))))", "((##expressionMatch1) or (((##expressionMatch2) and (##expressionMatch3))))")]
		[InlineData("(((f1 ct f2) or (((f3 ct f4) and (f5 ct f6)))))", "(((##expressionMatch1) or (((##expressionMatch2) and (##expressionMatch3)))))")]
		public void GetFilterExpressionStringParsed(string expression, string parsed)
		{
			var tokenParser = new FilterExpressionTokenizer(expression);
			tokenParser.GetTokens();
			var filterExpressionStringParsed = tokenParser.GetFilterExpressionStringParsed();

			Assert.Equal(filterExpressionStringParsed, parsed);
		}

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
		[InlineData("(displayValue ct \\(banana\\)anana)", "displayValue", "ct", "(banana)anana")]
		[InlineData("Field Type ct test!\"£$% ^&*_ +}{~@:?><test", "Field Type", "ct", "test!\"£$% ^&*_ +}{~@:?><test")]
		[InlineData("Field Type ct \"£$% ^&*_ +}{~@:?><", "Field Type", "ct", "\"£$% ^&*_ +}{~@:?><")]
		[InlineData("Field Type ct ~`!@#$%^&*_-+={}[]|\\:;<,>.?/", "Field Type", "ct", "~`!@#$%^&*_-+={}[]|\\:;<,>.?/")]
		[InlineData("Field Type ct ~`!@#$%^&*_-+={}[]\\(\\)|\\:;<,>.?/", "Field Type", "ct", "~`!@#$%^&*_-+={}[]()|\\:;<,>.?/")]
		[InlineData("Platform eq Airtable", "Platform", "eq", "Airtable")] //case when keyword or is in field value without space!
		[InlineData("neq|nin|ne eq ct|eq|in|nct|", "neq|nin|ne", "eq", "ct|eq|in|nct|")] //case when keywords ct|eq|in|nct|neq|nin|ne is in field value
		public void FullFilterExpressionSingle(string expression, string m1, string m2, string m3)
		{
			var tokenParser = new FilterExpressionTokenizer(expression);
			var matches = tokenParser.GetTokens();

			Assert.True(matches.Count() == 1, "Should return only 1 match");

			var firstMatch = matches[0];

			Assert.Equal(m1, firstMatch.Token.Field);
			Assert.Equal(m2, firstMatch.Token.Operator);
			Assert.Equal(m3, firstMatch.Token.Value);
		}

		[Theory]
		[InlineData("FieldType ct fieldValue and Field Type ne field value", "FieldType,ct,fieldValue", "Field Type,ne,field value", "")]
		[InlineData("(Data Catalog Type predicate ne null ) and ((Platform eq Airtable))", "Data Catalog Type predicate,ne,null", "Platform,eq,Airtable", "")]
		[InlineData("(FieldType ct fieldValue) and (Field Type ne field value)", "FieldType,ct,fieldValue", "Field Type,ne,field value", "")]
		[InlineData("((FieldType ct fieldValue) and (FieldType ne field value)) or (data eq test)", "FieldType,ct,fieldValue", "FieldType,ne,field value", "data,eq,test")]
		public void FullFilterExpressionMultiple(string expression, string m1, string m2, string m3)
		{
			var tokenParser = new FilterExpressionTokenizer(expression);
			var matches = tokenParser.GetTokens();

			if (!string.IsNullOrEmpty(m1))
			{
				var matchValues = m1.Split(',');
				var match = matches[0].Token;
				Assert.Equal(match.Field, matchValues[0]);
				Assert.Equal(match.Operator, matchValues[1]);
				Assert.Equal(match.Value, matchValues[2]);
			}
			if (!string.IsNullOrEmpty(m2))
			{
				var matchValues = m2.Split(',');
				var match = matches[1].Token;
				Assert.Equal(match.Field, matchValues[0]);
				Assert.Equal(match.Operator, matchValues[1]);
				Assert.Equal(match.Value, matchValues[2]);
			}
			if (!string.IsNullOrEmpty(m3))
			{
				var matchValues = m3.Split(',');
				var match = matches[2].Token;
				Assert.Equal(match.Field, matchValues[0]);
				Assert.Equal(match.Operator, matchValues[1]);
				Assert.Equal(match.Value, matchValues[2]);
			}
		}
	}

}


