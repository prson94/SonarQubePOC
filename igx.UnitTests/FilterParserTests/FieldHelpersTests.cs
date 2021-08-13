using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using d360.model.validators;
using d360.core.entities;
using d360.model.helpers;
using d360.model.helpers.filters;
using d360.model.helpers.filters.program;

namespace igx.UnitTests.FilterExpressionTests
{
    [Trait("Unit tests", "Filter helper test")]
    public class FieldHelpersTests : BaseTest
    {

        [Theory]
        [InlineData("boolean,lookup,relationship", "eq")]
        [InlineData("boolean,lookup,relationship", "ne")]
        [InlineData("boolean,lookup,relationship", "ct")]
        [InlineData("number,decimal,score,counter", "eq")]
        [InlineData("number,decimal,score,counter", "ne")]
        [InlineData("number,decimal,score,counter", "gt")]
        [InlineData("number,decimal,score,counter", "ge")]
        [InlineData("number,decimal,score,counter", "lt")]
        [InlineData("number,decimal,score,counter", "le")]
        [InlineData("date,datetime", "ne")]
        [InlineData("date,datetime", "eq")]
        [InlineData("date,datetime", "ct")]
        [InlineData("date,datetime", "nct")]
        [InlineData("date,datetime", "gt")]
        [InlineData("date,datetime", "ge")]
        [InlineData("date,datetime", "lt")]
        [InlineData("date,datetime", "le")]
        [InlineData("assettypeclass", "eq")]
        [InlineData("assettypeclass", "ne")]
        [InlineData("text", "eq")]
        [InlineData("text", "ne")]
        [InlineData("text", "ct")]
        [InlineData("text", "nct")]
        public void ValidOperatorForType(string fieldType, string @operator)
        {
            var types = fieldType.Split(',').ToList();
            foreach (var type in types)
            {
                var result = FilterHelpers.IsValidOperatorForFieldType(type, @operator);
                Assert.True(result);
            }
        }

        [Theory]
        [InlineData("boolean,lookup,relationship", "ge")]
        [InlineData("boolean,lookup,relationship", "gt")]
        [InlineData("boolean,lookup,relationship", "le")]
        [InlineData("boolean,lookup,relationship", "lt")]
        [InlineData("boolean,lookup,relationship", "nct")]
        [InlineData("number,decimal,score,counter", "ct")]
        [InlineData("number,decimal,score,counter", "nct")]
        [InlineData("date,datetime", "blabla")]
        [InlineData("assettypeclass", "ct")]
        [InlineData("assettypeclass", "nct")]
        [InlineData("assettypeclass", "gt")]
        [InlineData("assettypeclass", "ge")]
        [InlineData("assettypeclass", "lt")]
        [InlineData("assettypeclass", "le")]
        [InlineData("text", "gt")]
        [InlineData("text", "ge")]
        [InlineData("text", "lt")]
        [InlineData("text", "le")]
        public void InValidOperatorForType(string fieldType, string @operator)
        {
            var types = fieldType.Split(',').ToList();
            foreach (var type in types)
            {
                var result = FilterHelpers.IsValidOperatorForFieldType(type, @operator);
                Assert.False(result);
            }
        }

        [Theory]
        [InlineData("number", "23")]
        [InlineData("decimal", "23")]
        [InlineData("boolean", "true")]
        [InlineData("score", "12")]
        [InlineData("counter", "100")]
        [InlineData("text", "'value'")]
        [InlineData("assetpath", "'value'")]
        public void ValidateValueForTypeValidTests(string fieldType, string value)
        {
            FilterHelpers.ValidateValueForType(fieldType, value);
            //didnt throw error
            Assert.True(true);
        }


        [Theory]
        [InlineData("text", "23")]
        [InlineData("assetpath", "23")]
        [InlineData("lookup", "true")]
        public void ValidateValueForTypeInValidTests(string fieldType, string value)
        {

            bool didThrow = false;
            try
            {

                FilterHelpers.ValidateValueForType(fieldType, value);
            }
            catch
            {
                didThrow = true;
            }

            Assert.True(didThrow, "This expression should throw an error!");
        }
    }

}


