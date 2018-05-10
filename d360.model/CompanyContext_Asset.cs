using d360.core.entities;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        #region Engine Methods

        /// <summary>
        /// Not actively used at this point. Just for reference for performance comparison.
        /// </summary>
        public IEnumerable<dynamic> GetDynamicAssets(AssetType at, List<UiRequestFilterValue> filters, out int count, int pageNumber = 1, int pageSize = 25, bool useFieldNames = false, string sortField = "", string sortOrder = "", string simpleFilter = "")
        {
            count = 0; //initialize

            var assetTypeID = at.ID;

            var fieldTypes = Filter<FieldType>(i => i.AssetTypeID == assetTypeID).ToList();
            var selectFields = fieldTypes.Where(i => i.IsListable).OrderBy(i => i.ColumnOrder).ToList();

            #region Administrator Editability Sql Syntax

            var editRightsColumnStatement = "1 as P_CanEdit, 1 as P_CanDelete ";
            var editRightsJoinStatement = "";

            if (!CurrentResourceIsAdmin)
            {
                editRightsColumnStatement = " IIF(S_E.AssetID is null, 0, 1) as P_CanEdit, IIF(S_D.AssetID is null, 0, 1) as P_CanDelete ";
                editRightsJoinStatement = @"
left join responsibility.ClaimCore S_E on S_E.ResourceID = {CurrentResourceID} and S_E.ClaimObject = 1 and S_E.Claim = 3 and S_E.AssetID = A.ID 
left join responsibility.ClaimCore S_D on S_D.ResourceID = {CurrentResourceID} and S_D.ClaimObject = 1 and S_D.Claim = 4 and S_D.AssetID = A.ID ";
            }

            #endregion

            #region Filter Processing

            var filterJoinList = new List<string>();
            var whereList = new List<string>();

            var dbArgs = new DynamicParameters();
            foreach (var filter in filters)
            {
                if (filter is UiRequestAttributeFilterValue)
                {
                    var f = filter as UiRequestAttributeFilterValue;

                    var paramPrefix = $"AttType{f.AttributeTypeID}";
                    dbArgs.Add($"{paramPrefix}", f.AttributeTypeID);
                    dbArgs.Add($"{paramPrefix}Value", $"{f.RawValue}%");
                    whereList.Add($@"A.ObjectID in ( select ObjectID from [Attribute] where AttributeTypeID = @{paramPrefix} and FormattedValue like @{paramPrefix}Value ) ");
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
                        var columnName = $"F{thisFilterFieldType.ID}.FormattedValue";
                        var bind = $"fld{thisFilterFieldType.ID}";

                        if (thisFilterFieldType.AllowAllValue)
                        {
                            //allItemsBind = $"{prefix}{filterNumber}val_all";
                            //allValueBind = $"{thisFilterFieldType.AllowAllLabel.Replace("'", "''")}";
                        }

                        if (thisFilterFieldType.AllowMultipleValues)
                        {
                            if (f.Condition == "IN")
                                f.Condition = "IN_MULTI";
                            else
                                f.Condition = "CONTAINS";
                        }

                        var joinPrefix = $"inner join Field F{thisFilterFieldType.ID} on F{thisFilterFieldType.ID}.AssetID = A.ID and F{thisFilterFieldType.ID}.FieldTypeID = {thisFilterFieldType.ID}";
                        switch (f.Condition)
                        {
                            case "EQUAL":
                                dbArgs.Add(bind, $"\"{f.RawValue}\"");
                                if (inWhereClause)
                                    whereList.Add($"CONTAINS({columnName}, @{bind})");
                                else
                                    filterJoinList.Add($"{joinPrefix} and CONTAINS({columnName}, @{bind})");
                                break;
                            case "CONTAINS":
                                dbArgs.Add(bind, $"\"{f.RawValue}*\"");
                                if (inWhereClause)
                                    whereList.Add($"CONTAINS({columnName}, @{bind})");
                                else
                                    filterJoinList.Add($"{joinPrefix} and CONTAINS({columnName}, @{bind})");
                                break;
                            case "NOT_EQUAL":
                            case "DOES_NOT_CONTAIN":
                                dbArgs.Add(bind, $"\"{f.RawValue}\"");
                                if (inWhereClause)
                                    whereList.Add($"NOT CONTAINS({columnName}, @{bind})");
                                else
                                    filterJoinList.Add($"{joinPrefix} and NOT CONTAINS({columnName}, @{bind})");
                                break;
                            case "STARTS_WITH":
                                dbArgs.Add(bind, $"\"{f.RawValue}*\"");
                                if (inWhereClause)
                                    whereList.Add($"CONTAINS({columnName}, @{bind})");
                                else
                                    filterJoinList.Add($"{joinPrefix} and CONTAINS({columnName}, @{bind}");
                                break;
                            case "ENDS_WITH":
                                dbArgs.Add(bind, $"%{f.RawValue}");
                                if (inWhereClause)
                                    whereList.Add($"{columnName} LIKE @{bind}");
                                else
                                    filterJoinList.Add($"{joinPrefix} and {columnName} LIKE @{bind}");
                                break;
                            case "IN":
                                var values = f.RawValue.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries).Select(i => $"\"{i}\"").ToList();
                                var concatenatedValue = string.Join(" or ", values);
                                dbArgs.Add(bind, concatenatedValue);
                                if (inWhereClause)
                                    whereList.Add($"CONTAINS({columnName}, @{bind})");
                                else
                                    filterJoinList.Add($"{joinPrefix} and CONTAINS({columnName}, @{bind})");
                                break;
                            case "IN_MULTI":
                                var multiValues = f.RawValue.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries).Select(i => $"\"{i}\"").ToList();
                                var multiConcatenatedValue = string.Join($" {f.Operator} ", multiValues);
                                dbArgs.Add(bind, multiConcatenatedValue);
                                if (inWhereClause)
                                    whereList.Add($"CONTAINS({columnName}, @{bind})");
                                else
                                    filterJoinList.Add($"{joinPrefix} and CONTAINS({columnName}, @{bind})");
                                break;
                            case "NULL":
                                if (inWhereClause)
                                    whereList.Add($"{columnName} is null");
                                else
                                    filterJoinList.Add($"{joinPrefix} and {columnName} is null");
                                break;
                            case "NOT_NULL":
                                if (inWhereClause)
                                    whereList.Add($"{columnName} is not null");
                                else
                                    filterJoinList.Add($"{joinPrefix} and {columnName} is not null");
                                break;
                            case "EMPTY":
                                if (inWhereClause)
                                    whereList.Add($"{columnName} = ''");
                                else
                                    filterJoinList.Add($"{joinPrefix} and {columnName} = ''");
                                break;
                            case "NOT_EMPTY":
                                if (inWhereClause)
                                    whereList.Add($"{columnName} <> ''");
                                else
                                    filterJoinList.Add($"{joinPrefix} and {columnName} <> ''");
                                break;
                            default:
                                dbArgs.Add(bind, $"\"{f.RawValue}\"");
                                if (inWhereClause)
                                    whereList.Add($"CONTAINS({columnName}, @{bind})");
                                else
                                    filterJoinList.Add($"{joinPrefix} and CONTAINS({columnName}, @{bind})");
                                break;
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

                        whereList.Add($"A.ID in (select AssetID from ResponsibilityDetails where SecurityAsset = '{securityAsset}' and SecurityAssetID = {o.SecurityAssetID} and ResponsibilityTypeID = {o.ResponsibilityTypeID} )");

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
select ObjectID from [Intersect] where IntersectTypeID = @{paramPrefix} and Subject = @{paramPrefix}Obj and SubjectID in ({idList})
union
select SubjectID from [Intersect] where IntersectTypeID = @{paramPrefix} and Object = @{paramPrefix}Obj and ObjectID in ({idList})
)"
                            );
                    }
                    else
                    {
                        f.TargetObjectIDs.ForEach(id =>
                        {
                            whereList.Add(
        $@"A.ObjectID in (
select ObjectID from [Intersect] where IntersectTypeID = @{paramPrefix} and Subject = @{paramPrefix}Obj and SubjectID = {id})
union
select SubjectID from [Intersect] where IntersectTypeID = @{paramPrefix} and Object = @{paramPrefix}Obj and ObjectID = {id})
)"
                                );
                        });
                    }
                }
            }

            #endregion

            var selectFieldList = new List<string>();
            var listableJoinList = new List<string>();

            selectFields.ForEach(ft => {

                var columnName = useFieldNames ? $"{ft.Name}" : $"Field{ft.ID}";

                switch (ft.Type)
                {
                    case "Relationship":
                        #region Relationship Sql Syntax

                        selectFieldList.Add($@"case when F{ft.ID}.Subject = A.Object and F{ft.ID}.SubjectID = A.ObjectID then F{ft.ID}.ObjectName else F{ft.ID}.SubjectName end as {columnName}");
                        listableJoinList.Add($@"left join IntersectDetail F{ft.ID} on F{ft.ID}.IntersectTypeID = {ft.LookupObjectID} and ( (F{ft.ID}.Subject = A.Object and F{ft.ID}.SubjectID = A.ObjectID) or  (F{ft.ID}.Object = A.Object and F{ft.ID}.ObjectID = A.ObjectID) ) ");

                        #endregion
                        break;
                    case "FieldFromRelationship":
                        #region Relationship Field Sql Syntax

                        selectFieldList.Add($@"F{ft.ID}.FormattedValue as {columnName} ");
                        listableJoinList.Add($@"
left join Field F{ft.ID} on FT{ft.ID}.ID = {ft.LookupObjectFieldTypeID} and F{ft.ID}.ObjectType = case when F{ft.ID}.Subject = A.Object and F{ft.ID}.SubjectID = A.ObjectID then F{ft.ID}.Object else F{ft.ID}.Subject end 
and F{ft.ID}.ObjectType = case when F{ft.ID}.Subject = A.Object and F{ft.ID}.SubjectID = A.ObjectID then F{ft.ID}.ObjectID else F{ft.ID}.SubjectID end 
and F{ft.ID}.FieldTypeID = {ft.LookupObjectFieldTypeID} ");

                        #endregion
                        break;
                    default:
                        #region Regular Field
                        /*
                        switch (f.Type)
                        {
                            case "Decimal":

                                columns += $@"
        case     
            when {name}_T.Value is not null then cast({name}_T.FormattedValue as decimal(38,6))
            when {name}_TT.DefaultValue is not null then cast({name}_TT.DefaultFormattedValue  as decimal(38,6))
            else null 
        end as [{name}], ";
                                break;
                            case "Number":
                                columns += $@"
        case     
            when {name}_T.Value is not null then cast({name}_T.FormattedValue as bigint)
            when {name}_TT.DefaultValue is not null then cast({name}_TT.DefaultFormattedValue  as bigint)
            else null 
        end as [{name}], ";
                                break;
                            case "DateTime":
                                columns += $@"
        case     
            when {name}_T.Value is not null then cast({name}_T.FormattedValue as datetime)
            when {name}_TT.DefaultValue is not null then cast({name}_TT.DefaultFormattedValue  as datetime)
            else null 
        end as [{name}], ";
                                break;
                            default:
                                columns += $@"
        case 
            when {name}_TT.AllowAllValue = 1 and {name}_T.Value = '0' then {name}_TT.AllowAllLabel 
            when {name}_T.Value is not null then {name}_T.FormattedValue 
            when {name}_TT.DefaultValue is not null then {name}_TT.DefaultFormattedValue 
            else '' 
        end as [{name}], ";
                                break;
                        }                         
                         */
                        selectFieldList.Add($@"case 
	when FT{ft.ID}.AllowAllValue = 1 and F{ft.ID}.FormattedValue = '0' then cast(FT{ft.ID}.AllowAllLabel as nvarchar(max))
	when F{ft.ID}.FormattedValue is not null then F{ft.ID}.FormattedValue
	when FT{ft.ID}.DefaultFormattedValue is not null then cast(FT{ft.ID}.DefaultFormattedValue as nvarchar(max))
	else null
end as {columnName} ");
                        listableJoinList.Add($@"inner join FieldType FT{ft.ID} on FT{ft.ID}.AssetTypeID = A.AssetTypeID and FT{ft.ID}.ID = {ft.ID} left join Field F{ft.ID} on F{ft.ID}.FieldTypeID = FT{ft.ID}.ID and F{ft.ID}.AssetID = A.ID ");

                        #endregion
                        break;
                }
            });

            #region Parent Sql Syntax

            var parentIntersectType = Filter<IntersectType>(i => i.Object == at.Object && i.ObjectID == at.ObjectID && i.Predicate.Type == core.enums.PredicateType.InterTypeHierarchy).FirstOrDefault();

            var parentSqlColumn = @"";
            var parentSqlJoin = @"";

            if (parentIntersectType != null)
            {
                parentSqlColumn = @"PID.ParentID, PID.ParentDisplayValue as Parent, PID.ParentUrl, ";
                parentSqlJoin = @" cross apply [dbo].[GetArtifactParentByAssetID](A.ID) PID";
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
                        .Select(i => (useFieldNames ? $"[{i.Name}] asc" : $"[Field{i.ID}] asc"))
                );

                if (string.IsNullOrEmpty(orderFieldString))
                {
                    orderFieldString = string.Join(
                        ",",
                        selectFields
                            .Where(i => i.IsPartOfKey)
                            .OrderBy(i => i.ColumnOrder)
                            .Select(i => (useFieldNames ? $"[{i.Name}] asc" : $"[Field{i.ID}] asc"))
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
                    orderFieldString = useFieldNames ? $"[{sortFieldType.Name}] {sortOrder}" : $"[Field{sortFieldType.ID}] {sortOrder}";
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
                selectFieldString = string.Join(", ", selectFieldList) + ", ";
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
from	Asset A 
        {filterJoinString} 
where   A.AssetTypeID = {at.ID} and A.State = 1
";
            }
            else
            {
                countSql = $@"
select	count(1)
from	Asset A 
        {listableJoinString} 
        {filterJoinString} 
where   A.AssetTypeID = {at.ID} and A.State = 1
        {whereString}
";
            }

            count = Query<int>(countSql, dbArgs).Single();

            pageNumber = (pageNumber > 0) ? pageNumber - 1 : 0;
            if (pageSize < 0)
                pageSize = 25;

            var sql = $@"
select	A.ID as AssetID,
		A.Object,
		A.ObjectID,        
		{selectFieldString}
        {parentSqlColumn}
		{editRightsColumnStatement}
from	Asset A 
        {parentSqlJoin} 
        {listableJoinString} 
        {filterJoinString} 
        {editRightsJoinStatement} 
where   A.AssetTypeID = {at.ID} and A.State = 1  
        {whereString}
ORDER BY {orderFieldString}
OFFSET({pageNumber}) ROWS FETCH NEXT ({pageSize}) ROWS ONLY";

            return Query<dynamic>(sql, dbArgs);
        }

        /// <summary>
        /// This is the pivot version of the method above, but the sort is where this design broke down.
        /// </summary>
        public IEnumerable<dynamic> GetPivotVersionDynamicAssets(AssetType at, List<UiRequestFilterValue> filters, out int count, int pageNumber = 1, int pageSize = 25, bool useFieldNames = false, string sortField = "", string sortOrder = "", string simpleFilter = "")
        {
            count = 0; //initialize

            var assetTypeID = at.ID;

            var fieldTypes = Filter<FieldType>(i => i.AssetTypeID == assetTypeID).ToList();
            var selectFields = fieldTypes.Where(i => i.IsListable).OrderBy(i => i.ColumnOrder).ToList();

            var selectFieldString = string.Join(",", selectFields.Select(i => (useFieldNames ? $"[{i.ID}] as {i.Name}" : $"[{i.ID}] as Field{i.ID}")));

            var selectFieldIDs = string.Join(",", selectFields.Select(i => $"{i.ID}"));
            var pivotFieldIDs = string.Join(",", selectFields.Select(i => $"[{i.ID}]"));

            var tableHints = " with (NOLOCK)";

            #region Administrator Editability Sql Syntax

            var editRightsColumnStatement = "1 as P_CanEdit, 1 as P_CanDelete,";
            var editRightsJoinStatement = "";

            if (!CurrentResourceIsAdmin)
            {
                editRightsColumnStatement = " IIF(S_E.AssetID is null, 0, 1) as P_CanEdit, IIF(S_D.AssetID is null, 0, 1) as P_CanDelete, ";
                editRightsJoinStatement = $@"
left join cache.AssetEdit S_E{tableHints} on S_E.ResourceID = {CurrentResourceID} and S_E.AssetID = A.ID 
left join cache.AssetDelete S_D{tableHints} on S_D.ResourceID = {CurrentResourceID} and S_D.AssetID = A.ID ";
            }

            #endregion

            #region Relationship Sql Syntax

            var relationshipCaseStatement = "";
            var relationshipJoinStatement = "";
            if (selectFields.Any(i => i.Type == "Relationship"))
            {
                relationshipCaseStatement = @" 
when FT.Type = 'Relationship' and FT.LookupObjectType = 'IntersectType' and F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID then F_R.ObjectName
when FT.Type = 'Relationship' and FT.LookupObjectType = 'IntersectType' and F_R.Object = A.Object and F_R.ObjectID = A.ObjectID then F_R.SubjectName ";
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
 left join Field F_RF{tableHints} on F_RF.ObjectType = case when F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID then F_R.Object else F_R.Subject end
						and F_RF.ObjectType = case when F_R.Subject = A.Object and F_R.SubjectID = A.ObjectID then F_R.ObjectID else F_R.SubjectID end
						and F_RF.FieldTypeID = FT.LookupObjectFieldTypeID ";
            }

            #endregion

            #region Parent Sql Syntax

            var parentIntersectType = Filter<IntersectType>(i => i.Object == at.Object && i.ObjectID == at.ObjectID && i.Predicate.Type == core.enums.PredicateType.InterTypeHierarchy).FirstOrDefault();

            var parentOuterSqlColumn = @"";
            var parentSqlColumn = @"";
            var parentSqlJoin = @"";

            if (parentIntersectType != null)
            {
                parentOuterSqlColumn = @"ParentID, Parent, ParentUrl,";
                parentSqlColumn = @"PID.ParentID, PID.ParentDisplayValue as Parent, PID.ParentUrl, ";
                parentSqlJoin = @" cross apply [dbo].[GetArtifactParentByAssetID](A.ID) PID";
            }

            #endregion

            #region Filter Processing

            var filterJoinList = new List<string>();
            var filterWhereList = new List<string>();

            var dbArgs = new DynamicParameters();

            if (string.IsNullOrEmpty(simpleFilter))
            {
                foreach (var filter in filters)
                {
                    if (filter is UiRequestAttributeFilterValue)
                    {
                        var f = filter as UiRequestAttributeFilterValue;

                        var paramPrefix = $"AttType{f.AttributeTypeID}";
                        dbArgs.Add($"{paramPrefix}", f.AttributeTypeID);
                        dbArgs.Add($"{paramPrefix}Value", $"\"{f.RawValue}\"");
                        filterWhereList.Add(
    $@"A.ObjectID in (
select ObjectID from AttributeDetail{tableHints} where AttributeTypeID = @{paramPrefix} and CONTAINS(FormattedValue, @{paramPrefix}Value)
)"
                            );
                    }

                    if (filter is UiRequestFieldFilterValue)
                    {
                        var f = filter as UiRequestFieldFilterValue;

                        var thisFilterFieldType = useFieldNames ?
                            fieldTypes.FirstOrDefault(i => i.Name == f.FieldName) :
                            fieldTypes.FirstOrDefault(i => i.ID == int.Parse(f.FieldName.Replace("Field", "")));

                        if (thisFilterFieldType != null)
                        {
                            if (thisFilterFieldType.AllowAllValue)
                            {
                                //allItemsBind = $"{prefix}{filterNumber}val_all";
                                //allValueBind = $"{thisFilterFieldType.AllowAllLabel.Replace("'", "''")}";
                            }

                            if (thisFilterFieldType.AllowMultipleValues)
                            {
                                if (f.Condition == "IN")
                                    f.Condition = "IN_MULTI";
                                else
                                    f.Condition = "CONTAINS";
                            }

                            var bind = $"fld{thisFilterFieldType.ID}";
                            var nonPivotFieldName = $"F{thisFilterFieldType.ID}.FormattedValue";
                            var nonPivotInnerJoinPrefix = $"inner join Field F{thisFilterFieldType.ID}{tableHints} on F{thisFilterFieldType.ID}.AssetID = A.ID and F{thisFilterFieldType.ID}.FieldTypeID = {thisFilterFieldType.ID}";
                            switch (f.Condition)
                            {
                                case "EQUAL":
                                    dbArgs.Add(bind, $"\"{f.RawValue}\"");
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and CONTAINS({nonPivotFieldName}, @{bind})");
                                    break;
                                case "CONTAINS":
                                    dbArgs.Add(bind, $"\"{f.RawValue}*\"");
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and CONTAINS({nonPivotFieldName}, @{bind})");
                                    break;
                                case "NOT_EQUAL":
                                case "DOES_NOT_CONTAIN":
                                    dbArgs.Add(bind, $"\"{f.RawValue}\"");
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and NOT CONTAINS({nonPivotFieldName}, @{bind})");
                                    break;
                                case "STARTS_WITH":
                                    dbArgs.Add(bind, $"\"{f.RawValue}*\"");
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and CONTAINS({nonPivotFieldName}, @{bind}");
                                    break;
                                case "ENDS_WITH":
                                    dbArgs.Add(bind, $"%{f.RawValue}");
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and {nonPivotFieldName} LIKE @{bind}");
                                    break;
                                case "IN":
                                    var values = f.RawValue.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries).Select(i => $"\"{i}\"").ToList();
                                    var concatenatedValue = string.Join(" or ", values);
                                    dbArgs.Add(bind, concatenatedValue);
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and CONTAINS({nonPivotFieldName}, @{bind})");
                                    break;
                                case "IN_MULTI":
                                    var multiValues = f.RawValue.Split(new string[] { "!~!" }, StringSplitOptions.RemoveEmptyEntries).Select(i => $"\"{i}\"").ToList();
                                    var multiConcatenatedValue = string.Join($" {f.Operator} ", multiValues);
                                    dbArgs.Add(bind, multiConcatenatedValue);
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and CONTAINS({nonPivotFieldName}, @{bind})");
                                    break;
                                case "NULL":
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and {nonPivotFieldName} is null");
                                    break;
                                case "NOT_NULL":
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and {nonPivotFieldName} is not null");
                                    break;
                                case "EMPTY":
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and {nonPivotFieldName} = ''");
                                    break;
                                case "NOT_EMPTY":
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and {nonPivotFieldName} <> ''");
                                    break;
                                default:
                                    dbArgs.Add(bind, $"\"{f.RawValue}\"");
                                    filterJoinList.Add($"{nonPivotInnerJoinPrefix} and CONTAINS({nonPivotFieldName}, @{bind})");
                                    break;
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

                            filterWhereList.Add($"A.ID in (select AssetID from ResponsibilityDetails{tableHints} where SecurityAsset = '{securityAsset}' and SecurityAssetID = {o.SecurityAssetID} and ResponsibilityTypeID = {o.ResponsibilityTypeID} )");

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
                }
            }
            else
            {
                dbArgs.Add("simpleFilter", $"\"{simpleFilter}*\"");
                var simpleFilterIDs = string.Join(",", selectFields.Select(i => i.ID));
                filterJoinList.Add($@"
cross apply (
		    select	count(1) as [Count]
		    from	Field SF{tableHints} 
		    where	SF.AssetID = A.ID
				    and SF.FieldTypeID in ({simpleFilterIDs})
				    and CONTAINS(SF.FormattedValue, @simpleFilter) 
		    ) SF ");

                filterWhereList.Add("SF.[Count] > 0");
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
                        .Select(i => (useFieldNames ? $"[{i.Name}] asc" : $"[Field{i.ID}] asc"))
                );

                if (string.IsNullOrEmpty(orderFieldString))
                {
                    orderFieldString = string.Join(
                        ",",
                        selectFields
                            .Where(i => i.IsPartOfKey)
                            .OrderBy(i => i.ColumnOrder)
                            .Select(i => (useFieldNames ? $"[{i.Name}] asc" : $"[Field{i.ID}] asc"))
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
                    orderFieldString = useFieldNames ? $"[{sortFieldType.Name}] {sortOrder}" : $"[Field{sortFieldType.ID}] {sortOrder}";
                }
            }

            #endregion

            var countSql = $@"
select	count(1)
from	Asset A{tableHints}
        {filterJoinString} 
where	A.AssetTypeID = 1 
		and A.State = 1
        and A.ID not in (select AssetID from cache.NoRead where ResourceID = {CurrentResourceID})
        {filterWhereString}
OPTION (RECOMPILE)
";

            count = Query<int>(countSql, dbArgs).Single();

            pageNumber = (pageNumber > 0) ? pageNumber - 1 : 0;
            if (pageSize < 0)
                pageSize = 25;

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
						case 
							when FT.AllowAllValue = 1 and F_O.Value = '0' then FT.AllowAllLabel 
                            when F_O.Value is not null then F_O.FormattedValue
							when FT.DefaultValue is not null then FT.DefaultFormattedValue 
							{relationshipCaseStatement}
							{fieldFromRelationshipCaseStatement}
							else '' 
						end as [Field],
						{editRightsColumnStatement}
						dbo.GenerateObjectUrl('Artifact', AST.ObjectID, A.ObjectID) as Url
				from	Asset A{tableHints}
                        {parentSqlJoin} 
                        inner join AssetType AST{tableHints} on AST.ID = A.AssetTypeID and AST.ID = {at.ID} and A.State = 1  
                        {filterJoinString} 
                        {editRightsJoinStatement} 
						inner join FieldType FT{tableHints} on FT.ID in ({selectFieldIDs}) and FT.AssetTypeID = A.AssetTypeID
						left join Field F_O{tableHints} on F_O.AssetID = A.ID and F_O.FieldTypeID = FT.ID
						{relationshipJoinStatement}
						{fieldFromRelationshipJoinStatement} 
                where   A.ID not in (select AssetID from cache.NoRead where ResourceID = {CurrentResourceID})
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
        }

        #endregion
    }
}
