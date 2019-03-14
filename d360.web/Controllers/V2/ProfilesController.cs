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
using d360.web.Filters;
using d360.core.entities.Metric;
using d360.web.Models;
using d360.core.entities;
using Newtonsoft.Json;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/profiles"),
        Authorize
    ]

    public class ProfilesController : BaseApiController
    {
        #region DI

        public ProfilesController
(CommunityContext community, CompanyContext company)
            : base(community, company)
        {

        }

        #endregion

        /// <summary>
        /// Adds a set of data profiles based on the given asset Uid and effective date, and returns a list of results.
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
        ]
        public async Task<IHttpActionResult> InsertDataProfile(List<AssetDataProfileViewModel> model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Error adding data profile", "You are not allowed to add data profiles."));
            }
            if (model == null || model.Count == 0)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Error adding data profile", "The request is malformed or contains no data profiles."));
            }

            var results = new List<AssetDataProfileResult>();

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

                    if (profile.Top10Values == null)
                        profile.Top10Values = new List<string>();

                    row["Top10Values"] = JsonConvert.SerializeObject(profile.Top10Values.Take(10).ToList());

                    table.Rows.Add(row);

                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
         

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
                         ProcessIdentifier nvarchar(250)
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
                        bulkCopy.ColumnMappings.Add("Precision", "Precision");
                        bulkCopy.ColumnMappings.Add("Scale", "Scale");
                        bulkCopy.ColumnMappings.Add("Average", "Average");
                        bulkCopy.ColumnMappings.Add("Median", "Median");
                        bulkCopy.ColumnMappings.Add("StandardDeviation", "StandardDeviation");
                        bulkCopy.ColumnMappings.Add("Top10Values", "Top10Values");
                        bulkCopy.ColumnMappings.Add("ProcessIdentifier", "ProcessIdentifier");

                        await bulkCopy.WriteToServerAsync(table);

                        //get asset ids based on uid
                        conn.Execute(@"update p
                        set p.AssetID = a.ID 
from #postAssetDataProfile p
inner join Asset a on a.Uid = p.AssetUid", transaction: trans);

                        //get profile id based on assetid
                        conn.Execute(@"update p
                        set p.ID = e.ID 
from #postAssetDataProfile p
inner join AssetDataProfile e on e.AssetId = p.AssetId", transaction: trans);

                        var invalidAssetId = (await conn.QueryAsync<dynamic>(@"select * from #postAssetDataProfile where assetId = 0", transaction: trans)).ToList();
                        var validAssetId = (await conn.QueryAsync<dynamic>(@"select * from #postAssetDataProfile where assetId <> 0", transaction: trans)).ToList();
                        //add errors for records with invalid asset uids
                        if (invalidAssetId.Any())
                        {
                            results.AddRange(invalidAssetId.Select(a =>
                            new AssetDataProfileResult()
                            {
                                AssetUid = a.AssetUid,
                                Message = "Asset with this uid not found",
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
		T.ProcessIdentifier = S.ProcessIdentifier
when not matched then
insert (AssetId, [RowCount], Uniqueness, Completeness, NullCount, BlankCount, DataType, MinimumValue, MaximumValue, [Precision], Scale, Average, Median,
StandardDeviation, Top10Values, ProcessIdentifier) values
(S.AssetId, S.[RowCount], S.Uniqueness, S.Completeness, S.NullCount, S.BlankCount, S.DataType, S.MinimumValue, S.MaximumValue, S.[Precision], S.Scale,
S.Average, S.Median, S.StandardDeviation, S.Top10Values, S.ProcessIdentifier);", transaction: trans);

                        trans.Commit();
                    }
                    catch(Exception ex)
                    {
                        trans.Rollback();
                        throw ex;
                    }

                }

            }
           

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));

        }

        /// <summary>
        /// Deletes a set of data profiles based on the given asset Uid and effective date range, and returns a list of results.
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
        ]
        public async Task<IHttpActionResult> DeleteDataProfile(List<AssetDataProfileDelete> model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Error deleting data profile", "You are not allowed to delete data profiles."));
            }
            if (model == null || model.Count == 0)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Error deleting data profile", "The request is malformed or contains no data profiles."));
            }

            var results = new List<AssetDataProfileDeleteResult>();

            //foreach(var profile in model)
            //{

            //    if (!profile.EffectiveStartDate.HasValue || !profile.EffectiveEndDate.HasValue)
            //    {
            //        results.Add(new AssetDataProfileDeleteResult()
            //        {
            //            Success = false,
            //            Message = "Effective date range was not specified.",
            //            EffectiveEndDate = ((DateTime)profile.EffectiveEndDate).ToShortDateString(),
            //            EffectiveStartDate = ((DateTime)profile.EffectiveStartDate).ToShortDateString()
            //        });

            //        continue;
            //    }

            //    int recordCount = (await Company.QueryAsync<int>(@"select count(1) from metrics.dataprofile with (nolock) where 
            //        assetUid = @AssetUid and effectiveDate between @EffectiveStartDate and @EffectiveEndDate", new { profile.AssetUid, profile.EffectiveStartDate, profile.EffectiveEndDate })).First();

            //    if (recordCount < 1)
            //    {
            //        results.Add(new AssetDataProfileDeleteResult()
            //        {
            //            Success = false,
            //            Message = "There were no records found for this asset and effective date range.",
            //            EffectiveEndDate = ((DateTime)profile.EffectiveEndDate).ToShortDateString(),
            //            EffectiveStartDate = ((DateTime)profile.EffectiveStartDate).ToShortDateString()
            //        });
            //    }
            //    else
            //    {
            //        try
            //        {
            //            Company.Execute(@"delete from metrics.dataprofile where assetUid = @assetUid and effectivedate between @EffectiveStartDate and @EffectiveEndDate", new { profile.AssetUid, profile.EffectiveStartDate, profile.EffectiveEndDate });

            //            results.Add(new AssetDataProfileDeleteResult()
            //            {
            //                Success = true,
            //                Message = "",
            //                EffectiveEndDate = ((DateTime)profile.EffectiveEndDate).ToShortDateString(),
            //                EffectiveStartDate = ((DateTime)profile.EffectiveStartDate).ToShortDateString()
            //            });
            //        }
            //        catch (Exception ex)
            //        {
            //            results.Add(new AssetDataProfileDeleteResult()
            //            {
            //                Success = false,
            //                Message = "An unknown error occurred when deleting the data profiles",
            //                EffectiveEndDate = ((DateTime)profile.EffectiveEndDate).ToShortDateString(),
            //                EffectiveStartDate = ((DateTime)profile.EffectiveStartDate).ToShortDateString()
            //            });
            //        }
            //    }
            //}

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));

        }
    }
}
