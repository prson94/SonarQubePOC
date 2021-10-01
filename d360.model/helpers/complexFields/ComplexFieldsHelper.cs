using d360.core.entities;
using d360.core.Models;
using Dapper;
using System.Collections.Generic;
using System.Linq;

namespace d360.model.helpers
{
    public class ComplexFieldsHelper
    {
        public static string GetComplexRelationLookupSQL(FieldTypeComplexLookupDefinition definition, DynamicParameters dbArgs, List<FieldType> fields, out List<string> selects, bool isCountQuery = false)
        {
            selects = new List<string>();
            List<string> joins = new List<string>();
            int idx = 1;
            foreach (var rel in definition.Relations)
            {
                selects.Add($"dbo.GenerateAssetUrl(H{idx}.ID) as [H{idx}_Url]");
                selects.Add($"H{idx}.Uid as [H{idx}_Uid]");

                joins.Add($"inner join graph.AssetEdge R{idx} on R{idx}.$to_id = H{(idx == 1 ? idx : idx - 1)}.$node_id and R{idx}.IntersectTypeUID = '{rel.IntersectTypeUid}'");
                joins.Add($"inner join graph.AssetNode {(idx == 1 ? $"A{idx}" : $"H{idx}")} on {(idx == 1 ? $"A{idx}" : $"H{idx}")}.$node_id = R{idx}.$from_id {(idx == 1 ? $" and A{idx}.uid = @assetuid" : "")}");
                idx++;
            }

            foreach (var f in definition.Fields)
            {
                var ft = fields.FirstOrDefault(x => x.ID == f.FieldTypeID);

                if (ft == null && f.FieldTypeName.StartsWith("Related Item."))
                {
                    var index = f.RelationIndex + 1;

                    selects.Add($"H{index}_A{f.FieldTypeID}.Uid AS [H{index}_{f.FieldTypeID}_Uid]");
                    selects.Add($"H{index}_A{f.FieldTypeID}_DV.DisplayValue AS [H{index}_{f.FieldTypeID}_DisplayValue]");
                    selects.Add($"H{index}_R{f.FieldTypeID}.IntersectTypeUid AS [H{index}_{f.FieldTypeID}_IntersectTypeUid]");

                    joins.Add($@"LEFT JOIN graph.AssetEdge H{index}_R{f.FieldTypeID} ON H{index}_R{f.FieldTypeID}.$to_id = H{index}.$node_id AND H{index}_R{f.FieldTypeID}.IntersectTypeID = {f.FieldTypeID}
                                         LEFT JOIN graph.AssetNode H{index}_A{f.FieldTypeID} ON H{index}_A{f.FieldTypeID}.$node_id = H{index}_R{f.FieldTypeID}.$from_id
                                         LEFT JOIN AssetDisplayValue H{index}_A{f.FieldTypeID}_DV ON H{index}_A{f.FieldTypeID}_DV.AssetID = H{index}_A{f.FieldTypeID}.ID");
                }
                else if (f.FieldTypeID != 0)
                {
                    string fieldSelector = $"H{f.RelationIndex + 1}_F{f.FieldTypeID}";
                    string fieldAlias = $"H{f.RelationIndex + 1}_{f.FieldTypeID}";

                    switch (ft.Type.ToLowerInvariant())
                    {
                        case "boolean":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS bit) AS [{fieldAlias}]");
                            break;
                        case "number":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS int) AS [{fieldAlias}]");
                            break;
                        case "decimal":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS decimal) AS [{fieldAlias}]");
                            break;
                        case "date":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS date) AS [{fieldAlias}]");
                            break;
                        case "datetime":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS datetime) AS [{fieldAlias}]");
                            break;
                        case "counter":
                            string cnt_prefix = "cntprefix_" + ft.ID;
                            dbArgs.Add(cnt_prefix, ft.CounterPrefix);
                            selects.Add($"(@{cnt_prefix} + try_cast({fieldSelector}.FormattedValue AS nvarchar(20))) AS [{fieldAlias}]");
                            break;
                        default:
                            selects.Add($"{fieldSelector}.FormattedValue as {fieldAlias}");
                            break;
                    }

                    if (f.FieldTypeName.StartsWith("Relation."))
                    {
                        joins.Add($@"LEFT JOIN Field {fieldSelector} ON {fieldSelector}.ObjectType = 'Intersect'
                            AND {fieldSelector}.FieldTypeID = {f.FieldTypeID}
                            AND {fieldSelector}.ObjectID = R1.ID
                            AND {fieldSelector}.FormattedValue <> ''");
                    }
                    else if (ft.Type == "Counter")
                    {
                        joins.Add($@"OUTER apply
                              (SELECT top 1 [Value] AS FormattedValue
                               FROM dbo.FieldCounterValue
                               WHERE AssetId = H{f.RelationIndex + 1}.ID
                                 AND FieldTypeId = {ft.ID}){fieldSelector}");
                    }
                    else
                    {
                        joins.Add($"left join FieldDetail {fieldSelector} on {fieldSelector}.FieldTypeID = {f.FieldTypeID} and {fieldSelector}.AssetID = H{f.RelationIndex + 1}.ID and {fieldSelector}.FormattedValue <> ''");
                    }
                }
                else
                {
                    if (f.FieldTypeName.ToLowerInvariant() == "displayvalue")
                    {
                        selects.Add($"h{f.RelationIndex + 1}_dv.displayvalue AS [H{f.RelationIndex + 1}_DisplayValue]");
                        joins.Add($"LEFT JOIN assetdisplayvalue H{f.RelationIndex + 1}_DV ON h{f.RelationIndex + 1}_dv.assetid = h{f.RelationIndex + 1}.id");
                    }
                    if (f.FieldTypeName.ToLowerInvariant() == "_assetpath")
                    {
                        selects.Add($"h{f.RelationIndex + 1}p.displaypath AS [H{f.RelationIndex + 1}__assetPath]");
                        joins.Add($"LEFT JOIN graph.assetnodedisplaypath H{f.RelationIndex + 1}P ON h{f.RelationIndex + 1}p.id = h{f.RelationIndex + 1}.id");
                    }
                }


            }

            if (isCountQuery)
            {
                return $@"select distinct count(*)
                                from graph.AssetNode H1
                                {(string.Join("\n", joins))}";
            }

            return $@"select distinct 
                                {(string.Join(",", selects))}
                                from graph.AssetNode H1
                                {(string.Join("\n", joins))}";
        }

        public static (List<GridColumn>, List<GridField>) GetComplexRelationLookupFieldsAndColumns(List<FieldType> fields, FieldTypeComplexLookupDefinition definition)
        {
            List<GridColumn> Columns = new List<GridColumn>();
            List<GridField> Fields = new List<GridField>();
            int currentRel = 0;

            foreach (var f in definition.Fields.Where(x => x.Show == true).OrderBy(x => x.DisplayOrder))
            {
                string fieldName = string.IsNullOrEmpty(f.OverrideDisplayName) ? f.FieldTypeName : f.OverrideDisplayName;
                int? colWidth = f.Width;

                if (f.FieldTypeName.StartsWith("Related Item."))
                {
                    Columns.Add(new GridColumn
                    {
                        text = fieldName,
                        columnWidth = colWidth,
                        columntype = "preview",
                        datafield = $"H{f.RelationIndex + 1}_{f.FieldTypeID}_DisplayValue",
                        uidfield = $"H{f.RelationIndex + 1}_{f.FieldTypeID}_Uid",
                        urlfield = $"H{f.RelationIndex + 1}_{f.FieldTypeID}_Url"
                    });

                    Fields.Add(new GridField { type = "text", name = $"H{f.RelationIndex + 1}_{f.FieldTypeID}_Uid" });
                    Fields.Add(new GridField { type = "text", name = $"H{f.RelationIndex + 1}_{f.FieldTypeID}_Url" });
                    Fields.Add(new GridField { type = "preview", apiName = $"H{f.RelationIndex + 1}_{f.FieldTypeID}_DisplayValue", name = $"H{f.RelationIndex + 1}_{f.FieldTypeID}_DisplayValue", defaultFilter = f.Filter, sortOrder = f.SortOrder });
                }
                else if (f.FieldTypeID > 0)
                {
                    var gColumn = new GridColumn { text = fieldName, columnWidth = colWidth };
                    var gField = new GridField { type = "text", defaultFilter = f.Filter, sortOrder = f.SortOrder};
                    var ft = fields.FirstOrDefault(x => x.ID == f.FieldTypeID);
                    string fieldAlias = $"H{f.RelationIndex + 1}_{f.FieldTypeID}";
                    gField.name = gColumn.datafield = fieldAlias;
                    gField.apiName = gColumn.apiName = ft.Name;

                    switch (ft.Type.ToLowerInvariant())
                    {
                        case "boolean":
                            gColumn.columntype = "checkbox";
                            gField.type = "bool";
                            break;
                        case "number":
                        case "decimal":
                            gColumn.columntype = "numberinput";
                            gField.type = "number";
                            break;
                        case "date":
                            gColumn.columntype = "datetimeinput";
                            gField.type = "date";
                            break;
                        case "datetime":
                            gColumn.columntype = "datetimeinput";
                            gField.type = "datetime";
                            break;
                        case "counter":
                            gColumn.columntype = "counter";
                            gField.type = "counter";
                            break;
                        case "link":
                            gColumn.columntype = "link";
                            gField.type = "link";
                            break;
                        case "html":
                            gColumn.columntype = "textbox";
                            gField.type = "html";
                            break;
                        default:
                            gColumn.columntype = "textbox";
                            gField.type = "text";
                            break;
                    }

                    if (currentRel == f.RelationIndex)
                    {
                        gField.type = gColumn.columntype = "preview";
                        gColumn.uidfield = $"H{(f.RelationIndex + 1)}_Uid";
                        gColumn.urlfield = $"H{(f.RelationIndex + 1)}_Url";

                        Fields.Add(new GridField { name = gColumn.uidfield, type = "text" });
                        Fields.Add(new GridField { name = gColumn.urlfield, type = "text" });

                        currentRel++;
                    }

                    Columns.Add(gColumn);
                    Fields.Add(gField);
                }
                else if (f.FieldTypeName.ToLowerInvariant() == "displayvalue")
                {
                    var gColumn = new GridColumn { text = fieldName, columnWidth = colWidth };
                    var gField = new GridField { type = "text", defaultFilter = f.Filter, sortOrder = f.SortOrder };
                    gField.type = gColumn.columntype = "preview";
                    gField.name = gField.apiName = gColumn.datafield = $"H{f.RelationIndex + 1}_DisplayValue";
                    gColumn.uidfield = $"H{(f.RelationIndex + 1)}_Uid";
                    gColumn.urlfield = $"H{(f.RelationIndex + 1)}_Url";

                    Columns.Add(gColumn);
                    Fields.Add(gField);
                }
                else if (f.FieldTypeName.ToLowerInvariant() == "_assetpath")
                {
                    var gColumn = new GridColumn { text = fieldName, columnWidth = colWidth };
                    var gField = new GridField { type = "text", defaultFilter = f.Filter, sortOrder = f.SortOrder };
                    gField.type = gColumn.columntype = "preview";
                    gField.name = gField.apiName = gColumn.datafield = $"H{f.RelationIndex + 1}__assetPath";
                    gColumn.uidfield = $"H{(f.RelationIndex + 1)}_Uid";
                    gColumn.urlfield = $"H{(f.RelationIndex + 1)}_Url";

                    Columns.Add(gColumn);
                    Fields.Add(gField);
                }
            }

            return (Columns, Fields);
        }

        public static (List<GridColumn>, List<GridField>) GetComplexRefListFromRelFieldsAndColumns(List<FieldType> fields)
        {
            List<GridColumn> Columns = new List<GridColumn>();
            List<GridField> Fields = new List<GridField>();
            foreach (var ft in fields.OrderBy(x => x.SortOrder))
            {
                var gColumn = new GridColumn { text = ft.FriendlyName, datafield = ft.Name };
                var gField = new GridField { type = "text", name = ft.Name, apiName = ft.Name };

                switch (ft.Type.ToLowerInvariant())
                {
                    case "boolean":
                        gColumn.columntype = "checkbox";
                        gField.type = "bool";
                        break;
                    case "number":
                    case "decimal":
                        gColumn.columntype = "numberinput";
                        gField.type = "number";
                        break;
                    case "date":
                        gColumn.columntype = "datetimeinput";
                        gField.type = "date";
                        break;
                    case "datetime":
                        gColumn.columntype = "datetimeinput";
                        gField.type = "datetime";
                        break;
                    case "counter":
                        gColumn.columntype = "counter";
                        gField.type = "counter";
                        break;
                    case "link":
                        gColumn.columntype = "link";
                        gField.type = "link";
                        break;
                    case "html":
                        gColumn.columntype = "textbox";
                        gField.type = "html";
                        break;
                    case "color":
                        gColumn.columntype = "color";
                        gField.type = "color";
                        break;
                    default:
                        gColumn.columntype = "textbox";
                        gField.type = "text";
                        break;
                }

                Columns.Add(gColumn);
                Fields.Add(gField);

            }

            return (Columns, Fields);
        }

        public static string GetRefListFromRelSQL(List<FieldType> fields, DynamicParameters dbArgs, List<string> selects, List<string> joins, bool isCountQuery)
        {
            if (isCountQuery)
            {
                joins.Clear();
                selects.Clear();
            }

            foreach (var ft in fields)
            {

                if (ft.Name == "Code" && ft.ID == 0)
                {
                    selects.Add("A.[Code] as [Code]");
                }
                else if (ft.Name == "Color" && ft.ID == 0)
                {
                    selects.Add("ACJ.ColorJson as [Color]");
                    joins.Add("outer apply dbo.GetAssetColorJsonByColor(A.Color) ACJ");
                }
                else
                {
                    string fieldSelector = $"F{ft.ID}";
                    string fieldAlias = ft.Name;

                    switch (ft.Type.ToLowerInvariant())
                    {
                        case "boolean":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS bit) AS [{fieldAlias}]");
                            break;
                        case "number":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS int) AS [{fieldAlias}]");
                            break;
                        case "decimal":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS decimal) AS [{fieldAlias}]");
                            break;
                        case "date":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS date) AS [{fieldAlias}]");
                            break;
                        case "datetime":
                            selects.Add($"try_cast({fieldSelector}.FormattedValue AS datetime) AS [{fieldAlias}]");
                            break;
                        case "counter":
                            string cnt_prefix = "cntprefix_" + ft.ID;
                            dbArgs.Add(cnt_prefix, ft.CounterPrefix);
                            selects.Add($"(@{cnt_prefix} + try_cast({fieldSelector}.FormattedValue AS nvarchar(20))) AS [{fieldAlias}]");
                            break;
                        default:
                            selects.Add($"{fieldSelector}.FormattedValue as {fieldAlias}");
                            break;
                    }

                    joins.Add($"left join FieldDetail F{ft.ID} on F{ft.ID}.AssetID = A.ID and F{ft.ID}.FieldTypeID = {ft.ID} and F{ft.ID}.FormattedValue <> ''");
                }
            }

            if (isCountQuery)
            {
                return $@"  select distinct count(*)
                            from Asset A
                            {(string.Join("\n", joins))}";
            }


            return $@" select distinct 
                            {(string.Join(", ", selects))}
                            from Asset A
                            {(string.Join("\n", joins))}";
        }

    }
}
