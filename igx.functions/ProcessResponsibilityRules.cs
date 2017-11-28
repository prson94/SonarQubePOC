using d360.core;
using d360.core.entities;
using d360.model;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace igx.functions
{
    public static class ProcessResponsibilityRules
    {
        const string functionName = "ProcessResponsibilityRules";
        const string timerSettings = "0 */5 * * * *";
        //const string timerSettings = "*/5 * * * * *";

        [FunctionName(functionName)]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log) //   
        {
            //https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#schedule-examples

            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    try
                    {
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);

                        company.OpenWithRetry(RetryPolicy.DefaultFixed);

                        var items = company.Query<ResponsibilityTypeRelationRule>(@"select * from ResponsibilityTypeRelationRule").ToList();
                        var results = new List<EndResult>();

                        if (items.Count > 0)
                        {
                            var errorList = string.Empty;
                            items.ForEach(i => {
                                try
                                {
                                    i.SetDefinitionFromRaw();

                                    var oResults = company.GetWhenResults(i);
                                    var sResults = company.GetThenResults(i);

                                    results.AddRange(
                                              from o in oResults
                                              join s in sResults on 1 equals 1
                                              select new EndResult
                                              {
                                                  RuleID = i.ID,
                                                  ResponsibilityTypeID = i.ResponsibilityTypeID,
                                                  AssetID = o.AssetID,
                                                  SecurityAsset = s.SecurityAsset,
                                                  SecurityAssetID = s.SecurityAssetID
                                              });
                                }
                                catch (Exception ex)
                                {
                                    //errorList += $"Company [{c.CompanyID}] for Object [{i.Object} {i.ObjectID}]: [{ex.GetFullExceptionData()}]; ";
                                }
                            });

                            if (results.Count > 0)
                            {
                                #region Save results to temp table via bulk insert

                                using (var trans = company.BeginTransaction())
                                {
                                    company.Execute(@"
    set nocount on 
    create table #ResponsibilityTypeRelationItem (
	    RuleID int not null, 
	    ResponsibilityTypeID int not null, 
	    AssetID bigint not null, 
        [SecurityAsset] char(1) not null, 
	    [SecurityAssetID] int not null
    )
    set nocount off", commandTimeout: 3600, transaction: trans);

                                    #region Bulk insert the rows above.

                                    using (var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans))
                                    {
                                        bulkCopy.BatchSize = results.Count;
                                        bulkCopy.DestinationTableName = "#ResponsibilityTypeRelationItem";
                                        bulkCopy.BulkCopyTimeout = 3600;

                                        var table = new System.Data.DataTable();

                                        #region Create column mappings

                                        var columnName = "RuleID";
                                        table.Columns.Add(columnName, typeof(int));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        columnName = "ResponsibilityTypeID";
                                        table.Columns.Add(columnName, typeof(int));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        columnName = "AssetID";
                                        table.Columns.Add(columnName, typeof(long));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        columnName = "SecurityAsset";
                                        table.Columns.Add(columnName, typeof(string));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        columnName = "SecurityAssetID";
                                        table.Columns.Add(columnName, typeof(int));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        #endregion

                                        foreach (var item in results)
                                        {
                                            var row = table.NewRow();

                                            row["RuleID"] = item.RuleID;
                                            row["ResponsibilityTypeID"] = item.ResponsibilityTypeID;
                                            row["AssetID"] = item.AssetID;
                                            row["SecurityAsset"] = item.SecurityAsset;
                                            row["SecurityAssetID"] = item.SecurityAssetID;

                                            table.Rows.Add(row);
                                        }

                                        bulkCopy.WriteToServer(table);
                                    }

                                    #endregion

                                    #region  Merge the raw data you compiled above into the item table. These are rule results.

                                    company.Execute(@"
merge   ResponsibilityTypeRelationItem as T 
using   ( 
        select  *
        from    #ResponsibilityTypeRelationItem
        ) as S 
        on  (
            T.RuleID = S.RuleID 
            and T.ResponsibilityTypeID = S.ResponsibilityTypeID 
            and T.[AssetID] = S.[AssetID] 
            and T.[SecurityAsset] = S.[SecurityAsset] 
            and T.[SecurityAssetID] = S.[SecurityAssetID] 
            )
when    not matched by source and T.RuleID > 0 then 
        delete
when    not matched by target then 
        insert (RuleID, ResponsibilityTypeID, [AssetID], SecurityAsset, SecurityAssetID) 
        values (S.RuleID, S.ResponsibilityTypeID, S.[AssetID], S.SecurityAsset, S.SecurityAssetID);", 
                commandTimeout: 3600, transaction: trans);

                                    #endregion

                                    #region Merge the overrides into the item table. These are override items.

                                    company.Execute(@"
merge   ResponsibilityTypeRelationItem as T 
using   ( 
        select  *
        from    ResponsibilityTypeRelationOverrideItem
        ) as S 
        on  (
            T.RuleID = 0
            and T.ResponsibilityTypeID = S.ResponsibilityTypeID 
            and T.[AssetID] = S.[AssetID] 
            and T.[SecurityAsset] = S.[SecurityAsset] 
            and T.[SecurityAssetID] = S.[SecurityAssetID] 
            )
when    not matched by source and T.RuleID = 0 then 
        delete
when    not matched by target then 
        insert (RuleID, ResponsibilityTypeID, [AssetID], SecurityAsset, SecurityAssetID, OverrideItemID) 
        values (0, S.ResponsibilityTypeID, S.[AssetID], S.SecurityAsset, S.SecurityAssetID, S.ID);",
                commandTimeout: 3600, transaction: trans);

                                    #endregion

                                    #region Mark the overriden items generated from rules with overrides we laoded above.

                                    company.Execute(@"
update	T
set		T.Overriden = 1
from	ResponsibilityTypeRelationItem T
		inner join ResponsibilityTypeRelationItem S on S.RuleID = 0 and T.RuleID > 0 and S.AssetID = T.AssetID and S.ResponsibilityTypeID = T.ResponsibilityTypeID and T.Overriden = 0;",
                commandTimeout: 3600, transaction: trans);

                                    #endregion

                                    trans.Commit();
                                }

                                #endregion
                            }
                        }

                        //if (!string.IsNullOrEmpty(errorList))
                        //{
                        //    CoreFunction.AITrackException(functionName, new ApplicationException($"The following TextPath update errors occurred: {errorList}"), c.CompanyID);
                        //    log.Error(errorList);
                        //}
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }
                });

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }
    }
}
