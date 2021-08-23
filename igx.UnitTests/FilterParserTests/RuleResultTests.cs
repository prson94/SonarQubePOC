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

namespace igx.UnitTests.FilterExpressionTests
{
    [Trait("Unit tests", "Filter expression parser for Rule Results")]
    public class RuleResultTests : BaseTest
    {
        private FilterExpressionParser filterParser;

        //check if sql parameter exists in generated sql
        //check if there is only one instance of single param in case of multiple filters for same field type
        private Func<string, string, bool> CheckParamOccurance =
            (string sql, string param) => { return sql.Contains(param) && sql.IndexOf(param) == sql.LastIndexOf(param); };

        private Func<string, string, bool> CheckMultipleParamOccurance =
          (string sql, string param) => { return sql.Contains(param) && sql.IndexOf(param) != sql.LastIndexOf(param); };


        public RuleResultTests()
        {
            var filterDataProvider = new FilterDataProvider(GetCompany());

            this.filterParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.RuleResults);
        }

        [Theory]
        [InlineData("EvaluatedAssetClass lt 'BusinessAsset'")]
        [InlineData("EvaluatedAssetClass le 'BusinessAsset'")]
        [InlineData("EvaluatedAssetClass gt 'BusinessAsset'")]
        [InlineData("EvaluatedAssetClass ge 'BusinessAsset'")]
        [InlineData("EvaluatedAssetClass ct 'BusinessAsset'")]

        [InlineData("EffectiveDate ne '32.56.2020'")]
        [InlineData("EffectiveDate eq 'fasdfasdf'")]

        [InlineData("RunDate ne '32.56.2020'")]
        [InlineData("RunDate eq 'fasdfasdf'")]

        [InlineData("PassCount eq 1.000")]

        [InlineData("FailCount eq 1.000")]

        [InlineData("TotalCount eq 1.000")]

        [InlineData("PassFraction eq text")]
        [InlineData("PassFraction ct '2.4'")]

        [InlineData("Outdated eq text")]
        [InlineData("Outdated ct '2.4'")]
        [InlineData("Outdated lt true")]
        [InlineData("Outdated le true")]
        [InlineData("Outdated gt true")]
        [InlineData("Outdated ge true")]
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
        [InlineData("EvaluatedAssetClass eq 'BusinessAsset'", "E.Class = @filter_1")]
        [InlineData("EvaluatedAssetClass ne 'BusinessAsset'", "(E.Class <> @filter_1 or E.Class is null)")]

        [InlineData("EvaluatedAssetTypePath ct 'Column'", "P.Path like @filter_1")]
        [InlineData("EvaluatedAssetPath eq 'dbo'", "E.Segments.exist('/path/segment[.=sql:variable(\"@filter_1\")]') = 1")]
        [InlineData("EvaluatedAssetDisplayPath eq 'dbo'", "E.Segments.exist('/path/segment[.=sql:variable(\"@filter_1\")]') = 1")]

        [InlineData("EffectiveDate eq '02-10-2020'", "R.EffectiveDate = @filter_1")]
        [InlineData("EffectiveDate lt '02-10-2020'", "R.EffectiveDate < @filter_1")]
        [InlineData("EffectiveDate le '02-10-2020'", "R.EffectiveDate <= @filter_1")]
        [InlineData("EffectiveDate gt '02-10-2020'", "R.EffectiveDate > @filter_1")]
        [InlineData("EffectiveDate ge '02-10-2020'", "R.EffectiveDate >= @filter_1")]
        [InlineData("EffectiveDate ne '02-10-2020'", "(R.EffectiveDate <> @filter_1 or R.EffectiveDate is null)")]
        [InlineData("EffectiveDate ct '12-02-2020'", "CONVERT(VARCHAR,R.EffectiveDate,120) like @filter_1")]

        [InlineData("RunDate eq '02-10-2020'", "R.RunDate = @filter_1")]
        [InlineData("RunDate lt '02-10-2020'", "R.RunDate < @filter_1")]
        [InlineData("RunDate le '02-10-2020'", "R.RunDate <= @filter_1")]
        [InlineData("RunDate gt '02-10-2020'", "R.RunDate > @filter_1")]
        [InlineData("RunDate ge '02-10-2020'", "R.RunDate >= @filter_1")]
        [InlineData("RunDate ne '02-10-2020'", "(R.RunDate <> @filter_1 or R.RunDate is null)")]
        [InlineData("RunDate ct '12-02-2020'", "CONVERT(VARCHAR,R.RunDate,120) like @filter_1")]

        [InlineData("PassCount eq 1", "R.PassCount = @filter_1")]
        [InlineData("PassCount lt 1", "R.PassCount < @filter_1")]
        [InlineData("PassCount le 1", "R.PassCount <= @filter_1")]
        [InlineData("PassCount gt 1", "R.PassCount > @filter_1")]
        [InlineData("PassCount ge 1", "R.PassCount >= @filter_1")]
        [InlineData("PassCount ne 1", "(R.PassCount <> @filter_1 or R.PassCount is null)")]
        [InlineData("PassCount eq 1,000", "R.PassCount = @filter_1")]

        [InlineData("FailCount eq 1", "R.FailCount = @filter_1")]
        [InlineData("FailCount lt 1", "R.FailCount < @filter_1")]
        [InlineData("FailCount le 1", "R.FailCount <= @filter_1")]
        [InlineData("FailCount gt 1", "R.FailCount > @filter_1")]
        [InlineData("FailCount ge 1", "R.FailCount >= @filter_1")]
        [InlineData("FailCount ne 1", "(R.FailCount <> @filter_1 or R.FailCount is null)")]
        [InlineData("FailCount eq 1,000", "R.FailCount = @filter_1")]

        [InlineData("TotalCount eq 1", "R.TotalCount = @filter_1")]
        [InlineData("TotalCount lt 1", "R.TotalCount < @filter_1")]
        [InlineData("TotalCount le 1", "R.TotalCount <= @filter_1")]
        [InlineData("TotalCount gt 1", "R.TotalCount > @filter_1")]
        [InlineData("TotalCount ge 1", "R.TotalCount >= @filter_1")]
        [InlineData("TotalCount ne 1", "(R.TotalCount <> @filter_1 or R.TotalCount is null)")]
        [InlineData("TotalCount eq 1,000", "R.TotalCount = @filter_1")]

        [InlineData("PassFraction eq 0.5", "R.PassFraction = @filter_1")]
        [InlineData("PassFraction lt 0.5", "R.PassFraction < @filter_1")]
        [InlineData("PassFraction le 0.5", "R.PassFraction <= @filter_1")]
        [InlineData("PassFraction gt 0.5", "R.PassFraction > @filter_1")]
        [InlineData("PassFraction ge 0.5", "R.PassFraction >= @filter_1")]
        [InlineData("PassFraction ne 0.5", "(R.PassFraction <> @filter_1 or R.PassFraction is null)")]

        [InlineData("Outdated eq true", "coalesce(E.IsDuplicate, R.IsDuplicate) = @filter_1")]
        [InlineData("Outdated ne false", "(coalesce(E.IsDuplicate, R.IsDuplicate) <> @filter_1 or coalesce(E.IsDuplicate, R.IsDuplicate) is null)")]
        public void ValidValues(string expression, string expectedQuery)
        {
            Dictionary<string, object> sqlParams = new Dictionary<string, object>();

            List<int> filteredFields = new List<int>();
            string sql = filterParser.Parse(expression, out sqlParams, out filteredFields);
            Assert.True(sqlParams.Count == 1);
            Assert.True(sql == expectedQuery);
            foreach (var param in sqlParams)
            {
                Assert.True(CheckParamOccurance(sql, param.Key));
            }
        }
    }

}


