using d360.model;
using Microsoft.Web.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Dapper;
using System.Data.SqlClient;
using System.Data;
using Swashbuckle.Swagger.Annotations;
using System.Web.Http.Description;
using d360.core.enums;
using d360.web.Models;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/dataquality"), 
        Authorize,
        ApiExplorerSettings(IgnoreApi = true)
    ]

    public class DataQualityController : BaseV2ApiController
    {                
        public DataQualityController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {
        
        }

        /// <summary>
        /// Returns all rule results for the specified rule Uid the default implementation.
        /// If the user is not an admin, http status code 403 (forbidden) is returned.
        /// </summary>
        /// <param name="ruleUid">The Uid of the rule</param>
        /// <returns>The rule result object with the specified Uid for the default implementation.  If no such rule result or implementation exists http status code 404 not found is returned.</returns>
        [
            HttpGet, 
            MapToApiVersion("2.0"), 
            Route("ruleresults/{ruleUid}"),
            SwaggerResponse(HttpStatusCode.OK, "Indicates the request succeeded"),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid."),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that no such rule or implementation was found."),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you do not have access to this endpoint."),            
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.")
        ]
        public IQueryable<dynamic> GetRuleResults(Guid ruleUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            var rule = Company.Assets.FirstOrDefault(x => x.uid == ruleUid);

            if (rule == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"No such rule with id {ruleUid}"));

            return  Company.Query<dynamic>(@"
                        select  A.ObjectID as ID,
                                null as RuleImplementationID,
                                I.EffectiveDate,
                                I.RunDate,
                                I.RowsPassed,
                                I.RowsFailed,
                                I.PassFraction,
                                I.FailFraction,
                                cast(P.Passed as bit) as Passed
                        from    (
                            select	A.[uid] as AssetUid,
                                    R.[uid],
                                    R.EffectiveDate,
		                            R.RunDate,
		                            R.PassCount as RowsPassed,
		                            R.FailCount as RowsFailed,
		                            R.PassFraction,
		                            (1.0 - R.PassFraction) as FailFraction
                            from	graph.AssetNode A,
		                            dbo.AssetResultEdge E,
		                            dbo.AssetResult R
                            where	MATCH(A-(E)->R)
		                            and A.[uid] = @uid) I
                            inner join Asset A on A.[uid] = I.AssetUid
                            cross apply dbo.CalculatePassedPropertyForAssetResult(I.[uid]) P
                        ", new { rule.uid }, ApiTimeout).AsQueryable();

        }

        /// <summary>
        /// Creates a new Data Quality Rule result record for the specified Rule with the specified rule Uid.  
        ///  Looks for a rule implementation called 'default'.  If not found, it creates this rule implemention.        
        ///  Current user must be an admin or http status code 403 is returned. If the specified RuleUid is not found http status code 401 is returned.            
        ///  If any of the specified AssetUids are not valid, a http status code 400 bad request with the Uids of the invalid assets is returned.  
        ///  No rule results are written if there is any sort of error.  You must fix any errors and resubmit your request.  The timeout parameter is optional.  If specified,
        ///  a value in seconds is used to override the default timeout of 600 seconds.
        /// </summary>
        /// <param name="ruleUid">The Uid of the rule</param>
        /// <param name="ruleResults">The rule results and the mappings of fusionattributes that tie to the results</param>
        /// <returns></returns>
        [
            HttpPost,            
            MapToApiVersion("2.0"), 
            Route("{ruleUid}"),
            SwaggerResponse(HttpStatusCode.OK, "Indicates the request succeeded and the items were created and returned"),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid."),
            SwaggerResponse(HttpStatusCode.NotFound, "Indicates that no such rule Uid could be found."),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you do not have access to this endpoint."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred.")
        ]
        public async Task<DataQualityResultModel> Post(Guid ruleUid, DataQualityResultModel ruleResults)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            try
            {
                var rule = Company.Assets.FirstOrDefault(x => x.uid == ruleUid && x.Object == "Rule");
                // run validation of rule id
                if (rule == null) throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, "Rule with the specified uid was not found."));


                // user has created a request with no rule results just return back request nothing to do
                if (ruleResults.Results == null || ruleResults.Results.Count == 0)
                    return ruleResults;

                var resultList = new List<DataQualityResult>();
                for (var i = 0; i < ruleResults.Results.Count; i++)
                {
                    // add results to array of rule results to insert
                    resultList.Add(ruleResults.Results[i].Result);
                }

                int timeout = ruleResults.Timeout.HasValue ? ruleResults.Timeout.Value : 600;

                Company.Database.Connection.Open();

                //start a transaction for the insertion of data
                using (var transaction = (SqlTransaction)Company.Database.Connection.BeginTransaction())
                {
                    // merge the rule results
                    await SaveRuleResults(ruleUid, resultList, timeout, transaction);

                    //select from result id table
                    var resultCount = (await Company.Database.Connection.QueryAsync<int>("select count(*) from #AssetResultUids", transaction: transaction)).FirstOrDefault();

                    if (resultCount != ruleResults.Results.Count)
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Rule Result insert count doesnt match the expected number of items."));


                    transaction.Commit();
                }
            }
            catch(HttpResponseException e)
            {
                throw e;
            }

            catch(Exception e)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e.Message));
            }
            return ruleResults;
        }

        private async Task SaveRuleResults(Guid ruleUid, List<DataQualityResult> resultList, int timeout, SqlTransaction transaction)
        {
            await Company.Database.Connection.ExecuteAsync(@"
			                        drop table if exists #AssetResult;
		                            create table #AssetResult 
                                    (    
			                            EffectiveDate datetime not null, 
                                        PassCount bigint not null,
			                            FailCount bigint not null,
                                        RunDate datetime not null,
                                    );

			                        drop table if exists #AssetResultUids;
		                            create table #AssetResultUids 
                                    (    
			                            [uid] uniqueidentifier
                                    );
                                ", transaction: transaction);

            using (var bulkCopy = new SqlBulkCopy(Company.Database.Connection as SqlConnection, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.BatchSize = resultList.Count;
                bulkCopy.DestinationTableName = "#AssetResult";
                bulkCopy.BulkCopyTimeout = timeout;
                bulkCopy.EnableStreaming = true;

                var table = new DataTable();
                var columnName = "EffectiveDate";
                table.Columns.Add(columnName, typeof(DateTime));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "PassCount";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "FailCount";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "RunDate";
                table.Columns.Add(columnName, typeof(DateTime));
                bulkCopy.ColumnMappings.Add(columnName, columnName);


                foreach (var item in resultList)
                {
                    var row = table.NewRow();

                    row["EffectiveDate"] = item.EffectiveDate;
                    row["PassCount"] = item.PassCount;
                    row["FailCount"] = item.FailCount;
                    row["RunDate"] = item.RunDate;

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }

            //merge into rule results 
            await Company.Database.Connection.ExecuteAsync(@"
                            MERGE
	                        INTO    dbo.AssetResult D
	                        USING   (
			                        select  EffectiveDate,
									        PassCount,
                                            FailCount,
                                            RunDate
								    from    #AssetResult R                                        
			                        ) S
	                        ON      (1 != 1)
	                        WHEN NOT MATCHED THEN
	                            INSERT ([uid], EffectiveDate, PassCount, FailCount, RunDate, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
	                            VALUES (newid(), S.EffectiveDate, S.PassCount, S.FailCount, S.RunDate, getutcdate(), @u, getutcdate(), @u)                      
                            OUTPUT inserted.[uid] INTO #AssetResultUids;

                            insert into dbo.AssetResultEdge ($from_id, $to_id, [Class])
                            select  N.$node_id as from_id, 
                                    R.$node_id as to_id,
                                    @class as [Class]
                            from    #AssetResultUids AR
                                    inner join dbo.AssetResult R on R.[uid] = AR.[uid]
                                    inner join graph.AssetNode N on N.[uid] = @ruleUid
                            
                        ", new { u = Company.CurrentResourceID, ruleUid, @class = ResultRelationClass.Owns }, transaction: transaction, commandTimeout: timeout);
        }
    }
}
