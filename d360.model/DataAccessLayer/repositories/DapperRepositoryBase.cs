using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using d360.core;
using d360.core.entities;
using d360.model.helpers;
using d360.model.helpers.filters;

using Dapper;

namespace d360.model.DataAccessLayer.repositories
{
    internal abstract class DapperRepositoryBase<TConnectionProvider>
        where TConnectionProvider : IDbConnectionProvider
    {
        protected IDapperQueryComposer<TConnectionProvider> QueryComposer { get; }

        protected DapperRepositoryBase(IDapperQueryComposer<TConnectionProvider> queryComposer)
        {
            QueryComposer = queryComposer;
        }

        protected string CreatePageOffsetSql(int? pageNumber, int? pageSize, int? pageSizeLimit = 10000)
        {
            var offset = "";
            if (pageSize > 0 || pageNumber > 0)
            {
                if (pageSize < 1)
                {
                    pageSize = 1;
                }

                if (pageNumber < 1)
                {
                    pageNumber = 1;
                }

                if (pageSize > pageSizeLimit)
                {
                    pageSize = pageSizeLimit;
                }

                if (pageNumber > 10000)
                {
                    pageNumber = 10000;
                }

                offset = $"OFFSET {pageSize * (pageNumber - 1)} ROWS FETCH NEXT {pageSize} ROWS ONLY";
            }

            return offset;
        }

        protected string CreateOrderBySql(IEnumerable<OrderByModel> orderByEnumerable)
        {
            return $"ORDER BY {string.Join(", ", orderByEnumerable.Select(ConvertOrderByModel))}";
        }

        private string ConvertOrderByModel(OrderByModel orderBy)
        {
            var dir = orderBy.Direction == OrderByDirectionEnum.Ascending ? "ASC" : "DESC";
            return $"{orderBy.ColumnName} {dir}";
        }

        protected string ValidateOrderByColumnName(string columnName, IEnumerable<DefaultFilter> knownFilters)
        {
            bool EqualityCheck(string actual, DefaultFilter expected) => expected.ApiName.Equals(actual, StringComparison.OrdinalIgnoreCase);

            var field = Preconditions.Exists(nameof(columnName), columnName, knownFilters, EqualityCheck);
            var result = field.SqlExpression;

            return result;
        }

        protected async Task<PagedApiBaseViewModel<T>> QueryDynamicPagedResultsAsync<T>(
            string source,
            SqlMapper.IDynamicParameters dynamicParameters,
            IReadOnlyList<string> whereStatementList,
            IReadOnlyList<OrderByModel> orderByList,
            int pageNum,
            int pageSize
        )
        {
            Preconditions.NotNull(whereStatementList, nameof(whereStatementList));
            Preconditions.NotNull(orderByList, nameof(orderByList));

            var result = new PagedApiBaseViewModel<T>();
            var orderBySql = CreateOrderBySql(orderByList);
            var offsetSql = CreatePageOffsetSql(pageNum, pageSize);
            var whereSql = CreateWhereSql(whereStatementList);

            var query = $"select count(1) from ({source}) A {whereSql};" +
                        $"select * from ({source}) A {whereSql} {orderBySql} {offsetSql};";

            var reader = await QueryComposer.QueryMultipleAsync(query, dynamicParameters, 30);
            result.total = await reader.ReadSingleAsync<int>();
            result.pageNum = pageNum;
            result.pageSize = pageSize;
            result.items = await reader.ReadListAsync<T>();

            return result;
        }

        protected async Task<IReadOnlyList<T>> QueryDynamicResultsAsync<T>(
            string source,
            SqlMapper.IDynamicParameters dynamicParameters,
            IReadOnlyList<string> whereStatementList,
            IReadOnlyList<OrderByModel> orderByList
        )
        {
            Preconditions.NotNull(whereStatementList, nameof(whereStatementList));
            Preconditions.NotNull(orderByList, nameof(orderByList));

            var orderBySql = CreateOrderBySql(orderByList);
            var whereSql = CreateWhereSql(whereStatementList);

            var query = $"select * from ({source}) A {whereSql} {orderBySql}";

            var result = await QueryComposer.QueryListAsync<T>(query, dynamicParameters, 30);

            return result;
        }

        private string CreateWhereSql(IEnumerable<string> whereStatements)
        {
            whereStatements = whereStatements.ToArray();

            var whereCondition = string.Join(" AND ", whereStatements);
            if (whereStatements.Any())
            {
                return $"WHERE {whereCondition}";
            }

            return string.Empty;
        }

        protected static void ParseAdvancedFilterQueryParameter(
            ICompanyContext companyContext,
            string filter,
            List<DefaultFilter> fieldList,
            out DynamicParameters dbArgs,
            out List<string> whereStatements)
        {
            dbArgs = new DynamicParameters();
            whereStatements = new List<string>();

            if (string.IsNullOrEmpty(filter) == false)
            {
                var filterDataProvider = new FilterDataProvider(companyContext);
                var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.CustomFields, false);
                filterExpressionParser.OverrideAllowedDefaultFields(fieldList);
                whereStatements.Add("(" + filterExpressionParser.Parse(filter, out var sqlParams, out _) + ")");

                foreach (var item in sqlParams)
                {
                    dbArgs.Add(item.Key, item.Value);
                }
            }
        }
    }
}
