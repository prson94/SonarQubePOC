using d360.core;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;

namespace d360.model
{
    partial class CompanyContext : BaseContext
    {
        #region DbSets

        public DbSet<Asset> Assets { get; set; }

        public DbSet<AssetDetail> AssetDetails { get; set; }

        public DbSet<AssetType> AssetTypes { get; set; }

        public DbSet<AssetApiModel> AssetApiModels { get; set; }

        public DbSet<FieldApiModel> FieldApiModels { get; set; }

        #endregion

        internal List<string> CalculatedFieldTypes = new List<string>() { DataType.Attribute.ToString(), DataType.ComplexRelationLookup.ToString(), DataType.DataTableSelect.ToString(), DataType.File.ToString(), DataType.FilteredLookup.ToString(), DataType.OwnershipLookup.ToString() };

        #region Engine Methods

        private string wildcardValue(string value)
        {
            if (value.Contains("*") || value.Contains("?"))
                return value.Replace("*", "%").Replace("?", "_") + "%";
            else
                return value += "%";
        }

        /// <summary>
        /// Not actively used at this point. Just for reference for performance comparison.
        /// </summary>
        public IEnumerable<dynamic> GetDynamicAssets(AssetType at, List<UiRequestFilterValue> filters, out int count, int pageNumber = 1, int pageSize = 25, bool useFieldNames = false, string sortField = "", string sortOrder = "", string simpleFilter = "")
        {
            count = 0; //initialize

            var assetTypeID = at.ID;
            var tableHints = " with (NOLOCK)";
            var dbArgs = new DynamicParameters();

            var fieldTypes = Filter<FieldType>(i => i.AssetTypeID == assetTypeID && !CalculatedFieldTypes.Contains(i.Type)).ToList();
            var selectFields = fieldTypes.Where(i => i.IsListable).OrderBy(i => i.ColumnOrder).ToList();

            var selectFieldList = new List<string>();
            var listableJoinList = new List<string>();
            var filterJoinList = new List<string>();
            var whereList = new List<string>();

            dbArgs.Add("r", CurrentResourceID, System.Data.DbType.Int32);
            dbArgs.Add("atID", assetTypeID, System.Data.DbType.Int32);

            #region Administrator Editability Sql Syntax

            var editRightsColumnStatement = ", 1 as P_CanEdit, 1 as P_CanDelete ";
            var editRightsJoinStatement = "";

            if (!CurrentResourceIsAdmin)
            {
                editRightsColumnStatement = ", IIF(S_E.AssetID is null, 0, 1) as P_CanEdit, IIF(S_D.AssetID is null, 0, 1) as P_CanDelete ";
                editRightsJoinStatement = $@"
outer apply (select top 1 AssetID from ResponsibilityDetail where AssetID = A.ID and ResourceID = @r and (PermissionsBitMask & {(int)Permission.DeleteAsset}) = {(int)Permission.DeleteAsset}) S_E 
outer apply (select top 1 AssetID from ResponsibilityDetail where AssetID = A.ID and ResourceID = @r and (PermissionsBitMask & {(int)Permission.DeleteAsset}) = {(int)Permission.DeleteAsset}) S_D";
            }

            #endregion

            #region Column Generation

            selectFields.ForEach(ft => {

                var columnName = useFieldNames ? $"{ft.Name}" : $"Field{ft.ID}";

                switch (ft.Type)
                {
                    case "Relationship":
                        selectFieldList.Add($@"F{ft.ID}.FormattedValue as {columnName} ");
                        listableJoinList.Add($@"inner join DynamicGidRelationship F{ft.ID} on F{ft.ID}.FieldTypeID = {ft.ID} and F{ft.ID}.AssetID = A.ID");
                        break;
                    case "FieldFromRelationship":
                        selectFieldList.Add($@"F{ft.ID}.FormattedValue as {columnName} ");
                        listableJoinList.Add($@"inner join DynamicGidRelationshipField F{ft.ID} on F{ft.ID}.FieldTypeID = {ft.ID} and F{ft.ID}.AssetID = A.ID");
                        break;
                    default:
                        #region Regular Field
                        var convertedTypeColumn = "";
                        var convertedTypeDefaultColumn = "";
                        switch (ft.Type)
                        {
                            case "Decimal":
                                convertedTypeColumn = $"cast(F{ft.ID}.FormattedValue as decimal(38,6))";
                                convertedTypeDefaultColumn = $"cast(F{ft.ID}.DefaultFormattedValue as decimal(38,6))";
                                break;
                            case "Number":
                                convertedTypeColumn = $"cast(F{ft.ID}.FormattedValue as bigint)";
                                convertedTypeDefaultColumn = $"cast(F{ft.ID}.DefaultFormattedValue as bigint)";
                                break;
                            case "DateTime":
                                convertedTypeColumn = $"cast(F{ft.ID}.FormattedValue as datetime)";
                                convertedTypeDefaultColumn = $"cast(F{ft.ID}.DefaultFormattedValue as datetime)";
                                break;
                            case "Date":
                                convertedTypeColumn = $"cast(F{ft.ID}.FormattedValue as date)";
                                convertedTypeDefaultColumn = $"cast(F{ft.ID}.DefaultFormattedValue as date)";
                                break;
                            default:
                                convertedTypeColumn = $"F{ft.ID}.FormattedValue";
                                convertedTypeDefaultColumn = $"F{ft.ID}.DefaultFormattedValue";
                                break;
                        }                         

                        selectFieldList.Add($@"
case 
	when F{ft.ID}.AllowAllValue = 1 and F{ft.ID}.Value = '0' then F{ft.ID}.AllowAllLabel
	when F{ft.ID}.FormattedValue is not null then {convertedTypeColumn}
	when F{ft.ID}.DefaultFormattedValue is not null then {convertedTypeDefaultColumn}
	else null
end as {columnName} ");
                        listableJoinList.Add($@"inner join DynamicGidField F{ft.ID} on F{ft.ID}.FieldTypeID = {ft.ID} and F{ft.ID}.AssetID = A.ID ");

                        #endregion
                        break;
                }
            });

            #endregion

            #region Parent Sql Syntax

            var parentPresent = Any<IntersectType>(i => i.Object == at.Object && i.ObjectID == at.ObjectID && i.Predicate.Type == core.enums.PredicateType.InterTypeHierarchy);

            var parentSqlColumn = @"";
            var parentSqlJoin = @"";            

            if (parentPresent)
            {
                parentSqlColumn = @", PID.ParentID, PID.ParentDisplayValue as Parent, PID.ParentUrl ";
                parentSqlJoin = @" cross apply [dbo].[GetArtifactParentByAssetID](A.ID) PID";
            }

            #endregion

            #region Filter Processing

            if (string.IsNullOrEmpty(simpleFilter))
            {
                foreach (var filter in filters)
                {
                    if (filter is UiRequestAttributeFilterValue)
                    {
                        var f = filter as UiRequestAttributeFilterValue;

                        var paramPrefix = $"AttType{f.AttributeTypeID}";

                        dbArgs.Add($"{paramPrefix}", f.AttributeTypeID);
                        dbArgs.Add($"{paramPrefix}Value", $"{wildcardValue(f.RawValue)}");//$"\"{f.RawValue}\"");

                        whereList.Add($@"A.ObjectID in ( select ObjectID from AttributeDetail{tableHints} where AttributeTypeID = @{paramPrefix} and FormattedValue like @{paramPrefix}Value )"); 
                        //CONTAINS(FormattedValue, @{paramPrefix}Value)
                    }

                    if (filter is UiRequestFieldFilterValue)
                    {
                        var f = filter as UiRequestFieldFilterValue;

                        var thisFilterFieldType = useFieldNames ?
                            fieldTypes.FirstOrDefault(i => i.Name == f.FieldName) :
                            fieldTypes.FirstOrDefault(i => i.ID == int.Parse(f.FieldName.Replace("Field", "")));

                        if (thisFilterFieldType != null)
                        {
                            var inWhereClause = thisFilterFieldType.IsListable;
                            var bind = $"fld{thisFilterFieldType.ID}";
                            var filterFieldName = $"F{thisFilterFieldType.ID}.FormattedValue";
                            var valueColumnQuery = "";
                            switch (f.Condition)
                            {
                                case "EQUAL":
                                    dbArgs.Add(bind, $"{f.RawValue}");// $"\"{f.RawValue}\"");
                                    valueColumnQuery = $"{filterFieldName} = @{bind}";
                                    //valueColumnQuery = $"CONTAINS({nonPivotFieldName}, @{bind})"; 
                                    break;
                                case "CONTAINS":
                                    dbArgs.Add(bind, $"{wildcardValue(f.RawValue)}");
                                    valueColumnQuery = $"{filterFieldName} like @{bind}";
                                    break;
                                case "NOT_EQUAL":
                                    dbArgs.Add(bind, $"{f.RawValue}");
                                    valueColumnQuery = $"{filterFieldName} <> @{bind}";
                                    break;
                                case "DOES_NOT_CONTAIN":
                                    dbArgs.Add(bind, $"{wildcardValue(f.RawValue)}");
                                    valueColumnQuery = $"NOT {filterFieldName} not like @{bind}";
                                    break;
                                case "STARTS_WITH":
                                    dbArgs.Add(bind, $"{f.RawValue}%");
                                    valueColumnQuery = $"{filterFieldName} like @{bind}";
                                    break;
                                case "ENDS_WITH":
                                    dbArgs.Add(bind, $"%{f.RawValue}");
                                    valueColumnQuery = $"{filterFieldName} like @{bind}";
                                    break;
                                case "IN":
                                case "IN_MULTI":
                                    try
                                    {
                                        var values = f.RawValue.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i)).ToList();
                                        var concatenatedValue = string.Join(", ", values);
                                        dbArgs.Add(bind, concatenatedValue);
                                        valueColumnQuery = $"{filterFieldName} in (@{bind})";
                                    }
                                    catch { }
                                    break;
                                case "NULL":
                                    valueColumnQuery = $"{filterFieldName} is null";
                                    break;
                                case "NOT_NULL":
                                    valueColumnQuery = $"{filterFieldName} is not null";
                                    break;
                                case "EMPTY":
                                    valueColumnQuery = $"{filterFieldName} = ''";
                                    break;
                                case "NOT_EMPTY":
                                    valueColumnQuery = $"{filterFieldName} <> ''";
                                    break;
                                default:
                                    dbArgs.Add(bind, $"{wildcardValue(f.RawValue)}");
                                    valueColumnQuery = $"{filterFieldName} like @{bind}";
                                    break;
                            }

                            var filterInnerJoinPrefix = $"inner join Field F{thisFilterFieldType.ID}{tableHints} on F{thisFilterFieldType.ID}.AssetID = A.ID and F{thisFilterFieldType.ID}.FieldTypeID = {thisFilterFieldType.ID}";
                            if (inWhereClause)
                            {
                                if (thisFilterFieldType.AllowAllValue)
                                    whereList.Add($"({valueColumnQuery} or F{thisFilterFieldType.ID}.Value = '0')");
                                else
                                    whereList.Add($"{valueColumnQuery}");
                            }
                            else
                            {
                                if (thisFilterFieldType.AllowAllValue)
                                    filterJoinList.Add($"{filterInnerJoinPrefix} and ({valueColumnQuery} or F{thisFilterFieldType.ID}.Value = '0')");
                                else
                                    filterJoinList.Add($"{filterInnerJoinPrefix} and {valueColumnQuery}");
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

                            var paramPrefix = $"OwnerFilter{index}";
                            dbArgs.Add($"{paramPrefix}SA", securityAsset);
                            dbArgs.Add($"{paramPrefix}SAID", o.SecurityAssetID);
                            dbArgs.Add($"{paramPrefix}RTID", o.ResponsibilityTypeID);
                            whereList.Add($"A.ID in (select AssetID from ResponsibilityDetail where SecurityAsset = @{paramPrefix}SA and SecurityAssetID = @{paramPrefix}SAID and ResponsibilityTypeID = @{paramPrefix}RTID )");

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
                            whereList.Add(
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
                                whereList.Add(
            $@"A.ObjectID in (
select ObjectID from [Intersect]{tableHints} where IntersectTypeID = @{paramPrefix} and Subject = @{paramPrefix}Obj and SubjectID = {id})
union
select SubjectID from [Intersect]{tableHints} where IntersectTypeID = @{paramPrefix} and Object = @{paramPrefix}Obj and ObjectID = {id})
)"
                                    );
                            });
                        }
                    }
                }
            }
            else
            {
                dbArgs.Add("allValuesFilter", "0", System.Data.DbType.String, System.Data.ParameterDirection.Input);
                dbArgs.Add("simpleFilter", $"{wildcardValue(simpleFilter)}", System.Data.DbType.String, System.Data.ParameterDirection.Input);
                //dbArgs.Add("simpleFilter", $"\"{simpleFilter}*\"", System.Data.DbType.String, System.Data.ParameterDirection.Input);

                var simpleFilterIDs = string.Join(",", selectFields.Select(i => i.ID));
                filterJoinList.Add($@"
inner join (
		    select	AssetID
		    from	Field SF{tableHints} 
		    where	FieldTypeID in ({simpleFilterIDs})
				    --and (CONTAINS(FormattedValue, @simpleFilter) OR Value = @allValuesFilter)
                    and (FormattedValue like @simpleFilter OR Value = @allValuesFilter)
            group by AssetID
		    ) SF on SF.AssetID = A.ID ");
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
                        .Select(i => $"F{i.ID}.FormattedValue asc")//.Select(i => (useFieldNames ? $"[{i.Name}] asc" : $"[Field{i.ID}] asc"))
                );

                if (string.IsNullOrEmpty(orderFieldString))
                {
                    orderFieldString = string.Join(
                        ",",
                        selectFields
                            .Where(i => i.IsPartOfKey)
                            .OrderBy(i => i.ColumnOrder)
                            .Select(i => $"F{i.ID}.FormattedValue asc")//.Select(i => (useFieldNames ? $"[{i.Name}] asc" : $"[Field{i.ID}] asc"))
                    );
                }
            }
            else
            {
                var sortFieldType = (useFieldNames) ?
                    selectFields.SingleOrDefault(i => i.Name == sortField) :
                    selectFields.SingleOrDefault(i => sortField == $"Field{i.ID}");
                if (sortFieldType != null)
                {
                    orderFieldString = $"F{sortFieldType.ID}.FormattedValue {sortOrder}";//useFieldNames ? $"[{sortFieldType.Name}] {sortOrder}" : $"[Field{sortFieldType.ID}] {sortOrder}";
                }
            }

            if (string.IsNullOrEmpty(orderFieldString))
            {
                orderFieldString = "A.ID asc";
            }

            #endregion

            #region Join Lists above into SQL strings

            var selectFieldString = "";
            if (selectFieldList.Count > 0)
            {
                selectFieldString = ", " + string.Join(", ", selectFieldList);
            }

            var listableJoinString = "";
            if (listableJoinList.Count > 0)
            {
                listableJoinString = string.Join(" ", listableJoinList);
            }

            var filterJoinString = "";
            if (filterJoinList.Count > 0)
            {
                filterJoinString = string.Join(" ", filterJoinList);
            }

            var whereString = "";
            if (whereList.Count > 0)
            {
                whereString = string.Join(" and ", whereList);
                if (!string.IsNullOrEmpty(whereString))
                    whereString = $" and {whereString}";
            }

            #endregion

            var countSql = "";

            if (string.IsNullOrEmpty(whereString))
            {
                countSql = $@"
select	count(1)
from	Asset A{tableHints} 
        {filterJoinString} 
where   A.AssetTypeID = {at.ID} and A.State = 1 
OPTION (RECOMPILE)";
            }
            else
            {
                countSql = $@"
select	count(1)
from	Asset A{tableHints} 
        {listableJoinString} 
        {filterJoinString} 
where   A.AssetTypeID = @atID 
        and A.State = 1
        and A.ID not in (select AssetID from ResponsibilityDetail where PermissionsBitMask & {(int)Permission.ReadAsset} = 0 and ResourceID = @r) 
        and A.AssetTypeID not in (select AssetTypeID from ResponsibilityDetail where AssetID = 0 and PermissionsBitMask & {(int)Permission.ReadAsset} = 0 and ResourceID = @r)
{whereString} 
OPTION (RECOMPILE)";
            }

            count = Query<int>(countSql, dbArgs).Single();

            pageNumber = ((pageNumber < 0) ? 0 : pageNumber) * pageSize;

            if (pageSize < 0)
                pageSize = 25;

            var sql = $@"
select	A.ID as AssetID,
		A.Object,
		A.ObjectID      
		{selectFieldString}
        {parentSqlColumn}
		{editRightsColumnStatement}
from	Asset A{tableHints} 
        {parentSqlJoin} 
        {listableJoinString} 
        {filterJoinString} 
        {editRightsJoinStatement} 
where   A.AssetTypeID = @atID 
        and A.State = 1  
        and A.ID not in (select AssetID from ResponsibilityDetail where PermissionsBitMask & {(int)Permission.ReadAsset} = 0 and ResourceID = @r) 
        {whereString}
ORDER BY {orderFieldString}
OFFSET({pageNumber}) ROWS FETCH NEXT ({pageSize}) ROWS ONLY 
OPTION (RECOMPILE)";

            return Query<dynamic>(sql, dbArgs);
        }

        /// <summary>
        /// This is the pivot version of the method above, but the paging is where this design broke down.
        /// </summary>
        public IEnumerable<dynamic> GetPivotVersionDynamicAssets(AssetType at, List<UiRequestFilterValue> filters, out int count, int pageNumber = 0, int pageSize = 25, bool useFieldNames = false, string sortField = "", string sortOrder = "", string simpleFilter = "")
        {
            count = 0; //initialize

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
            var editRightsJoinStatement = "";

            if (!CurrentResourceIsAdmin)
            {
                editRightsColumnStatement = " IIF(S_E.[Count] = 0, 0, 1) as P_CanEdit, IIF(S_D.[Count] = 0, 0, 1) as P_CanDelete, ";
                editRightsJoinStatement = $@"
cross apply (select count(1) as [Count] from ResponsibilityDetail where ResourceID = @r and ( (AssetID = A.ID) or (AssetTypeID = A.AssetTypeID and AssetID = 0) ) and PermissionsBitMask & {(int)Permission.ModifyAsset} = {(int)Permission.ModifyAsset}) as S_E 
cross apply (select count(1) as [Count] from ResponsibilityDetail where ResourceID = @r and ( (AssetID = A.ID) or (AssetTypeID = A.AssetTypeID and AssetID = 0) ) and PermissionsBitMask & {(int)Permission.DeleteAsset} = {(int)Permission.DeleteAsset}) as S_D ";
            }

            #endregion

            #region Relationship Sql Syntax

            var relationshipCaseStatement = "";
            var relationshipJoinStatement = "";
            if (selectFields.Any(i => i.Type == "Relationship"))
            {
                if (selectFields.Any(i => i.Type == "Relationship"))
                {
                    relationshipCaseStatement = @" 
when FT.Type = 'Relationship' and FT.LookupObjectType = 'IntersectType' and F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID then F_R.ObjectName
when FT.Type = 'Relationship' and FT.LookupObjectType = 'IntersectType' and F_R.Object = A.Object and F_R.ObjectID = A.ObjectID then F_R.SubjectName ";
                }

                relationshipJoinStatement = $@"
 left join IntersectDetail F_R{tableHints} on	FT.LookupObjectType = 'IntersectType' 
										and F_R.IntersectTypeID = FT.LookupObjectID 
										and (
											(F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID) or 
											(F_R.Object = A.Object and F_R.ObjectID = A.ObjectID)
											) ";
            }

            #endregion

            #region Relationship Field Sql Syntax

            var fieldFromRelationshipCaseStatement = "";
            var fieldFromRelationshipJoinStatement = "";
            if (selectFields.Any(i => i.Type == "FieldFromRelationship"))
            {
                fieldFromRelationshipCaseStatement = @"when FT.Type = 'FieldFromRelationship' and FT.LookupObjectType = 'IntersectType' then F_RF.FormattedValue";
                fieldFromRelationshipJoinStatement = $@"
 left join [Intersect] F_R{tableHints} on	FT.LookupObjectType = 'IntersectType' 
										and F_R.IntersectTypeID = FT.LookupObjectID 
										and (
											(F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID) or 
											(F_R.Object = A.Object and F_R.ObjectID = A.ObjectID)
											)
left join Field F_RF{tableHints} on F_RF.ObjectType = case when F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID then F_R.Object else F_R.Subject end
						and F_RF.ObjectID = case when F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID then F_R.ObjectID else F_R.SubjectID end
						and F_RF.FieldTypeID = FT.LookupObjectFieldTypeID ";
            }

            #endregion

            #region Parent Sql Syntax

            var parentIntersectType = Filter<IntersectType>(i => i.Object == at.Object && i.ObjectID == at.ObjectID && i.Predicate.Type == core.enums.PredicateType.InterTypeHierarchy).FirstOrDefault();

            var parentOuterSqlColumn = @"";
            var parentSqlColumn = @"";
            var parentSqlJoin = @"";         
            var countRequireParentJoin = false;

            if (parentIntersectType != null)
            {
                parentOuterSqlColumn = @"ParentID, Parent, ParentUrl,";
                parentSqlColumn = @"arp.ParentArtifactID as ParentID, parp.DisplayValue as Parent, '' as ParentUrl, ";
                //parentSqlJoin = @" cross apply [dbo].[GetArtifactParentByAssetID](A.ID) PID";
                parentSqlJoin = @" inner join [utility].[ArtifactAssetParent] arp on a.id = arp.assetid cross apply [dbo].[GetArtifactDisplayValue](arp.ParentAssetID) parp";
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
                        dbArgs.Add($"{paramPrefix}Value", $"{wildcardValue(f.RawValue)}");//$"\"{f.RawValue}\"");

                        filterWhereList.Add(
    $@"A.ObjectID in (
select ObjectID from AttributeDetail{tableHints} where AttributeTypeID = @{paramPrefix} and FormattedValue like @{paramPrefix}Value
)"
                            ); //CONTAINS(FormattedValue, @{paramPrefix}Value)
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
                                    nonPivotInnerJoinPrefix = $"inner join Field F{thisFilterFieldType.ID}{tableHints} on F{thisFilterFieldType.ID}.AssetID = A.ID and F{thisFilterFieldType.ID}.FieldTypeID = {thisFilterFieldType.ID}";
                                }

                                var bind = $"fld{thisFilterFieldType.ID}";
                                var nonPivotFieldName = $"F{thisFilterFieldType.ID}.FormattedValue";
                                var valueColumnQuery = GetFilterCondition(f.Condition, nonPivotFieldName, bind, dbArgs, f.RawValue);
                                if (thisFilterFieldType.AllowAllValue)
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and ({valueColumnQuery} or F{thisFilterFieldType.ID}.Value = '0')");
                                else
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and {valueColumnQuery}");

                            }
                        }
                    }

                    if (filter is UiRequestOwnershipFilterValue)
                    {
                        var f = filter as UiRequestOwnershipFilterValue;

                        //var groupFilters = f.Items.Where(i => i.FilterType == UiRequestOwnershipFilterType.Group).ToList();
                        //var organizationFilters = f.Items.Where(i => i.FilterType == UiRequestOwnershipFilterType.Organization).ToList();
                        //var userFilters = f.Items.Where(i => i.FilterType == UiRequestOwnershipFilterType.User).ToList();

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

                            filterWhereList.Add($"A.ID in (select AssetID from ResponsibilityDetail{tableHints} where SecurityAsset = '{securityAsset}' and SecurityAssetID = {o.SecurityAssetID} and ResponsibilityTypeID = {o.ResponsibilityTypeID} )");

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
                dbArgs.Add("simpleFilter", $"{wildcardValue(simpleFilter)}", System.Data.DbType.String, System.Data.ParameterDirection.Input);             

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

                if (parentIntersectType != null)
                {   
                    var joinInfo = fieldFromRelationshipAssets.Any() ? "" : " ) SF on SF.AssetID = A.ID ";
                    
                    filterJoinList.Add($@"
                        inner join (
		                            select	SF.AssetID
		                            from	Field SF{tableHints}                                     
                                    inner join[utility].[ArtifactAssetParent] arp on SF.AssetID = arp.AssetID cross apply [dbo].[GetArtifactDisplayValue](arp.ParentAssetID) parp
                                    where	FieldTypeID in ({simpleFilterIDs})				                            
                                            and (FormattedValue like @simpleFilter OR parp.DisplayValue like @simpleFilter)
                                    group by SF.AssetID
		                            {joinInfo} 
                                   {fieldFromRelationshipAssets}");
                }
                else
                {
                    var simpleFilterFieldJoin = string.IsNullOrEmpty(fieldFromRelationshipAssets) ? ") SF on SF.AssetID = A.ID " : fieldFromRelationshipAssets;

                    filterJoinList.Add($@"
                        inner join (
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
            if (filterWhereList.Count > 0)
            {
                filterWhereString = string.Join(" and ", filterWhereList);
                if (!string.IsNullOrEmpty(filterWhereString))
                    filterWhereString = $"and {filterWhereString}";
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
        and not exists (select 1 from ResponsibilityDetail where PermissionsBitMask & {(int)Permission.ReadAsset} = 0 and ResourceID = @r and AssetID = A.ID)
        and not exists (select 1 from ResponsibilityDetail where PermissionsBitMask & {(int)Permission.ReadAsset} = 0 and ResourceID = @r and AssetTypeID = A.AssetTypeID and AssetID = 0)
        {filterWhereString}
OPTION (RECOMPILE)";

            count = Query<int>(countSql, dbArgs).Single();
            //count = ExecuteQuery<int>(countSql, countParameters).Single();
            
            pageNumber = (pageNumber < 0) ? 0 : pageNumber;
            if (pageSize < 0)
                pageSize = 25;
            pageNumber = ((pageNumber < 0) ? 0 : pageNumber) * pageSize;

            var sql = $@"
select	*
from	(
		select	AssetID, Object, ObjectID, Type, TypeID, 
               {parentOuterSqlColumn}
                P_CanEdit, P_CanDelete,
				{selectFieldString}
		from	(
				select	A.ID as AssetID,
						A.Object,
						A.ObjectID,        
						AST.Object as Type,
						AST.ObjectID as TypeID,
                        {parentSqlColumn}
						FT.ID as FieldTypeID,
                        {editRightsColumnStatement}
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
                        {editRightsJoinStatement} 
						inner join FieldType FT{tableHints} on FT.ID in ({selectFieldIDs}) and FT.AssetTypeID = A.AssetTypeID
						left join Field F_O{tableHints} on F_O.AssetID = A.ID and F_O.FieldTypeID = FT.ID
						{relationshipJoinStatement}
						{fieldFromRelationshipJoinStatement} 
                where   not exists (select 1 from ResponsibilityDetail where PermissionsBitMask & {(int)Permission.ReadAsset} = 0 and ResourceID = @r and AssetID = A.ID)
                        and not exists (select 1 from ResponsibilityDetail where PermissionsBitMask & {(int)Permission.ReadAsset} = 0 and ResourceID = @r and AssetTypeID = A.AssetTypeID and AssetID = 0)
                        {filterWhereString}
				) A
		pivot	(
				MIN([Field]) for FieldTypeID in ({pivotFieldIDs})
				) pvt
		ORDER BY {orderFieldString}
		OFFSET({pageNumber}) ROWS FETCH NEXT ({pageSize}) ROWS ONLY
		) A
OPTION (RECOMPILE)";

            return Query<dynamic>(sql, dbArgs);
            //var items = ExecuteQuery<dynamic>(sql, queryParameters);
            //Dapper.SqlMapper.Parse()
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
                    dbArgs.Add(bind, $"{value}");// $"\"{f.RawValue}\"");
                    valueColumnQuery = $"{fieldName} = @{bind}";
                    //valueColumnQuery = $"CONTAINS({nonPivotFieldName}, @{bind})"; 
                    break;
                case "CONTAINS":
                    dbArgs.Add(bind, $"{wildcardValue(value)}");
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
                        var values = value.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries);//.Select(i => int.Parse(i)).ToList();
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
                //case "IN_MULTI":
                //    var multiValues = f.RawValue.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries).Select(i => $"\"{i}\"").ToList();
                //    var multiConcatenatedValue = string.Join($" {f.Operator} ", multiValues);
                //    dbArgs.Add(bind, multiConcatenatedValue);
                //    valueColumnQuery = $"CONTAINS({nonPivotFieldName}, @{bind})";
                //    break;
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
