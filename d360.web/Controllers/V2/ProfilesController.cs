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
using d360.web.Filters;
using d360.web.Models;
using d360.core.entities;
using Newtonsoft.Json;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using d360.core;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/profiles"),
        Authorize
    ]

    public class ProfilesController : BaseApiController
    {
        private const int TechAssetFusionAttributeTypeId = 1820;

        #region DI

        public ProfilesController
(CommunityContext community, CompanyContext company)
            : base(community, company)
        {

        }

        #endregion

        /// <summary>
        /// Adds a set of data profiles based on the given asset Uid, and returns a list of results. If a profile for this asset Uid already exists, it will be overwritten
        /// </summary>
        /// <param name="model">A list of metric data profiles</param>
        /// <returns>An HTTP status code with an appropriate status message.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of data profile results, including any error messages.", typeof(List<AssetDataProfileResult>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You do not have permissions to add data profiles.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is malformed or contains no data profiles.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> InsertDataProfile(List<AssetDataProfileViewModel> model)
        {

            var prefix = "Profiles.InsertDataProfile => ";

            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Error adding data profile", "You are not allowed to add data profiles."));
            }
            if (model == null || model.Count == 0)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Error adding data profile", "The request is malformed or contains no data profiles."));
            }

            var results = new List<AssetDataProfileResult>();
            var execution = getApiExecution(model.Count);

            Company.Add(execution);

            #region Build DataTable

            //load the data into a table
            var table = new DataTable();

            try
            {
                table.Columns.Add("Id", typeof(int));
                table.Columns.Add("AssetUid", typeof(Guid));
                table.Columns.Add("AssetId", typeof(long));
                table.Columns.Add("RowCount", typeof(int));
                table.Columns.Add("Uniqueness", typeof(decimal));
                table.Columns.Add("UniqueCount", typeof(int));
                table.Columns.Add("Completeness", typeof(decimal));
                table.Columns.Add("NullCount", typeof(int));
                table.Columns.Add("BlankCount", typeof(int));
                table.Columns.Add("DataType", typeof(string));
                table.Columns.Add("MinimumValue", typeof(string));
                table.Columns.Add("MaximumValue", typeof(string));
                table.Columns.Add("Precision", typeof(int));
                table.Columns.Add("Scale", typeof(int));
                table.Columns.Add("Average", typeof(decimal));
                table.Columns.Add("Median", typeof(decimal));
                table.Columns.Add("StandardDeviation", typeof(decimal));
                table.Columns.Add("Top10Values", typeof(string));
                table.Columns.Add("ProcessIdentifier", typeof(string));
                table.Columns.Add("CreatedBy", typeof(int));
                table.Columns.Add("CreatedOn", typeof(DateTime));

                foreach (DataColumn col in table.Columns)
                {
                    col.AllowDBNull = true;
                }

                foreach (var profile in model)
                {
                    var row = table.NewRow();

                    row["Id"] = 0;
                    row["AssetUid"] = profile.AssetUid;
                    row["AssetId"] = 0;
                    row["RowCount"] = profile.RowCount;
                    row["Uniqueness"] = profile.Uniqueness;
                    row["UniqueCount"] = profile.UniqueCount;
                    row["Completeness"] = profile.Completeness;
                    row["NullCount"] = profile.NullCount;
                    row["BlankCount"] = profile.BlankCount;
                    row["DataType"] = profile.DataType;
                    row["MinimumValue"] = profile.MinimumValue;
                    row["MaximumValue"] = profile.MaximumValue;
                    row["Precision"] = (object)profile.Precision ?? DBNull.Value;
                    row["Scale"] = (object)profile.Scale ?? DBNull.Value;
                    row["Average"] = (object)profile.Average ?? DBNull.Value;
                    row["Median"] = (object)profile.Median ?? DBNull.Value;
                    row["StandardDeviation"] = (object)profile.StandardDeviation ?? DBNull.Value;
                    row["ProcessIdentifier"] = profile.ProcessIdentifier;
                    row["CreatedBy"] = Company.CurrentResourceID;
                    row["CreatedOn"] = DateTime.UtcNow;


                    if (profile.Top10Values == null)
                        profile.Top10Values = new List<AssetDataProfileValueCount>();

                    row["Top10Values"] = JsonConvert.SerializeObject(profile.Top10Values.Take(10).ToList());

                    table.Rows.Add(row);

                }
            }
            catch (Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetDataProfileCount", $"{((model != null) ? model.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Error adding data profile", "An unknown error occurred while processing this request."));

            }

            #endregion

            #region Merge Profiles

            using (SqlConnection conn = new SqlConnection(Company.CompanyConnectionString))
            {
                if (conn.State != ConnectionState.Open)
                    conn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        conn.Execute(@"
                        create table #postAssetDataProfile
                        (
                         ID int,
                         AssetID bigint,
                         AssetUid uniqueidentifier,
                         [RowCount] int,
                         Uniqueness decimal(22,4),
                         UniqueCount int,
                         Completeness decimal(22,4),
                         NullCount int,
                         BlankCount int,
                         DataType nvarchar(250),
                         MinimumValue nvarchar(500),
                         MaximumValue nvarchar(500),
                         [Precision] int,
                         Scale int,
                         Average decimal(22,4),
                         Median decimal(22,4),
                         StandardDeviation decimal(28,4),
                         Top10Values nvarchar(max),
                         ProcessIdentifier nvarchar(250),
                         CreatedBy int,
                         CreatedOn datetime
                        )
                        ", transaction: trans);

                        SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepNulls, trans);
                        bulkCopy.BatchSize = table.Rows.Count;
                        bulkCopy.DestinationTableName = "#postAssetDataProfile";
                        bulkCopy.BulkCopyTimeout = 3600;

                        bulkCopy.ColumnMappings.Add("ID", "ID");
                        bulkCopy.ColumnMappings.Add("AssetID", "AssetID");
                        bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");
                        bulkCopy.ColumnMappings.Add("RowCount", "RowCount");
                        bulkCopy.ColumnMappings.Add("Uniqueness", "Uniqueness");
                        bulkCopy.ColumnMappings.Add("UniqueCount", "UniqueCount");
                        bulkCopy.ColumnMappings.Add("Completeness", "Completeness");
                        bulkCopy.ColumnMappings.Add("NullCount", "NullCount");
                        bulkCopy.ColumnMappings.Add("BlankCount", "BlankCount");
                        bulkCopy.ColumnMappings.Add("DataType", "DataType");
                        bulkCopy.ColumnMappings.Add("MinimumValue", "MinimumValue");
                        bulkCopy.ColumnMappings.Add("MaximumValue", "MaximumValue");
                        bulkCopy.ColumnMappings.Add("Precision", "Precision");
                        bulkCopy.ColumnMappings.Add("Scale", "Scale");
                        bulkCopy.ColumnMappings.Add("Average", "Average");
                        bulkCopy.ColumnMappings.Add("Median", "Median");
                        bulkCopy.ColumnMappings.Add("StandardDeviation", "StandardDeviation");
                        bulkCopy.ColumnMappings.Add("Top10Values", "Top10Values");
                        bulkCopy.ColumnMappings.Add("ProcessIdentifier", "ProcessIdentifier");
                        bulkCopy.ColumnMappings.Add("CreatedBy", "CreatedBy");
                        bulkCopy.ColumnMappings.Add("CreatedOn", "CreatedOn");


                        await bulkCopy.WriteToServerAsync(table);

                        //get asset ids based on uid
                        conn.Execute($@"update p
                            set p.AssetID = a.ID 
                            from #postAssetDataProfile p
                            inner join Asset a on a.Uid = p.AssetUid
                            inner join AssetType t on t.id = a.assetTypeId and t.Object = 'FusionAttributeType' and t.ObjectID = {TechAssetFusionAttributeTypeId}"
                        , transaction: trans);

                        //get profile id based on assetid
                        conn.Execute(@"update p
                            set p.ID = e.ID 
                            from #postAssetDataProfile p
                            inner join AssetDataProfile e on e.AssetId = p.AssetId", transaction: trans);

                        var missingAssetId = (await conn.QueryAsync<dynamic>(@"select * from #postAssetDataProfile where assetId = 0", transaction: trans)).ToList();
                        var validAssetId = (await conn.QueryAsync<dynamic>(@"select * from #postAssetDataProfile where assetId <> 0", transaction: trans)).ToList();

                        //add errors for records with invalid asset uids
                        if (missingAssetId.Any())
                        {
                            results.AddRange(missingAssetId.Select(a =>
                            new AssetDataProfileResult()
                            {
                                AssetUid = a.AssetUid,
                                Message = "A Technology Asset with this uid was not found",
                                Success = false
                            }));
                        }

                        if (validAssetId.Any())
                        {
                            results.AddRange(validAssetId.Select(a =>
                            new AssetDataProfileResult()
                            {
                                AssetUid = a.AssetUid,
                                Message = "",
                                Success = true
                            }));
                        }

                        conn.Execute(@"merge	AssetDataProfile as T
                            using	(
			                            select	*
			                            from	#postAssetDataProfile
			                            where AssetId <> 0
		                            ) S
                            on		(T.AssetId = S.AssetId)
                            when matched then
	                            update set
		                            T.[RowCount] = S.[RowCount],
		                            T.Uniqueness = S.Uniqueness,
                                    T.UniqueCount = S.UniqueCount,
		                            T.Completeness = S.Completeness,
		                            T.NullCount = S.NullCount,
		                            T.BlankCount = S.BlankCount,
		                            T.DataType = S.DataType,
		                            T.MinimumValue = S.MinimumValue,
		                            T.MaximumValue = S.MaximumValue,
		                            T.[Precision] = S.[Precision],
		                            T.Scale = S.Scale,
		                            T.Average = S.Average,
		                            T.Median = S.Median,
		                            T.StandardDeviation = S.StandardDeviation,
		                            T.Top10Values = S.Top10Values,
		                            T.ProcessIdentifier = S.ProcessIdentifier,
                                    T.CreatedBy = S.CreatedBy,
                                    T.CreatedOn = S.CreatedOn
                            when not matched then
                            insert (AssetId, [RowCount], Uniqueness, UniqueCount, Completeness, NullCount, BlankCount, DataType, MinimumValue, MaximumValue, [Precision], Scale, Average, Median,
                            StandardDeviation, Top10Values, ProcessIdentifier, CreatedBy, CreatedOn) values
                            (S.AssetId, S.[RowCount], S.Uniqueness, S.UniqueCount, S.Completeness, S.NullCount, S.BlankCount, S.DataType, S.MinimumValue, S.MaximumValue, S.[Precision], S.Scale,
                            S.Average, S.Median, S.StandardDeviation, S.Top10Values, S.ProcessIdentifier, S.CreatedBy, S.CreatedOn);", transaction: trans);

                        trans.Commit();

                        execution.CompletedOn = DateTime.UtcNow;
                        execution.Error = results.Count(r => r.Success == false);
                        Company.Update(execution);

                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        execution.ErrorMessage = ex.GetFullExceptionData(false);
                        execution.CompletedOn = DateTime.UtcNow;
                        Company.Update(execution);

                        SendException(ex, new Dictionary<string, string>() {
                            { "Endpoint Method", prefix },
                            { "AssetDataProfileCount", $"{((model != null) ? model.Count : 0)}" }
                        });

                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Error adding data profile", "An unknown error occurred while processing this request."));

                    }

                }

            }

            #endregion

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));

        }

        /// <summary>
        /// Deletes a set of data profiles based on the given asset Uids, and returns a list of results.
        /// </summary>
        /// <param name="model">A list of metric data profiles</param>
        /// <returns>An HTTP status code with an appropriate status message.</returns>
        [
            HttpDelete,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of data profile results, including any error messages.", typeof(List<AssetDataProfileDeleteResult>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You do not have permissions to delete data profiles.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is malformed or contains no data profiles.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> DeleteDataProfile(List<AssetDataProfileDelete> model)
        {

            var prefix = "Profiles.DeleteDataProfile => ";


            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Error deleting data profile", "You are not allowed to delete data profiles."));
            }
            if (model == null || model.Count == 0)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Error deleting data profile", "The request is malformed or contains no data profiles."));
            }

            var results = new List<AssetDataProfileDeleteResult>();
            var execution = getApiExecution(model.Count);
            Company.Add(execution);

            var table = new DataTable();


            #region Build DataTable

            try
            {
                table.Columns.Add("AssetUid", typeof(Guid));
                table.Columns.Add("AssetId", typeof(long));


                foreach (var profile in model)
                {
                    if (profile.AssetUid == null || string.IsNullOrEmpty(profile.AssetUid.ToString()))
                    {
                        results.Add(new AssetDataProfileDeleteResult()
                        {
                            Message = "AssetUid provided is not in a valid format",
                            Success = false,
                            AssetUid = profile.AssetUid
                        });
                        continue;
                    }

                    var row = table.NewRow();
                    row["AssetUid"] = profile.AssetUid;
                    row["AssetId"] = 0;

                    table.Rows.Add(row);
                }
            }
            catch(Exception ex)
            {
                execution.ErrorMessage = ex.GetFullExceptionData(false);
                execution.CompletedOn = DateTime.UtcNow;
                Company.Update(execution);

                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetDataProfileCount", $"{((model != null) ? model.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Error deleting data profile", "An unknown error occurred while processing this request."));

            }

            #endregion

            #region Delete Profiles

            using (SqlConnection conn = new SqlConnection(Company.CompanyConnectionString))
            {
                if (conn.State != ConnectionState.Open)
                    conn.OpenWithRetry(RetryPolicy.DefaultProgressive);

                using (SqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        conn.Execute(@"
                        create table #deleteAssetDataProfile
                        (
                         AssetID bigint,
                         AssetUid uniqueidentifier,
                        )
                        ", transaction: trans);

                        SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepNulls, trans);
                        bulkCopy.BatchSize = table.Rows.Count;
                        bulkCopy.DestinationTableName = "#deleteAssetDataProfile";
                        bulkCopy.BulkCopyTimeout = 3600;

                        bulkCopy.ColumnMappings.Add("AssetID", "AssetID");
                        bulkCopy.ColumnMappings.Add("AssetUid", "AssetUid");

                        await bulkCopy.WriteToServerAsync(table);

                        //get asset ids based on uid
                        conn.Execute(@"update p
                            set p.AssetID = a.ID 
                            from #deleteAssetDataProfile p
                            inner join Asset a on a.Uid = p.AssetUid
                            inner join AssetDataProfile ap on ap.AssetId = a.Id", transaction: trans);


                        var missingAssetId = (await conn.QueryAsync<dynamic>(@"select * from #deleteAssetDataProfile where assetId = 0", transaction: trans)).ToList();
                        var validAssetId = (await conn.QueryAsync<dynamic>(@"select * from #deleteAssetDataProfile where assetId <> 0", transaction: trans)).ToList();

                        //add errors for records with invalid asset uids
                        if (missingAssetId.Any())
                        {
                            results.AddRange(missingAssetId.Select(a =>
                            new AssetDataProfileDeleteResult()
                            {
                                AssetUid = a.AssetUid,
                                Message = "Asset Profile with this uid not found",
                                Success = false
                            }));
                        }

                        if (validAssetId.Any())
                        {
                            results.AddRange(validAssetId.Select(a =>
                            new AssetDataProfileDeleteResult()
                            {
                                AssetUid = a.AssetUid,
                                Message = "",
                                Success = true
                            }));
                        }


                        //perform the delete
                        conn.Execute(@"delete from AssetDataProfile where AssetId in (select d.AssetId from #deleteAssetDataProfile d where d.AssetId <> 0)", transaction: trans);

                        trans.Commit();

                        execution.CompletedOn = DateTime.UtcNow;
                        execution.Error = results.Count(r => r.Success == false);
                        execution.Processed = results.Count(r => r.Success == true);
                        Company.Update(execution);

                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        execution.ErrorMessage = ex.GetFullExceptionData(false);
                        execution.CompletedOn = DateTime.UtcNow;
                        Company.Update(execution);

                        SendException(ex, new Dictionary<string, string>() {
                            { "Endpoint Method", prefix },
                            { "AssetDataProfileCount", $"{((model != null) ? model.Count : 0)}" }
                        });

                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Error deleting data profile", "An unknown error occurred while processing this request."));

                    }
                }
            }

            #endregion

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));

        }
    }
}
