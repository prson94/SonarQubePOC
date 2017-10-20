using d360.core;
using d360.core.entities;
using d360.utils.company;
using d360.model;
using Dapper;
using igx.functions.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace igx.functions.Timer
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
                                                  Object = o.Object,
                                                  ObjectID = o.ObjectID,
                                                  ResourceID = s.ResourceID,
                                                  GroupID = s.GroupID
                                              });

                                    //company.Execute($"update [{i.Object}] set TextPath = @tp where ID = @id", new { tp = i.CorrectTextPath, id = i.ObjectID });
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
    create table #ResponsibilityTypeRelationRuleItem (
	    RuleID int not null,
	    ResponsibilityTypeID int NOT NULL,
	    [Object] varchar(50) NOT NULL,
	    ObjectID int NOT NULL,	
	    [ResourceID] int not null,
	    [GroupID] int not null
    )
    set nocount off", commandTimeout: 3600, transaction: trans);

                                    using (var bulkCopy = new SqlBulkCopy(company, SqlBulkCopyOptions.Default, trans))
                                    {
                                        bulkCopy.BatchSize = results.Count;
                                        bulkCopy.DestinationTableName = "#ResponsibilityTypeRelationRuleItem";
                                        bulkCopy.BulkCopyTimeout = 3600;

                                        var table = new System.Data.DataTable();

                                        #region Create column mappings

                                        var columnName = "RuleID";
                                        table.Columns.Add(columnName, typeof(int));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        columnName = "ResponsibilityTypeID";
                                        table.Columns.Add(columnName, typeof(int));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        columnName = "Object";
                                        table.Columns.Add(columnName, typeof(string));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        columnName = "ObjectID";
                                        table.Columns.Add(columnName, typeof(int));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        columnName = "ResourceID";
                                        table.Columns.Add(columnName, typeof(int));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        columnName = "GroupID";
                                        table.Columns.Add(columnName, typeof(int));
                                        bulkCopy.ColumnMappings.Add(columnName, columnName);

                                        #endregion

                                        foreach (var item in results)
                                        {
                                            var row = table.NewRow();

                                            row["RuleID"] = item.RuleID;
                                            row["ResponsibilityTypeID"] = item.ResponsibilityTypeID;
                                            row["Object"] = item.Object;
                                            row["ObjectID"] = item.ObjectID;
                                            row["ResourceID"] = item.ResourceID;
                                            row["GroupID"] = item.GroupID ?? 0;

                                            table.Rows.Add(row);
                                        }

                                        bulkCopy.WriteToServer(table);
                                    }

                                    company.Execute(@"
        merge   ResponsibilityTypeRelationRuleItem as T 
        using   ( 
                select  *
                from    #ResponsibilityTypeRelationRuleItem
                ) as S 
                on  (
                    T.RuleID = S.RuleID 
                    and T.ResponsibilityTypeID = S.ResponsibilityTypeID 
                    and T.[Object] = S.[Object] 
                    and T.ObjectID = S.ObjectID 
                    and T.[ResourceID] = S.[ResourceID] 
                    and T.[GroupID] = S.[GroupID] 
                    )
        when    matched then 
                update set  T.UpdatedOn = getutcdate()
        when    not matched by source then 
                delete
        when    not matched by target then 
                insert (RuleID, ResponsibilityTypeID, [Object], ObjectID, ResourceID, GroupID, UpdatedOn) 
                values (S.RuleID, S.ResponsibilityTypeID, S.[Object], S.ObjectID, S.ResourceID, S.GroupID, getutcdate());", commandTimeout: 3600, transaction: trans);

                                    trans.Commit();
                                }

                                #endregion

                                // Merge into the responsibility cache.

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
