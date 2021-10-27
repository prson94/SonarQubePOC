using d360.core.entities;
using d360.core.Models;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace d360.model.helpers
{
    public static class ComplexFieldsHelper
    {

        public static string GetComplexRelationLookupSQL(FieldTypeComplexLookupDefinition definition, DynamicParameters dbArgs, List<FieldType> fields, List<string> selects, List<Tuple<int, FieldTypeComplexLookupRelationDirection>> fieldRelationDirectionMapping, bool isCountQuery = false)
        {
            Guid resourceTypeUid = Guid.Parse("00000001-0000-0000-0000-a00000000011");

            List<string> joins = new List<string>();
            int idx = 1;
            foreach (var rel in definition.Relations)
            {
                selects.Add($"concat('asset/', H{idx}.Uid) as [H{idx}_Url]");
                selects.Add($"H{idx}.Uid as [H{idx}_Uid]");

                if (definition.Relations.IndexOf(rel) == 0)
                {
                    if (rel.Direction == FieldTypeComplexLookupRelationDirection.Forward)
                    {
                        joins.Add($"inner join graph.AssetEdge R{idx} on R{idx}.$to_id = H{(idx == 1 ? idx : idx - 1)}.$node_id and R{idx}.IntersectTypeUID = '{rel.IntersectTypeUid}'");
                        joins.Add($"inner join graph.AssetNode {(idx == 1 ? $"A{idx}" : $"H{idx}")} on {(idx == 1 ? $"A{idx}" : $"H{idx}")}.$node_id = R{idx}.$from_id {(idx == 1 ? $" and A{idx}.uid = @assetuid" : "")}");
                    }
                    else
                    {
                        joins.Add($"inner join graph.AssetEdge R{idx} on R{idx}.$from_id = H{(idx == 1 ? idx : idx - 1)}.$node_id and R{idx}.IntersectTypeUID = '{rel.IntersectTypeUid}'");
                        joins.Add($"inner join graph.AssetNode {(idx == 1 ? $"A{idx}" : $"H{idx}")} on {(idx == 1 ? $"A{idx}" : $"H{idx}")}.$node_id = R{idx}.$to_id {(idx == 1 ? $" and A{idx}.uid = @assetuid" : "")}");
                    }
                }
                else
                {
                    if (rel.Direction == FieldTypeComplexLookupRelationDirection.Forward)
                    {
                        joins.Add($"inner join graph.AssetEdge R{idx} on R{idx}.$from_id = H{(idx == 1 ? idx : idx - 1)}.$node_id and R{idx}.IntersectTypeUID = '{rel.IntersectTypeUid}'");
                        joins.Add($"inner join graph.AssetNode {(idx == 1 ? $"A{idx}" : $"H{idx}")} on {(idx == 1 ? $"A{idx}" : $"H{idx}")}.$node_id = R{idx}.$to_id {(idx == 1 ? $" and A{idx}.uid = @assetuid" : "")}");
                    }
                    else
                    {
                        joins.Add($"inner join graph.AssetEdge R{idx} on R{idx}.$to_id = H{(idx == 1 ? idx : idx - 1)}.$node_id and R{idx}.IntersectTypeUID = '{rel.IntersectTypeUid}'");
                        joins.Add($"inner join graph.AssetNode {(idx == 1 ? $"A{idx}" : $"H{idx}")} on {(idx == 1 ? $"A{idx}" : $"H{idx}")}.$node_id = R{idx}.$from_id {(idx == 1 ? $" and A{idx}.uid = @assetuid" : "")}");
                    }
                }
                bool isResource = definition.Fields.Where(x => (x.RelationIndex + 1) == idx && x.AssetTypeUid == resourceTypeUid).Any();
                if (isResource)
                {
                    joins.Add($"INNER JOIN reporting.global_resource U{idx} ON u{idx}.uid = h{idx}.uid");
                }

                idx++;
            }

            foreach (var f in definition.Fields)
            {
                var ft = fields.FirstOrDefault(x => x.ID == f.FieldTypeID);

                if (ft == null && f.FieldTypeName.StartsWith("Related Item."))
                {
                    var index = f.RelationIndex + 1;

                    selects.Add($"H{index}_A{f.FieldTypeID}.Uid AS [H{index}_{f.FieldTypeID}_Uid]");
                    selects.Add($"concat('asset/', H{index}_A{f.FieldTypeID}.Uid) AS [H{index}_{f.FieldTypeID}_Url]");
                    selects.Add($"H{index}_A{f.FieldTypeID}_DV.DisplayValue AS [H{index}_{f.FieldTypeID}_DisplayValue]");
                    selects.Add($"H{index}_R{f.FieldTypeID}.IntersectTypeUid AS [H{index}_{f.FieldTypeID}_IntersectTypeUid]");

                    var direction = fieldRelationDirectionMapping.FirstOrDefault(x => x.Item1 == f.FieldTypeID)?.Item2;

                    if (direction == null || direction == FieldTypeComplexLookupRelationDirection.Back)
                    {
                        joins.Add($@"LEFT JOIN graph.AssetEdge H{index}_R{f.FieldTypeID} ON H{index}_R{f.FieldTypeID}.$to_id = H{index}.$node_id AND H{index}_R{f.FieldTypeID}.IntersectTypeID = {f.FieldTypeID}
                                 LEFT JOIN graph.AssetNode H{index}_A{f.FieldTypeID} ON H{index}_A{f.FieldTypeID}.$node_id = H{index}_R{f.FieldTypeID}.$from_id
                                 LEFT JOIN AssetDisplayValue H{index}_A{f.FieldTypeID}_DV ON H{index}_A{f.FieldTypeID}_DV.AssetID = H{index}_A{f.FieldTypeID}.ID");
                    }
                    else
                    {
                        joins.Add($@"LEFT JOIN graph.AssetEdge H{index}_R{f.FieldTypeID} ON H{index}_R{f.FieldTypeID}.$from_id = H{index}.$node_id AND H{index}_R{f.FieldTypeID}.IntersectTypeID = {f.FieldTypeID}
                                 LEFT JOIN graph.AssetNode H{index}_A{f.FieldTypeID} ON H{index}_A{f.FieldTypeID}.$node_id = H{index}_R{f.FieldTypeID}.$to_id
                                 LEFT JOIN AssetDisplayValue H{index}_A{f.FieldTypeID}_DV ON H{index}_A{f.FieldTypeID}_DV.AssetID = H{index}_A{f.FieldTypeID}.ID");
                    }

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
                            selects.Add($"(@{cnt_prefix} + try_cast(F{ft.ID}.FormattedValue AS nvarchar(20))) AS [{fieldAlias}]");
                            break;
                        case "jsonelement":
                            selects.Add($"JSON_VALUE(F_{ft.ID}.FormattedValue,'$.'+FT_JSON_{ft.ID}.Name) as [{fieldAlias}]");
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
                                 AND FieldTypeId = {ft.ID})F{ft.ID}");
                    }
                    else if (ft.Type == "Score")
                    {
                        joins.Add($"outer apply dbo.GetAssetScoreById(H{f.RelationIndex + 1}.ID, {ft.ScoreType}){fieldSelector}");
                    }
                    else if (ft.Type == "JsonElement")
                    {
                        joins.Add($"left join FieldType FT_JSON_{ft.ID} on FT_JSON_{ft.ID}.Id = {ft.ID}");
                        joins.Add($"left join Field F_{ft.ID} on F_{ft.ID}.FieldTypeId = try_parse(JSON_VALUE(FT_JSON_{ft.ID}.Definition, '$.FieldTypeID') as int) and F_{ft.ID}.AssetId = H{f.RelationIndex + 1}.Id");
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
                    if (f.FieldTypeName.ToLowerInvariant() == "code")
                    {
                        selects.Add($"H{f.RelationIndex + 1}_CODE.Code as [H{f.RelationIndex + 1}_Code]");
                        joins.Add($"left join asset H{f.RelationIndex + 1}_CODE on H{f.RelationIndex + 1}_CODE.uid = H{f.RelationIndex + 1}.Uid");
                    }

                    bool isResource = f.AssetTypeUid == resourceTypeUid;
                    if (isResource)
                    {
                        selects.Add($"u{f.RelationIndex + 1}.{f.FieldTypeName} AS [H{f.RelationIndex + 1}_{f.FieldTypeName}]");
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
                    var gField = new GridField { type = "text", defaultFilter = f.Filter, sortOrder = f.SortOrder };
                    var ft = fields.FirstOrDefault(x => x.ID == f.FieldTypeID);
                    var gColumn = new GridColumn { text = ft.FriendlyName, columnWidth = colWidth };
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
                        case "score":
                            gColumn.columntype = "Score";
                            gField.type = "Score";
                            if (ft.ScoreType == 1)
                            {
                                gColumn.description = "This is the Governance Score";
                            }
                            else
                            {
                                gColumn.description = "This is the DQ Score";
                            }
                            break;
                        default:
                            gColumn.columntype = "textbox";
                            gField.type = "text";
                            break;
                    }

                    if ((currentRel == f.RelationIndex && gField.type != "html") || gField.apiName.ToLowerInvariant() == "name")
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
                else if (f.FieldTypeName.ToLowerInvariant() == "code")
                {
                    var gColumn = new GridColumn { text = "Code", datafield = $"H{f.RelationIndex + 1}_Code", columntype = "text", };
                    gColumn.uidfield = $"H{(f.RelationIndex + 1)}_Uid";
                    gColumn.urlfield = $"H{(f.RelationIndex + 1)}_Url";

                    var gField = new GridField { name = $"H{f.RelationIndex + 1}_Code", apiName = $"H{f.RelationIndex + 1}_Code", type = "Text" };
                    Columns.Add(gColumn);
                    Fields.Add(gField);
                }

                Guid resourceTypeUid = Guid.Parse("00000001-0000-0000-0000-a00000000011");
                bool isResource = f.AssetTypeUid == resourceTypeUid;
                if (isResource)
                {
                    var gColumn = new GridColumn
                    {
                        text = fieldName,
                        datafield = $"H{(f.RelationIndex + 1)}_{f.FieldTypeName}",
                        columnWidth = colWidth,
                        columntype = "textbox",
                        apiName = $"H{(f.RelationIndex + 1)}_{f.FieldTypeName}",
                        uidfield = $"H{(f.RelationIndex + 1)}_Uid",
                        urlfield = $"H{(f.RelationIndex + 1)}_Url"
                    };
                    var gField = new GridField
                    {
                        name = $"H{(f.RelationIndex + 1)}_{f.FieldTypeName}",
                        type = "text",
                        apiName = $"H{(f.RelationIndex + 1)}_{f.FieldTypeName}",
                        defaultFilter = f.Filter,
                        sortOrder = f.SortOrder
                    };
                    Columns.Add(gColumn);
                    Fields.Add(gField);

                    Fields.Add(new GridField { name = $"H{(f.RelationIndex + 1)}_Uid", type = "text" });
                    Fields.Add(new GridField { name = $"H{(f.RelationIndex + 1)}_Url", type = "text" });
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
                        gColumn.text = "Color";
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
            selects.Add("A.[uid] as [Uid]");
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
