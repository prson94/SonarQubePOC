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
using igx.UnitTests.Core;

namespace igx.UnitTests.FilterExpressionTests
{
    [Trait("Unit tests", "Complex Fields Filtering Tests")]
    public class ComplexFieldsFilteringTests : BaseTest
    {
        private FilterExpressionParser filterParser;

        //check if sql parameter exists in generated sql
        //check if there is only one instance of single param in case of multiple filters for same field type
        private Func<string, string, bool> CheckParamOccurance =
            (string sql, string param) => { return sql.Contains(param) && sql.IndexOf(param) == sql.LastIndexOf(param); };

        private Func<string, string, bool> CheckMultipleParamOccurance =
          (string sql, string param) => { return sql.Contains(param) && sql.IndexOf(param) != sql.LastIndexOf(param); };


        public ComplexFieldsFilteringTests()
        {
            List<FieldType> fieldTypes = new List<FieldType>();
            fieldTypes.Add(new FieldType() { Name = "H1_00001", ID = 1, Type = "Number" });
            fieldTypes.Add(new FieldType() { Name = "H1_00002", ID = 2, Type = "Decimal" });
            fieldTypes.Add(new FieldType() { Name = "H1_00003", ID = 3, Type = "Boolean" });
            fieldTypes.Add(new FieldType() { Name = "H1_00004", ID = 4, Type = "Date" });
            fieldTypes.Add(new FieldType() { Name = "H1_00005", ID = 5, Type = "Text" });
            fieldTypes.Add(new FieldType() { Name = "H1_00006", ID = 6, Type = "Lookup", LookupObjectType = "ArtifactType", LookupObjectID = 1 });
            fieldTypes.Add(new FieldType() { Name = "H1_00007", ID = 7, Type = "Counter" });
            fieldTypes.Add(new FieldType() { Name = "$Related:4df68f30-daa0-48da-912f-2daaea6961e0", ID = 8, Type = "Relationship", LookupObjectType = "IntersectType", LookupObjectID = 1 });

            var filterDataProvider = GetFilterDataProvider();
            this.filterParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.ComplexLookupField);
            this.filterParser.LoadFieldTypes(fieldTypes, null);

        }

        [Theory]
        [InlineData("H1_00005 eq 'text'", "( h1_00005  =  'text')")]
        [InlineData("H1_00005 ne 'text'", "( h1_00005  <>  'text')")]
        [InlineData("H1_00005 ct 'text'", "( h1_00005  like  '%text%')")]
        [InlineData("H1_00005 nct 'text'", "( h1_00005  not like  '%text%')")]
        [InlineData("H1_00005 eq null", "( h1_00005  is null)")]
        [InlineData("H1_00005 ne null", "( h1_00005  is not null)")]
        public void TextTests(string input, string expectedOutput)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            var result = this.filterParser.Parse(input, out parameters, out _);
            Assert.True(result == expectedOutput);
            Assert.True(parameters.Count == 0);
        }

        [Theory]
        [InlineData("H1_00003 eq true", "( h1_00003  =  'True')")]
        [InlineData("H1_00003 eq True", "( h1_00003  =  'True')")]
        [InlineData("H1_00003 eq false", "( h1_00003  =  'False')")]
        [InlineData("H1_00003 eq False", "( h1_00003  =  'False')")]
        [InlineData("H1_00003 eq null", "( h1_00003  is null)")]
        [InlineData("H1_00003 ne null", "( h1_00003  is not null)")]
        public void BooleanTests(string input, string expectedOutput)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            var result = this.filterParser.Parse(input, out parameters, out _);
            Assert.True(result == expectedOutput);
            Assert.True(parameters.Count == 0);
        }

        [Theory]
        [InlineData("H1_00001 eq 5", "( h1_00001  =  '5')")]
        [InlineData("H1_00001 ne 8", "( h1_00001  <>  '8')")]
        [InlineData("H1_00001 gt 10", "( h1_00001  >  '10')")]
        [InlineData("H1_00001 ge 20", "( h1_00001  >=  '20')")]
        [InlineData("H1_00001 le 10", "( h1_00001  <=  '10')")]
        [InlineData("H1_00001 lt 20", "( h1_00001  <  '20')")]
        [InlineData("H1_00001 eq null", "( h1_00001  is null)")]
        [InlineData("H1_00001 ne null", "( h1_00001  is not null)")]
        [InlineData("h1_00002 eq 5", "( h1_00002  =  '5')")]
        [InlineData("h1_00002 ne 8", "( h1_00002  <>  '8')")]
        [InlineData("h1_00002 gt 10", "( h1_00002  >  '10')")]
        [InlineData("h1_00002 ge 20", "( h1_00002  >=  '20')")]
        [InlineData("h1_00002 le 10", "( h1_00002  <=  '10')")]
        [InlineData("h1_00002 lt 20", "( h1_00002  <  '20')")]
        [InlineData("h1_00002 eq null", "( h1_00002  is null)")]
        [InlineData("h1_00002 ne null", "( h1_00002  is not null)")]
        public void NumberAndDecimalTests(string input, string expectedOutput)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            var result = this.filterParser.Parse(input, out parameters, out _);
            Assert.True(result == expectedOutput);
            Assert.True(parameters.Count == 0);
        }

        [InlineData("h1_00007 eq 5", "( h1_00007  =  '5')")]
        [InlineData("h1_00007 ne 8", "( h1_00007  <>  '8')")]
        [InlineData("h1_00007 gt 10", "( h1_00007  >  '10')")]
        [InlineData("h1_00007 ge 20", "( h1_00007  >=  '20')")]
        [InlineData("h1_00007 le 10", "( h1_00007  <=  '10')")]
        [InlineData("h1_00007 lt 20", "( h1_00007  <  '20')")]
        [InlineData("h1_00007 eq null", "( h1_00007  is null)")]
        [InlineData("h1_00007 ne null", "( h1_00007  is not null)")]
        public void CounterTests(string input, string expectedOutput)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            var result = this.filterParser.Parse(input, out parameters, out _);
            Assert.True(result == expectedOutput);
            Assert.True(parameters.Count == 0);
        }

        [Theory]
        [InlineData("h1_00004 eq '15-10-2020'")]
        [InlineData("h1_00004 ne '15-10-2020'")]
        [InlineData("h1_00004 gt '02-10-2020'")]
        [InlineData("h1_00004 ge '02-10-2020'")]
        [InlineData("h1_00004 le '02-10-2020'")]
        [InlineData("h1_00004 lt '02-10-2020'")]
        [InlineData("h1_00004 eq null")]
        [InlineData("h1_00004 ne null")]
        public void DateTests(string input)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            var result = this.filterParser.Parse(input, out parameters, out _);
            Assert.True(parameters.Count == 0);
        }

        [Theory]
        [InlineData("((h1_00006 eq 'France') or (h1_00006 eq 'Brand new country'))", "((( h1_00006  like  '%France%')) or (( h1_00006  like  '%Brand new country%')))")]
        [InlineData("((h1_00006 ne 'France') or (h1_00006 eq 'Brand new country'))", "((( h1_00006  not like  '%France%')) or (( h1_00006  like  '%Brand new country%')))")]
        [InlineData("((h1_00006 eq 'France') and (h1_00006 eq 'Brand new country'))", "((( h1_00006  like  '%France%')) and (( h1_00006  like  '%Brand new country%')))")]
        [InlineData("h1_00006 eq null", "( h1_00006  is null)")]
        [InlineData("h1_00006 ne null", "( h1_00006  is not null)")]
        public void LookupTests(string input, string expectedOutput)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            var result = this.filterParser.Parse(input, out parameters, out _);
            Assert.True(result == expectedOutput);
            Assert.True(parameters.Count == 0);
        }

        [Theory]
        [InlineData("$related:f8bf1431-0d7b-4381-9cec-dd32c05e0158 eq f8bf1431-0d7b-4381-9cec-dd32c05e0159")]
        public void ValidRelationshipsTestsEquals(string expression)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();

            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == 2);
            Assert.True(sqlParams["@intersectFilter1"].ToString() == DataConstants.ValidGUID);
            Assert.True(sqlParams["@intersectAssetFilter1"].ToString() == DataConstants.ValidGUID2);
            Assert.Contains(@"IntersectTypeUid = @intersectFilter1", sql);
            Assert.Contains(@"O.Uid = @intersectAssetFilter1", sql);
            Assert.Contains(@"MATCH(S <- (E) - O)", sql);
            Assert.Contains(@"MATCH(S - (E) -> O)", sql);
        }

        [Theory]
        [InlineData("H1_00003 ct true")]
        [InlineData("H1_00003 ct 'true'")]
        [InlineData("H1_00003 gt true")]
        [InlineData("H1_00003 ge true")]
        [InlineData("H1_00003 lt true")]
        [InlineData("H1_00003 le true")]
        [InlineData("H1_00001 ct 2")]
        [InlineData("h1_00002 eq 'dadada'")]
        [InlineData("H1_00003 ct 2")]
        [InlineData("h1_00003 eq 'dadada'")]
        [InlineData("H1_00004 ct 2")]
        [InlineData("h1_00004 eq 'dadada'")]
        [InlineData("H1_00005 eq text")]
        [InlineData("H1_00005 eq 4")]
        [InlineData("H1_00005 gt '4'")]
        [InlineData("H1_00005 lt 4")]
        [InlineData("h1_00006 ct 'France'")]
        [InlineData("h1_00006 nct 'France'")]
        [InlineData("h1_00006 gt France")]
        [InlineData("h1_00006 eq France")]
        public void InvalidTests(string input)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            try
            {
                var result = this.filterParser.Parse(input, out parameters, out _);
                Assert.True(false);
            }
            catch (Exception ex)
            {
                Assert.True(true);
            }

        }

    }

}


