using d360.core;
using d360.core.entities.Plugins;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace igx.jobs.fusiondeploymentprocessor
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

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class FusionDeploymentProcessor
    {
        #region Utility

        static string cleanObjectName(string name)
        {
            name = name.Replace("'", "").Replace(" ", "").Replace("-", "").Replace("&", "And").Replace(":", "").Replace(";", "").Trim();
            Regex rgx = new Regex("[^a-zA-Z0-9-]");
            name = rgx.Replace(name, "");
            return name;
        }

        static void getDynamicFieldJoinStatements(List<FieldType> fields, string type, out string joins, out string columns, string idColumn = "A.ID")
        {
            columns = "";
            joins = "";

            var typesToIgnore = new List<string> {
                DataType.Attribute.ToString(), DataType.Color.ToString(), DataType.ComplexRelationLookup.ToString(), DataType.DataTableSelect.ToString(),
                DataType.File.ToString(), DataType.FilteredLookup.ToString(), DataType.Hidden.ToString(), DataType.OwnershipLookup.ToString(),
                DataType.Password.ToString(), DataType.RefListRelationship.ToString(), DataType.UncLink.ToString()
            };

            fields.RemoveAll(i => typesToIgnore.Contains(i.Type));

            foreach (var f in fields)
            {
                var name = cleanObjectName(f.Name);
                columns += (f.Type == "Lookup") ? $"[{name}].Value as [{name}ID], [{name}].FormattedValue as [{name}], " : $"[{name}].FormattedValue as [{name}], ";
                joins += $" left join FieldDetail [{name}] on [{name}].Object = '{type}' and [{name}].ObjectID = {idColumn} and [{name}].FieldTypeID = {f.ID}";
            }

            fields = null;
        }

        static void executeSqlWithTry(SqlConnection companyConnection, string viewSql)
        {
            try
            {
                companyConnection.Execute(viewSql.ToString());
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, null, new Dictionary<string, string>() { { "Attempted SQL: ", viewSql } });
            }
        }

        #endregion

        const string functionName = "FusionDeployment_Process";
#if DEBUG
        const string timerSettings = "*/1 * * * * *";
#else
        const string timerSettings = "0 */15 * * * *";
#endif
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                var community = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
                community.OpenWithRetry(RetryPolicy.DefaultProgressive);

                var clientFusionTypes = community.Query<ClientFusionType>("select * from plugin.ClientFusionType");
                var fusionTypes = community.Query<FusionType>("select * from plugin.FusionType");
                var fusionTypeFields = community.Query<FusionTypeField>("select * from plugin.FusionTypeField");
                var fusionAttributeTypes = community.Query<FusionAttributeType>(@"
select	A.ID,
		I.StartFusionAttributeTypeID as ParentID,
		A.FusionTypeID,
		A.Name
from	plugin.FusionAttributeType A
		left join plugin.FusionIntersectType I on I.EndFusionAttributeTypeID = A.ID and I.PredicateType = 3").ToList();
                var fusionAttributeTypeFields = community.Query<FusionAttributeTypeField>("select * from plugin.FusionAttributeTypeField").ToList();
                var fusionIntersectTypes = community.Query<FusionIntersectType>("select * from plugin.FusionIntersectType").ToList();

                community.Close();
                community.Dispose();

#if DEBUG
                var companies = CompanyConnectionUtils.GetCompaniesWithDatabaseServerSettings().Where(i => i.CompanyID == 215).ToList();
#else
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
#endif
                var environment = CoreFunction.GetConfigValueByKey("Environment");

                companies.ForEach(c =>
                {
                    try
                    {
                        #region Lists

                        var c_FusionTypes = (
                            from cft in clientFusionTypes
                            from ft in fusionTypes
                            where cft.ClientID == c.ClientID
                            where cft.FusionTypeID == ft.ID
                            select ft
                        ).ToList();


                        var c_FusionTypeFields = (
                            from ft in c_FusionTypes
                            from f in fusionTypeFields
                            where f.FusionTypeID == ft.ID
                            select f
                            ).ToList();

                        var c_FusionTypeIntersects = (
                            from ft in c_FusionTypes
                            from f in fusionIntersectTypes
                            where f.FusionTypeID == ft.ID
                            select f
                            ).ToList();

                        if (environment == "ALTERNATE" || environment == "PROD")
                        {
                            c_FusionTypeIntersects = c_FusionTypeIntersects.Where(i => i.PredicateType != 3).ToList();
                        }


                        var c_FusionAttributeTypes = (
                            from cft in clientFusionTypes
                            from ft in fusionTypes
                            from fa in fusionAttributeTypes
                            where cft.ClientID == c.ClientID
                            where cft.FusionTypeID == ft.ID
                            where fa.FusionTypeID == ft.ID
                            select fa
                            ).ToList();

                        var c_FusionAttributeTypeFields = (
                            from fa in c_FusionAttributeTypes
                            from f in fusionAttributeTypeFields
                            where f.FusionAttributeTypeID == fa.ID
                            select f
                            ).ToList();

                        #endregion

                        #region Data Tables

                        #region Fusion Types

                        var tbl_FusionTypes = new System.Data.DataTable();

                        tbl_FusionTypes.Columns.Add("ID", typeof(int));
                        tbl_FusionTypes.Columns.Add("Name", typeof(string));
                        tbl_FusionTypes.Columns.Add("Description", typeof(string));

                        c_FusionTypes.ForEach(o => {
                            var row = tbl_FusionTypes.NewRow();

                            row["ID"] = o.ID;
                            row["Name"] = o.Name;
                            row["Description"] = o.Description;

                            tbl_FusionTypes.Rows.Add(row);
                        });

                        #endregion

                        #region Fusion Type Fields

                        var tbl_FusionTypeFields = new System.Data.DataTable();

                        tbl_FusionTypeFields.Columns.Add("FusionTypeID", typeof(int));
                        tbl_FusionTypeFields.Columns.Add("Name", typeof(string));
                        tbl_FusionTypeFields.Columns.Add("FriendlyName", typeof(string));
                        tbl_FusionTypeFields.Columns.Add("Type", typeof(string));
                        tbl_FusionTypeFields.Columns.Add("SortOrder", typeof(int));
                        tbl_FusionTypeFields.Columns.Add("IsListable", typeof(bool));
                        tbl_FusionTypeFields.Columns.Add("Category", typeof(string));

                        c_FusionTypeFields.ForEach(o => {
                            var row = tbl_FusionTypeFields.NewRow();

                            row["FusionTypeID"] = o.FusionTypeID;
                            row["Name"] = o.Name;
                            row["FriendlyName"] = o.FriendlyName;
                            row["Type"] = o.Type;
                            row["SortOrder"] = o.SortOrder;
                            row["IsListable"] = o.IsListable;
                            row["Category"] = o.Category;

                            tbl_FusionTypeFields.Rows.Add(row);
                        });

                        #endregion

                        #region Fusion Type Intersects

                        var tbl_FusionTypeIntersects = new System.Data.DataTable();

                        tbl_FusionTypeIntersects.Columns.Add("StartFusionAttributeTypeID", typeof(int));
                        tbl_FusionTypeIntersects.Columns.Add("EndFusionAttributeTypeID", typeof(int));
                        tbl_FusionTypeIntersects.Columns.Add("ReadOnly", typeof(bool));
                        tbl_FusionTypeIntersects.Columns.Add("FusionTypeID", typeof(int));
                        tbl_FusionTypeIntersects.Columns.Add("PredicateType", typeof(int));

                        c_FusionTypeIntersects.ForEach(o => {
                            var row = tbl_FusionTypeIntersects.NewRow();

                            row["StartFusionAttributeTypeID"] = o.StartFusionAttributeTypeID;
                            row["EndFusionAttributeTypeID"] = o.EndFusionAttributeTypeID;
                            row["ReadOnly"] = o.ReadOnly;
                            row["FusionTypeID"] = o.FusionTypeID;
                            if (o.PredicateType.HasValue)
                                row["PredicateType"] = o.PredicateType;
                            else
                                row["PredicateType"] = DBNull.Value;

                            tbl_FusionTypeIntersects.Rows.Add(row);
                        });

                        #endregion

                        #region Fusion Attribute Types

                        var tbl_FusionAttributeTypes = new System.Data.DataTable();

                        tbl_FusionAttributeTypes.Columns.Add("ID", typeof(int));
                        tbl_FusionAttributeTypes.Columns.Add("ParentID", typeof(int));
                        tbl_FusionAttributeTypes.Columns.Add("FusionTypeID", typeof(int));
                        tbl_FusionAttributeTypes.Columns.Add("Name", typeof(string));

                        c_FusionAttributeTypes.ForEach(o => {
                            var row = tbl_FusionAttributeTypes.NewRow();

                            row["ID"] = o.ID;
                            if (o.ParentID.HasValue)
                                row["ParentID"] = o.ParentID;
                            else
                                row["ParentID"] = DBNull.Value;
                            row["FusionTypeID"] = o.FusionTypeID;
                            row["Name"] = o.Name;

                            tbl_FusionAttributeTypes.Rows.Add(row);
                        });

                        #endregion

                        #region Fusion Attribute Type Fields

                        var tbl_FusionAttributeTypeFields = new System.Data.DataTable();

                        tbl_FusionAttributeTypeFields.Columns.Add("FusionAttributeTypeID", typeof(int));
                        tbl_FusionAttributeTypeFields.Columns.Add("Name", typeof(string));
                        tbl_FusionAttributeTypeFields.Columns.Add("FriendlyName", typeof(string));
                        tbl_FusionAttributeTypeFields.Columns.Add("Type", typeof(string));
                        tbl_FusionAttributeTypeFields.Columns.Add("SortOrder", typeof(int));
                        tbl_FusionAttributeTypeFields.Columns.Add("IsListable", typeof(bool));
                        tbl_FusionAttributeTypeFields.Columns.Add("IsRequired", typeof(bool));

                        c_FusionAttributeTypeFields.ForEach(o => {
                            var row = tbl_FusionAttributeTypeFields.NewRow();

                            row["FusionAttributeTypeID"] = o.FusionAttributeTypeID;
                            row["Name"] = o.Name;
                            row["FriendlyName"] = o.FriendlyName;
                            row["Type"] = o.Type;
                            row["SortOrder"] = o.SortOrder;
                            row["IsListable"] = o.IsListable;
                            row["IsRequired"] = o.IsRequired;

                            tbl_FusionAttributeTypeFields.Rows.Add(row);
                        });

                        #endregion

                        #endregion

                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        company.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        company.Execute("DISABLE TRIGGER FieldType_AfterUpsert ON dbo.FieldType");

                        using (var trans = company.BeginTransaction())
                        {
                            try
                            {
                                #region Create temp tables

                                company.Execute(@"
    create table #tbl_FusionTypes (
        ID int not null,
        Name nvarchar(250) not null,
        Description nvarchar(max) null
    );

    create table #tbl_FusionTypeFields (
	    [FusionTypeID] [int] NOT NULL,
	    [Name] [nvarchar](250) NOT NULL,
	    [FriendlyName] [nvarchar](250) NOT NULL,
	    [Type] [varchar](25) NULL,
	    [SortOrder] [int] NOT NULL,
	    [IsListable] [bit] NOT NULL,
	    [Category] [nvarchar](250) NULL
    );

    create table #tbl_FusionTypeIntersects (
	    [StartFusionAttributeTypeID] [int] NOT NULL,
	    [EndFusionAttributeTypeID] [int] NOT NULL,
	    [ReadOnly] [bit] NOT NULL,
	    [FusionTypeID] [int] NOT NULL,
	    [PredicateType] [int] NULL
    );

    create table #tbl_FusionAttributeTypes (
	    [ID] [int] NOT NULL,
	    [ParentID] [int] NULL,
	    [FusionTypeID] [int] NOT NULL,
	    [Name] [nvarchar](250) NOT NULL
    );

    create table #tbl_FusionAttributeTypeFields (
	    [FusionAttributeTypeID] [int] NOT NULL,
	    [Name] [nvarchar](250) NOT NULL,
	    [FriendlyName] [nvarchar](250) NOT NULL,
	    [Type] [varchar](25) NULL,
	    [SortOrder] [int] NOT NULL,
	    [IsListable] [bit] NOT NULL,
	    [IsRequired] [bit] NOT NULL
    );
    ", transaction: trans);
                                #endregion

                                SqlBulkCopy bulkCopy = null;

                                #region Merge fusion types

                                bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans);

                                bulkCopy.BatchSize = tbl_FusionTypes.Rows.Count;
                                bulkCopy.DestinationTableName = "#tbl_FusionTypes";
                                bulkCopy.BulkCopyTimeout = 3600;

                                bulkCopy.ColumnMappings.Add("ID", "ID");
                                bulkCopy.ColumnMappings.Add("Name", "Name");
                                bulkCopy.ColumnMappings.Add("Description", "Description");

                                bulkCopy.WriteToServer(tbl_FusionTypes);

                                company.Execute(@"
        SET IDENTITY_INSERT FusionType ON;
        MERGE
	        INTO    [FusionType] T
	        USING   (
			        SELECT	* from #tbl_FusionTypes
			        ) S
	        ON      (S.ID = T.ID)
        WHEN NOT MATCHED THEN
	        INSERT  (ID, Name, Description, UpdatedOn, UpdatedBy)
	        VALUES  (S.ID, S.Name, S.Description, getutcdate(), 0);
        SET IDENTITY_INSERT FusionType OFF;", transaction: trans);

                                #endregion

                                #region Merge fusion type fields

                                bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans);

                                bulkCopy.BatchSize = tbl_FusionTypeFields.Rows.Count;
                                bulkCopy.DestinationTableName = "#tbl_FusionTypeFields";
                                bulkCopy.BulkCopyTimeout = 3600;

                                bulkCopy.ColumnMappings.Add("FusionTypeID", "FusionTypeID");
                                bulkCopy.ColumnMappings.Add("Name", "Name");
                                bulkCopy.ColumnMappings.Add("FriendlyName", "FriendlyName");
                                bulkCopy.ColumnMappings.Add("Type", "Type");
                                bulkCopy.ColumnMappings.Add("SortOrder", "SortOrder");
                                bulkCopy.ColumnMappings.Add("IsListable", "IsListable");
                                bulkCopy.ColumnMappings.Add("Category", "Category");

                                bulkCopy.WriteToServer(tbl_FusionTypeFields);

                                company.Execute(@"
    MERGE
	    INTO    [FieldType] T
	    USING   (
                SELECT  * from #tbl_FusionTypeFields
			    ) S
	    ON      (T.Object = 'FusionType' and S.FusionTypeID = T.ObjectID and S.Name = T.Name)
    WHEN NOT MATCHED THEN
	    INSERT  (Name, FriendlyName, [Type], [Object], ObjectID, SortOrder, IsRequired, IsListable, IsDisplayable, IsEditable, Category)
	    VALUES  (S.Name, S.FriendlyName, S.[Type], 'FusionType', S.FusionTypeID, S.SortOrder, 1, S.IsListable, 1, 1, S.Category);", transaction: trans);

                                #endregion

                                #region Merge fusion type intersects

                                bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans);

                                bulkCopy.BatchSize = tbl_FusionTypeIntersects.Rows.Count;
                                bulkCopy.DestinationTableName = "#tbl_FusionTypeIntersects";
                                bulkCopy.BulkCopyTimeout = 3600;

                                bulkCopy.ColumnMappings.Add("StartFusionAttributeTypeID", "StartFusionAttributeTypeID");
                                bulkCopy.ColumnMappings.Add("EndFusionAttributeTypeID", "EndFusionAttributeTypeID");
                                bulkCopy.ColumnMappings.Add("ReadOnly", "ReadOnly");
                                bulkCopy.ColumnMappings.Add("FusionTypeID", "FusionTypeID");
                                bulkCopy.ColumnMappings.Add("PredicateType", "PredicateType");

                                bulkCopy.WriteToServer(tbl_FusionTypeIntersects);

                                company.Execute(@"
		    INSERT INTO IntersectType (Subject, SubjectID, Object, ObjectID, UpdatedOn, UpdatedBy, IsSystem, CreatedBy, CreatedOn, PredicateID, SubjectCardinality, ObjectCardinality) 
                select  @type as Subject, I.StartFusionAttributeTypeID,
                        @type as Object, I.EndFusionAttributeTypeID,
                        getutcdate(), 0,
                        I.[ReadOnly],
                        0, getutcdate(),
                        P.ID,
                        2, 2
                from    #tbl_FusionTypeIntersects I
                        outer apply (
                                    select top 1 ID from [Predicate] where [Type] = I.PredicateType
                                    ) P
                        left join IntersectType E on E.Subject = @type and E.Object = @type and E.SubjectID = I.StartFusionAttributeTypeID and E.ObjectID = I.EndFusionAttributeTypeID
                where   E.ID is null", new { type = "FusionAttributeType" }, transaction: trans);

                                #endregion

                                #region Merge fusion attribute types

                                bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans);

                                bulkCopy.BatchSize = tbl_FusionAttributeTypes.Rows.Count;
                                bulkCopy.DestinationTableName = "#tbl_FusionAttributeTypes";
                                bulkCopy.BulkCopyTimeout = 3600;

                                bulkCopy.ColumnMappings.Add("ID", "ID");
                                bulkCopy.ColumnMappings.Add("ParentID", "ParentID");
                                bulkCopy.ColumnMappings.Add("FusionTypeID", "FusionTypeID");
                                bulkCopy.ColumnMappings.Add("Name", "Name");

                                bulkCopy.WriteToServer(tbl_FusionAttributeTypes);

                                company.Execute(@"
    SET IDENTITY_INSERT FusionAttributeType ON;
    MERGE
	    INTO    [FusionAttributeType] T
	    USING   (
			    SELECT	* from #tbl_FusionAttributeTypes
			    ) S
	    ON      (S.ID = T.ID) 
    WHEN MATCHED THEN
        UPDATE SET
        T.Name = S.Name,
        T.ParentID = S.ParentID,
        T.UpdatedOn = getutcdate(),
        T.UpdatedBy = 0
    WHEN NOT MATCHED THEN
	    INSERT  (ID, ParentID, FusionTypeID, Name, Assignable, UpdatedOn, UpdatedBy, ScanEnabled)
	    VALUES  (S.ID, S.ParentID, S.FusionTypeID, S.Name, 1, getutcdate(), 0, 1);
    SET IDENTITY_INSERT FusionAttributeType OFF;", transaction: trans);

                                #endregion

                                #region Merge fusion attribute type fields

                                bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans);

                                bulkCopy.BatchSize = tbl_FusionAttributeTypeFields.Rows.Count;
                                bulkCopy.DestinationTableName = "#tbl_FusionAttributeTypeFields";
                                bulkCopy.BulkCopyTimeout = 3600;

                                bulkCopy.ColumnMappings.Add("FusionAttributeTypeID", "FusionAttributeTypeID");
                                bulkCopy.ColumnMappings.Add("Name", "Name");
                                bulkCopy.ColumnMappings.Add("FriendlyName", "FriendlyName");
                                bulkCopy.ColumnMappings.Add("Type", "Type");
                                bulkCopy.ColumnMappings.Add("SortOrder", "SortOrder");
                                bulkCopy.ColumnMappings.Add("IsListable", "IsListable");
                                bulkCopy.ColumnMappings.Add("IsRequired", "IsRequired");

                                bulkCopy.WriteToServer(tbl_FusionAttributeTypeFields);

                                company.Execute(@"
    MERGE
	    INTO    [FieldType] T
	    USING   (
                SELECT  * from #tbl_FusionAttributeTypeFields
			    ) S
	    ON      (T.Object = 'FusionAttributeType' and S.FusionAttributeTypeID = T.ObjectID and S.Name = T.Name) 
    WHEN NOT MATCHED THEN
	    INSERT  (Name, FriendlyName, [Type], [Object], ObjectID, SortOrder, IsRequired, IsListable, IsDisplayable, IsEditable)
	    VALUES  (S.Name, S.FriendlyName, S.[Type], 'FusionAttributeType', S.FusionAttributeTypeID, S.SortOrder, S.IsRequired, S.IsListable, 1, 0);", transaction: trans);

                                #endregion

                                trans.Commit();
                            }
                            catch (Exception ex)
                            {
                                trans.Rollback();
                                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                            }
                        }

                        company.Execute("ENABLE TRIGGER FieldType_AfterUpsert ON dbo.FieldType");

                        company.Close();
                        company.Dispose();
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                    }
                });
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }
            finally
            {
                //CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
        }
    }
}
