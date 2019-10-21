using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d360.model.DataAccessLayer
{
    public class RelationshipRepository : IRelationshipRepository
    {
        ICompanyContext companyContext;
        IQueueSource QueueSource;
        IStorageProvider Storage;
        ICommunityContext communityContext;
        public RelationshipRepository(ICommunityContext communityContext, ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider)
        {
            this.companyContext = companyContext;
            this.QueueSource = queueSource;
            this.Storage = storageProvider;
            this.communityContext = communityContext;
        }

        public Intersect GetRelationshipByUID(Guid relationshipUid)
        {
            return this.companyContext.Filter<Intersect>(i => i.uid == relationshipUid).SingleOrDefault();
        }

        public IntersectType GetRelationshipTypeByUID(Guid relationshipTypUid)
        {
            return companyContext.Filter<IntersectType>(i => i.uid == relationshipTypUid).SingleOrDefault();
        }

        public async Task<IEnumerable<PredicateApiViewModel>> GetPredicates(Guid? PredicateUid = null, PredicateType? Type = null, string Name = null, string Inverse = null, bool? IsUsed = null)
        {
            string whereClause = string.Empty;
            List<string> whereConditions = new List<string>();
            var dbArgs = new DynamicParameters();

            if (PredicateUid.HasValue)
            {
                whereConditions.Add("P.Uid = @PredicateUid");
                dbArgs.Add("@PredicateUid", PredicateUid.Value);
            }

            if (Type.HasValue)
            {
                whereConditions.Add("P.Type = @Type");
                dbArgs.Add("@Type", Type.Value);
            }

            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrWhiteSpace(Name))
            {
                Name = Name.Trim().ToLower();
                whereConditions.Add("P.Name = @Name");
                dbArgs.Add("@Name", Name);
            }

            if (!string.IsNullOrEmpty(Inverse) && !string.IsNullOrWhiteSpace(Inverse))
            {
                Inverse = Inverse.Trim().ToLower();
                dbArgs.Add("@Inverse", Inverse);
                whereConditions.Add("P.Inverse = @Inverse");
            }

            if (IsUsed.HasValue)
            {
                if (IsUsed.Value)
                    whereConditions.Add("Usage.Id is not null");
                else whereConditions.Add("Usage.Id is null");
            }

            if (whereConditions.Count > 0)
                whereClause = $"WHERE {string.Join(" AND ", whereConditions)}";

            return await companyContext.QueryAsync<PredicateApiViewModel>($@"select 
                                                                             P.Uid,
                                                                             P.Name,
                                                                             P.Inverse,
                                                                             P.IsSystem,
                                                                             P.[Type],
                                                                             CASE
                                                                             WHEN Usage.Id is null then 0
                                                                             ELSE 1
                                                                             END AS IsInUse
                                                                            from[Predicate] P
                                                                            outer apply(select top 1 id from IntersectType where PredicateID = P.Id)Usage
                                                                            {whereClause}          
                                                                            order by[Type], Name", dbArgs);
        }

        public async Task<JObject> GetRelationships(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "")
        {
            var dbArgs = new DynamicParameters();

            var baseTableSql = @"from [Intersect] I 
inner join IntersectType T on T.ID = I.IntersectTypeID 
left join [Predicate] P on P.ID = T.PredicateID 
left join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID 
inner join AssetType ST on ( ( S.ID is not null and ST.ID = S.AssetTypeID ) or ( S.ID is null and ST.Object = I.Subject and ST.ObjectID = I.SubjectID ) )
left join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID 
inner join AssetType OT on ( ( O.ID is not null and OT.ID = O.AssetTypeID ) or ( O.ID is null and OT.Object = I.Object and OT.ObjectID = I.ObjectID ) ) ";
            whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + " coalesce(ST.ID,S.ID) is not null and coalesce(OT.ID,O.ID) is not null ";

            var countSql = baseTableSql;

            List<FieldType> fieldTypes = null;
            bool filteringByFields = false;
            int pageNumber = 1;
            int pageSize = 250;

            whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" S.ID not in ({companyContext.GetNoReadSqlStatement(Permission.ReadRelationships)}) and S.AssetTypeID not in ({companyContext.GetAssetTypeNoReadSqlStatement(Permission.ReadRelationships)})";
            whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" O.ID not in ({companyContext.GetNoReadSqlStatement(Permission.ReadRelationships)}) and O.AssetTypeID not in ({companyContext.GetAssetTypeNoReadSqlStatement(Permission.ReadRelationships)})";

            if (queryParams != null)
            {
                var queryParamsList = queryParams.ToList();

                if (queryParamsList.Any(q => q.Key.ToLower() == "relationshiptypeuid"))
                {
                    Guid relationshipTypeUid;
                    var relationshipTypeUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
                    if (Guid.TryParse(relationshipTypeUidString, out relationshipTypeUid))
                    {
                        dbArgs.Add("@relationshiptypeuid", relationshipTypeUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" T.[Uid] = @relationshiptypeuid";
                        //countSql += $" inner join IntersectType T on T.ID = I.IntersectTypeID";
                        fieldTypes = companyContext.Query<FieldType>("select F.* from FieldType F inner join IntersectType I on F.Object = 'IntersectType' and I.ID = F.ObjectID and I.[Uid] = @relationshipTypeUid", new { relationshipTypeUid }).ToList();
                    }
                }
                if (queryParamsList.Any(q => q.Key.ToLower() == "state"))
                {
                    State state;
                    var stateString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "state").Value;
                    if (Enum.TryParse(stateString, out state))
                    {
                        dbArgs.Add("@state", state);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" I.[State] = @state";
                    }
                }
                if (queryParamsList.Any(q => q.Key.ToLower() == "predicateuid"))
                {
                    Guid predicateUid;
                    var predicateUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "predicateuid").Value;
                    if (Guid.TryParse(predicateUidString, out predicateUid))
                    {
                        dbArgs.Add("@predicateuid", predicateUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (P.Uid = @predicateuid)";
                        //if (!countSql.Contains("inner join IntersectType T"))
                        //{
                        //    countSql += $" inner join IntersectType T on T.ID = I.IntersectTypeID";
                        //}
                        //countSql += $" inner join [Predicate] P on P.ID = T.PredicateID and P.[Uid] = @predicateuid";
                    }
                }
                if (queryParamsList.Any(q => q.Key.ToLower() == "subjectuid"))
                {
                    Guid subjectUid;
                    var subjectUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "subjectuid").Value;
                    if (Guid.TryParse(subjectUidString, out subjectUid))
                    {
                        dbArgs.Add("@subjectuid", subjectUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (S.Uid = @subjectuid)";
                    }
                }
                if (queryParamsList.Any(q => q.Key.ToLower() == "objectuid"))
                {
                    Guid objectUid;
                    var objectUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "objectuid").Value;
                    if (Guid.TryParse(objectUidString, out objectUid))
                    {
                        dbArgs.Add("@objectuid", objectUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (O.Uid = @objectuid)";
                    }
                }
                if (queryParamsList.Any(q => q.Key.ToLower() == "_pagenum"))
                {
                    var pageNumberString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_pagenum").Value;
                    if (!int.TryParse(pageNumberString, out pageNumber))
                    {
                        pageNumber = 1;
                    }
                }
                if (queryParamsList.Any(q => q.Key.ToLower() == "_pagesize"))
                {
                    var pageSizeString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_pagesize").Value;
                    if (!int.TryParse(pageSizeString, out pageSize))
                    {
                        pageSize = 250;
                    }
                }

                // Now deal with dynamic field filters
                if (fieldTypes != null)
                {
                    var avoidFields = new List<string> { "relationshiptypeuid", "subjectuid", "objectuid", "predicateuid", "_pagenum", "_pagesize", "state" };
                    queryParamsList.ForEach(qp =>
                    {
                        if (!avoidFields.Contains(qp.Key.ToLower()))
                        {
                            var fieldType = fieldTypes.FirstOrDefault(i => i.Name.ToLower() == qp.Key.ToLower());
                            if (fieldType != null)
                            {
                                whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $@" case 
 when FT{fieldType.ID}.AllowAllValue = 1 and F{fieldType.ID}.Value = '0' then cast(FT{fieldType.ID}.AllowAllLabel as nvarchar(max))
 when F{fieldType.ID}.FormattedValue is not null then F{fieldType.ID}.FormattedValue
 when FT{fieldType.ID}.DefaultFormattedValue is not null then cast(FT{fieldType.ID}.DefaultFormattedValue as nvarchar(max))
end = @f{fieldType.ID}Value";
                                dbArgs.Add($"@f{fieldType.ID}Value", qp.Value);
                                filteringByFields = true;
                            }
                        }
                    });
                }
            }

            var fieldColumns = "";
            var fieldJoins = "";

            if (fieldTypes != null)
            {
                fieldColumns = string.Join(",", fieldTypes.Select(f => $@"case 
 when FT{f.ID}.AllowAllValue = 1 and F{f.ID}.Value = '0' then cast(FT{f.ID}.AllowAllLabel as nvarchar(max)) 
 when F{f.ID}.FormattedValue is not null then F{f.ID}.FormattedValue
 when FT{f.ID}.DefaultFormattedValue is not null then cast(FT{f.ID}.DefaultFormattedValue as nvarchar(max))
 else null
end as {f.Name}"));
                fieldColumns += string.IsNullOrEmpty(fieldColumns) ? "" : ",";
                fieldJoins = " " + string.Join(" ", fieldTypes.Select(f => $"inner join FieldType FT{f.ID} on FT{f.ID}.ID = {f.ID} left join Field F{f.ID} on F{f.ID}.ObjectType = 'Intersect' and F{f.ID}.ObjectID = I.ID and F{f.ID}.FieldTypeID = FT{f.ID}.ID"));
            }

            if (pageNumber < 0)
            {
                pageNumber = 1;
            }
            if (pageSize < 0 || pageSize > 250)
            {
                pageSize = 250;
            }

            dbArgs.Add("@pageNum", pageNumber);
            dbArgs.Add("@pageSize", pageSize);

            var stateSql = "case I.State ";
            State.Active.GetList().ForEach(s =>
            {
                stateSql += $"when {(int)s.ID} then '{s.ID.ToString()}' ";
            });
            stateSql += " end as State, ";

            var predicateTypeSql = "case P.Type ";
            PredicateType.DataLineage.GetAsList().ForEach(p =>
            {
                predicateTypeSql += $"when {(int)p.ID} then '{p.ID.ToString()}' ";
            });
            predicateTypeSql += " end as 'Predicate.Type', ";


            var sql = $@"
declare @total int
select	@total = count(1) {countSql} {(filteringByFields ? fieldJoins : "")} {whereClause}

select	@pageSize as 'pageSize',
		@pageNum as 'pageNum',
		@total as 'total',
		(
		select	I.Uid,
				T.Uid as RelationshipTypeUid,
				{stateSql}
				{fieldColumns}
				P.UID as 'Predicate.Uid',
				{predicateTypeSql}
				P.Name as 'Predicate.Name',
				P.Inverse as 'Predicate.Inverse',
				S.Uid as 'Subject.Uid',
				ST.Uid as 'Subject.AssetTypeUid',
				O.Uid as 'Object.Uid',
				OT.Uid as 'Object.AssetTypeUid'
		{baseTableSql}
                {fieldJoins} 
        {whereClause} 
        order by I.IntersectTypeID
		offset ((@pageNum-1) * @pageSize) rows fetch next @pageSize rows only
		for json path
		) as 'items'
for json path, WITHOUT_ARRAY_WRAPPER";

            var models = await companyContext.GetDatabaseJsonAsObjectAsync<JObject>(sql, dbArgs);

            return models;
        }

        public IQueryable<IntersectType> GetIntersectTypeById(int id)
        {
            return companyContext.Filter<IntersectType>(i => i.ID == id);
        }

        public IntersectType GetIntersectTypeByUid(Guid intersectTypeUid)
        {
            return companyContext.Filter<IntersectType>(i => i.uid == intersectTypeUid).SingleOrDefault();
        }

        public async Task<List<IntersectTypeApiViewModel>> GetRelationshipTypes(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "")
        {
            var dbArgs = new DynamicParameters();

            if (queryParams != null)
            {
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "predicateuid"))
                {
                    Guid predicateUid;
                    var predicateUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "predicateuid").Value;
                    if (Guid.TryParse(predicateUidString, out predicateUid))
                    {
                        dbArgs.Add("@predicateUid", predicateUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" P.[UID] = @predicateUid";
                    }
                }
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "assettypeuid"))
                {
                    Guid assetTypeUid;
                    var assetTypeUidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "assettypeuid").Value;
                    if (Guid.TryParse(assetTypeUidString, out assetTypeUid))
                    {
                        dbArgs.Add("@assettypeuid", assetTypeUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (S.Uid = @assettypeuid OR O.Uid = @assettypeuid)";
                    }
                }
                if (queryParams.ToList().Any(q => q.Key.ToLower() == "state"))
                {
                    State state;
                    var stateString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "state").Value;
                    if (Enum.TryParse(stateString, out state))
                    {
                        dbArgs.Add("@state", state);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" I.State = @state";
                    }
                }
            }

            if (!communityContext.IsFusionEnabled())
            {
                List<string> filteredObjects = new List<string>();
                filteredObjects.Add(SystemObjects.FusionAttributeType.ToString());
                filteredObjects.Add(SystemObjects.FusionQueryAttributeType.ToString());
                string notInSql = $"not in ({string.Join(",", filteredObjects.Select(x => "'" + x + "'"))})";

                whereClause += $" and (I.Object {notInSql} and I.Subject {notInSql})";
            }



            var sql = $@"
select	I.Id,
        I.Uid,
		I.State as State,
        coalesce(I.IsSystem, 0) as IsSystem,
		P.UID as 'Predicate.Uid',
		coalesce(P.[Type],0) as 'Predicate.Type',
		coalesce(P.Name,'') as 'Predicate.Name',
		coalesce(P.Inverse,'') as 'Predicate.Inverse',
		coalesce(SI.Uid, S.Uid) as 'Subject.Uid',
		case 
			when I.Subject = 'IntersectType' then SI.SubjectName + ' ' + SI.PredicateName + ' ' + SI.ObjectName + ' relationship'
			else coalesce(SFT.Name + ' / ','') + coalesce(SP.[Path], S.Name)
		end as 'Subject.Name',
		coalesce(S.Class, 0) as 'Subject.Class',
		I.SubjectCardinality as 'Subject.Cardinality',
		O.Uid as 'Object.Uid',
		coalesce(OFT.Name + ' / ','') + coalesce(OP.[Path], O.Name)  as 'Object.Name',
		coalesce(O.Class, 0) as 'Object.Class',
		I.ObjectCardinality as 'Object.Cardinality'
from	IntersectType I
		left join [Predicate] P on P.ID = I.PredicateID

		left join AssetType S on (S.uid = I.SubjectUid OR (S.Object = I.Subject and S.ObjectID = I.SubjectID))
        left join FusionAttributeType SFAT on I.Subject = 'FusionAttributeType' and SFAT.ID = I.SubjectID 
        left join FusionType SFT on SFT.ID = SFAT.FusionTypeID 
        outer apply dbo.GetAssetTypeTextPathById(S.ID, '/') SP

		left join IntersectTypeDetail SI on I.Subject = 'IntersectType' and SI.ID = I.SubjectID
		left join AssetType O on (O.uid = I.ObjectUid OR (O.Object = I.Object and O.ObjectID = I.ObjectID))
        left join FusionAttributeType OFAT on I.Object = 'FusionAttributeType' and OFAT.ID = I.ObjectID 
        left join FusionType OFT on OFT.ID = OFAT.FusionTypeID 
        outer apply dbo.GetAssetTypeTextPathById(O.ID, '/') OP
        {whereClause} for json path";

            var models = await companyContext.GetDatabaseJsonAsObjectAsync<List<IntersectTypeApiViewModel>>(sql, dbArgs);

            return models;
        }

        public Task<List<IntersectTypeApiViewModel>> GetActiveIntersectTypesByObjectType(int id, SystemObjects type)
        {
            return this.GetRelationshipTypes(null, $"where I.State = 1 and ((I.SubjectID = {id} and I.[Subject] = '{type.ToString()}') or (I.ObjectID = {id} and I.Object = '{type.ToString()}'))");
        }

        public async Task<ApiExecutionInfo> BulkPostRelationships(Guid intersectTypeUid, RelationshipInserts relationships, Func<int, object, int, int, ApiExecution> getApiExecution, bool triggerWorkflow = false)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = companyContext.CurrentCompanyID,
                ResourceID = companyContext.CurrentResourceID,
                CompanyDomainPrefix = companyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                Action = ApiExecutionAction.PostRelationships,
                SendWorkflowEvents = triggerWorkflow
            };

            Storage.CreateFolder(executionInfo.StorageFolder);
            Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(relationships));

            await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);
            var execution = getApiExecution(relationships.Count, new ApiExecutionFields_PostRelationships { IntersectTypeUid = intersectTypeUid }, 0, 0);
            execution.ExecutionID = executionInfo.ExecutionID;
            companyContext.Add(execution);
            return executionInfo;
        }

        public IEnumerable<dynamic> GetExportModel(int id)
        {
            return companyContext.Query<dynamic>(
                @"select 
                    ID,
                    [Subject], 
                    SubjectID, 
                    SubjectUid,
                    SubjectName, 
                    SubjectTypeName, 
                    [Object], 
                    ObjectID, 
                    ObjectUid,
                    ObjectName, 
                    ObjectTypeName, 
                    PredicateName 
                from 
                    intersectdetail 
                where intersecttypeid = @id", new { id = id });
        }

        public IEnumerable<dynamic> GetExportModelWithCustomFields(int id, IEnumerable<string> customColumns)
        {
            var customColumnName = "[" + customColumns.Aggregate((x, y) => x + "],[" + y) + "]";
            var CteColumnName = "CTE.[" + customColumns.Aggregate((x, y) => x + "],CTE.[" + y) + "]";


            var sql = @"WITH CTE (ObjectID, " + customColumnName +
                ") AS ( SELECT ObjectId, " + customColumnName +
                " FROM ( select f2.ObjectID, f.FriendlyName,FormattedValue from fieldtype f  " +
                "inner join field f2 on f2.fieldtypeid = f.id where f.[object] = 'IntersectType'" +
                " and f.objectid = @id  ) as PivotData " +
                "PIVOT (max(FormattedValue) FOR FriendlyName IN (" + customColumnName + ") ) AS PivotResult) " +
                "select i.ID, i.[Subject],i.SubjectID, i.SubjectName, i.SubjectTypeName, i.[Object], " +
                "i.ObjectID, i.ObjectName, i.ObjectTypeName, i.PredicateName , " + CteColumnName +
                " from  intersectdetail as i left join CTE  on CTE.ObjectID =i.id where intersecttypeid=@id ";
            var models = companyContext.Query<dynamic>(sql, new { id = id });
            return models;
        }

        public bool AnyExists(Guid uid)
        {
            return companyContext.Any<IntersectType>(i => i.uid == uid);
        }

        public bool AnyPredicateExists(Guid uid)
        {
            return companyContext.Any<Predicate>(i => i.UID == uid);
        }

        public List<DatabaseBulkAssetResult> GetBulkResults(ApiExecutionInfo info)
        {
            List<DatabaseBulkAssetResult> results = null;
            try
            {
                var resultsJson = Storage.GetFileContentsAsString(info.StorageFolder, info.ResponseFileName);
                results = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(resultsJson);
            }
            catch
            {
            }

            return results;
        }

        public async Task<List<DatabaseBulkRelationshipResult>> DeleteRelationships(ApiExecution execution, IntersectType intersectType, RelationshipDeletes relationships, int timeout = 3600, bool triggerWorkflow = false)
        {
            return companyContext.DeleteRelationships(execution, intersectType, relationships, timeout, triggerWorkflow);
        }

        public async Task<ApiExecutionInfo> BulkDeleteRelationships(Guid intersectTypeUid, RelationshipDeletes relationships, Func<int, object, int, int, ApiExecution> getApiExecution, bool triggerWorkflow = false)
        {
            var executionInfo = new ApiExecutionInfo
            {
                CompanyID = companyContext.CurrentCompanyID,
                ResourceID = companyContext.CurrentResourceID,
                CompanyDomainPrefix = companyContext.CurrentCompanyDomain,
                ExecutionID = Guid.NewGuid(),
                Action = ApiExecutionAction.DeleteRelationships,
                SendWorkflowEvents = triggerWorkflow
            };

            Storage.CreateFolder(executionInfo.StorageFolder);
            Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(relationships));

            await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);
            var execution = getApiExecution(relationships.Count, new ApiExecutionFields_DeleteRelationships { IntersectTypeUid = intersectTypeUid }, 0, 0);
            execution.ExecutionID = executionInfo.ExecutionID;
            companyContext.Add(execution);
            return executionInfo;
        }

        public List<PredicateDeleteResult> DeletePredicates(PredicateDeletes predicates, ApiExecution execution)
        {
            companyContext.Add(execution);

            List<PredicateDeleteResult> results = null;
            try
            {
                results = companyContext.RemovePredicates(execution, predicates);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }

            return results;
        }

        public List<PredicateUpsertResult> UpsertPredicates(PredicateUpserts predicates, ApiExecution execution)
        {
            companyContext.Add(execution);

            List<PredicateUpsertResult> results = null;
            try
            {
                results = companyContext.UpdatePredicates(execution, predicates);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }

            return results;
        }

        public async Task<bool> IsTransformPredicateExists(int assetTypeId)
        {
            string sql = @"
                           Select A.Name from AssetType A
                        where Id=@Id
                        and 
                        (
                            exists (select 1 from IntersectType I
	                        inner join [Predicate] P on
	                        P.Id = I.PredicateID
                            where P.[Type]  = @type and I.[Subject] = A.[Object] and I.SubjectID = A.ObjectID )
                            or exists (select 1 from IntersectType I
	                        inner join [Predicate] P on
	                        P.Id = I.PredicateID
                            where P.[Type] = @type and I.[Object] = A.[Object] and I.ObjectID = A.ObjectID )
                        )     ";
            var result = await companyContext.QueryAsync<string>(sql, new { id = assetTypeId, type=(int) PredicateType.Transformation });
            return  string.IsNullOrEmpty(result.FirstOrDefault()) ? false : true;
        }
        public List<RelationshipTypeResult> PostRelationshipTypes(List<RelationshipTypeInsert> relationshipTypes,  ApiExecution execution)
        {
            companyContext.Add(execution);

            List<RelationshipTypeResult> results = null;
            try
            {
                results = companyContext.ImportRelationshipTypes(execution, relationshipTypes);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }

            return results;
        }
        
        public List<RelationshipTypeResult> PutRelationshipTypes(List<RelationshipTypeUpdate> relationshipTypes, ApiExecution execution)
        {
            companyContext.Add(execution);

            List<RelationshipTypeResult> results = null;
            try
            {
                results = companyContext.ImportRelationshipTypes(execution, relationshipTypes);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }

            return results;
        }

        public List<RelationshipTypeResult> DeleteRelationshipTypes(List<RelationshipTypeDelete> relationshipTypes, ApiExecution execution) {
            companyContext.Add(execution);

            List<RelationshipTypeResult> results = null;
            try
            {
                results = companyContext.DeleteRelationshipTypes(execution, relationshipTypes);

                // Close execution record.
                execution.Processed = results.Count;
                execution.Error = results.Count(i => !i.Success);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }

            return results;
        }
    }
}
