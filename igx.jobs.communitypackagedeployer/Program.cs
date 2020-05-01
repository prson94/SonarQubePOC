using d360.core;
using d360.core.entities.Community.Templates;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace igx.jobs.communitypackagedeployer
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
#if DEBUG
            config.UseDevelopmentSettings();
#endif

            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
    
    internal class FieldTypeDbColumn
    {
        public string Name { get; set; }
        public string DbType { get; set; }
        public bool DbNull { get; set; }
        public Type CodeType { get; set; }
    }

    internal class CompanyProcessor
    {
        SqlConnection connection;

        public List<FieldTypeDbColumn> FieldTypeDefinitions { 
            get 
            {
                return new List<FieldTypeDbColumn>() {
                    new FieldTypeDbColumn { Name = "Name", CodeType = typeof(string), DbNull = false, DbType = "nvarchar(250)" },
                    new FieldTypeDbColumn { Name = "FriendlyName", CodeType = typeof(string), DbNull = false, DbType = "nvarchar(250)" },
                    new FieldTypeDbColumn { Name = "Type", CodeType = typeof(string), DbNull = false, DbType = "nvarchar(25)" },
                    new FieldTypeDbColumn { Name = "Category", CodeType = typeof(string), DbNull = true, DbType = "nvarchar(250)" },
                    new FieldTypeDbColumn { Name = "DisplayDescription", CodeType = typeof(string), DbNull = true, DbType = "nvarchar(4000)" },
                    new FieldTypeDbColumn { Name = "FormDescription", CodeType = typeof(string), DbNull = true, DbType = "nvarchar(4000)" },
                    new FieldTypeDbColumn { Name = "MinimumLength", CodeType = typeof(decimal), DbNull = true, DbType = "decimal(38,18)" },
                    new FieldTypeDbColumn { Name = "MaximumLength", CodeType = typeof(decimal), DbNull = true, DbType = "decimal(38,18)" },
                    new FieldTypeDbColumn { Name = "Length", CodeType = typeof(int), DbNull = true, DbType = "int" },
                    new FieldTypeDbColumn { Name = "Pattern", CodeType = typeof(string), DbNull = true, DbType = "nvarchar(1000)" },
                    new FieldTypeDbColumn { Name = "SortOrder", CodeType = typeof(int), DbNull = false, DbType = "int" },
                    new FieldTypeDbColumn { Name = "IsRequired", CodeType = typeof(bool), DbNull = false, DbType = "bit" },
                    new FieldTypeDbColumn { Name = "IsListable", CodeType = typeof(bool), DbNull = false, DbType = "bit" },
                    new FieldTypeDbColumn { Name = "ValidationDescription", CodeType = typeof(string), DbNull = true, DbType = "nvarchar(500)" },
                    new FieldTypeDbColumn { Name = "IsDisplayable", CodeType = typeof(bool), DbNull = false, DbType = "bit" },
                    new FieldTypeDbColumn { Name = "IsEditable", CodeType = typeof(bool), DbNull = false, DbType = "bit" },
                    new FieldTypeDbColumn { Name = "DefaultValue", CodeType = typeof(string), DbNull = true, DbType = "nvarchar(max)" },
                    new FieldTypeDbColumn { Name = "AllowAllValue", CodeType = typeof(bool), DbNull = false, DbType = "bit" },
                    new FieldTypeDbColumn { Name = "AllowAllLabel", CodeType = typeof(string), DbNull = true, DbType = "nvarchar(250)" },
                    new FieldTypeDbColumn { Name = "IsPrimaryFilter", CodeType = typeof(bool), DbNull = false, DbType = "bit" },
                    new FieldTypeDbColumn { Name = "IsPartOfKey", CodeType = typeof(bool), DbNull = false, DbType = "bit" },
                    new FieldTypeDbColumn { Name = "ColumnOrder", CodeType = typeof(int), DbNull = false, DbType = "int" },
                    new FieldTypeDbColumn { Name = "ColumnWidth", CodeType = typeof(int), DbNull = true, DbType = "int" },
                    new FieldTypeDbColumn { Name = "AllowMultipleValues", CodeType = typeof(bool), DbNull = false, DbType = "bit" },
                    new FieldTypeDbColumn { Name = "Increment", CodeType = typeof(decimal), DbNull = true, DbType = "decimal(38,18)" },
                    new FieldTypeDbColumn { Name = "Precision", CodeType = typeof(int), DbNull = true, DbType = "int" }
                };
            }
        }

        public CompanyProcessor(SqlConnection cnn)
        {
            connection = cnn;
        }

        List<d360.core.entities.FieldType> convertToGovernFieldTypes(List<d360.core.entities.FieldTypeApiEditModel> apiFieldTypes)
        {
            var list = new List<d360.core.entities.FieldType>();

            apiFieldTypes.ForEach(f =>
            {
                var newFieldType = new d360.core.entities.FieldType
                {
                    Category = f.Category,
                    Name = f.Name,
                    FriendlyName = f.FriendlyName
                };

                if (f.Type.Boolean != null)
                {
                    newFieldType.Type = DataType.Boolean.ToString();
                    newFieldType.ColumnOrder = f.Type.Boolean.ColumnOrder.Value;
                    newFieldType.ColumnWidth = f.Type.Boolean.ColumnWidth;
                    if (f.Type.Boolean.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.Boolean.DefaultValue.Value.ToString().ToLower();
                    if (f.Type.Boolean.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Boolean.Description.Display;
                        newFieldType.FormDescription = f.Type.Boolean.Description.Form;
                    }
                    newFieldType.IsDisplayable = f.Type.Boolean.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Boolean.IsEditable;
                    newFieldType.IsListable = f.Type.Boolean.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Boolean.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Boolean.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Boolean.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Boolean.SortOrder;
                }
                else if (f.Type.Date != null)
                {
                    newFieldType.Type = DataType.Date.ToString();
                    newFieldType.ColumnOrder = f.Type.Date.ColumnOrder.Value;
                    newFieldType.ColumnWidth = f.Type.Date.ColumnWidth;
                    if (f.Type.Date.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.Date.DefaultValue.Value.ToString();
                    if (f.Type.Date.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Date.Description.Display;
                        newFieldType.FormDescription = f.Type.Date.Description.Form;
                    }
                    newFieldType.IsDisplayable = f.Type.Date.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Date.IsEditable;
                    newFieldType.IsListable = f.Type.Date.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Date.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Date.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Date.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Date.SortOrder;
                    if (f.Type.Date.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Date.Validation.IsRequired;
                    }
                }
                else if (f.Type.DateTime != null)
                {
                    newFieldType.Type = DataType.DateTime.ToString();
                    newFieldType.ColumnOrder = f.Type.DateTime.ColumnOrder.Value;
                    newFieldType.ColumnWidth = f.Type.DateTime.ColumnWidth;
                    if (f.Type.DateTime.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.DateTime.DefaultValue.Value.ToString();
                    if (f.Type.DateTime.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.DateTime.Description.Display;
                        newFieldType.FormDescription = f.Type.DateTime.Description.Form;
                    }
                    newFieldType.IsDisplayable = f.Type.DateTime.IsDisplayable;
                    newFieldType.IsEditable = f.Type.DateTime.IsEditable;
                    newFieldType.IsListable = f.Type.DateTime.IsListable;
                    newFieldType.IsPartOfKey = f.Type.DateTime.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.DateTime.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.DateTime.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.DateTime.SortOrder;
                    if (f.Type.DateTime.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.DateTime.Validation.IsRequired;
                    }
                }
                else if (f.Type.Decimal != null)
                {
                    newFieldType.Type = DataType.Decimal.ToString();
                    newFieldType.ColumnOrder = f.Type.Decimal.ColumnOrder.Value;
                    newFieldType.ColumnWidth = f.Type.Decimal.ColumnWidth;
                    if (f.Type.Decimal.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.Decimal.DefaultValue.Value.ToString();
                    if (f.Type.Decimal.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Decimal.Description.Display;
                        newFieldType.FormDescription = f.Type.Decimal.Description.Form;
                    }
                    newFieldType.IsDisplayable = f.Type.Decimal.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Decimal.IsEditable;
                    newFieldType.IsListable = f.Type.Decimal.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Decimal.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Decimal.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Decimal.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Decimal.SortOrder;
                    newFieldType.Increment = f.Type.Decimal.Increment;
                    if (f.Type.Decimal.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Decimal.Validation.IsRequired;
                        newFieldType.MaximumLength = f.Type.Decimal.Validation.MaximumValue;
                        newFieldType.MinimumLength = f.Type.Decimal.Validation.MinimumValue;
                        newFieldType.Precision = f.Type.Decimal.Validation.Precision;
                    }
                }
                else if (f.Type.Html != null)
                {
                    newFieldType.Type = DataType.Html.ToString();
                    newFieldType.ColumnOrder = f.Type.Html.ColumnOrder.Value;
                    newFieldType.ColumnWidth = f.Type.Html.ColumnWidth;
                    newFieldType.DefaultValue = f.Type.Html.DefaultValue;
                    if (f.Type.Html.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Html.Description.Display;
                        newFieldType.FormDescription = f.Type.Html.Description.Form;
                    }
                    newFieldType.IsDisplayable = f.Type.Html.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Html.IsEditable;
                    newFieldType.IsListable = f.Type.Html.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Html.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Html.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Html.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Html.SortOrder;
                    if (f.Type.Html.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Html.Validation.IsRequired;
                        newFieldType.MaximumLength = f.Type.Html.Validation.MaximumLength;
                        newFieldType.MinimumLength = f.Type.Html.Validation.MinimumLength;
                    }
                }
                else if (f.Type.Link != null)
                {
                    newFieldType.Type = DataType.Link.ToString();
                    newFieldType.ColumnOrder = f.Type.Link.ColumnOrder.Value;
                    newFieldType.ColumnWidth = f.Type.Link.ColumnWidth;
                    if (f.Type.Link.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Link.Description.Display;
                        newFieldType.FormDescription = f.Type.Link.Description.Form;
                    }
                    newFieldType.IsDisplayable = f.Type.Link.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Link.IsEditable;
                    newFieldType.IsListable = f.Type.Link.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Link.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Link.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Link.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Link.SortOrder;
                    if (f.Type.Link.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Link.Validation.IsRequired;
                    }
                }
                else if (f.Type.Number != null)
                {
                    newFieldType.Type = DataType.Number.ToString();
                    newFieldType.ColumnOrder = f.Type.Number.ColumnOrder.Value;
                    newFieldType.ColumnWidth = f.Type.Number.ColumnWidth;
                    if (f.Type.Number.DefaultValue.HasValue) newFieldType.DefaultValue = f.Type.Number.DefaultValue.Value.ToString();
                    if (f.Type.Number.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Number.Description.Display;
                        newFieldType.FormDescription = f.Type.Number.Description.Form;
                    }
                    newFieldType.IsDisplayable = f.Type.Number.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Number.IsEditable;
                    newFieldType.IsListable = f.Type.Number.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Number.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Number.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Number.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Number.SortOrder;
                    newFieldType.Increment = f.Type.Number.Increment;
                    if (f.Type.Number.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Number.Validation.IsRequired;
                        newFieldType.MaximumLength = f.Type.Number.Validation.MaximumValue;
                        newFieldType.MinimumLength = f.Type.Number.Validation.MinimumValue;
                    }
                }
                else if (f.Type.Text != null)
                {
                    newFieldType.Type = DataType.Text.ToString();
                    newFieldType.ColumnOrder = f.Type.Text.ColumnOrder.Value;
                    newFieldType.ColumnWidth = f.Type.Text.ColumnWidth;
                    newFieldType.DefaultValue = f.Type.Text.DefaultValue;
                    if (f.Type.Text.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Text.Description.Display;
                        newFieldType.FormDescription = f.Type.Text.Description.Form;
                    }
                    newFieldType.IsDisplayable = f.Type.Text.IsDisplayable;
                    newFieldType.IsEditable = f.Type.Text.IsEditable;
                    newFieldType.IsListable = f.Type.Text.IsListable;
                    newFieldType.IsPartOfKey = f.Type.Text.IsPartOfKey;
                    newFieldType.IsPrimaryFilter = f.Type.Text.IsPrimaryFilter;
                    newFieldType.ShowIfEmpty = f.Type.Text.ShowIfEmpty;
                    newFieldType.SortOrder = f.Type.Text.SortOrder;
                    if (f.Type.Text.Validation != null)
                    {
                        newFieldType.IsRequired = f.Type.Text.Validation.IsRequired;
                        newFieldType.ValidationDescription = f.Type.Text.Validation.Message;
                        newFieldType.MaximumLength = f.Type.Text.Validation.MaximumLength;
                        newFieldType.MinimumLength = f.Type.Text.Validation.MinimumLength;
                        newFieldType.Pattern = f.Type.Text.Validation.Pattern;
                    }
                }
                else if (f.Type.Tag != null)
                {
                    newFieldType.Type = DataType.Tag.ToString();
                    newFieldType.ColumnOrder = f.Type.Tag.ColumnOrder.Value;
                    newFieldType.ColumnWidth = f.Type.Tag.ColumnWidth;
                    if (f.Type.Tag.Description != null)
                    {
                        newFieldType.DisplayDescription = f.Type.Tag.Description.Display;
                    }
                    newFieldType.IsDisplayable = true;
                    newFieldType.IsEditable = false;
                    newFieldType.IsListable = f.Type.Tag.IsListable;
                    newFieldType.IsPartOfKey = false;
                    newFieldType.ShowIfEmpty = true;
                    newFieldType.IsPrimaryFilter = f.Type.Tag.IsPrimaryFilter;
                }

                if (!string.IsNullOrEmpty(newFieldType.Type))
                {
                    list.Add(newFieldType);
                }
            });

            return list;
        }

        void convertGovernFieldTypeToDataRow(DataRow fRow, Guid uid, d360.core.entities.FieldType f)
        {
            fRow["Uid"] = uid;
            fRow["Name"] = f.Name;
            fRow["FriendlyName"] = f.FriendlyName;
            if (!string.IsNullOrEmpty(f.Category)) fRow["Category"] = f.Category;
            fRow["Type"] = f.Type;
            if (!string.IsNullOrEmpty(f.DisplayDescription)) fRow["DisplayDescription"] = f.DisplayDescription;
            if (!string.IsNullOrEmpty(f.FormDescription)) fRow["FormDescription"] = f.FormDescription;
            if (f.MinimumLength.HasValue) fRow["MinimumLength"] = f.MinimumLength;
            if (f.MaximumLength.HasValue) fRow["MaximumLength"] = f.MaximumLength;
            if (f.Length.HasValue) fRow["Length"] = f.Length;
            if (!string.IsNullOrEmpty(f.Pattern)) fRow["Pattern"] = f.Pattern;
            fRow["SortOrder"] = f.SortOrder;
            fRow["IsRequired"] = f.IsRequired;
            fRow["IsListable"] = f.IsListable;
            if (!string.IsNullOrEmpty(f.ValidationDescription)) fRow["ValidationDescription"] = f.ValidationDescription;
            fRow["IsDisplayable"] = f.IsDisplayable;
            fRow["IsEditable"] = f.IsEditable;
            if (!string.IsNullOrEmpty(f.DefaultValue)) fRow["DefaultValue"] = f.DefaultValue;
            fRow["AllowAllValue"] = f.AllowAllValue;
            fRow["AllowAllLabel"] = f.AllowAllLabel;
            fRow["IsPrimaryFilter"] = f.IsPrimaryFilter;
            fRow["IsPartOfKey"] = f.IsPartOfKey;
            fRow["ColumnOrder"] = f.ColumnOrder;
            if (f.ColumnWidth.HasValue) fRow["ColumnWidth"] = f.ColumnWidth;
            fRow["AllowMultipleValues"] = f.AllowMultipleValues;
            if (f.Increment.HasValue) fRow["Increment"] = f.Increment;
            if (f.Precision.HasValue) fRow["Precision"] = f.Precision;
        }

        DataTable buildFieldTypeDataTable()
        {
            var fields = new DataTable();

            fields.Columns.Add("Uid", typeof(Guid));

            FieldTypeDefinitions.ForEach(f => {
                fields.Columns.Add(f.Name, f.CodeType);
            });

            return fields;
        }

        void buildFieldTypeColumnMappings(SqlBulkCopy bulkCopy)
        {
            bulkCopy.ColumnMappings.Add("Uid", "Uid");

            var names = FieldTypeDefinitions;
            names.ForEach(n => {
                bulkCopy.ColumnMappings.Add(n.Name, n.Name);
            });
        }

        string buildFieldMergeSql(string sourceSql, string sourceExtraColumns = "", string targetExtraColumns = "")
        {
            var names = FieldTypeDefinitions;
            var updateColumns = string.Join(",", names.Select(n => $"T.[{n.Name}] = S.[{n.Name}]"));
            var insertTargetColumnColumns = string.Join(",", names.Select(n => $"[{n.Name}]"));
            var insertSourceColumnColumns = string.Join(",", names.Select(n => $"S.[{n.Name}]"));

            return $@"
merge   into FieldType T 
using   ({sourceSql}) S 
on      (T.Object = S.Object and T.ObjectID = S.ObjectID and T.[Name] = S.[Name]) 
when not matched then 
insert (Object, ObjectID, {insertTargetColumnColumns}{targetExtraColumns}) 
values (S.Object, S.ObjectID, {insertSourceColumnColumns}{sourceExtraColumns});";

            /*
when    matched then
update  set {updateColumns}
 */
        }

        public void SynchronizeAssetTypes(List<AssetType> assetTypes, List<AssetTypeVersion> assetTypeVersions)
        {
            var theseAssetTypes =   (
                                    from v in assetTypeVersions
                                    join t in assetTypes on v.AssetTypeUid equals t.Uid
                                    select new
                                    {
                                        v.AssetTypeUid,
                                        v.AutoDisplayDescription,
                                        v.BackColor,
                                        v.CanOwnFusion,
                                        v.CreatedOn,
                                        v.Description,
                                        v.DisplayFormat,
                                        v.Field_Items,
                                        v.ForeColor,
                                        v.HierarchyMaximumDepth,
                                        v.Icon,
                                        v.Level_Items,
                                        t.Class,
                                        t.Hierarchical,
                                        t.Name,
                                        t.Object,
                                        t.ObjectID,
                                        v.State,
                                        v.UpdatedOn,
                                        v.UseAsTransformation
                                    }).ToList();

            #region Data Tables

            var table = new DataTable();
            table.Columns.Add("Uid", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Description", typeof(string));
            table.Columns.Add("Class", typeof(int));
            table.Columns.Add("DisplayFormat", typeof(string));
            table.Columns.Add("State", typeof(int));
            table.Columns.Add("Hierarchical", typeof(bool));
            table.Columns.Add("HierarchyMaximumDepth", typeof(int));
            table.Columns.Add("Object", typeof(string));
            table.Columns.Add("CanOwnFusion", typeof(bool));
            table.Columns.Add("AutoDisplayDescription", typeof(bool));
            table.Columns.Add("UseAsTransformation", typeof(bool));
            table.Columns.Add("UpdatedOn", typeof(DateTime));

            var levels = new DataTable();
            levels.Columns.Add("Uid", typeof(Guid));
            levels.Columns.Add("Name", typeof(string));
            levels.Columns.Add("Description", typeof(string));
            levels.Columns.Add("Level", typeof(int));

            var styles = new DataTable();
            styles.Columns.Add("Uid", typeof(Guid));
            styles.Columns.Add("BackColor", typeof(string));
            styles.Columns.Add("ForeColor", typeof(string));
            styles.Columns.Add("Icon", typeof(string));

            var fields = buildFieldTypeDataTable();

            #endregion

            var assetTypeUids = new List<Guid>();

            theseAssetTypes.ForEach(at =>
            {
                if (!assetTypeUids.Contains(at.AssetTypeUid))
                {
                    assetTypeUids.Add(at.AssetTypeUid);

                    var row = table.NewRow();

                    row["Uid"] = at.AssetTypeUid;
                    row["Name"] = at.Name;
                    row["Description"] = at.Description;
                    row["Class"] = (int)at.Class;
                    row["DisplayFormat"] = at.DisplayFormat;
                    row["State"] = (int)at.State;
                    row["Hierarchical"] = at.Hierarchical;
                    row["HierarchyMaximumDepth"] = at.HierarchyMaximumDepth;
                    row["Object"] = at.Object;
                    row["CanOwnFusion"] = at.CanOwnFusion;
                    row["AutoDisplayDescription"] = at.AutoDisplayDescription;
                    row["UseAsTransformation"] = at.UseAsTransformation;
                    row["UpdatedOn"] = at.UpdatedOn;

                    table.Rows.Add(row);

                    at.Level_Items.ForEach(l =>
                    {
                        var lRow = levels.NewRow();

                        lRow["Uid"] = at.AssetTypeUid;
                        lRow["Name"] = l.Name;
                        lRow["Description"] = l.Description;
                        lRow["Level"] = l.Level;

                        levels.Rows.Add(lRow);
                    });

                    var fieldTypes = convertToGovernFieldTypes(at.Field_Items);

                    fieldTypes.ForEach(f =>
                    {
                        var fRow = fields.NewRow();
                        convertGovernFieldTypeToDataRow(fRow, at.AssetTypeUid, f);
                        fields.Rows.Add(fRow);
                    });

                    var sRow = styles.NewRow();

                    sRow["Uid"] = at.AssetTypeUid;
                    sRow["BackColor"] = at.BackColor;
                    sRow["ForeColor"] = at.ForeColor;
                    sRow["Icon"] = at.Icon;

                    styles.Rows.Add(sRow);
                }
            });

            if (table.Rows.Count > 0)
            {
                using (SqlTransaction trans = connection.BeginTransaction("AssetTypes"))
                {
                    var tempTable = $@"create table #AssetTypes (
	[Uid] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Class] [int] NOT NULL,
	[DisplayFormat] [nvarchar](250) NOT NULL,
	[State] [int] NOT NULL,
	[Hierarchical] [bit] NOT NULL,
	[HierarchyMaximumDepth] [int] NOT NULL,
	[Object] [varchar](50) NOT NULL,
	[UpdatedOn] [datetime] NULL,
	[CanOwnFusion] [bit] NOT NULL,
	[AutoDisplayDescription] [bit] NOT NULL,
	[UseAsTransformation] [bit] NOT NULL
);
create table #AssetTypeLevels (
	[Uid] [uniqueidentifier] NOT NULL,
    [AssetTypeID] int NULL,
	[Name] [nvarchar](250) NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Level] [int] NOT NULL
);
create table #AssetTypeFields (
	[Uid] [uniqueidentifier] NOT NULL,
    {string.Join(",", FieldTypeDefinitions.Select(f => $"[{f.Name}] {f.DbType} {(f.DbNull ? "null" : "not null")}"))}
);
create table #AssetTypeStyles (
	[Uid] uniqueidentifier NOT NULL,
    [AssetTypeID] int NULL,
	[BackColor] varchar(7) NULL,
	[ForeColor] varchar(7) NULL,
	[Icon] varchar(50) NULL
);
";
                    connection.Execute(tempTable, transaction: trans);

                    SqlBulkCopy bulkCopy;

                    #region Types

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = table.Rows.Count,
                        DestinationTableName = "#AssetTypes",
                        BulkCopyTimeout = 1000
                    };

                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("Name", "Name");
                    bulkCopy.ColumnMappings.Add("Description", "Description");
                    bulkCopy.ColumnMappings.Add("Class", "Class");
                    bulkCopy.ColumnMappings.Add("DisplayFormat", "DisplayFormat");
                    bulkCopy.ColumnMappings.Add("State", "State");
                    bulkCopy.ColumnMappings.Add("Hierarchical", "Hierarchical");
                    bulkCopy.ColumnMappings.Add("HierarchyMaximumDepth", "HierarchyMaximumDepth");
                    bulkCopy.ColumnMappings.Add("Object", "Object");
                    bulkCopy.ColumnMappings.Add("CanOwnFusion", "CanOwnFusion");
                    bulkCopy.ColumnMappings.Add("AutoDisplayDescription", "AutoDisplayDescription");
                    bulkCopy.ColumnMappings.Add("UseAsTransformation", "UseAsTransformation");
                    bulkCopy.ColumnMappings.Add("UpdatedOn", "UpdatedOn");

                    bulkCopy.WriteToServer(table);

                    #endregion

                    #region Levels

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = levels.Rows.Count,
                        DestinationTableName = "#AssetTypeLevels",
                        BulkCopyTimeout = 1000
                    };

                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("Name", "Name");
                    bulkCopy.ColumnMappings.Add("Description", "Description");
                    bulkCopy.ColumnMappings.Add("Level", "Level");

                    bulkCopy.WriteToServer(levels);

                    #endregion

                    #region Fields

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = fields.Rows.Count,
                        DestinationTableName = "#AssetTypeFields",
                        BulkCopyTimeout = 1000
                    };

                    buildFieldTypeColumnMappings(bulkCopy);

                    bulkCopy.WriteToServer(fields);

                    #endregion

                    #region Styles

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = styles.Rows.Count,
                        DestinationTableName = "#AssetTypeStyles",
                        BulkCopyTimeout = 1000
                    };

                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("BackColor", "BackColor");
                    bulkCopy.ColumnMappings.Add("ForeColor", "ForeColor");
                    bulkCopy.ColumnMappings.Add("Icon", "Icon");

                    bulkCopy.WriteToServer(styles);

                    #endregion

                    bulkCopy = null;

                    try
                    {
                        connection.Execute(@"
merge   into AssetType T 
using   #AssetTypes S 
on      (T.Uid = S.Uid) 
when not matched then 
insert (Uid, Name, Description, Class, Object, DisplayFormat, State, Hierarchical, HierarchyMaximumDepth, CanOwnFusion, AutoDisplayDescription, UseAsTransformation, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy) 
values (S.Uid, S.Name, S.Description, S.Class, S.Object, S.DisplayFormat, S.State, S.Hierarchical, S.HierarchyMaximumDepth, S.CanOwnFusion, S.AutoDisplayDescription, S.UseAsTransformation, S.UpdatedOn, 0, S.UpdatedOn, 0);", transaction: trans);

/*
when    matched and S.UpdatedOn > T.UpdatedOn then
update  set
        T.Name = S.Name,
        T.Description = S.Description,
        T.Class = S.Class,
        T.DisplayFormat = S.DisplayFormat,
        T.Hierarchical = S.Hierarchical,
        T.HierarchyMaximumDepth = S.HierarchyMaximumDepth,
        T.CanOwnFusion = S.CanOwnFusion,
        T.AutoDisplayDescription = S.AutoDisplayDescription,
        T.UseAsTransformation = S.UseAsTransformation,
        T.UpdatedOn = S.UpdatedOn,
        T.UpdatedBy = 0                         
*/

                        connection.Execute(@"
update  T
set     T.AssetTypeID = S.ID
from    #AssetTypeLevels T
        inner join AssetType S on S.Uid = T.Uid;

merge   into AssetTypeLevel T 
using   #AssetTypeLevels S 
on      (T.AssetTypeID = S.AssetTypeID and T.[Level] = S.[Level]) 
when not matched then 
insert (AssetTypeID, Name, Description, [Level]) 
values (S.AssetTypeID, S.Name, S.Description, S.[Level]);
", transaction: trans);

/*
when    matched then
update  set
        T.Name = S.Name,
        T.Description = S.Description



delete  T
from    AssetTypeLevel T
        left join #AssetTypeLevels S on S.AssetTypeID = T.AssetTypeID and S.[Level] = T.[Level]
where   S.AssetTypeID is null
        and T.AssetTypeID in (select AssetTypeID from #AssetTypeLevels);
*/

                        var fieldSql = buildFieldMergeSql(@"
select  ATF.*,
        IAS.ID as AssetTypeID,
        IAS.Object,
        IAS.ObjectID
from    #AssetTypeFields ATF
        inner join AssetType IAS on IAS.Uid = ATF.Uid", ", S.AssetTypeID", ", AssetTypeID");

                        connection.Execute(fieldSql, transaction: trans);

                        connection.Execute(@"
update  T
set     T.AssetTypeID = S.ID
from    #AssetTypeStyles T
        inner join AssetType S on S.Uid = T.Uid;

merge   into AssetTypeStyle T 
using   #AssetTypeStyles S 
on      (T.ID = S.AssetTypeID) 
when not matched then 
insert (ID, IconBackColor, IconForeColor, Icon) 
values (S.AssetTypeID, S.BackColor, S.ForeColor, S.Icon);", transaction: trans);
                        /*
                        when    matched then
                        update  set
                                T.IconBackColor = S.BackColor,
                                T.IconForeColor = S.ForeColor,
                                T.Icon = S.Icon 
                        */
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                    }
                }
            }
        }

        public void SynchronizeIntersectTypes(List<IntersectType> intersectTypes, List<AssetTypeVersion> assetTypeVersions)
        {
            #region Data Tables

            var table = new DataTable();
            table.Columns.Add("Uid", typeof(Guid));
            table.Columns.Add("SubjectUid", typeof(Guid));
            table.Columns.Add("SubjectCardinality", typeof(int));
            table.Columns.Add("ObjectUid", typeof(Guid));
            table.Columns.Add("ObjectCardinality", typeof(int));
            table.Columns.Add("PredicateUid", typeof(Guid));
            table.Columns.Add("UpdatedOn", typeof(DateTime));

            var fields = buildFieldTypeDataTable();

            #endregion

            var intersectTypeUids = new List<Guid>();
            
            intersectTypes.ForEach(it =>
            {
                if (!intersectTypeUids.Contains(it.Uid))
                {
                    intersectTypeUids.Add(it.Uid);

                    var row = table.NewRow();

                    var sub = assetTypeVersions.SingleOrDefault(i => i.Uid == it.SubjectVersionUid);
                    var obj = assetTypeVersions.SingleOrDefault(i => i.Uid == it.ObjectVersionUid);

                    if (sub != null && obj != null)
                    {
                        row["Uid"] = it.Uid;
                        row["SubjectUid"] = sub.AssetTypeUid;
                        row["SubjectCardinality"] = (int)it.SubjectCardinality;
                        row["ObjectUid"] = obj.AssetTypeUid;
                        row["ObjectCardinality"] = (int)it.ObjectCardinality;
                        row["PredicateUid"] = it.PredicateUid;
                        row["UpdatedOn"] = it.UpdatedOn;

                        table.Rows.Add(row);

                        var fieldTypes = convertToGovernFieldTypes(it.Field_Items);

                        fieldTypes.ForEach(f =>
                        {
                            var fRow = fields.NewRow();
                            convertGovernFieldTypeToDataRow(fRow, it.Uid, f);
                            fields.Rows.Add(fRow);
                        });
                    }
                }
            });

            if (table.Rows.Count > 0)
            {
                using (SqlTransaction trans = connection.BeginTransaction("IntersectTypes"))
                {
                    var tempTable = $@"create table #IntersectTypes (
Uid uniqueidentifier not null, 
SubjectUid uniqueidentifier not null, SubjectCardinality int not null,
ObjectUid uniqueidentifier not null, ObjectCardinality int not null, 
PredicateUid uniqueidentifier not null, 
UpdatedOn datetime not null);

create table #IntersectTypeFields (
	[Uid] [uniqueidentifier] NOT NULL,
    {string.Join(",", FieldTypeDefinitions.Select(f => $"[{ f.Name}] { f.DbType} { (f.DbNull ? "null" : "not null")}"))}
); ";
                    connection.Execute(tempTable, transaction: trans);

                    SqlBulkCopy bulkCopy;

                    #region Intersect Type Bulk Load

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = table.Rows.Count,
                        DestinationTableName = "#IntersectTypes",
                        BulkCopyTimeout = 1000
                    };

                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("SubjectUid", "SubjectUid");
                    bulkCopy.ColumnMappings.Add("SubjectCardinality", "SubjectCardinality");
                    bulkCopy.ColumnMappings.Add("ObjectUid", "ObjectUid");
                    bulkCopy.ColumnMappings.Add("ObjectCardinality", "ObjectCardinality");
                    bulkCopy.ColumnMappings.Add("PredicateUid", "PredicateUid");
                    bulkCopy.ColumnMappings.Add("UpdatedOn", "UpdatedOn");

                    bulkCopy.WriteToServer(table);

                    #endregion

                    #region Fields

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = fields.Rows.Count,
                        DestinationTableName = "#IntersectTypeFields",
                        BulkCopyTimeout = 1000
                    };

                    buildFieldTypeColumnMappings(bulkCopy);

                    bulkCopy.WriteToServer(fields);

                    #endregion

                    bulkCopy = null;

                    try
                    {
                        connection.Execute(@"
merge   into [IntersectType] T
using   (
            select  I.*,
                    S.Object as Subject,
                    S.ObjectID as SubjectID,
                    O.Object,
                    O.ObjectID,
                    P.ID as PredicateID 
            from    #IntersectTypes I
                    inner join AssetType S on S.Uid = I.SubjectUid
                    inner join AssetType O on O.Uid = I.ObjectUid
                    inner join [Predicate] P on P.Uid = I.PredicateUid
        ) S
on (T.Uid = S.Uid)
when not matched then 
insert (Uid, Subject, SubjectID, SubjectCardinality, Object, ObjectID, ObjectCardinality, PredicateID, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy, IsSystem) 
values (S.Uid, S.Subject, S.SubjectID, S.SubjectCardinality, S.Object, S.ObjectID, S.ObjectCardinality, S.PredicateID, S.UpdatedOn, 0, S.UpdatedOn, 0, 1);", transaction: trans);

                        /*
when matched and S.UpdatedOn > T.UpdatedOn then
update  set
        T.Subject = S.Subject,
        T.SubjectID = S.SubjectID,
        T.SubjectCardinality = S.SubjectCardinality,
        T.Object = S.Object,
        T.ObjectID = S.ObjectID,
        T.ObjectCardinality = S.ObjectCardinality,
        T.PredicateID = S.PredicateID,
        T.UpdatedOn = S.UpdatedOn                         
                         */

                        var fieldSql = buildFieldMergeSql(@"
select  ATF.*,
        IAS.ID as IntersectTypeID,
        'IntersectType' as Object,
        IAS.ID as ObjectID
from    #IntersectTypeFields ATF
        inner join IntersectType IAS on IAS.Uid = ATF.Uid");

                        connection.Execute(fieldSql, transaction: trans);

                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                    }
                }
            }
        }

        public void SynchronizePredicates(List<Predicate> predicates)
        {
            var table = new DataTable();
            table.Columns.Add("Uid", typeof(Guid));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Inverse", typeof(string));
            table.Columns.Add("Type", typeof(int));

            var predicateUids = new List<Guid>();

            predicates.ForEach(p =>
            {
                if (!predicateUids.Contains(p.Uid))
                {
                    predicateUids.Add(p.Uid);

                    var row = table.NewRow();

                    row["Uid"] = p.Uid;
                    row["Name"] = p.Name;
                    row["Inverse"] = p.Inverse;
                    row["Type"] = (int)p.Type;

                    table.Rows.Add(row);
                }
            });

            if (table.Rows.Count > 0)
            {
                using (SqlTransaction trans = connection.BeginTransaction("Predicates"))
                {
                    var tempTable = @"create table #Predicates (
Uid uniqueidentifier not null, 
Name nvarchar(100) not null,
Inverse nvarchar(100) not null, 
[Type] int not null)";
                    connection.Execute(tempTable, transaction: trans);

                    SqlBulkCopy bulkCopy;

                    #region Bulk Load

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = table.Rows.Count,
                        DestinationTableName = "#Predicates",
                        BulkCopyTimeout = 1000
                    };

                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("Name", "Name");
                    bulkCopy.ColumnMappings.Add("Inverse", "Inverse");
                    bulkCopy.ColumnMappings.Add("Type", "Type");

                    bulkCopy.WriteToServer(table);

                    #endregion

                    bulkCopy = null;

                    try
                    {
                        connection.Execute(@"
merge   into [Predicate] T
using   #Predicates S
on (T.Uid = S.Uid)
when not matched then 
insert (Uid, Name, Inverse, [Type], IsSystem) 
values (S.Uid, S.Name, S.Inverse, S.[Type], 1);", transaction: trans);
                        /*
when    matched then
update  set
        T.Name = S.Name,
        T.Inverse = S.Inverse,
        T.[Type] = S.[Type]                         
                         */

                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                    }
                }
            }
        }

        public void SynchronizeAssets(List<Asset> assets, List<AssetTypeVersion> assetTypeVersions, List<AssetType> assetTypes)
        {
            #region Data Tables

            var table = new DataTable();
            table.Columns.Add("Uid", typeof(Guid));
            table.Columns.Add("AssetTypeUid", typeof(Guid));
            table.Columns.Add("State", typeof(int));
            table.Columns.Add("Code", typeof(string));
            table.Columns.Add("Object", typeof(string));
            table.Columns.Add("UpdatedOn", typeof(DateTime));

            var fields = new DataTable();
            fields.Columns.Add("Uid", typeof(Guid));
            fields.Columns.Add("AssetTypeUid", typeof(Guid));
            fields.Columns.Add("Name", typeof(string));
            fields.Columns.Add("Value", typeof(string));

            #endregion

            var uids = new List<Guid>();

            assets.ForEach(a =>
            {
                if (!uids.Contains(a.Uid))
                {
                    uids.Add(a.Uid);

                    var row = table.NewRow();

                    var atv = assetTypeVersions.FirstOrDefault(i => i.Uid == a.AssetTypeVersionUid);

                    if (atv != null)
                    {
                        var at = assetTypes.FirstOrDefault(i => i.Uid == atv.AssetTypeUid);

                        if (at != null)
                        { 
                            row["Uid"] = a.Uid;
                            row["AssetTypeUid"] = atv.AssetTypeUid;
                            if (a.Field_Items.Any(i => i.Name == "Code"))
                            {
                                row["Code"] = a.Field_Items.First(i => i.Name == "Code").Value;
                            }
                            row["Object"] = at.Object.ToString().Replace("Type", "");
                            row["State"] = (int)a.State;
                            row["UpdatedOn"] = a.UpdatedOn;

                            table.Rows.Add(row);

                            a.Field_Items.ForEach(f =>
                            {
                                var fRow = fields.NewRow();

                                fRow["Uid"] = a.Uid;
                                fRow["AssetTypeUid"] = atv.AssetTypeUid;
                                fRow["Name"] = f.Name;
                                fRow["Value"] = f.Value;

                                fields.Rows.Add(fRow);
                            });                         
                        }
                    }
                }
            });

            if (table.Rows.Count > 0)
            {
                using (SqlTransaction trans = connection.BeginTransaction("Assets"))
                {
                    var tempTable = @"create table #Assets (
Uid uniqueidentifier not null, 
AssetTypeUid uniqueidentifier not null, 
Code nvarchar(250) null,
State int not null, 
Object varchar(50) not null,
UpdatedOn datetime not null);

create table #AssetFields (
Uid uniqueidentifier not null, 
AssetTypeUid uniqueidentifier not null, 
Name nvarchar(250) not null,
Value nvarchar(max) not null);";
                    connection.Execute(tempTable, transaction: trans);

                    SqlBulkCopy bulkCopy;

                    #region Asset Bulk Load

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = table.Rows.Count,
                        DestinationTableName = "#Assets",
                        BulkCopyTimeout = 1000
                    };

                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("AssetTypeUid", "AssetTypeUid");
                    bulkCopy.ColumnMappings.Add("Code", "Code");
                    bulkCopy.ColumnMappings.Add("State", "State");
                    bulkCopy.ColumnMappings.Add("Object", "Object");
                    bulkCopy.ColumnMappings.Add("UpdatedOn", "UpdatedOn");

                    bulkCopy.WriteToServer(table);

                    #endregion

                    #region Fields

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = fields.Rows.Count,
                        DestinationTableName = "#AssetFields",
                        BulkCopyTimeout = 1000
                    };

                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("AssetTypeUid", "AssetTypeUid");
                    bulkCopy.ColumnMappings.Add("Name", "Name");
                    bulkCopy.ColumnMappings.Add("Value", "Value");

                    bulkCopy.WriteToServer(fields);

                    #endregion

                    bulkCopy = null;

                    try
                    {
                        connection.Execute(@"
merge   into [Asset] T
using   (
            select  I.*,
                    S.ID as AssetTypeID
            from    #Assets I
                    inner join AssetType S on S.Uid = I.AssetTypeUid
        ) S
on (T.Uid = S.Uid)
when not matched then 
insert (Uid, Object, AssetTypeID, State, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy, Code) 
values (S.Uid, S.Object, S.AssetTypeID, S.State, S.UpdatedOn, 0, S.UpdatedOn, 0, S.Code);", transaction: trans);

                        /*
when    matched and S.UpdatedOn > T.UpdatedOn then
update  set
        T.State = S.State,
        T.UpdatedOn = S.UpdatedOn,
        T.UpdatedBy = 0
                         */

                        connection.Execute(@"
merge   into Field T 
using   (
        select  F.Value,
                FT.ID as FieldTypeID,
                A.ID as AssetID,
                A.Object,
                A.ObjectID
        from    #AssetFields F
                inner join [Asset] A on A.Uid = F.Uid
                inner join FieldType FT on FT.AssetTypeID = A.AssetTypeID and FT.Name = F.Name
        ) S 
on      (T.FieldTypeID = S.FieldTypeID and T.AssetID = S.AssetID) 
when not matched then 
insert (ObjectType, ObjectID, AssetID, FieldTypeID, Value) 
values (S.Object, S.ObjectID, S.AssetID, S.FieldTypeID, S.Value);", transaction: trans);

                        /*
when matched then
update  set
        T.Value = S.Value
                        */

                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                    }
                }
            }
        }

        public void SynchronizeIntersects(List<Intersect> intersects)
        {
            #region Data Tables

            var table = new DataTable();
            table.Columns.Add("Uid", typeof(Guid));
            table.Columns.Add("SubjectUid", typeof(Guid));
            table.Columns.Add("ObjectUid", typeof(Guid));
            table.Columns.Add("IntersectTypeUid", typeof(Guid));
            table.Columns.Add("State", typeof(int));
            table.Columns.Add("UpdatedOn", typeof(DateTime));

            var fields = new DataTable();
            fields.Columns.Add("Uid", typeof(Guid));
            fields.Columns.Add("IntersectTypeUid", typeof(Guid));
            fields.Columns.Add("Name", typeof(string));
            fields.Columns.Add("Value", typeof(string));

            #endregion

            var intersectUids = new List<Guid>();

            intersects.ForEach(it =>
            {
                if (!intersectUids.Contains(it.Uid))
                {
                    intersectUids.Add(it.Uid);

                    var row = table.NewRow();

                    row["Uid"] = it.Uid;
                    row["SubjectUid"] = it.SubjectUid;
                    row["ObjectUid"] = it.ObjectUid;
                    row["IntersectTypeUid"] = it.IntersectTypeUid;
                    row["State"] = (int)it.State;
                    row["UpdatedOn"] = it.UpdatedOn;

                    table.Rows.Add(row);

                    it.Field_Items.ForEach(f =>
                    {
                        var fRow = fields.NewRow();

                        fRow["Uid"] = it.Uid;
                        fRow["IntersectTypeUid"] = it.IntersectTypeUid;
                        fRow["Name"] = f.Name;
                        fRow["Value"] = f.Value;

                        fields.Rows.Add(fRow);
                    });
                }
            });

            if (table.Rows.Count > 0)
            {
                using (SqlTransaction trans = connection.BeginTransaction("Intersects"))
                {
                    var tempTable = @"create table #Intersects (
Uid uniqueidentifier not null, 
SubjectUid uniqueidentifier not null, SubjectCardinality int not null,
ObjectUid uniqueidentifier not null, ObjectCardinality int not null, 
IntersectTypeUid uniqueidentifier not null, 
UpdatedOn datetime not null);

create table #IntersectFields (
Uid uniqueidentifier not null, 
IntersectTypeUid uniqueidentifier not null, 
Name nvarchar(250) not null,
Value nvarchar(max) not null);";
                    connection.Execute(tempTable, transaction: trans);

                    SqlBulkCopy bulkCopy;

                    #region Intersect Bulk Load

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = table.Rows.Count,
                        DestinationTableName = "#Intersects",
                        BulkCopyTimeout = 1000
                    };

                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("SubjectUid", "SubjectUid");
                    bulkCopy.ColumnMappings.Add("ObjectUid", "ObjectUid");
                    bulkCopy.ColumnMappings.Add("IntersectTypeUid", "IntersectTypeUid");
                    bulkCopy.ColumnMappings.Add("State", "State");
                    bulkCopy.ColumnMappings.Add("UpdatedOn", "UpdatedOn");

                    bulkCopy.WriteToServer(table);

                    #endregion

                    #region Fields

                    bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, trans)
                    {
                        BatchSize = fields.Rows.Count,
                        DestinationTableName = "#IntersectFields",
                        BulkCopyTimeout = 1000
                    };

                    bulkCopy.ColumnMappings.Add("Uid", "Uid");
                    bulkCopy.ColumnMappings.Add("IntersectTypeUid", "IntersectTypeUid");
                    bulkCopy.ColumnMappings.Add("Name", "Name");
                    bulkCopy.ColumnMappings.Add("Value", "Value");

                    bulkCopy.WriteToServer(fields);

                    #endregion

                    bulkCopy = null;

                    try
                    {
                        connection.Execute(@"
merge   into [Intersect] T
using   (
            select  I.*,
                    S.Object as Subject,
                    S.ObjectID as SubjectID,
                    O.Object,
                    O.ObjectID,
                    IT.ID as IntersectTypeID
            from    #Intersects I
                    inner join Asset S on S.Uid = I.SubjectUid
                    inner join Asset O on O.Uid = I.ObjectUid
                    inner join [IntersectType] IT on IT.Uid = I.IntersectTypeUid
        ) S
on (T.Uid = S.Uid)
when not matched then 
insert (Uid, IntersectTypeID, Subject, SubjectID, Object, ObjectID, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy, State) 
values (S.Uid, S.IntersectTypeID, S.Subject, S.SubjectID, S.Object, S.ObjectID, S.UpdatedOn, 0, S.UpdatedOn, 0, S.State);", transaction: trans);

                        /*
when    matched and S.UpdatedOn > T.UpdatedOn then
update  set
        T.Subject = S.Subject,
        T.SubjectID = S.SubjectID,
        T.Object = S.Object,
        T.ObjectID = S.ObjectID,
        T.State = S.State                         
                         */

                        connection.Execute(@"
merge   into Field T 
using   (
        select  F.Value,
                FT.ID as FieldTypeID,
                'Intersect' as Object,
                I.ID as ObjectID
        from    #IntersectTypeFields F
                inner join [Intersect] I on I.Uid = F.Uid
                inner join [IntersectType] IT on IT.Uid = I.IntersectTypeUid
                inner join FieldType FT on FT.Object = 'IntersectType' and FT.ObjectID = IT.ID and FT.Name = F.Name
        ) S 
on      (T.FieldTypeID = S.FieldTypeID and T.ObjectType = S.Object and T.ObjectID = S.ObjectID) 
when not matched then 
insert (ObjectType, ObjectID, FieldTypeID, Value) 
values (S.Object, S.ObjectID, S.FieldTypeID, S.Value);", transaction: trans);

                        /*
when    matched then
update  set
        T.Value = S.Value                         
                         */

                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                    }
                }
            }
        }
    }

    public static class CommunityPackageCheckTimerJob
    {
        const string functionName = "CommunityPackageCheck_TimerJob";
        const string timerSettings = "0 */15 * * * *";

        public static void Run([TimerTrigger(timerSettings, RunOnStartup = true)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var community = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                community.OpenWithRetry(RetryPolicy.DefaultProgressive);

                var environment = CoreFunction.GetConfigValueByKey("Environment");
                
                DateTime Now = DateTime.UtcNow;

                #region Get Package Lists

                var lvl = CoreFunction.GetEnvironmentLevelCurrentSlot();
                var reader = community.QueryMultiple(@"exec community.GetChanges @lvl", new { lvl = (int)lvl }, commandTimeout: 60);

                var packageVersions = reader.Read<PackageVersion>().ToList();
                var packageClients = reader.Read<PackageClient>().ToList();
                var allocations = reader.Read<Allocation>().ToList();
                var assetTypes = reader.Read<AssetType>().ToList();
                var assetTypeVersions = reader.Read<AssetTypeVersion>().ToList();
                var assets = reader.Read<Asset>().ToList();
                var predicates = reader.Read<Predicate>().ToList();
                var intersectTypes = reader.Read<IntersectType>().ToList();
                var intersects = reader.Read<Intersect>().ToList();
                
                #endregion

                community.Close();
                community.Dispose();

#if DEBUG
                var companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings().Where(i => i.CompanyID == 1).ToList();
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif

                bool shouldCreateSynchTimeUpdatedRecord = (packageVersions.Count > 0 && packageClients.Count > 0);

                companies.ForEach(c =>
                {
                    try
                    {
                        #region Get package data specific to this client

                        var thisClientPackages = packageClients.Where(p => p.ClientID == c.ClientID).ToList();

                        var clientPredicates = (
                            from pa in thisClientPackages
                            join al in allocations on pa.PackageVersionUid equals al.PackageVersionUid
                            from ituid in al.IntersectTypes_Items
                            join it in intersectTypes on ituid equals it.Uid
                            join pr in predicates on it.PredicateUid equals pr.Uid
                            select pr
                            ).ToList();

                        var clientIntersectTypes = (
                            from pa in thisClientPackages
                            join al in allocations on pa.PackageVersionUid equals al.PackageVersionUid
                            from ituid in al.IntersectTypes_Items
                            join it in intersectTypes on ituid equals it.Uid
                            select it
                            ).ToList();

                        var clientAssetTypes = (
                            from pa in thisClientPackages
                            join al in allocations on pa.PackageVersionUid equals al.PackageVersionUid
                            from atvuid in al.AssetTypeVersions_Items
                            join atv in assetTypeVersions on atvuid equals atv.Uid
                            join at in assetTypes on atv.AssetTypeUid equals at.Uid
                            select at
                            ).ToList();

                        var clientAssetTypeVersions = (
                            from pa in thisClientPackages
                            join al in allocations on pa.PackageVersionUid equals al.PackageVersionUid
                            from atvuid in al.AssetTypeVersions_Items
                            join atv in assetTypeVersions on atvuid equals atv.Uid
                            select atv
                            ).ToList();

                        #endregion

                        using (var conn = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password))
                        {
                            try
                            {
                                conn.OpenWithRetry(RetryPolicy.DefaultProgressive);
                                var processor = new CompanyProcessor(conn);

                                processor.SynchronizePredicates(clientPredicates);
                                processor.SynchronizeAssetTypes(clientAssetTypes, clientAssetTypeVersions);
                                processor.SynchronizeIntersectTypes(clientIntersectTypes, clientAssetTypeVersions);
                                processor.SynchronizeAssets(assets, assetTypeVersions, assetTypes);
                                processor.SynchronizeIntersects(intersects);

                                processor = null;

                                conn.Close();
                            }
                            catch (Exception ex)
                            {
                                shouldCreateSynchTimeUpdatedRecord = false;
                                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                    }
                });

                if (shouldCreateSynchTimeUpdatedRecord)
                {
                    using (var communityConn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                    {
                        communityConn.OpenWithRetry(RetryPolicy.DefaultProgressive);
                        communityConn.Execute("insert into community.PackageDeploymentHistory (ChangeOn) values (@Now)", new { Now });
                        communityConn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }            
        }
    }
}
