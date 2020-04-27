using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using d360.model.validators;
using d360.core.entities;
using d360.model.helpers;

namespace igx.UnitTests.FilterExpressionTests
{
    [Trait("Unit tests", "Filter expression parser")]
    public class FilterExpressionParserTests : BaseTest
    {
        private FilterExpressionParser filterParser;

        //check if sql parameter exists in generated sql
        //check if there is only one instance of single param in case of multiple filters for same field type
        private Func<string, string, bool> CheckParamOccurance =
            (string sql, string param) => { return sql.Contains(param) && sql.IndexOf(param) == sql.LastIndexOf(param); };

        private Func<string, string, bool> CheckMultipleParamOccurance =
          (string sql, string param) => { return sql.Contains(param) && sql.IndexOf(param) != sql.LastIndexOf(param); };


        public FilterExpressionParserTests()
        {
            List<FieldType> fieldTypes = new List<FieldType>();
            List<string> columns = new List<string>();
            fieldTypes.Add(new FieldType() { Name = "number", ID = 1, Type = "Number" });
            fieldTypes.Add(new FieldType() { Name = "decimal", ID = 2, Type = "Decimal" });
            fieldTypes.Add(new FieldType() { Name = "boolean", ID = 3, Type = "Boolean" });
            fieldTypes.Add(new FieldType() { Name = "date", ID = 4, Type = "Date" });
            fieldTypes.Add(new FieldType() { Name = "text", ID = 5, Type = "Text" });
            fieldTypes.Add(new FieldType() { Name = "lookup", ID = 6, Type = "Lookup", LookupObjectType = "ArtifactType", LookupObjectID = 1 });
            fieldTypes.Add(new FieldType() { Name = "relationship", ID = 6, Type = "Relationship", LookupObjectType = "IntersectType", LookupObjectID = 1 });

            fieldTypes.ForEach(x =>
            {
                columns.Add($"F{x.ID}.FormattedValue");
            });


            this.filterParser = new FilterExpressionParser(GetCompany());
            this.filterParser.LoadFieldTypes(fieldTypes, columns);

        }

        [Theory]
        [InlineData("number eq '1'")]
        [InlineData("number ct 1")]
        [InlineData("number eq 2.4")]
        [InlineData("number eq text")]
        [InlineData("decimal eq text")]
        [InlineData("decimal ct '2.4'")]
        [InlineData("boolean eq text")]
        [InlineData("boolean ct '2.4'")]
        [InlineData("boolean lt true")]
        [InlineData("boolean le true")]
        [InlineData("boolean gt true")]
        [InlineData("boolean ge true")]
        [InlineData("date ct '12-02-2020'")]
        [InlineData("date eq 'fasdfasdf'")]
        [InlineData("date ne '32.56.2020'")]
        [InlineData("text ct 2.4")]
        [InlineData("text lt true")]
        [InlineData("text le true")]
        [InlineData("text gt true")]
        [InlineData("text ge true")]
        [InlineData("text")]
        [InlineData("text ge ")]
        [InlineData("text ge and")]
        [InlineData("text eq 'text' and number eq")]
        [InlineData("'text eq 'text'")]
        [InlineData("text eq 'text")]
        [InlineData("(text eq 'text'")]
        [InlineData("(text eq 'text'))")]
        [InlineData("(text eq 'text') and test and")]
        [InlineData("text bla 'word'")]
        [InlineData("text eq 'word' xor text eq 'test'")]
        [InlineData("lookup lt 'validlookupvalue'")]
        [InlineData("lookup gt 'validlookupvalue'")]
        [InlineData("lookup le 'validlookupvalue'")]
        [InlineData("lookup ge 'validlookupvalue'")]
        [InlineData("lookup eq 'invalidlookupvalue'")]
        [InlineData("lookup ne 'invalidlookupvalue'")]
        [InlineData("relationship lt 'relationshipassetvalue'")]
        [InlineData("relationship gt 'relationshipassetvalue'")]
        [InlineData("relationship le 'relationshipassetvalue'")]
        [InlineData("relationship ge 'relationshipassetvalue'")]
        [InlineData("nonexistingfield ge 'relationshipassetvalue'")]
        [InlineData("text eq Chetna's ^&*()_+-={}[]|\\;:\",./<>? Check~` All")]
        public void InvalidFormatExpressions(string expression)
        {
            bool didThrow = false;
            try
            {
                Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                List<int> filteredFields = new List<int>();
                filterParser.Parse(expression, out sqlParams, out filteredFields);
            }
            catch
            {
                didThrow = true;
            }

            Assert.True(didThrow, "This expression should throw an error!");
        }

        [Theory]
        [InlineData("number eq 1")]
        [InlineData("number lt 1")]
        [InlineData("number le 1")]
        [InlineData("number gt 1")]
        [InlineData("number ge 1")]
        [InlineData("number ne 1")]
        public void ValidNumberTests(string expression)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();
            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == 1);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
            }
        }

        [Theory]
        [InlineData("decimal eq 1.5")]
        [InlineData("decimal lt 1.5")]
        [InlineData("decimal le 1.5")]
        [InlineData("decimal gt 1.5")]
        [InlineData("decimal ge 1.5")]
        [InlineData("decimal ne 1.5")]
        public void ValidDecimalTests(string expression)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();
            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == 1);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
            }
        }


        [Theory]
        [InlineData("boolean eq true")]
        [InlineData("boolean ne false")]
        [InlineData("boolean eq True")]
        [InlineData("boolean ne False")]
        [InlineData("boolean ne 0")]
        [InlineData("boolean ne 1")]
        public void ValidBooleanTests(string expression)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();
            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == 1);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
            }
        }

        [Theory]
        [InlineData("date eq '02-10-2020'")]
        [InlineData("date lt '02-10-2020'")]
        [InlineData("date le '02-10-2020'")]
        [InlineData("date gt '02-10-2020'")]
        [InlineData("date ge '02-10-2020'")]
        [InlineData("date ne '02-10-2020'")]
        public void ValidDateTests(string expression)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();

            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == 1);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
            }
        }

        [Theory]
        [InlineData("text eq 'some text'")]
        [InlineData("text ne 'text'")]
        [InlineData("text ct 'text'")]
        [InlineData("text eq 'Chetna&apos;s ^&*()_+-={}[]|\\;&apos;:\",./<>? Check~` All'")]
        [InlineData("text eq 'Chetna&apos;s ^&*)_+-={}[]|\\;&apos;:\",./<>? Check~` All'")]
        [InlineData("text eq 'Chetna&apos;s ^&*(_+-={}[]|\\;&apos;:\",./<>? Check~` All'")]
        public void ValidTextTests(string expression)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();

            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == 1);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
            }
        }

        [Theory]
        [InlineData("lookup eq 'validlookupvalue'")]
        [InlineData("lookup ne 'validlookupvalue'")]
        [InlineData("lookup ct 'validlookupvalue'")]
        public void ValidLookupTests(string expression)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();
            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == 1);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
                Assert.True(sql.Contains("string_split"));
            }
        }

        [Theory]
        [InlineData("relationship eq 'relationshipassetvalue'")]
        [InlineData("relationship ne 'relationshipassetvalue'")]
        [InlineData("relationship ct 'relationshipassetvalue'")]
        public void ValidRelationshipTests(string expression)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();

            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == 1);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckMultipleParamOccurance(sql, param.Key));
                Assert.True(sql.Contains("select id from intersectdetail where intersecttypeid"));
            }
        }

        [Theory]
        [InlineData("text eq 'some text' and number lt 10", 2)]
        [InlineData("decimal eq 24.5 and (text ne 'text' or number gt 20)", 3)]
        [InlineData("text ct 'text' and text ct 'bla'", 2)]
        [InlineData("(decimal eq 0 or (number gt 0 and (text ct 'text' and text ct 'bla')))", 4)]
        public void ValidFilterCombinationsTests(string expression, int paramCount)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();
            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == paramCount);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
            }
        }

        [Theory]
        [InlineData("text ct 'Chetna&apos;s ^&*(_+-={}[]|\\;&apos;:\",./<>? Check~` All'", "Chetna's [^]&%([_]+-={}[[]]|\\;':\",./<>_ Check~` All")]
        [InlineData("text eq 'Chetna&apos;s ^&*(_+-={}[]|\\;&apos;:\",./<>? Check~` All'", "Chetna's ^&*(_+-={}[]|\\;':\",./<>? Check~` All")]
        [InlineData("text eq '*&_Bangalore'","*&_Bangalore")]
        [InlineData("text ct '*&_Bangalore'","%&[_]Bangalore")]
        public void IsSQLEscapingValue(string expression, string expectedParam)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();
            List<int> fieldIds = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out fieldIds);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
                Assert.True(param.Value.ToString().ToLower() == expectedParam.ToLower());
            }
        }


    }

}


