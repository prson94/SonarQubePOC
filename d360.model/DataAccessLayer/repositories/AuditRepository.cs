using d360.core;
using d360.core.entities;
using d360.core.Models;
using d360.extensions;
using d360.model.helpers.filters;
using Dapper;
using repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer.repositories
{
	internal sealed class AuditRepository : DapperRepositoryBase<ICompanyDbConnectionProvider>, IAuditRepository
    {
		internal IQueueSource QueueSource;
		internal ISecurityContextProvider SecurityContext;

		// this property is added only to support code which should be fixed in GOV-16916
		private ICompanyContext CompanyContext { get; }

        public AuditRepository(IDapperQueryComposer<ICompanyDbConnectionProvider> queryComposer, ICompanyContext companyContext, ISecurityContextProvider securityContext, IQueueSource queueSource) : base(queryComposer)
        {
            CompanyContext = companyContext;
			QueueSource = queueSource;
			SecurityContext = securityContext;
        }

        private readonly Dictionary<string, string> ActionObjectDictionary = new Dictionary<string, string>
        {
            { "Intersect", "Relationship" },
            { "IntersectType", "RelationshipType" },
            { "Taxonomy", "Model" },
            { "TaxonomyType", "ModelType" },
            { "ResponsibilityTypeRelationOverrideItem", "Responsibility Type Relation Override Item" },
        };
		public async Task CreateHistoryJob(ObjectInfo info)
		{
			info.ResourceId =  SecurityContext.ResourceID;
			await QueueSource.CreateMessageAsync(constants.Queue.PostExecution,
				new PostExecutionQueueMessage
				{
					Action = PostExecutionQueueMessageAction.History,
					CompanyID = SecurityContext.CompanyID,
					ExecutionId = -1,
					ObjectInfo = info
				});
		}

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
			bool IncludeTotal = false;

			if ((assetUid != null && assetUid != Guid.Empty) ||
				(assetTypeUid != null && assetTypeUid != Guid.Empty))
			{
				IncludeTotal = true;
			}
			var preparedParameters = await PrepareAuditViewParametersAsync(assetUid, assetTypeUid, action, startDate, endDate, filter, orderByList);

			var result = await QueryDynamicPagedResultsAsync<AssetAuditApiItemModel>(preparedParameters.viewName, preparedParameters.parameters, preparedParameters.whereStatementList, preparedParameters.orderByList, pageNum, pageSize, CompanyContext.ApiTimeout, IncludeTotal);

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
			string viewnamerepl = "dbo.AuditViewCustomFilter";

			if (startDate.HasValue && endDate.HasValue && !assetTypeUid.HasValue && !assetUid.HasValue)
			{
				if ((endDate.Value - startDate.Value).TotalDays <= 10)
				{
					viewnamerepl = await GetAuditDateRangeQuery();
				}
			}

			var viewName = @$"SELECT * 
							  FROM {viewnamerepl}
							  WHERE (@assetUid IS NULL OR ActionAssetUid = @assetUid)
							  AND(@assetTypeUid IS NULL OR ActionAssetTypeUid = @assetTypeUid)
							  AND(@action IS NULL OR Action = @action)
							  AND(@startDate IS NULL OR Date >= @startDate)
							  AND(@endDate IS NULL OR Date <= @endDate)";

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
                new DefaultFilter("fieldType", "A.FieldType", SqlFieldType.Text),
				new DefaultFilter("auditid", "A.AuditID", SqlFieldType.Number)
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

            ParseAdvancedFilterQueryParameter(CompanyContext, filter, fieldList, out DynamicParameters advFilterArgs, out List<string> advFilterStatements);

            if (advFilterArgs != null && advFilterStatements != null)
            {
                dbArgs.AddDynamicParams(advFilterArgs);
                whereStatements.AddRange(advFilterStatements);
            }

            if (orderByList.Count == 0)
            {
                orderByList = new[] { OrderByModel.Create("AuditID", OrderByDirectionEnum.Descending) };
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

            var result = await QueryDynamicResultsAsync<AssetAuditApiItemModel>(preparedParameters.viewName, preparedParameters.parameters, preparedParameters.whereStatementList, preparedParameters.orderByList, CompanyContext.ApiTimeout);

            result = await PostProcessAuditCollectionAsync(result);

            return result;
        }

		public async Task<string> GetAuditDateRangeQuery()
		{
			string query = @"select count(1)
							from sys.indexes
							where name = 'IX_ReportingAudit_Date_Include'
							and object_id= object_id(N'Reporting.Global_Audit')";

			int isExists = await CompanyContext.QueryFirstOrDefaultAsync<int>(query);

			if (isExists == 0)
			{
				return "dbo.AuditViewCustomFilter";
			}
			else
			{
				return @"(
SELECT 	ad.uid AS [Uid],
		ad.DisplayValue as [Name],
		r.uid as [ResourceUid],
		CAST(IIF ( R.State = 3, 1, 0 ) AS BIT) AS [ResourceIsDeleted],
		R.FirstName + ' ' + R.LastName AS [ResourceName],
		ga.Date as [Date],
		ga.action as [Action],
		ActionA.uid as [ActionAssetUid],
		ActionAT.uid as [ActionAssetTypeUid],
		CASE 
			WHEN ga.ActionObject = 'Intersect' then 'Relationship'
			WHEN ga.ActionObject = 'IntersectType' then 'RelationshipType'
			ELSE ga.ActionObject
		END AS [ActionObject],
		CASE 
			WHEN ga.ActionObjectTypeName = 'Intersect Type'
			THEN 'Relationship Type'
			WHEN ga.ActionObjectTypeName = '$IntersectTypeName'
			THEN COALESCE(RelationPlaceHolderResolve.Relationship_ActionObjectTypeName,'--RelationShip Not Found--')
			ELSE ga.ActionObjectTypeName
		END AS [ActionObjectTypeName],
		case when ga.ActionObjectName = '$IntersectName' then COALESCE(RelationPlaceHolderResolve.Relationship_ActionObjectName,'--RelationShip Not Found--') else ga.actionObjectName end AS [ActionObjectName],
		replace(ga.actionDescription,'$IntersectTypeName','Relationship') AS [ActionDescription],
		COALESCE(FT.FriendlyName,fa.FieldName) as [Field],
		CASE 
			WHEN ga.Action = 'Tag Consolidate' THEN ga.ObjectName
			ELSE fa.Value
		END AS [NewValue],
		COALESCE(AT.Class, AD.AssetTypeClass) AS [Class],
		ga.[Version],
		CASE 
			WHEN ga.Action  = 'Tag Consolidate' THEN ga.ActionObjectName
			ELSE fa.PreviousValue
		END AS [PreviousValue],
		ft.[Type] as [FieldType],
		ga.id Auditid
FROM 	reporting.global_audit ga with (index(IX_ReportingAudit_Date_Include))
		LEFT JOIN reporting.global_fieldaudit fa on ( fa.auditid = ga.id)
		left JOIN [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
		LEFT JOIN AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID
		LEFT JOIN Asset ActionA on ActionA.Object = ga.ActionObject and ActionA.ObjectID = ga.ActionObjectID
		LEFT JOIN AssetType ActionAT on ActionA.AssetTypeID = ActionAT.ID
		LEFT JOIN FieldType FT on FT.ID = fa.FieldTypeID
		outer apply (
					Select I.SubjectName Relationship_ActionObjectName,
					I.SubjectTypeName + ' (' + I.PredicateInverse + ')'  as Relationship_ActionObjectTypeName
					From IntersectDetail I
					Where I.ID = ga.ActionObjectID and I.Object = ga.Object and I.ObjectID = ga.ObjectID and ga.ActionObjectName = '$IntersectName'
					union all
					Select I.ObjectName Relationship_ActionObjectName,
					I.ObjectTypeName + ' (' + I.PredicateName + ')'  as Relationship_ActionObjectTypeName
					From IntersectDetail I
					Where I.ID = ga.ActionObjectID and I.Subject = ga.Object and I.SubjectID = ga.ObjectID and ga.ActionObjectName = '$IntersectName'
					) RelationPlaceHolderResolve
			cross apply (
			select uid, DisplayValue, Object, objectid, AssetTypeClass 
			from AssetDetail a where ((a.Object = ga.Object and a.objectid = ga.ObjectID) or (a.Object = ga.ActionObject and a.objectid = ga.ActionObjectID))
			union  all
			select uid, value as DisplayName, 'Tag' as Object, id as ObjectID, 11 as AssetTypeClass 
			from Tag t where ((ga.Object = 'Tag' and t.id = ga.ObjectID) or (ga.ActionObject = 'Tag' and t.id = ga.ActionObjectID))
			union  all
			select uid, name as DisplayName, 'IssueType' as Object, id as ObjectID, null as AssetTypeClass 
			from dbo.IssueType where ((ga.Object = 'IssueType' and id = ga.ObjectID) or (ga.ActionObject = 'IssueType' and id = ga.ActionObjectID))
			union  all
			select uid, itn.name as DisplayValue, 'IntersectType' as Object, id as ObjectID, null as AssetTypeClass 
			from dbo.[IntersectType] IT
			outer APPLY dbo.GetIntersectTypeNames(IT.ID) ITN  
			where ((ga.Object = 'IntersectType' and IT.id = ga.ObjectID) or (ga.ActionObject = 'IntersectType' and IT.id = ga.ActionObjectID))
			union  all
			select uid, name as DisplayName, 'ResponsibilityType' as Object, id as ObjectID, null as AssetTypeClass 
			from dbo.ResponsibilityType rt where ((ga.Object = 'ResponsibilityType' and rt.id = ga.ObjectID) or (ga.ActionObject = 'ResponsibilityType' and rt.id = ga.ActionObjectID))
			union all
			select uid, name as DisplayName, 'Report' as Object, id as ObjectID, null as AssetTypeClass 
			from dbo.[Report] r where ((ga.Object = 'Report' and r.id = ga.ObjectID) or (ga.ActionObject = 'Report' and r.id = ga.ActionObjectID))
			union all
			select MA.uid, AT.Name as DisplayName, 'MetricAllocation' as Object, MA.ID as ObjectID, null as AssetTypeClass 
			from metrics.Allocation MA 
			inner join [dbo].[AssetType] AT on AT.uid = MA.AssetTypeUid 
			where ((ga.Object = 'MetricAllocation' and MA.id = ga.ObjectID) or (ga.ActionObject = 'MetricAllocation' and MA.id = ga.ActionObjectID))
			union all
			select uid, name as DisplayName, 'Predicate' as Object, id as ObjectID, null as AssetTypeClass 
			from dbo.[Predicate] p where ((ga.Object = 'Predicate' and p.id = ga.ObjectID) or (ga.ActionObject = 'Predicate' and p.id = ga.ActionObjectID))
			union all
			select uid, name as DisplayName, Object, ObjectID, Class as AssetTypeClass 
			from dbo.AssetType atc where ((atc.Object = ga.Object and atc.objectid = ga.ObjectID) or (atc.Object = ga.ActionObject and atc.objectid = ga.ActionObjectID))
			union all
			select uid, Name as DisplayName, 'Semantic' as Object, S.id as ObjectID, null as AssetTypeClass 
			from dbo.Semantic S where ((ga.Object = 'Semantic' and S.id = ga.ObjectID) or (ga.ActionObject = 'Semantic' and s.id = ga.ActionObjectID))
		) AD
	 ) az ";
			}
		}

	}
}
