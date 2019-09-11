using d360.extensions;
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
using System.Data.Entity;
using Swashbuckle.Swagger.Annotations;
using System.Web.Http.Description;
using d360.web.Filters;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/dataquality"), 
        Authorize        
    ]

    public class DataQualityController : BaseV2ApiController
    {
        #region DI
                
        public DataQualityController
(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {
        
        }

        #endregion

        private static string DefaultImplementationName = "default";

        /// <summary>
        /// Returns all the rule results for the specified rule Uid that are under the default implementation
        /// If the user is not an admin http status code 403 forbidden is returned
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.")
        ]
        public IQueryable<core.entities.RuleResult> GetRuleResults(Guid ruleUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            var rule = Company.Assets.FirstOrDefault(x => x.uid == ruleUid);

            if (rule == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"No such rule with id {ruleUid}"));

            //get the default implemenation
            var impl = Company.RuleImplementations.FirstOrDefault(x => x.RuleID == rule.ObjectID && x.Name == DefaultImplementationName);

            if (impl == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"No default rule implementation for rule {ruleUid}"));

            return Company.RuleResults.Where(x => x.RuleImplementationID == impl.ID);
        }

        /// <summary>
        /// Returns the rule result with the specified rule result id
        /// If the user is not an admin http status code 403 forbidden is returned
        /// </summary>
        /// <param name="ruleResultId">The id of the rule result</param>
        /// <returns>The rule result object with the specified id.  If no such rule result exists http status code 404 not found is returned.</returns>
        [
            HttpGet, 
            MapToApiVersion("2.0"), 
            Route("ruleresult/{ruleResultId:int}"),
            SwaggerResponse(HttpStatusCode.OK, "Indicates the request succeeded"),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid."),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that no such rule result was found."),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you do not have access to this endpoint."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.")
        ]
        public core.entities.RuleResult GetRuleResult(int ruleResultId)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            var result =  Company.RuleResults.FirstOrDefault(x => x.ID == ruleResultId);

            if(result == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"No such rule result with id {ruleResultId}"));

            return result;
        }

        /// <summary>
        /// Returns the asset information of fusion tied to a rule result with the specified rule result id
        /// If the user is not an admin http status code 403 forbidden is returned
        /// </summary>
        /// <param name="ruleResultId">The id of the rule result</param>
        /// <returns>The asset information tied to the rule result object with the specified id.  If no such rule result exists http status code 404 not found is returned.</returns>
        [
            HttpGet, 
            MapToApiVersion("2.0"), 
            Route("ruleresult/{ruleResultId:int}/fusionattributes"),
            SwaggerResponse(HttpStatusCode.OK, "Indicates the request succeeded"),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid."),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that no such rule result was found."),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you do not have access to this endpoint."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.")
        ]
        public IEnumerable<core.entities.Asset> GetRuleResultFusionAttributes(int ruleResultId)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            var result = Company.RuleResults.FirstOrDefault(x => x.ID == ruleResultId);

            if (result == null)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"No such rule result with id {ruleResultId}"));

            var fus = Company.RuleResultFusionAttributes.Where(x => x.RuleResultID == ruleResultId).Select(x => x.FusionAttributeID).ToList();

            if (fus == null)
                return new List<core.entities.Asset>();

            return Company.Assets.Where(x => x.Object == "FusionAttribute" && fus.Contains(x.ObjectID)).Include(x=>x.Fields);
        }

        /// <summary>
        /// Deletes a rule result with the specified Rule Result ID.  It also deletes any ruleresultfusion records for the same rule result.
        /// </summary>
        /// <param name="ruleResultId">The id of the rule result</param>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted</returns>
        [
            HttpDelete, 
            MapToApiVersion("2.0"), 
            Route("{ruleResultId:int}"),
            SwaggerResponse(HttpStatusCode.OK, "Indicates the request succeeded"),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid."),
            SwaggerResponse(HttpStatusCode.NotFound, "Indicates that no such rule result could be deleted."),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you do not have access to this endpoint."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.")
        ]
        public async Task<HttpResponseMessage> DeleteByRuleResultID(int ruleResultId)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            
            //deletes the rule result fusion attribute records
            await Company.Database.Connection.ExecuteAsync("delete ruleresultfusionattribute where ruleresultid = @ruleResultId", new { ruleResultId });

            //deletes the rule result record
            var res = await Company.Database.Connection.ExecuteAsync("delete ruleresult where id = @ruleResultId", new { ruleResultId });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
        }

        /// <summary>
        /// Deletes all rule result fusion attributes for the specified asset Uid.
        /// </summary>
        /// <param name="assetUid">The Uid of the asset</param>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted</returns>
        [
            HttpDelete, 
            MapToApiVersion("2.0"), 
            Route("fusionattribute/{assetUid}"),
            SwaggerResponse(HttpStatusCode.OK, "Indicates the request succeeded"),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid."),
            SwaggerResponse(HttpStatusCode.NotFound, "Indicates that no such rule result for the specified asset Uid could be deleted."),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you do not have access to this endpoint."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.")
        ]
        public async Task<HttpResponseMessage> DeleteFusionAttributeByAssetUID(Guid assetUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            //deletes the rule result fusion attribute records
            var res = await Company.Database.Connection.ExecuteAsync("delete from ruleresultfusionattribute where fusionattributeid in (select a.objectid from asset a where a.[uid] = @uid)", new { uid = assetUid });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
        }

        /// <summary>
        /// Deletes specified rule results fusion attributes for the specified asset Uid.
        /// </summary>
        /// <param name="ruleResultId">The id of the rule result</param>
        /// <param name="assetUid">The Uid of the asset</param>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted</returns>
        [
            HttpDelete, 
            MapToApiVersion("2.0"), 
            Route("fusionattribute/{ruleResultId:int}/{assetUid}"),
            SwaggerResponse(HttpStatusCode.OK, "Indicates the request succeeded"),
            SwaggerResponse(HttpStatusCode.BadRequest, "Indicates the request was invalid."),
            SwaggerResponse(HttpStatusCode.NotFound, "Indicates that no such rule result for the specified rule result id /asset Uid could be deleted."),
            SwaggerResponse(HttpStatusCode.Forbidden, "An error to indicate that you do not have access to this endpoint."),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.")
        ]
        public async Task<HttpResponseMessage> DeleteFusionAttributeByRuleResultAndAssetUID(int ruleResultId, Guid assetUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            //deletes the rule result fusion attribute records
            var res = await Company.Database.Connection.ExecuteAsync("delete from ruleresultfusionattribute where  ruleresultid = @ruleResultId and fusionattributeid in (select a.objectid from asset a where a.[uid] = @uid)", new { uid = assetUid, ruleResultId });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured.")
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

                // look for the implementations for the rule
                var ruleImpl = Company.RuleImplementations.FirstOrDefault(x => x.Name == DefaultImplementationName && x.RuleID == rule.ObjectID);

                if (ruleImpl == null)
                {
                    //create the implementation and use this.
                    ruleImpl = new core.entities.RuleImplementation
                    {
                        Name = DefaultImplementationName,
                        RuleID = rule.ObjectID
                    };

                    Company.RuleImplementations.Add(ruleImpl);

                    Company.SaveChanges();
                }

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

                var mustOpen = Company.Database.Connection.State != ConnectionState.Open;
                Company.Database.Connection.Open();

                //start a transaction for the insertion of data
                using (var transaction = (SqlTransaction)Company.Database.Connection.BeginTransaction())
                {
                    // 3. insert the rule results into ruleresult table
                    // merge the rule results
                    await SaveRuleResults(resultList, timeout, transaction, ruleImpl.ID);

                    //select from result id table
                    var ruleResultIds = (await Company.Database.Connection.QueryAsync("select RowIndex, RuleResultID from #RuleResultIdentifiers", transaction: transaction)).ToDictionary(
                                            row => (int)row.RowIndex,
                                            row => (int)row.RuleResultID);

                    if (ruleResultIds.Count != ruleResults.Results.Count)
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Rule Result insert count doesnt match the expected number of items."));

                    PopulateRuleResultIDs(ruleResults.Results, ruleResultIds);

                    // build a structure of the asset ids and the rule result ids that we need to store mappings for
                    List<DataQualityRuleAssetMapping> AssetIDRuleMapping = GenerateAssetIDRuleMapping(ruleResults.Results);

                    // store ruleresultfusionattribute mappings
                    if (AssetIDRuleMapping.Count > 0)
                        await StoreRuleResultFusionMappings(AssetIDRuleMapping, timeout, transaction);

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
               
        private async Task SaveRuleResults(List<DataQualityResult> resultList, int timeout, SqlTransaction transaction, int implementationId)
        {
            await Company.Database.Connection.ExecuteAsync(@"
                                    IF OBJECT_ID('tempdb..#RuleResult') IS NOT NULL
			                            DROP TABLE #RuleResult;

		                            create table #RuleResult (    
                                            ID int identity not null,
			                                EffectiveDate datetime not null, 
                                            RowsPassed int not null,
			                                RowsFailed int not null,
                                            RunDate datetime not null,
                                            RuleImplementationID int not null
                                    );

                                    IF OBJECT_ID('tempdb..#RuleResultIdentifiers') IS NOT NULL
			                                DROP TABLE #RuleResultIdentifiers;

		                                create table #RuleResultIdentifiers (    
                                            RowIndex int not null,
			                                RuleResultID int not null
		                                );
                                ", transaction: transaction);

            using (var bulkCopy = new SqlBulkCopy(Company.Database.Connection as SqlConnection, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.BatchSize = resultList.Count;
                bulkCopy.DestinationTableName = "#RuleResult";
                bulkCopy.BulkCopyTimeout = timeout;
                bulkCopy.EnableStreaming = true;

                var table = new DataTable();
                var columnName = "EffectiveDate";
                table.Columns.Add(columnName, typeof(DateTime));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "RowsPassed";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "RowsFailed";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "RunDate";
                table.Columns.Add(columnName, typeof(DateTime));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "RuleImplementationID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                foreach (var item in resultList)
                {
                    var row = table.NewRow();

                    row["EffectiveDate"] = item.EffectiveDate;
                    row["RowsPassed"] = item.PassCount;
                    row["RowsFailed"] = item.FailCount;
                    row["RunDate"] = item.RunDate;
                    row["RuleImplementationID"] = implementationId;

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }

            //merge into rule results 
            await Company.Database.Connection.ExecuteAsync(@"
                            MERGE
	                            INTO    RuleResult d
	                            USING   (
			                            select
									        EffectiveDate,
									        RowsPassed,
                                            RowsFailed,
                                            RunDate,
                                            RuleImplementationID,
                                            ID
								        from
									        #RuleResult r                                        
			                            ) S
	                            ON      (1 != 1)
	                            WHEN NOT MATCHED THEN
	                                INSERT  (EffectiveDate, RowsPassed, RowsFailed, RunDate, RuleImplementationID, CreatedOn, CreatedBy)
	                                VALUES  (S.EffectiveDate, S.RowsPassed, S.RowsFailed, S.RunDate, S.RuleImplementationID, getutcdate(), @u)                      
                                OUTPUT S.ID, inserted.Id INTO #RuleResultIdentifiers;
                        ", new { u = Company.CurrentResourceID }, transaction: transaction, commandTimeout: timeout);
        }


        private async Task StoreRuleResultFusionMappings(List<DataQualityRuleAssetMapping> assetIDRuleMapping, int timeout, SqlTransaction transaction)
        {
            await Company.Database.Connection.ExecuteAsync(@"IF OBJECT_ID('tempdb..#AssetUIRuleResultMap') IS NOT NULL
			                                DROP TABLE #AssetUIRuleResultMap;

		                                create table #AssetUIRuleResultMap (    
                                            ID int identity not null,
			                                AssetUID uniqueidentifier not null, 
                                            RuleResultID int not null
                                        );
                                ", transaction: transaction);

            using (var bulkCopy = new SqlBulkCopy(Company.Database.Connection as SqlConnection, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.BatchSize = assetIDRuleMapping.Count;
                bulkCopy.DestinationTableName = "#AssetUIRuleResultMap";
                bulkCopy.BulkCopyTimeout = timeout;
                bulkCopy.EnableStreaming = true;

                var table = new DataTable();
                var columnName = "AssetUID";
                table.Columns.Add(columnName, typeof(Guid));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                columnName = "RuleResultID";
                table.Columns.Add(columnName, typeof(int));
                bulkCopy.ColumnMappings.Add(columnName, columnName);

                foreach (var item in assetIDRuleMapping)
                {
                    var row = table.NewRow();

                    row["RuleResultID"] = item.RuleID;
                    row["AssetUID"] = item.AssetUID;

                    table.Rows.Add(row);
                }

                await bulkCopy.WriteToServerAsync(table);
            }

            //check for invalid AssetUIDs
            var invalidAssetUIDs = await Company.Database.Connection.QueryAsync<Guid>(@"select map.AssetUID from #AssetUIRuleResultMap map where
                                                not exists (select 1 from Asset a inner join RuleResult rr on (rr.ID = map.RuleResultID) where a.uid = map.AssetUID and a.[Object] = 'FusionAttribute')
                                          ", transaction:transaction);

            if (invalidAssetUIDs.Any())
            {
                var invalid = string.Join(",", invalidAssetUIDs.ToArray());

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid asset uid mappings: [{invalid}]"));
            }

            //merge into ruleresultfusion table
            await Company.Database.Connection.ExecuteAsync(@"
                            MERGE
	                            INTO    RuleResultFusionAttribute d
	                            USING   (
			                            select
									        map.RuleResultID,
                                            a.ObjectID as FusionAttributeID
								        from
									        #AssetUIRuleResultMap map
                                            inner join Asset a on (map.AssetUID = a.uid and a.[Object] = 'FusionAttribute')
                                            inner join RuleResult rr on (rr.ID = map.RuleResultID)
			                            ) S
	                            ON      (1 != 1)
	                            WHEN NOT MATCHED THEN
	                                INSERT  (RuleResultID, FusionAttribute, FusionAttributeID)
	                                VALUES  (S.RuleResultID, '', S.FusionAttributeID);                                                       
                        ", transaction: transaction, commandTimeout: timeout);
        }

        private List<DataQualityRuleAssetMapping> GenerateAssetIDRuleMapping(List<DataQualityResultItem> results)
        {
            List<DataQualityRuleAssetMapping> map = new List<DataQualityRuleAssetMapping>();

            foreach (var result in results)
            {
                foreach (var mapping in result.AssetsMappings)
                {
                    if(mapping.AssetUID.HasValue)
                        map.Add(new DataQualityRuleAssetMapping { RuleID = result.Result.ID, AssetUID = mapping.AssetUID.Value });
                    else
                        throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"Invalid mapping specified no AssetUID."));
                }                
            }
            return map;
        }

        private void PopulateRuleResultIDs(List<DataQualityResultItem>  results, Dictionary<int, int> mappings)
        {
            // populate the rule result ids
            for (var i = 0; i < results.Count; i++)
            {
                if (!mappings.TryGetValue(i +1, out int ruleResultId))
                    throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"Rule Result insert map doesnt contain the expected item. Index {i}"));

                results[i].Result.ID = ruleResultId;
            }
        }

        public class DataQualityResultModel
        {            
            public List<DataQualityResultItem> Results{ get; set; }

            public int? Timeout { get; set; }
        }

        public class DataQualityResultItem
        {
            public DataQualityResult Result { get; set; }
            public List<DataQualityAssetMapping> AssetsMappings { get; set; }
        }
}


    public class DataQualityResult
    {
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime RunDate { get; set; }        
        public int ID { get; set; }
    }

    public class DataQualityAssetMapping
    {
        public string AssetPath { get; set; }
        public Guid? AssetUID { get; set; }
    }

    public class DataQualityRuleAssetMapping
    {
        public Guid AssetUID { get; set; }
        public int RuleID { get; set; }
    }
}
