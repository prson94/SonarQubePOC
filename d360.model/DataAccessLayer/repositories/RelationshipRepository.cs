using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using Newtonsoft.Json.Linq;

namespace d360.model.DataAccessLayer { 
    public class RelationshipRepository : IRelationshipRepository
    {
        ICompanyContext companyContext;
        public RelationshipRepository(ICompanyContext companyContext)
        {
            this.companyContext = companyContext;
        }
        public IntersectType GetRelationshipByUID(Guid relationshipTypUid)
        {
            return companyContext.Filter<IntersectType>(i => i.uid == relationshipTypUid).SingleOrDefault();
        }
        public async Task<IEnumerable<PredicateApiViewModel>> GetPredicates()
        {
            return await companyContext.QueryAsync<PredicateApiViewModel>("select Uid, Name, Inverse, IsSystem, [Type] from [Predicate] order by [Type], Name");
        }

        public async Task<JObject> GetRelationships(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "")
        {
            var dbArgs = new DynamicParameters();

            var countSql = "from [Intersect] I inner join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID inner join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID";

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
                        countSql += $" inner join IntersectType T on T.ID = I.IntersectTypeID";
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
                        if (!countSql.Contains("inner join IntersectType T"))
                        {
                            countSql += $" inner join IntersectType T on T.ID = I.IntersectTypeID";
                        }
                        countSql += $" inner join [Predicate] P on P.ID = T.PredicateID and P.[Uid] = @predicateuid";
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
		from	[Intersect] I
				inner join IntersectType T on T.ID = I.IntersectTypeID
				left join [Predicate] P on P.ID = T.PredicateID
				inner join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID
				inner join AssetType ST on ST.ID = S.AssetTypeID
				inner join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID
				inner join AssetType OT on OT.ID = O.AssetTypeID
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
    }
}
