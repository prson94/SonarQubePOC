//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Xunit;
//using d360.model.validators;
//using d360.core.entities;
//using d360.model.helpers;
//using d360.model.helpers.filters;

//namespace igx.UnitTests.FilterExpressionTests
//{
//    [Trait("Unit tests", "Filter token tests")]
//    public class FilterTokenTests : BaseTest
//    {

//        FilterDataProvider dataProvider;
//        public FilterTokenTests()
//        {
//            this.dataProvider = new FilterDataProvider(GetCompany());
//        }


//        [Fact]
//        public void IsOnlyOperator()
//        {
//            var token = new FilterToken(this.dataProvider, null, "and", null);
//            Assert.True(token.IsOnlyOperator);
//        }

//        [Fact]
//        public void IsOnlyOperatorInvalid()
//        {
//            var token = new FilterToken(this.dataProvider, "value", "and", null);
//            Assert.True(!token.IsOnlyOperator);

//            token = new FilterToken(this.dataProvider, null, "and", "value");
//            Assert.True(!token.IsOnlyOperator);

//            token = new FilterToken(this.dataProvider, "value", "and", "value");
//            Assert.True(!token.IsOnlyOperator);
//        }

//        [Theory]
//        [InlineData("(", "(")]
//        [InlineData(")", ")")]
//        [InlineData("and", " and ")]
//        [InlineData("or", " or ")]
//        public void OperatorParsing(string @operator, string result)
//        {
//            var token = new FilterToken(this.dataProvider, null, @operator, null);
//            var res = token.GetSQLForOperator();
//            Assert.True(res == result);
//        }

//        [Theory]
//        [InlineData("")]
//        [InlineData("a")]
//        public void OperatorParsingError(string @operator)
//        {
//            bool didThrow = false;
//            try
//            {
//                var token = new FilterToken(this.dataProvider, null, @operator, null);
//                var res = token.GetSQLForOperator();
//            }
//            catch
//            {
//                didThrow = true;
//            }

//            Assert.True(didThrow, "This expression should throw an error!");
//        }


//        [Theory]
//        [InlineData("eq", " is null")]
//        [InlineData("ne", " is not null")]
//        public void GetSQLNullOperator(string @operator, string result)
//        {
//            var token = new FilterToken(this.dataProvider, null, @operator, null);
//            var res = token.GetSQLNullOperator(token.@operator);
//            Assert.True(res == result);
//        }


//        [Theory]
//        [InlineData("")]
//        [InlineData("a")]
//        public void GetSQLNullOperatorError(string @operator)
//        {
//            bool didThrow = false;
//            try
//            {
//                var token = new FilterToken(this.dataProvider, null, @operator, null);
//                var res = token.GetSQLNullOperator(token.@operator);
//            }
//            catch
//            {
//                didThrow = true;
//            }

//            Assert.True(didThrow, "This expression should throw an error!");
//        }
//    }

//}


