using d360.core;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace d360.model
{
    public class AssetResults
    {
        public int Count { get; set; }
        public IEnumerable<dynamic> Results { get; set; }        
    }

    partial class CompanyContext : BaseContext
    {
        #region DbSets

        public DbSet<Asset> Assets { get; set; }

        public DbSet<AssetDetail> AssetDetails { get; set; }

        public DbSet<AssetType> AssetTypes { get; set; }

        public DbSet<AssetApiModel> AssetApiModels { get; set; }

        public DbSet<FieldApiModel> FieldApiModels { get; set; }

        #endregion

        #region Engine Methods

        private string wildcardValue(string value)
        {
            if (value.Contains("*") || value.Contains("?"))
                return value.Replace("*", "%").Replace("?", "_") + "%";
            else
                return value += "%";
        }

        /// <summary>
        /// This is the pivot version of the method above, but the paging is where this design broke down.
        /// </summary>
        public async Task<AssetResults> GetPivotVersionDynamicAssets(AssetType at, List<UiRequestFilterValue> filters, int pageNumber = 0, int pageSize = 25, bool useFieldNames = false, string sortField = "", string sortOrder = "", string simpleFilter = "")
        {
            AssetResults results = new AssetResults();
            results.Count = 0; //initialize

            var assetTypeID = at.ID;

            var fieldTypes = Filter<FieldType>(i => i.AssetTypeID == assetTypeID).ToList();
            var selectFields = fieldTypes.Where(i => i.IsListable).OrderBy(i => i.ColumnOrder).ToList();

            var selectFieldString = string.Join(",", selectFields.Select(i => (useFieldNames ? $"[{i.ID}] as {i.Name}" : $"[{i.ID}] as Field{i.ID}")));

            var selectFieldIDs = string.Join(",", selectFields.Select(i => $"{i.ID}"));
            var pivotFieldIDs = string.Join(",", selectFields.Select(i => $"[{i.ID}]"));

            var tableHints = " with (NOLOCK)";

            var dbArgs = new DynamicParameters();
            dbArgs.Add("r", CurrentResourceID, System.Data.DbType.Int32);
            dbArgs.Add("atID", assetTypeID, System.Data.DbType.Int32);

            #region Administrator Editability Sql Syntax

            var editRightsColumnStatement = "1 as P_CanEdit, 1 as P_CanDelete,";
            
            if (!CurrentResourceIsAdmin)
            {
                editRightsColumnStatement = @" case when exists (
							 select 1 from UserAssetPermissions(@r,AssetTypeID) u where u.PermissionsBitMask & 2 = 2 and (u.AssetID = pvt.AssetID or u.AssetTypeID = pvt.AssetTypeID)
						   ) 
						   then 1 
						   else 0 
						end as P_CanEdit,
						 case when exists (
							 select 1 from UserAssetPermissions(@r,AssetTypeID) u where u.PermissionsBitMask & 4 = 4 and (u.AssetID = pvt.AssetID or u.AssetTypeID = pvt.AssetTypeID)
						   ) 
						   then 1 
						   else 0 
						end as P_CanDelete, ";            
            }            

            #endregion

            #region Relationship Sql Syntax

            var relationshipCaseStatement = "";
            var relationshipJoinStatement = "";
            if (selectFields.Any(i => i.Type == "Relationship"))
            {
                relationshipCaseStatement = @" when FT.Type = 'Relationship' and FT.LookupObjectType = 'IntersectType' then RD.DisplayValue ";
                
                relationshipJoinStatement = $@"
 left join [Intersect] F_R{tableHints} on	FT.LookupObjectType = 'IntersectType' 
										and F_R.IntersectTypeID = FT.LookupObjectID 
										and (
											(F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID) or 
											(F_R.Object = A.Object and F_R.ObjectID = A.ObjectID)
											) 
outer apply dbo.GetRelationshipDisplayValue(
	case when F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID then F_R.Object else F_R.Subject end,
	case when F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID then F_R.ObjectID else F_R.SubjectID end) as RD";
            }

            #endregion

            #region Relationship Field Sql Syntax

            var fieldFromRelationshipCaseStatement = "";
            var fieldFromRelationshipJoinStatement = "";
            if (selectFields.Any(i => i.Type == "FieldFromRelationship"))
            {
                fieldFromRelationshipCaseStatement = @"when FT.Type = 'FieldFromRelationship' and FT.LookupObjectType = 'IntersectType' then F_RF.FormattedValue";
                fieldFromRelationshipJoinStatement = $@"
 left join [Intersect] F_REL{tableHints} on	FT.LookupObjectType = 'IntersectType' 
										and F_REL.IntersectTypeID = FT.LookupObjectID 
										and (
											(F_REL.Subject = A.Object and F_REL.SubjectID = A.ObjectID) or 
											(F_REL.Object = A.Object and F_REL.ObjectID = A.ObjectID)
											)
left join Field F_RF{tableHints} on F_RF.ObjectType = case when F_REL.Subject = A.Object and F_REL.SubjectID = A.ObjectID then F_REL.Object else F_REL.Subject end
						and F_RF.ObjectID = case when F_REL.Subject = A.Object and F_REL.SubjectID = A.ObjectID then F_REL.ObjectID else F_REL.SubjectID end
						and F_RF.FieldTypeID = FT.LookupObjectFieldTypeID ";
            }

            #endregion

            #region Parent Sql Syntax

            var artifactTypeHasParent = TypeHasParent(SystemObjects.ArtifactType, at.ObjectID);
            
            var parentOuterSqlColumn = @"";
            var parentSqlColumn = @"";
            var parentSqlJoin = @"";         
            var countRequireParentJoin = false;

            if (artifactTypeHasParent)
            {
                parentOuterSqlColumn = @"ParentID, Parent, ParentUrl,";
                parentSqlColumn = @"arp.ParentArtifactID as ParentID, parp.DisplayValue as Parent, '' as ParentUrl, ";                
                parentSqlJoin = @" inner join [utility].[ArtifactAssetParent] arp on a.id = arp.assetid inner join AssetDisplayValue parp on parp.AssetID = arp.ParentAssetID";
            }

            #endregion

            #region Filter Processing

            var filterJoinList = new List<string>();
            var filterWhereList = new List<string>();
            int filterIndex = 0;

            if (string.IsNullOrEmpty(simpleFilter))
            {
                foreach (var filter in filters)
                {
                    filterIndex++;
                    if (filter is UiRequestAttributeFilterValue)
                    {
                        var f = filter as UiRequestAttributeFilterValue;

                        var paramPrefix = $"AttType{f.AttributeTypeID}";

                        dbArgs.Add($"{paramPrefix}", f.AttributeTypeID);
                        dbArgs.Add($"{paramPrefix}Value", $"{wildcardValue(f.RawValue)}");

                        filterWhereList.Add(
    $@"A.ObjectID in (
select ObjectID from AttributeDetail{tableHints} where AttributeTypeID = @{paramPrefix} and FormattedValue like @{paramPrefix}Value
)"
                            );
                    }

                    if (filter is UiRequestFieldFilterValue)
                    {
                        var f = filter as UiRequestFieldFilterValue;


                        if (f.IsParentField)
                        {
                            countRequireParentJoin = true;

                            dbArgs.Add($"parentFilterVal{filterIndex}", f.RawValue);

                            filterWhereList.Add($"parp.DisplayValue = @parentFilterVal{filterIndex}");
                        }
                        else
                        {

                            var thisFilterFieldType = useFieldNames ?
                                fieldTypes.FirstOrDefault(i => i.Name == f.FieldName) :
                                fieldTypes.FirstOrDefault(i => i.ID == int.Parse(f.FieldName.Replace("Field", "")));

                            if (thisFilterFieldType != null)
                            {
                                if (thisFilterFieldType.AllowMultipleValues)
                                {
                                    if (f.Condition == "IN")
                                        f.Condition = "IN_MULTI";
                                    else
                                        f.Condition = "CONTAINS";
                                }

                                var nonPivotInnerJoinPrefix = "";

                                if (thisFilterFieldType.Type == "FieldFromRelationship")
                                {
                                    var intersectSql = $@"left join [Intersect] FI{thisFilterFieldType.ID}{tableHints} on
                                     FI{thisFilterFieldType.ID}.IntersectTypeID = {thisFilterFieldType.LookupObjectID}
                                    and(
                                        (FI{thisFilterFieldType.ID}.Subject = A.Object and FI{thisFilterFieldType.ID}.SubjectID = A.ObjectID) or
                                        (FI{thisFilterFieldType.ID}.Object = A.Object and FI{thisFilterFieldType.ID}.ObjectID = A.ObjectID)
										)";

                                    var joinSql = $@"inner join Field F{thisFilterFieldType.ID}{tableHints} on F{thisFilterFieldType.ID}.ObjectType = case when FI{thisFilterFieldType.ID}.Subject = A.Object and FI{thisFilterFieldType.ID}.SubjectID = A.ObjectID then FI{thisFilterFieldType.ID}.Object else FI{thisFilterFieldType.ID}.Subject end
												and F{thisFilterFieldType.ID}.ObjectID = case when FI{thisFilterFieldType.ID}.Subject = A.Object and FI{thisFilterFieldType.ID}.SubjectID = A.ObjectID then FI{thisFilterFieldType.ID}.ObjectID else FI{thisFilterFieldType.ID}.SubjectID end
												and F{thisFilterFieldType.ID}.FieldTypeID = {thisFilterFieldType.LookupObjectFieldTypeID}";

                                    nonPivotInnerJoinPrefix = intersectSql + '\n' + joinSql;
                                }
                                else
                                {
                                    nonPivotInnerJoinPrefix = $@"inner join (
                                                                                select AssetID, FieldTypeID, FormattedValue as Value, [Value] as Val from Field{tableHints} where FieldTypeID = {thisFilterFieldType.ID}
                                                                                union all
                                                                                select SA{thisFilterFieldType.ID}.ID as AssetID, SFT{thisFilterFieldType.ID}.ID as FieldTypeID, DefaultFormattedValue as Value, '' as Val from FieldType SFT{thisFilterFieldType.ID}{tableHints}
							                                                    inner join Asset SA{thisFilterFieldType.ID}{tableHints} on SA{thisFilterFieldType.ID}.AssetTypeID = @atID
							                                                    where not exists (select 1 from Field where AssetID = SA{thisFilterFieldType.ID}.ID and FieldTYpeID = {thisFilterFieldType.ID})
                                                                            ) F{thisFilterFieldType.ID} on F{thisFilterFieldType.ID}.AssetID = A.ID and F{thisFilterFieldType.ID}.FieldTypeID = {thisFilterFieldType.ID}";
                                }

                                var bind = $"fld{thisFilterFieldType.ID}";
                                var nonPivotFieldName = $"F{thisFilterFieldType.ID}.Value";
                                var valueColumnQuery = GetFilterCondition(f.Condition, nonPivotFieldName, bind, dbArgs, f.RawValue);
                                if (thisFilterFieldType.AllowAllValue)
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and ({valueColumnQuery} or F{thisFilterFieldType.ID}.Val = '0')");
                                else
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and {valueColumnQuery}");

                            }
                        }
                    }

                    if (filter is UiRequestOwnershipFilterValue)
                    {
                        var f = filter as UiRequestOwnershipFilterValue;
                        
                        var index = 1;
                        foreach (var o in f.Items)
                        {
                            var securityAsset = "";
                            switch (o.FilterType)
                            {
                                case UiRequestOwnershipFilterType.Group:
                                    securityAsset = "G";
                                    break;
                                case UiRequestOwnershipFilterType.Organization:
                                    securityAsset = "O";
                                    break;
                                case UiRequestOwnershipFilterType.User:
                                    securityAsset = "R";
                                    break;
                            }

                            filterWhereList.Add($"A.ID in (select AssetID from ResponsibilityAllAsset{tableHints} where SecurityAsset = '{securityAsset}' and SecurityAssetID = {o.SecurityAssetID} and ResponsibilityTypeID = {o.ResponsibilityTypeID} )");

                            index++;
                        }
                    }

                    if (filter is UiRequestRelationshipFilterValue)
                    {
                        var f = filter as UiRequestRelationshipFilterValue;

                        var paramPrefix = $"RelType{f.IntersectTypeID}";
                        dbArgs.Add($"{paramPrefix}", f.IntersectTypeID);
                        dbArgs.Add($"{paramPrefix}Obj", f.TargetObject);

                        if (f.Operator == "OR")
                        {
                            var idList = string.Join(",", f.TargetObjectIDs);
                            filterWhereList.Add(
        $@"A.ObjectID in (
select ObjectID from [Intersect]{tableHints} where IntersectTypeID = @{paramPrefix} and Subject = @{paramPrefix}Obj and SubjectID in ({idList})
union
select SubjectID from [Intersect]{tableHints} where IntersectTypeID = @{paramPrefix} and Object = @{paramPrefix}Obj and ObjectID in ({idList})
)"
                                );
                        }
                        else
                        {
                            f.TargetObjectIDs.ForEach(id =>
                            {
                                filterWhereList.Add(
            $@"A.ObjectID in (
select ObjectID from [Intersect]{tableHints} where IntersectTypeID = @{paramPrefix} and Subject = @{paramPrefix}Obj and SubjectID = {id})
union
select SubjectID from [Intersect]{tableHints} where IntersectTypeID = @{paramPrefix} and Object = @{paramPrefix}Obj and ObjectID = {id})
)"
                                    );
                            });
                        }
                    }

                    if (filter is UiRequestRelationshipFieldFilterValue)
                    {
                        var f = filter as UiRequestRelationshipFieldFilterValue;
                        var paramPrefix = $"RelField{f.FieldTypeID}";
                        var tableAlias = $"RF{f.FieldTypeID}";
                        var cond = "";

                        dbArgs.Add($"{paramPrefix}Value", f.Value);

                        if (f.Condition == "CONTAINS")
                            cond = $"'%' + @{paramPrefix}Value + '%'";
                        else
                            cond = $"@{paramPrefix}Value + '%'";

                        filterJoinList.Add($@"
                                inner join [Field] [{tableAlias}]{tableHints} on [{tableAlias}].FieldTypeId = {f.FieldTypeID} and [{tableAlias}].Value like {cond}
                                inner join [Intersect] [I{tableAlias}]{tableHints} on [I{tableAlias}].ID = [{tableAlias}].ObjectID and (([I{tableAlias}].[Subject] = A.[Object] and [I{tableAlias}].SubjectID = A.ObjectID) or ([I{tableAlias}].Object = A.[Object] and [I{tableAlias}].ObjectID = A.ObjectID))
                            ");
                    }
                }
            }
            else
            {
                dbArgs.Add("simpleFilter", $"{wildcardValue(simpleFilter)}", System.Data.DbType.String, System.Data.ParameterDirection.Input, 250);             

                var simpleFilterIDs = string.Join(",", selectFields.Select(i => i.ID));
                var fieldFromRelationshipAssets = selectFields.Any(i => i.Type == "FieldFromRelationship") ? $@"union
                                    select	A.ID as AssetID 
                                    from	Field SF
		                                    inner join FieldType FT on FT.ID in ({simpleFilterIDs}) 
			                                    and FT.[Type] = 'FieldFromRelationship' 
			                                    and FT.LookupObjectType = 'IntersectType' 
			                                    and FT.LookupObjectFieldTypeID = SF.FieldTypeID
		                                    inner join Asset O on O.ID = SF.AssetID
		                                    inner join [Intersect] I on I.IntersectTypeID = FT.LookupObjectID 
			                                    and ((I.[Object] = O.[Object] and I.ObjectID = O.ObjectID) 
				                                    or (I.[Subject] = O.[Object] and I.SubjectID = O.ObjectID))
		                                    inner join Asset A on A.[Object] = case when I.[Object] = O.[Object] and I.ObjectID = O.ObjectID then I.[Subject] else I.[Object] end 
			                                    and A.ObjectID = case when I.[Object] = O.[Object] and I.ObjectID = O.ObjectID then I.SubjectID else I.ObjectID end
                                    where	(SF.FormattedValue like @simpleFilter)
		                                    group by A.ID
		                            ) SF on SF.AssetID = A.ID" : "";

                if (artifactTypeHasParent)
                {   
                    var joinInfo = fieldFromRelationshipAssets.Any() ? "" : " ) SF on SF.AssetID = A.ID ";
                    
                    filterJoinList.Add($@"
                        inner join (
		                            select	SF.AssetID
		                            from	Field SF{tableHints}                                     
                                    inner join[utility].[ArtifactAssetParent] arp on SF.AssetID = arp.AssetID inner join AssetDisplayValue parp on (parp.assetid = arp.ParentAssetID)
                                    where	FieldTypeID in ({simpleFilterIDs})				                            
                                            and (FormattedValue like @simpleFilter OR parp.DisplayValuePrefix like @simpleFilter)
                                    group by SF.AssetID
		                            {joinInfo} 
                                   {fieldFromRelationshipAssets}");
                }
                else
                {
                    var simpleFilterFieldJoin = string.IsNullOrEmpty(fieldFromRelationshipAssets) ? ") SF on SF.AssetID = A.ID " : fieldFromRelationshipAssets;

                    filterJoinList.Add($@"
                        inner join (
                                    select  SA.ID as AssetID
                                    from    FieldType SFT{tableHints} 
                                            inner join Asset SA{tableHints} on SA.AssetTypeID = @atID
                                    where   SFT.ID in ({simpleFilterIDs})
                                            and not exists (select 1 from Field where FieldTypeID = SFT.ID and AssetID = SA.ID)
                                            and (DefaultFormattedValue like @simpleFilter)
                                    union all
		                            select	AssetID
		                            from	Field SF{tableHints}                                             
		                            where	FieldTypeID in ({simpleFilterIDs})
                                            and (FormattedValue like @simpleFilter)
                                    group by AssetID                                    
                                    {simpleFilterFieldJoin}");
                }
            }

            var filterJoinString = "";
            if (filterJoinList.Count > 0)
            {
                filterJoinString = string.Join(" ", filterJoinList);
            }

            var filterWhereString = "";
            var filterWhereStringRaw = "";
            if (filterWhereList.Count > 0)
            {
                filterWhereStringRaw = string.Join(" and ", filterWhereList);
                if (!string.IsNullOrEmpty(filterWhereStringRaw))
                    filterWhereString = $"and {filterWhereStringRaw}";
            }

            #endregion

            #region Order Processing

            var orderFieldString = "";

            if (string.IsNullOrEmpty(sortField))
            {
                orderFieldString = string.Join(
                    ",",
                    selectFields
                        .Where(i => i.SortOrder != 0)
                        .OrderBy(i => i.SortOrder)
                        .Select(i => (useFieldNames ? GetFieldTypeSort(i.Name, true, i.Type) : GetFieldTypeSort($"{i.ID}", true, i.Type)))
                );

                if (string.IsNullOrEmpty(orderFieldString))
                {
                    orderFieldString = string.Join(
                        ",",
                        selectFields
                            .Where(i => i.IsPartOfKey)
                            .OrderBy(i => i.ColumnOrder)
                            .Select(i => (useFieldNames ? GetFieldTypeSort(i.Name, true, i.Type) : GetFieldTypeSort($"Field{i.ID}", true, i.Type)))
                    );
                }
                
                //check if after that there is no order by field because the key field is not listable
                if(string.IsNullOrEmpty(orderFieldString))
                {
                    orderFieldString = "AssetID ASC";
                }
            }
            else
            {
                var sortFieldType = (useFieldNames) ?
                    selectFields.SingleOrDefault(i => i.Name == sortField) :
                    selectFields.SingleOrDefault(i => sortField == $"Field{i.ID}");
                if (sortFieldType != null)
                {
                    orderFieldString = useFieldNames ? GetFieldTypeSort(sortFieldType.Name, (sortOrder ??"").ToUpper() == "ASC" , sortFieldType.Type) : GetFieldTypeSort($"{sortFieldType.ID}", (sortOrder ?? "").ToUpper() == "ASC", sortFieldType.Type);
                }
                else if(string.Compare(sortField,"PARENT",true) == 0)
                {
                    orderFieldString = $"Parent ";

                    // you cant trust the user must validate its asc or desc
                    orderFieldString += ((sortOrder ?? "").ToUpper() == "ASC" ? "ASC" : "DESC");
                }
            }

            #endregion

            var countSql = $@"
select	count(1)
from	Asset A{tableHints}
        {(countRequireParentJoin ? parentSqlJoin : "")} 
        {filterJoinString}         
where	A.AssetTypeID = @atID
		and A.State = 1
        and not exists (select 1 from AssetTypesUserCantRead(@r) u where u.AssetTypeID = @atID) and not exists (select 1 from AssetsByTypeUserCantRead(@r,@atID) u where u.AssetID = A.ID) 
        {filterWhereString}
OPTION (RECOMPILE)";
            
            results.Count = await Database.Connection.ExecuteScalarAsync<int>(countSql, dbArgs);
                        
            pageNumber = (pageNumber < 0) ? 0 : pageNumber;
            if (pageSize < 0)
                pageSize = 25;
            pageNumber = ((pageNumber < 0) ? 0 : pageNumber) * pageSize;

            var whereClause = " where ";

            if (string.IsNullOrEmpty(filterWhereStringRaw)) whereClause = "";
            
                var sql = $@"
select	*
from	(
		select	AssetID, Object, ObjectID, Type, TypeID, 
               {editRightsColumnStatement}
               {parentOuterSqlColumn}                  
				{selectFieldString} 
		from	(
				select	A.ID as AssetID,
                        A.AssetTypeID as AssetTypeID,
						A.Object,
						A.ObjectID,        
						AST.Object as Type,
						AST.ObjectID as TypeID,
                        {parentSqlColumn}
						FT.ID as FieldTypeID,                        
						case 
							when FT.AllowAllValue = 1 and F_O.Value = '0' then FT.AllowAllLabel 
                            when F_O.Value is not null then F_O.FormattedValue
							when FT.DefaultValue is not null then FT.DefaultFormattedValue 
							{relationshipCaseStatement}
							{fieldFromRelationshipCaseStatement}
							else '' 
						end as [Field]	
				from	Asset A{tableHints} 
                        {parentSqlJoin} 
                        inner join AssetType AST{tableHints} on AST.ID = A.AssetTypeID and AST.ID = @atID and A.State = 1  
                        {filterJoinString}
						inner join FieldType FT{tableHints} on FT.ID in ({selectFieldIDs}) and FT.AssetTypeID = A.AssetTypeID
						left join Field F_O{tableHints} on F_O.AssetID = A.ID and F_O.FieldTypeID = FT.ID
						{relationshipJoinStatement}
						{fieldFromRelationshipJoinStatement} 
                {whereClause}                         
                        {filterWhereStringRaw}
				) A
		pivot	(
				MIN([Field]) for FieldTypeID in ({pivotFieldIDs})
				) pvt        
        where not exists (select 1 from AssetTypesUserCantRead(@r) u where u.AssetTypeID = @atID) and not exists (select 1 from AssetsByTypeUserCantRead(@r,@atID) u where u.AssetID = pvt.AssetID)  
		ORDER BY {orderFieldString}
		OFFSET({pageNumber}) ROWS FETCH NEXT ({pageSize}) ROWS ONLY
		) A
OPTION (RECOMPILE)";

            results.Results =  await QueryAsync<dynamic>(sql, dbArgs);

            return results;
        }

        private string GetFieldTypeSort(string fieldName, bool ascending, string datatype)
        {
            if ((datatype ?? "").ToUpper() == "NUMBER")
                return " CAST( [" + fieldName + "] AS bigint) " + (ascending ? "ASC" : "DESC");
            else if ((datatype ?? "").ToUpper() == "DATE")
                return " CAST( [" + fieldName + "] AS date) " + (ascending ? "ASC" : "DESC");
            else if ((datatype ?? "").ToUpper() == "DECIMAL")
                return " TRY_CONVERT( DECIMAL(18, 4),  [" + fieldName + "]) " + (ascending ? "ASC" : "DESC");

            return " [" + fieldName + "] " + (ascending ? "ASC" : "DESC"); ;
        }

        private string GetFilterCondition(string condition, string fieldName, string bind, DynamicParameters dbArgs, string value)
        {
            string valueColumnQuery = "";

            switch (condition)
            {
                case "EQUAL":
                    dbArgs.Add(bind, $"{value}");
                    valueColumnQuery = $"{fieldName} = @{bind}";                    
                    break;
                case "CONTAINS":
                    dbArgs.Add(bind, $"%{wildcardValue(value)}%");
                    valueColumnQuery = $"{fieldName} like @{bind}";
                    break;
                case "NOT_EQUAL":
                    dbArgs.Add(bind, $"{value}");
                    valueColumnQuery = $"{fieldName} <> @{bind}";
                    break;
                case "DOES_NOT_CONTAIN":
                    dbArgs.Add(bind, $"{wildcardValue(value)}");
                    valueColumnQuery = $"NOT {fieldName} not like @{bind}";
                    break;
                case "STARTS_WITH":
                    dbArgs.Add(bind, $"{value}%");
                    valueColumnQuery = $"{fieldName} like @{bind}";
                    break;
                case "ENDS_WITH":
                    dbArgs.Add(bind, $"%{value}");
                    valueColumnQuery = $"{fieldName} like @{bind}";
                    break;
                case "IN":
                case "IN_MULTI":
                    try
                    {
                        var values = value.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries);
                        var inParamsList = new List<string>();
                        for (var iLoop = 0; iLoop < values.Length; iLoop++)
                        {
                            dbArgs.Add($"{bind}{iLoop}", values[iLoop]);
                            inParamsList.Add($"@{bind}{iLoop}");

                        }
                        if (values.Length > 0)
                            valueColumnQuery = $"{fieldName} in ({string.Join(",", inParamsList)})";
                    }
                    catch { }
                    break;                
                case "NULL":
                    valueColumnQuery = $"{fieldName} is null";
                    break;
                case "NOT_NULL":
                    valueColumnQuery = $"{fieldName} is not null";
                    break;
                case "EMPTY":
                    valueColumnQuery = $"{fieldName} = ''";
                    break;
                case "NOT_EMPTY":
                    valueColumnQuery = $"{fieldName} <> ''";
                    break;
                default:
                    dbArgs.Add(bind, $"{wildcardValue(value)}");
                    valueColumnQuery = $"{fieldName} like @{bind}";
                    break;
            }

            return valueColumnQuery;
        }

        #endregion
    }
}
