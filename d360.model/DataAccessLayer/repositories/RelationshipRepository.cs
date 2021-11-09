using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers;
using d360.model.helpers.filters;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SpreadsheetLight;
using d360.utils.excel;
using d360.core.resources;
using SmartFormat;

namespace d360.model.DataAccessLayer
{
    public class RelationshipRepository : BaseRepository, IRelationshipRepository
    {
        ICompanyContext companyContext;
        IQueueSource QueueSource;
        IStorageProvider Storage;
        ICommunityContext communityContext;
        public RelationshipRepository(ICommunityContext communityContext, ICompanyContext companyContext, IQueueSource queueSource, IStorageProvider storageProvider)
            : base(companyContext)
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
                {
                    whereConditions.Add("Usage.Id is not null");
                }
                else
                {
                    whereConditions.Add("Usage.Id is null");
                }
            }

            if (whereConditions.Count > 0)
            {
                whereClause = $"WHERE {string.Join(" AND ", whereConditions)}";
            }

            var allPredicates = await companyContext.QueryAsync<PredicateApiViewModel>($@"select 
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
                                                                            order by[Type], Name", dbArgs, ApiTimeout);
            return allPredicates;
        }

        public async Task<JObject> GetRelationships(IEnumerable<KeyValuePair<string, string>> queryParams, string whereClause = "", bool isExport = false)
        {
            var dbArgs = new DynamicParameters();
            bool includeTotal = true;
            bool includeAssetPath = false;
            bool orderByAssetPath = false;
            bool listColorsAsJSON = false;

            string _orderBy = "I.IntersectTypeID,I.ID";
            string _orderDirection = "asc";

            Guid objectUid;
            Guid relationshipTypeUid;
            bool isSubject = false;

            var baseTableSql = @"from [Intersect] I 
inner join IntersectType T on T.ID = I.IntersectTypeID 
left join [Predicate] P on P.ID = T.PredicateID 
left join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID 
left join AssetType ST1 on S.ID is not null and ST1.ID = S.AssetTypeID
left join AssetType ST2 on S.ID is null and ST2.Object = I.Subject and ST2.ObjectID = I.SubjectID
left join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID 
left join AssetType OT1 on O.ID is not null and OT1.ID = O.AssetTypeID
left join AssetType OT2 on O.ID is null and OT2.Object = I.Object and OT2.ObjectID = I.ObjectID
";
            whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + " coalesce(ISNULL(ST1.ID, ST2.ID),S.ID) is not null and coalesce(ISNULL(OT1.ID,OT2.ID),O.ID) is not null ";

            var countSql = baseTableSql;

            List<FieldType> fieldTypes = null;
            bool filteringByFields = false;
            int pageNumber = 1;
            int pageSize = 250;

            whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" ISNULL(S.ID,0) not in ({companyContext.GetNoReadSqlStatement(Permission.ReadRelationships)}) and S.AssetTypeID not in ({companyContext.GetAssetTypeNoReadSqlStatement(Permission.ReadRelationships)})";
            whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" ISNULL(O.ID,0) not in ({companyContext.GetNoReadSqlStatement(Permission.ReadRelationships)}) and O.AssetTypeID not in ({companyContext.GetAssetTypeNoReadSqlStatement(Permission.ReadRelationships)})";

            if (queryParams != null)
            {
                var queryParamsList = queryParams.ToList();

                if (queryParamsList.Any(q => q.Key.ToLower() == "relationshiptypeuid"))
                {
                    //if the search is by intersecttypeid we should change the default order by to I.ID for consistent results
                    _orderBy = "I.ID";
                    var relationshipTypeUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "relationshiptypeuid").Value;
                    if (Guid.TryParse(relationshipTypeUidString, out relationshipTypeUid))
                    {
                        dbArgs.Add("@relationshiptypeuid", relationshipTypeUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" T.[Uid] = @relationshiptypeuid";
                        fieldTypes = companyContext.Query<FieldType>("select F.* from FieldType F inner join IntersectType I on F.Object = 'IntersectType' and I.ID = F.ObjectID and I.[Uid] = @relationshipTypeUid", new { relationshipTypeUid }, ApiTimeout).ToList();
                    }
                }
                if (queryParamsList.Any(k => k.Key.ToLower() == "_listcolorsasjson"))
                {
                    bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_listcolorsasjson").Value, out listColorsAsJSON);
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
                if (queryParamsList.Any(q => q.Key.ToLower() == "uid"))
                {
                    Guid uid;
                    var uidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "state").Value;
                    if (Guid.TryParse(uidString, out uid))
                    {
                        dbArgs.Add("@uid", uid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" I.[Uid] = @uid";
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
                    }
                }
                if (queryParamsList.Any(q => q.Key.ToLower() == "subjectuid"))
                {
                    Guid subjectUid;
                    var subjectUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "subjectuid").Value;
                    if (Guid.TryParse(subjectUidString, out subjectUid))
                    {
                        dbArgs.Add("@subjectuid", subjectUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (S.Uid = @subjectuid or (S.Uid is null and st2.uid = @subjectuid))";
                    }
                }
                if (queryParamsList.Any(q => q.Key.ToLower() == "objectuid"))
                {
                    var objectUidString = queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "objectuid").Value;
                    if (Guid.TryParse(objectUidString, out objectUid))
                    {
                        dbArgs.Add("@objectuid", objectUid);
                        whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $" (O.Uid = @objectuid or (O.Uid is null and ot2.uid = @objectuid))";
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

                if (queryParamsList.Any(q => q.Key.ToLower() == "_includetotal"))
                {
                    if (!bool.TryParse(queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_includetotal").Value, out includeTotal))
                    {
                        includeTotal = true;
                    }
                }

                if (queryParamsList.Any(q => q.Key.ToLower() == "_includepath"))
                {
                    if (!bool.TryParse(queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_includepath").Value, out includeAssetPath))
                    {
                        includeAssetPath = false;
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
                                whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") +
                                $@" case {(fieldType.AllowAllValue == true ? $"when F{fieldType.ID}.Value = '0' then @F{fieldType.ID}_AllValue " : "")}
                                    when F{fieldType.ID}.FormattedValue is not null then F{fieldType.ID}.FormattedValue
                                    {(!string.IsNullOrEmpty(fieldType.DefaultFormattedValue) ? $"else @defaultValueF{fieldType.ID} " : "")}
                                    end = @f{fieldType.ID}Value ";

                                dbArgs.Add($"@f{fieldType.ID}Value", qp.Value);
                                filteringByFields = true;
                            }
                        }
                    });
                }
            }

            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();

            if (fieldTypes != null)
            {
                getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns, "'Intersect'", "i.Id", listColorsAsJSON);
            }

            if (queryParams.Any(x => x.Key.ToLower() == "_order"))
            {
                var orderValue = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_order").Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);
                var joinColumn = fieldColumns.FirstOrDefault(x => x.ToLower().Contains($"[{orderValue}]"));
                if (!string.IsNullOrEmpty(joinColumn))
                {
                    _orderBy = joinColumn.Substring(0, joinColumn.IndexOf(" as ["));
                }
                else if (orderValue == "object.[path]")
                {
                    _orderBy = "ISNULL(ANDP_Object.DisplayPath,OT2.Name)";
                    isSubject = true;
                    orderByAssetPath = true;
                }
                else if (orderValue == "subject.[path]")
                {
                    _orderBy = "ISNULL(ANDP_Subject.DisplayPath,ST2.Name)";
                    orderByAssetPath = true;
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_simplefilter"))
            {
                var simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value.Trim();
                if (!string.IsNullOrEmpty(simpleFilter))
                {
                    filteringByFields = true;
                    simpleFilter = companyContext.GetEscapedFilterString(simpleFilter);

                    dbArgs.Add("@simpleFilter", simpleFilter);

                    List<string> simpleFilters = new List<string>();
                    //There may be multiple OwnershipLookup fields, but they all look to the same table for filtering, so that will be dealt with below
                    foreach (var ft in fieldTypes.Where(x => x.IsListable == true && x.Type != DataType.OwnershipLookup.ToString()))
                    {
                        if (ft.Type == DataType.Tag.ToString())
                        {
                            string simpleFilterTagSql = @"exists (select top 1 AT.TagId from AssetTag AT
						                                inner join Tag T on AT.TagId = T.Id
						                                where AT.AssetID = A.ID and T.Value like @simpleFilter)";

                            simpleFilters.Add(simpleFilterTagSql);
                        }
                        else if (ft.Type == DataType.Lookup.ToString() && ft.AllowAllValue)
                        {
                            string ftformatted = companyContext.LookupFieldHasColorItem(ft) ? $@"JSON_VALUE(F{ft.ID}.FormattedValue, '$[0].name')" : $@"F{ft.ID}.FormattedValue";
                            simpleFilters.Add($"(select case when F{ft.ID}.[Value] = '0' then @F{ft.ID}_AllValue else {ftformatted} end as value) like @simpleFilter");
                        }
                        else if (ft.Type == DataType.Lookup.ToString() && companyContext.LookupFieldHasColorItem(ft))
                        {
                            simpleFilters.Add($"JSON_VALUE(F{ft.ID}.FormattedValue, '$[0].name') like @simpleFilter");
                        }
                        else
                        {
                            simpleFilters.Add($"F{ft.ID}.FormattedValue like @simpleFilter");
                        }
                    }

                    if (includeAssetPath)
                    {
                        if (isSubject)
                        {
                            simpleFilters.Add($"ISNULL(ANDP_Object.DisplayPath,OT2.Name) like @simpleFilter");
                        }
                        else
                        {
                            simpleFilters.Add($"ISNULL(ANDP_Subject.DisplayPath,ST2.Name) like @simpleFilter");

                        }
                    }

                    whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + $"({string.Join(" or ", simpleFilters)})";
                }
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_filter"))
            {
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
                if (!string.IsNullOrEmpty(value))
                {
                    filteringByFields = true;

                    var tempArgs = new DynamicParameters();
                    List<string> tempJoins = new List<string>();
                    List<string> tempFieldColumns = new List<string>();

                    getFieldSql(fieldTypes, tempArgs, tempJoins, tempFieldColumns);

                    var filterDataProvider = new FilterDataProvider(companyContext);

                    var filterExpressionParser = new FilterExpressionParser(filterDataProvider, FilterExpressionParseType.RelationshipCustomFields);
                    filterExpressionParser.LoadFieldTypes(fieldTypes, tempFieldColumns);
                    Dictionary<string, object> sqlParams;
                    List<int> filteredFields;
                    var fieldsQuery = "(" + filterExpressionParser.Parse(value, out sqlParams, out filteredFields) + ")";

                    whereClause += (string.IsNullOrEmpty(whereClause) ? " where" : " and") + fieldsQuery;

                    foreach (var item in sqlParams)
                    {
                        dbArgs.Add(item.Key, item.Value);
                    }
                }
            }

            if (queryParams.Any(x => x.Key.ToLower() == "_direction"))
            {
                _orderDirection = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_direction").Value.ToLower(System.Globalization.CultureInfo.InvariantCulture);
            }


            if (pageNumber < 0)
            {
                pageNumber = 1;
            }
            if (pageSize < 0 || pageSize > 5000)
            {
                pageSize = 5000;
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

            if (includeAssetPath || orderByAssetPath)
            {
                fieldJoins.Add(" left join graph.AssetNodeDisplayPath ANDP_Object on ANDP_Object.Id = O.Id ");
                fieldJoins.Add(" left join graph.AssetNodeDisplayPath ANDP_Subject on ANDP_Subject.Id = S.Id ");
            }

            if (isExport)
            {                
                fieldJoins.Add(" left join AssetDisplayValue ADVS on S.ID = ADVS.AssetID ");
                fieldJoins.Add(" left join AssetDisplayValue ADVO on O.ID = ADVO.AssetID ");
                fieldJoins.Add(" outer apply dbo.GetAssetTypeTextPathById(S.AssetTypeID, ' > ') PS ");
                fieldJoins.Add(" outer apply dbo.GetAssetTypeTextPathById(O.AssetTypeID, ' > ') PO ");
            }

            string fieldColumnsSql = "";
            if (fieldColumns.Count > 0)
                fieldColumnsSql = string.Join(",\n", fieldColumns) + ",";

            var countFullSql = $@"select	@total = count(1) {countSql} {(filteringByFields ? string.Join("\n", fieldJoins) : "")} {whereClause}";

            string orderByClause = $"order by {_orderBy} {_orderDirection}";

            var sql = $@"
declare @total int
{(includeTotal ? countFullSql : "")}

select	@pageSize as 'pageSize',
		@pageNum as 'pageNum',
		@total as 'total',
		(
		select	lower(I.Uid) as Uid,
				lower(T.Uid) as RelationshipTypeUid,
				{stateSql}
				{fieldColumnsSql}
				lower(P.UID) as 'Predicate.Uid',
				{predicateTypeSql}
				P.Name as 'Predicate.Name',
				P.Inverse as 'Predicate.Inverse',
				lower(S.Uid) as 'Subject.Uid',
				ISNULL(lower(ST1.Uid),lower(ST2.Uid)) as 'Subject.AssetTypeUid'
                {(includeAssetPath ? ",ISNULL(ANDP_Subject.DisplayPath,ST2.Name) as 'Subject.[Path]'" : "")}
                {(isExport ? ",PS.[Path] as 'Subject.AssetTypePath'" : "")}                
                {(isExport ? ",ADVS.DisplayValue as 'Subject.DisplayName'" : "")}
				,lower(O.Uid) as 'Object.Uid'
				,ISNULL(lower(OT1.Uid),lower(OT2.Uid)) as 'Object.AssetTypeUid'
                {(isExport ? ",ADVO.DisplayValue as 'Object.DisplayName'" : "")}
                {(isExport ? ",PO.[Path] as 'Object.AssetTypePath'" : "")}
                {(includeAssetPath ? ",ISNULL(ANDP_Object.DisplayPath,OT2.Name) as 'Object.[Path]'" : "")}
                
                
                
		{baseTableSql}
        {string.Join("\n", fieldJoins)}
        {whereClause} 
        {orderByClause}
		offset ((@pageNum-1) * @pageSize) rows fetch next @pageSize rows only
		for json path,INCLUDE_NULL_VALUES
		) as 'items'
for json path, WITHOUT_ARRAY_WRAPPER";

            var models = await companyContext.GetDatabaseJsonAsObjectAsync<JObject>(sql, dbArgs, ApiTimeout);

            return models;
        }
        public async Task<JObject> GetRelationship(Guid uid)
        {
            var dbArgs = new DynamicParameters();

            var baseTableSql = @"from [Intersect] I 
inner join IntersectType T on T.ID = I.IntersectTypeID 
left join [Predicate] P on P.ID = T.PredicateID 
left join Asset S on S.Object = I.Subject and S.ObjectID = I.SubjectID 
left join AssetType ST1 on S.ID is not null and ST1.ID = S.AssetTypeID
left join AssetType ST2 on S.ID is null and ST2.Object = I.Subject and ST2.ObjectID = I.SubjectID
left join graph.AssetNodeKeyPath SKP on SKP.ID = S.ID
left join Asset O on O.Object = I.Object and O.ObjectID = I.ObjectID 
left join AssetType OT1 on O.ID is not null and OT1.ID = O.AssetTypeID
left join AssetType OT2 on O.ID is null and OT2.Object = I.Object and OT2.ObjectID = I.ObjectID
left join graph.AssetNodeKeyPath OKP on OKP.ID = O.ID 
";
            var whereClause = " WHERE I.[Uid] = @uid ";
            dbArgs.Add("@uid", uid);

            List<FieldType> fieldTypes = null;

            fieldTypes = companyContext.Query<FieldType>(
                $@"select F.* from FieldType F 
					inner join IntersectType IT on F.Object = 'IntersectType' and IT.ID = F.ObjectID 
					inner join [intersect] I on I.IntersectTypeID = IT.ID
                    WHERE I.uid = @uid"
                , new { uid }, ApiTimeout).ToList();

            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();

            if (fieldTypes != null)
            {
                getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns, "'Intersect'", "i.Id");
            }

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

            string fieldColumnsSql = "";
            if (fieldColumns.Count > 0)
            {
                fieldColumnsSql = string.Join(",\n", fieldColumns) + ",";
            }

            var sql = $@"
        select	lower(I.Uid) as Uid,
				lower(T.Uid) as RelationshipTypeUid,
				{stateSql}
                I.[Owner],
				{fieldColumnsSql}
				lower(P.UID) as 'Predicate.Uid',
				{predicateTypeSql}
				P.Name as 'Predicate.Name',
				P.Inverse as 'Predicate.Inverse',
				lower(S.Uid) as 'Subject.Uid',
                SKP.KeyPath as 'Subject.Path',
				ISNULL(lower(ST1.Uid),lower(ST2.Uid)) as 'Subject.AssetTypeUid',
				lower(O.Uid) as 'Object.Uid',
                OKP.KeyPath as 'Object.Path',
				ISNULL(lower(OT1.Uid),lower(OT2.Uid)) as 'Object.AssetTypeUid'
		{baseTableSql}
        {string.Join("\n", fieldJoins)}
        {whereClause} 
        for json path, INCLUDE_NULL_VALUES, WITHOUT_ARRAY_WRAPPER";

            var models = await companyContext.GetDatabaseJsonAsObjectAsync<JObject>(sql, dbArgs, ApiTimeout);

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


            var sql = $@"
select	I.Id,
        I.Uid,
		I.State as State,
        coalesce(I.IsSystem, 0) as IsSystem,
		P.UID as 'Predicate.Uid',
		coalesce(P.[Type],0) as 'Predicate.Type',
		coalesce(P.Name,'') as 'Predicate.Name',
		coalesce(P.Inverse,'') as 'Predicate.Inverse',
		S.Uid as 'Subject.Uid',		
		coalesce(SP.[Path], S.Name) as 'Subject.Name',
		coalesce(S.Class, 0) as 'Subject.Class',
		I.SubjectCardinality as 'Subject.Cardinality',
		O.Uid as 'Object.Uid',
		coalesce(OP.[Path], O.Name)  as 'Object.Name',
		coalesce(O.Class, 0) as 'Object.Class',
		I.ObjectCardinality as 'Object.Cardinality'
from	IntersectType I
		left join [Predicate] P on P.ID = I.PredicateID

		left join AssetType S on (S.Object = I.Subject and S.ObjectID = I.SubjectID)
        outer apply dbo.GetAssetTypeTextPathById(S.ID, '/') SP
		
		left join AssetType O on (O.Object = I.Object and O.ObjectID = I.ObjectID)
        outer apply dbo.GetAssetTypeTextPathById(O.ID, '/') OP
        {whereClause} for json path";

            var models = await companyContext.GetDatabaseJsonAsObjectAsync<List<IntersectTypeApiViewModel>>(sql, dbArgs, ApiTimeout);

            return models;
        }

        public Task<List<IntersectTypeApiViewModel>> GetActiveIntersectTypesByObjectType(int id, SystemObjects type)
        {
            return this.GetRelationshipTypes(null, $"where I.State = 1 and ((I.SubjectID = {id} and I.[Subject] = '{type.ToString()}') or (I.ObjectID = {id} and I.Object = '{type.ToString()}'))");
        }

        public async Task<ApiExecutionInfo> BulkPostRelationships(Guid intersectTypeUid, RelationshipInserts relationships, ApiExecution execution, bool triggerWorkflow = false)
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

            await Storage.CreateFolder(executionInfo.StorageFolder);
            await Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(relationships));

            execution.ExecutionID = executionInfo.ExecutionID;
            companyContext.Add(execution);

            await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

            return executionInfo;
        }

        public async Task<ApiExecutionInfo> BulkPostRelationships(Guid intersectTypeUid, RelationshipInserts relationships, Func<int, object, int, int, ApiExecution> getApiExecution, bool triggerWorkflow = false)
        {
            var execution = getApiExecution(relationships.Count, new ApiExecutionFields_PostRelationships { IntersectTypeUid = intersectTypeUid }, 0, 0);
            return await BulkPostRelationships(intersectTypeUid, relationships, execution, triggerWorkflow);
        }

        public IEnumerable<dynamic> GetExportModel(int id)
        {
            return companyContext.Query<dynamic>(
                @"select 
                    UID,
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
                where intersecttypeid = @id", new { id }, ApiTimeout);
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
                "i.ObjectID, i.ObjectName, i.ObjectTypeName, i.PredicateName , i.SubjectUid, i.ObjectUid, " + CteColumnName +
                " from  intersectdetail as i left join CTE  on CTE.ObjectID =i.id where intersecttypeid=@id ";
            var models = companyContext.Query<dynamic>(sql, new { id }, ApiTimeout);
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

        public async Task<List<DatabaseBulkAssetResult>> GetBulkResults(ApiExecutionInfo info)
        {
            List<DatabaseBulkAssetResult> results = null;
            try
            {
                results = await Storage.DeserializeJsonObjectFromBlobAsync<List<DatabaseBulkAssetResult>>(info.StorageFolder, info.ResponseFileName);
            }
            catch
            {
            }

            return results;
        }

        public List<DatabaseBulkRelationshipResult> DeleteRelationships(ApiExecution execution, IntersectType intersectType, RelationshipDeletes relationships, int timeout = 3600, bool triggerWorkflow = false)
        {
            return companyContext.DeleteRelationships(execution, intersectType, relationships, timeout, triggerWorkflow);
        }

        public async Task<ApiExecutionInfo> BulkDeleteRelationships(Guid intersectTypeUid, RelationshipDeletes relationships, ApiExecution execution, bool triggerWorkflow = false)
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

            await Storage.CreateFolder(executionInfo.StorageFolder);
            await Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(relationships));

            execution.ExecutionID = executionInfo.ExecutionID;
            companyContext.Add(execution);

            await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

            return executionInfo;
        }

        public async Task<ApiExecutionInfo> BulkDeleteRelationships(Guid intersectTypeUid, RelationshipDeletes relationships, Func<int, object, int, int, ApiExecution> getApiExecution, bool triggerWorkflow = false)
        {
            var execution = getApiExecution(relationships.Count, new ApiExecutionFields_DeleteRelationships { IntersectTypeUid = intersectTypeUid }, 0, 0);
            return await BulkDeleteRelationships(intersectTypeUid, relationships, execution, triggerWorkflow);
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
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
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
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
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
            var result = await companyContext.QueryAsync<string>(sql, new { id = assetTypeId, type = (int)PredicateType.Transformation });
            return string.IsNullOrEmpty(result.FirstOrDefault()) ? false : true;
        }
        public List<RelationshipTypeResult> PostRelationshipTypes(List<RelationshipTypeInsert> relationshipTypes, ApiExecution execution)
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
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
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
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }

            return results;
        }

        public List<RelationshipTypeResult> DeleteRelationshipTypes(List<RelationshipTypeDelete> relationshipTypes, ApiExecution execution)
        {
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
                string message = ex.GetFullExceptionData(false, constants.ERROR_MESSAGE_CHARACTER_LIMIT);
                execution.ErrorMessage = message;
                execution.CompletedOn = DateTime.UtcNow;
                companyContext.Update(execution);
            }

            return results;
        }
        public async Task<SLDocument> GetRelationshipsExcel(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var apiTimeout = ApiTimeout;
            JObject results = await GetRelationships(queryParams, isExport: true).ConfigureAwait(false);
            var includeTotal = true;
            var includeAssetPath = false;

            if (queryParams != null)
            {
                var queryParamsList = queryParams.ToList();

                if (queryParamsList.Any(q => q.Key.ToLower() == "_includetotal"))
                {
                    if (!bool.TryParse(queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_includetotal").Value, out includeTotal))
                    {
                        includeTotal = true;
                    }
                }

                if (queryParamsList.Any(q => q.Key.ToLower() == "_includepath"))
                {
                    if (!bool.TryParse(queryParamsList.FirstOrDefault(q => q.Key.ToLower() == "_includepath").Value, out includeAssetPath))
                    {
                        includeAssetPath = false;
                    }
                }
            }

            var apiInfo = results.Children().ToList();

            var excelDocument = new ExcelDocument(Smart.Format(ExcelExports.Relationships_DocumentName, DateTime.Now));
                   
            var fields = new List<FieldType>();

            var headerRow = new ExcelRow();
            var itemsSheet = new ExcelSheet(ExcelExports.Relationships_SheetName);

            //add default fields
            fields.Add(new FieldType { Type = "string", Object = "Uid", Name = "", FriendlyName = ExcelExports.Relationships_Relationship_UID });
            fields.Add(new FieldType { Type = "string", Object = "Subject", Name = "Uid", FriendlyName = ExcelExports.Relationships_Subject_UID });
            fields.Add(new FieldType { Type = "string", Object = "Subject", Name = "DisplayName", FriendlyName = ExcelExports.Relationships_Subject_Display_Name });
            if (includeAssetPath)
            {
                fields.Add(new FieldType { Type = "string", Object = "Subject", Name = "[Path]", FriendlyName = ExcelExports.Relationships_Subject_Asset_Path });
            }
            fields.Add(new FieldType { Type = "string", Object = "Subject", Name = "AssetTypePath", FriendlyName = ExcelExports.Relationships_Subject_Asset_Type_Path });
            fields.Add(new FieldType { Type = "string", Object = "Predicate", Name = "Name", FriendlyName = ExcelExports.Relationships_Predicate_Name });
            fields.Add(new FieldType { Type = "string", Object = "Object", Name = "Uid", FriendlyName = ExcelExports.Relationships_Object_UID });
            fields.Add(new FieldType { Type = "string", Object = "Object", Name = "DisplayName", FriendlyName = ExcelExports.Relationships_Object_Display_Name });
            if (includeAssetPath)
            {
                fields.Add(new FieldType { Type = "string", Object = "Object", Name = "[Path]", FriendlyName = ExcelExports.Relationships_Object_Asset_Path });
            }
            fields.Add(new FieldType { Type = "string", Object = "Object", Name = "AssetTypePath", FriendlyName = ExcelExports.Relationships_Object_Asset_Type_Path });
            fields.Add(new FieldType { Type = "string", Object = "RelationshipTypeUid", Name = "", FriendlyName = ExcelExports.Relationships_Relationship_Type_UID });
            fields.Add(new FieldType { Type = "string", Object = "Subject", Name = "AssetTypeUid", FriendlyName = ExcelExports.Relationships_Subject_Asset_Type_UID });
            fields.Add(new FieldType { Type = "string", Object = "Object", Name = "AssetTypeUid", FriendlyName = ExcelExports.Relationships_Object_Asset_Type_UID });
            fields.Add(new FieldType { Type = "string", Object = "Predicate", Name = "Uid", FriendlyName = ExcelExports.Relationships_Predicate_UID });
            fields.Add(new FieldType { Type = "string", Object = "Predicate", Name = "Type", FriendlyName = ExcelExports.Relationships_Predicate_Type });
            fields.Add(new FieldType { Type = "string", Object = "Predicate", Name = "Inverse", FriendlyName = ExcelExports.Relationships_Predicate_Inverse });

            #region Populate Excel Document            

            #region API Info Sheet
            var apiInfoSheet = new ExcelSheet(ExcelExports.Common_ApiInfoSheetName);

            var pageSizeRow = new ExcelRow { ExcelExports.Common_PageSize, results.GetValue("pageSize").ToString() };
            var pageNumRow = new ExcelRow { ExcelExports.Common_PageNum, results.GetValue("pageNum").ToString() };
            apiInfoSheet.ValueRows.Add(pageSizeRow);
            apiInfoSheet.ValueRows.Add(pageNumRow);

            if (includeTotal)
            {
                var totalRow = new ExcelRow { ExcelExports.Common_Total, results.GetValue("total").ToString() };
                apiInfoSheet.ValueRows.Add(totalRow);
            }
            #endregion    

            var items = results.GetValue("items");
            var rowData = new List<JToken>();

            if (items != null)
            {
                #region Populate Items Sheet
                rowData = items.ToList();

                List<ExcelRow> rows = new List<ExcelRow>();
                foreach (var row in rowData)
                {
                    var relationshipTypeUid = row["RelationshipTypeUid"];
                    var customColumns = GetCustomFieldsForExcel(relationshipTypeUid.ToString(), apiTimeout);

                    if (customColumns.Count() > 0)
                    {
                        int customCount = 0;
                        foreach (var cus in customColumns)
                        {                            
                            var name = cus.Name;
                            var friendlyName = cus.FriendlyName;
                            var exists = fields.Where(x => x.Object.ToLower() == name.ToLower()).FirstOrDefault();
                            if (exists == null)
                            {
                                var cusField = new FieldType { Type = "string", Object = name, Name = "", FriendlyName = friendlyName };
                                fields.Insert((includeAssetPath ? 10 : 8) + customCount, cusField);
                                customCount++;
                            }
                        }
                    }

                    ExcelRow excelRow = new ExcelRow();
                    foreach (var field in fields)
                    {
                        var token = row[field.Object];
                        if (field.Name == "")
                        {
                            token = row[field.Object];
                        }
                        else
                        {
                            token = row[field.Object][field.Name];
                        }
                        string value = "";
                        if (token != null)
                        {
                            value = token.Value<string>();
                        }
                        excelRow.Add(value);
                    }
                    rows.Add(excelRow);
                }
                itemsSheet.ValueRows.AddRange(rows);
                #endregion
            }

            #endregion
            fields.ForEach((field) => headerRow.Add(field.FriendlyName));
            itemsSheet.HeaderRows.Add(headerRow);
            excelDocument.Add(itemsSheet);
            excelDocument.Add(apiInfoSheet);

            SLDocument document = excelDocument.ToSLDocument();
            document.SelectWorksheet(ExcelExports.Relationships_SheetName);
            return document;
        }

        public IEnumerable<dynamic> GetCustomFieldsForExcel(string intersectUid, int apiTimeout)
        {
            return companyContext.Query<dynamic>(
                @"select distinct  f.Name   as Name,f.FriendlyName as FriendlyName, f.ColumnOrder from fieldtype f  
				inner join IntersectType i on i.uid = @uid
				 where f.[object] = 'IntersectType' and f.objectid = i.ID and IsListable = 1
				 order by f.ColumnOrder", new { uid = intersectUid }, apiTimeout);
        }

        public async Task<RelationshipUidResult> GetRelationshipsUids(int intersectTypeID, int pageSize, int pageNum, bool includeTotal, string owner)
        {
            int? total = null;
            string whereFilter = string.IsNullOrEmpty(owner) ? " " : " and i.owner = @owner";

            if (includeTotal)
            {
                var cntsql = $@"select
	                        count(1)
                        from
	                        [intersect] i	                        
                        where 
	                        i.IntersectTypeID = @intersectTypeID
                            {whereFilter}";

                total = await companyContext.QueryFirstOrDefaultAsync<int>(cntsql, new { intersectTypeID, owner }, ApiTimeout);
            }

            var sql = $@"
                        begin                         
                         -- create temp table
                         drop table if exists #TempIntersectInfo
                         create table #TempIntersectInfo
                        (
                            IntersectUid UniqueIdentifier not null, 
                            SubjectUid UniqueIdentifier null,
	                        ObjectUid UniqueIdentifier null,
	                        [Object] varchar(20) not null,
	                        [ObjectID] int not null,
	                        [Subject] varchar(20) not null,
	                        [SubjectID] int not null,
                            [Owner] varchar(100) null
                        )

                        create nonclustered index temp_intersectInfo_idx on #TempIntersectInfo ([Object],[ObjectID],[Subject],[SubjectID])

                         -- add intersect info into temp table

                         insert into #TempIntersectInfo
	                        (IntersectUid, [Object],[ObjectID], [Subject], [SubjectID],[Owner])
                           select 
	                        I.[UID],
	                        I.[Object],
	                        I.[ObjectID],
	                        I.[Subject],
	                        I.[SubjectID],
                            I.[Owner]
                           from [intersect] I 
                           where I.IntersectTypeID = @intersectTypeID
                                {whereFilter}
                            Order by I.ID OFFSET @offset ROWS 
                                FETCH NEXT @rows ROWS ONLY


	                        UPDATE
		                        #TempIntersectInfo
	                        SET
		                        #TempIntersectInfo.SubjectUID =  a.[uid]
	                        FROM 
		                        asset a
		                        INNER JOIN #TempIntersectInfo t ON a.[object] = t.[subject] and a.[objectid] = t.[subjectid];

	                        UPDATE
		                        #TempIntersectInfo
	                        SET
		                        #TempIntersectInfo.ObjectUID =  a.[uid]
	                        FROM 
		                        asset a
		                        INNER JOIN #TempIntersectInfo t ON a.[object] = t.[object] and a.[objectid] = t.[objectid];

	                        select IntersectUid as RelationshipUid,ObjectUid,SubjectUid,Owner from #TempIntersectInfo


                        end";

            var results = await companyContext.QueryAsync<RelationshipUidResultItem>(sql, new { intersectTypeID, offset = ((pageNum - 1) * (pageSize)), rows = pageSize, owner }, ApiTimeout);

            return new RelationshipUidResult { Total = total, pageSize = pageSize, pageNum = pageNum, Results = results };
        }
    }
}
