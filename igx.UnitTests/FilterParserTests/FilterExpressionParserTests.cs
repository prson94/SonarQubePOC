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
            fieldTypes.Add(new FieldType() { Name = "counter", ID = 7, Type = "Counter" });

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
        [InlineData("number eq 1.000")]
        [InlineData("decimal eq text")]
        [InlineData("decimal ct '2.4'")]
        [InlineData("boolean eq text")]
        [InlineData("boolean ct '2.4'")]
        [InlineData("boolean lt true")]
        [InlineData("boolean le true")]
        [InlineData("boolean gt true")]
        [InlineData("boolean ge true")]
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
        [InlineData("counter eq test")]
        [InlineData("counter eq 'test'")]
        [InlineData("counter eq '12'")]
        [InlineData("counter ct 12")]
        [InlineData("counter nct 12")]
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
        [InlineData("number eq 1,000")]
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
        [InlineData("boolean eq tr")]
        [InlineData("boolean eq fal")]
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
        [InlineData("date ct '12-02-2020'")]
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
        [InlineData("counter eq 12", "F7.FormattedValue = @filter_1")]
        [InlineData("counter ne 12", "(F7.FormattedValue <> @filter_1 or F7.FormattedValue is null)")]
        [InlineData("counter ge 12", "F7.FormattedValue >= @filter_1")]
        [InlineData("counter gt 12", "F7.FormattedValue > @filter_1")]
        [InlineData("counter le 12", "F7.FormattedValue <= @filter_1")]
        [InlineData("counter lt 12", "F7.FormattedValue < @filter_1")]
        [InlineData("counter eq null", "F7.FormattedValue is null", false)]
        [InlineData("counter ne null", "F7.FormattedValue is not null", false)]
        public void ValidCounterTests(string expression, string expectedQuery, bool checkParamCount = true)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();

            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            if (checkParamCount)
            {
                Assert.True(sqlParams.Count == 1);
            }
            Assert.True(sql == expectedQuery);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
            }
        }

        [Theory]
        [InlineData("lookup eq 'validlookupvalue'")]
        [InlineData("lookup ne 'validlookupvalue'")]
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
        [InlineData("text ct 'Chetna&apos;s ^&*(_+-={}[]|\\;&apos;:\",./<>? Check~` All'", "%Chetna's [^]&%([_]+-={}[[]]|\\;':\",./<>_ Check~` All%")]
        [InlineData("text eq 'Chetna&apos;s ^&*(_+-={}[]|\\;&apos;:\",./<>? Check~` All'", "Chetna's ^&*(_+-={}[]|\\;':\",./<>? Check~` All")]
        [InlineData("text eq '*&_Bangalore'", "*&_Bangalore")]
        [InlineData("text ct '*&_Bangalore'", "%&[_]Bangalore")]
        [InlineData("text ct 'string for contains'", "%string for contains%")]
        [InlineData("text eq 'string for equal'", "string for equal")]
        [InlineData("text ne 'string for equal'", "string for equal")]
        public void CheckSQLQueryParsing(string expression, string expectedParam)
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

        [Theory]
        [InlineData("number eq 100", "F1.FormattedValue = @filter_1")]
        [InlineData("number ne 100", "(F1.FormattedValue <> @filter_1 or F1.FormattedValue is null)")]
        [InlineData("number ge 100", "F1.FormattedValue >= @filter_1")]
        [InlineData("number gt 100", "F1.FormattedValue > @filter_1")]
        [InlineData("number le 100", "F1.FormattedValue <= @filter_1")]
        [InlineData("number lt 100", "F1.FormattedValue < @filter_1")]
        [InlineData("decimal eq 100.34", "F2.FormattedValue = @filter_1")]
        [InlineData("decimal ne 100.34", "(F2.FormattedValue <> @filter_1 or F2.FormattedValue is null)")]
        [InlineData("decimal ge 100.34", "F2.FormattedValue >= @filter_1")]
        [InlineData("decimal gt 100.34", "F2.FormattedValue > @filter_1")]
        [InlineData("decimal le 100.34", "F2.FormattedValue <= @filter_1")]
        [InlineData("decimal lt 100.34", "F2.FormattedValue < @filter_1")]
        [InlineData("boolean eq True", "F3.FormattedValue = @filter_1")]
        [InlineData("boolean ne True", "(F3.FormattedValue <> @filter_1 or F3.FormattedValue is null)")]
        [InlineData("date eq '02-10-2020'", "F4.FormattedValue = @filter_1")]
        [InlineData("date ne '02-10-2020'", "(F4.FormattedValue <> @filter_1 or F4.FormattedValue is null)")]
        [InlineData("date ge '02-10-2020'", "F4.FormattedValue >= @filter_1")]
        [InlineData("date gt '02-10-2020'", "F4.FormattedValue > @filter_1")]
        [InlineData("date le '02-10-2020'", "F4.FormattedValue <= @filter_1")]
        [InlineData("date lt '02-10-2020'", "F4.FormattedValue < @filter_1")]
        [InlineData("text eq 'string'", "F5.FormattedValue = @filter_1")]
        [InlineData("text ne 'string'", "(F5.FormattedValue <> @filter_1 or F5.FormattedValue is null)")]
        [InlineData("text ct 'string'", "F5.FormattedValue like @filter_1")]
        [InlineData("text nct 'string'", "(F5.FormattedValue not like @filter_1 or F5.FormattedValue is null)")]
        [InlineData("lookup eq 'validlookupvalue'", "@filter_1 in (select * from string_split(F6.Value,','))")]
        [InlineData("lookup ne 'validlookupvalue'", "@filter_1 not in (select * from string_split(F6.Value,','))")]
        [InlineData("number gt 100 and text eq '100' or boolean eq true", "F1.FormattedValue > @filter_1 and F5.FormattedValue = @filter_2 or F3.FormattedValue = @filter_3")]
        public void CheckSQLStatementForOperators(string expression, string expectedQuery)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();
            List<int> fieldIds = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out fieldIds);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
            }
            Assert.True(sql.ToLower().Replace(Environment.NewLine, "") == expectedQuery.ToLower().Replace(Environment.NewLine, ""));

        }

        [Theory]
        [InlineData("relationship eq 'relationshipassetvalue'", @"exists
                                    (select id from intersectdetail where intersecttypeid = 1 and subjectuid = a.uid and subjecttypeid = T.ObjectId and subjecttype = T.Object and objectname = @filter_1
                                    union select id from IntersectDetail where intersecttypeid = 1 and objectuid = a.uid and objecttypeid = T.ObjectId and objecttype = T.Object and subjectname = @filter_1)")]
        [InlineData("relationship ne 'relationshipassetvalue'", @"not exists
                                    (select id from intersectdetail where intersecttypeid = 1 and subjectuid = a.uid and subjecttypeid = T.ObjectId and subjecttype = T.Object and objectname = @filter_1
                                    union select id from IntersectDetail where intersecttypeid = 1 and objectuid = a.uid and objecttypeid = T.ObjectId and objecttype = T.Object and subjectname = @filter_1)")]
        [InlineData("relationship ct 'relationshipassetvalue'", @"exists
                                    (select id from intersectdetail where intersecttypeid = 1 and subjectuid = a.uid and subjecttypeid = T.ObjectId and subjecttype = T.Object and objectname like @filter_1
                                    union select id from IntersectDetail where intersecttypeid = 1 and objectuid = a.uid and objecttypeid = T.ObjectId and objecttype = T.Object and subjectname like @filter_1)")]

        public void CheckSQLStatementForOperatorsAndRelationships(string expression, string expectedQuery)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();
            List<int> fieldIds = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out fieldIds);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckMultipleParamOccurance(sql, param.Key));
            }
            Assert.True(sql.ToLower().Replace(Environment.NewLine, "") == expectedQuery.ToLower().Replace(Environment.NewLine, ""));

        }

    }

}


