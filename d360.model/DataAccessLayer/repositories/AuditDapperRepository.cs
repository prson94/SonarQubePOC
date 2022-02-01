using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using d360.core.entities;
using d360.model.helpers.filters;
using Dapper;

namespace d360.model.DataAccessLayer.repositories
{
    internal sealed class AuditDapperRepository : DapperRepositoryBase<ICompanyDbConnectionProvider>, IAuditDapperRepository
    {
        // this property is added only to support code which should be fixed in GOV-16916
        private ICompanyContext CompanyContext { get; }

        public AuditDapperRepository(IDapperQueryComposer<ICompanyDbConnectionProvider> queryComposer, ICompanyContext companyContext) : base(queryComposer)
        {
            CompanyContext = companyContext;
        }

        private readonly Dictionary<string, string> ActionObjectDictionary = new Dictionary<string, string>
        {
            { "Intersect", "Relationship" },
            { "IntersectType", "RelationshipType" },
            { "Taxonomy", "Model" },
            { "TaxonomyType", "ModelType" },
            { "ResponsibilityTypeRelationOverrideItem", "Responsibility Type Relation Override Item" },
        };

        public async Task<PagedApiBaseViewModel<AssetAuditApiItemModel>> PagedAuditViewAsync(
            Guid? assetUid,
            Guid? assetTypeUid,
            string action,
            DateTime? startDate,
            DateTime? endDate,
            string filter,
            IReadOnlyList<OrderByModel> orderByList,
            int pageNum,
            int pageSize
        )
        {
            var preparedParameters = await PrepareAuditViewParametersAsync(assetUid, assetTypeUid, action, startDate, endDate, filter, orderByList);

            var result = await QueryDynamicPagedResultsAsync<AssetAuditApiItemModel>(preparedParameters.viewName, preparedParameters.parameters, preparedParameters.whereStatementList, preparedParameters.orderByList, pageNum, pageSize);

            result.items = await PostProcessAuditCollectionAsync(result.items);

            return result;
        }

        private async Task<(string viewName, SqlMapper.IDynamicParameters parameters, IReadOnlyList<string> whereStatementList, IReadOnlyList<OrderByModel> orderByList)> PrepareAuditViewParametersAsync(
            Guid? assetUid,
            Guid? assetTypeUid,
            string action,
            DateTime? startDate,
            DateTime? endDate,
            string filter,
            IReadOnlyList<OrderByModel> orderByList
        )
        {
            var viewName = "SELECT * " +
                           "  FROM dbo.AuditView" +
                           " WHERE (@assetUid IS NULL OR ActionAssetUid = @assetUid)" +
                           "   AND (@assetTypeUid IS NULL OR ActionAssetTypeUid = @assetTypeUid)" +
                           "   AND (@action IS NULL OR Action = @action)" +
                           "   AND (@startDate IS NULL OR Date >= @startDate)" +
                           "   AND (@endDate IS NULL OR Date <= @endDate)";

            var fieldList = new List<DefaultFilter>
            {
                new DefaultFilter("uid", "A.Uid", SqlFieldType.Guid),
                new DefaultFilter("name", "A.Name", SqlFieldType.Text),
                new DefaultFilter("resourceUid", "A.ResourceUid", SqlFieldType.Guid),
                new DefaultFilter("resourceIsDeleted", "A.ResourceIsDeleted", SqlFieldType.Boolean),
                new DefaultFilter("resourceName", "A.ResourceName", SqlFieldType.Text),
                new DefaultFilter("date", "A.Date", SqlFieldType.DateTime),
                new DefaultFilter("action", "A.Action", SqlFieldType.Text),
                new DefaultFilter("actionAssetUid", "A.ActionAssetUid", SqlFieldType.Guid),
                new DefaultFilter("actionAssetTypeUid", "A.ActionAssetTypeUid", SqlFieldType.Guid),
                new DefaultFilter("actionObject", "A.ActionObject", SqlFieldType.Text),
                new DefaultFilter("actionObjectTypeName", "A.ActionObjectTypeName", SqlFieldType.Text),
                new DefaultFilter("actionObjectName", "A.ActionObjectName", SqlFieldType.Text),
                new DefaultFilter("actionDescription", "A.ActionDescription", SqlFieldType.Text),
                new DefaultFilter("field", "A.Field", SqlFieldType.Text),
                new DefaultFilter("newValue", "A.NewValue", SqlFieldType.Text),
                new DefaultFilter("class", "A.Class", SqlFieldType.Number),
                new DefaultFilter("version", "A.Version", SqlFieldType.Number),
                new DefaultFilter("previousValue", "A.PreviousValue", SqlFieldType.Text),
                new DefaultFilter("fieldType", "A.FieldType", SqlFieldType.Text)
            };

            if (string.IsNullOrEmpty(filter) == false && filter.Contains("actionObject"))
            {
                List<string> operators = new List<string>
                {
                    "eq",
                    "ne"
                };
                Dictionary<string, string> lookups = ActionObjectDictionary.ToDictionary(d => d.Value, d => d.Key);
                lookups.Add("Business Asset", "Artifact");
                lookups.Add("Technical Asset", "Artifact");

                filter = lookups.SelectMany(l => operators, (l, o) => new { l, o })
                    .ToDictionary(s => $"actionObject {s.o} '{s.l.Key}'", s => $"actionObject {s.o} '{s.l.Value}'")
                    .Aggregate(filter, (current, value) => current.Replace(value.Key, value.Value));
            }

            var dbArgs = new DynamicParameters();
            var whereStatements = new List<string>();

            dbArgs.Add("assetUid", assetUid, DbType.Guid);
            dbArgs.Add("assetTypeUid", assetTypeUid, DbType.Guid);
            dbArgs.Add("action", action, DbType.String);
            dbArgs.Add("startDate", startDate, DbType.DateTime2);
            dbArgs.Add("endDate", endDate, DbType.DateTime2);

            DynamicParameters advFilterArgs = null;
            List<string> advFilterStatements = null;
            ParseAdvancedFilterQueryParameter(CompanyContext, filter, fieldList, out advFilterArgs, out advFilterStatements);

            if (advFilterArgs != null && advFilterStatements != null)
            {
                dbArgs.AddDynamicParams(advFilterArgs);
                whereStatements.AddRange(advFilterStatements);
            }

            if (orderByList.Count == 0)
            {
                orderByList = new[] { OrderByModel.Create("date", OrderByDirectionEnum.Descending) };
            }

            foreach (var orderBy in orderByList)
            {
                ValidateOrderByColumnName(orderBy.ColumnName, fieldList);
            }

            return (viewName, parameters: dbArgs, whereStatements, orderByList);
        }

        private async Task<IReadOnlyList<AssetAuditApiItemModel>> PostProcessAuditCollectionAsync(IReadOnlyList<AssetAuditApiItemModel> list)
        {
            await Task.CompletedTask;
            var result = list.ToArray();

            //Translate actionObject values
            foreach (var entity in result)
            {
                if (new[] { "Artifact", "ArtifactType" }.Contains(entity.actionObject))
                {
                    if (entity.@class == 1)
                    {
                        entity.actionObject = "Business Asset";
                        entity.actionDescription = entity.actionDescription.Replace("Artifact", "Business Asset");
                    }
                    else if (entity.@class == 8)
                    {
                        entity.actionObject = "Technical Asset";
                        entity.actionDescription = entity.actionDescription.Replace("Artifact", "Technical Asset");
                    }
                }
                else if (ActionObjectDictionary.ContainsKey(entity.actionObject))
                {
                    entity.actionObject = ActionObjectDictionary[entity.actionObject];
                }
            }

            return result;
        }

        public async Task<IReadOnlyList<AssetAuditApiItemModel>> AuditViewAsync(Guid? assetUid, Guid? assetTypeUid, string action, DateTime? startDate, DateTime? endDate, string filter, IReadOnlyList<OrderByModel> orderByList)
        {
            var preparedParameters = await PrepareAuditViewParametersAsync(assetUid, assetTypeUid, action, startDate, endDate, filter, orderByList);

            var result = await QueryDynamicResultsAsync<AssetAuditApiItemModel>(preparedParameters.viewName, preparedParameters.parameters, preparedParameters.whereStatementList, preparedParameters.orderByList);

            result = await PostProcessAuditCollectionAsync(result);

            return result;
        }
    }
}