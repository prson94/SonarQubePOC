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
            SwaggerResponse(HttpStatusCode.OK, "A list of data profile results, including any error messages.", typeof(List<MetricDataProfilePostResult>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You do not have permissions to add data profiles.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is malformed or contains no data profiles.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> InsertDataProfile(List<MetricDataProfile> model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Error adding data profile", "You are not allowed to add data profiles."));
            }
            if (model == null || model.Count == 0)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Error adding data profile", "The request is malformed or contains no data profiles."));
            }

            var results = new List<MetricDataProfilePostResult>();
            foreach (var profile in model)
            {
                bool assetExists = (await Company.QueryAsync<int>("select count(1) from asset with (nolock) where uid = @assetUid", new { profile.AssetUid })).First() > 0;

                if (assetExists)
                {
                    if (!profile.EffectiveDate.HasValue)
                    {
                        profile.EffectiveDate = DateTime.UtcNow;
                    }

                    bool profileExists = (await Company.QueryAsync<int>("select count(1) from metrics.dataprofile with (nolock) where assetuid = @assetuid and effectivedate = @effectivedate", new { profile.AssetUid, profile.EffectiveDate })).First() > 0;

                    if (profileExists)
                    {
                        results.Add(new MetricDataProfilePostResult()
                        {
                            Success = false,
                            Message = "A profile for this asset and effective date already exists",
                            AssetUid = profile.AssetUid,
                            EffectiveDate = ((DateTime)profile.EffectiveDate).ToShortDateString()
                        });
                    }
                    else
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(profile.DataType))
                            {
                                results.Add(new MetricDataProfilePostResult()
                                {
                                    Success = false,
                                    Message = "A data type was not provided",
                                    AssetUid = profile.AssetUid,
                                    EffectiveDate = ((DateTime)profile.EffectiveDate).ToShortDateString()
                                });

                                continue;
                            }


                            if (profile.Top10Values == null)
                                profile.Top10Values = new List<string>();

                            profile.Top10ValuesString = JsonConvert.SerializeObject(profile.Top10Values.Take(10).ToList());

                            Company.Add(profile);
                            Company.SaveChanges();
                            results.Add(new MetricDataProfilePostResult()
                            {
                                Success = true,
                                Message = "",
                                AssetUid = profile.AssetUid,
                                EffectiveDate = ((DateTime)profile.EffectiveDate).ToShortDateString()
                            });

                        }
                        catch(Exception ex)
                        {
                            results.Add(new MetricDataProfilePostResult()
                            {
                                Success = false,
                                Message = "An error occurred when inserting the data profile record",
                                AssetUid = profile.AssetUid,
                                EffectiveDate = ((DateTime)profile.EffectiveDate).ToShortDateString()
                            });
                        }
                    }
                }
                else
                {
                    results.Add(new MetricDataProfilePostResult()
                    {
                        Success = false,
                        Message = "The technology asset for the provided Uid was not found",
                        AssetUid = profile.AssetUid,
                        EffectiveDate = ((DateTime)profile.EffectiveDate).ToShortDateString()
                    });
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
            SwaggerResponse(HttpStatusCode.OK, "A list of data profile results, including any error messages.", typeof(List<MetricDataProfileDeleteResult>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You do not have permissions to delete data profiles.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error indicating the request is malformed or contains no data profiles.", typeof(ErrorResponse)),
        ]
        public async Task<IHttpActionResult> DeleteDataProfile(List<MetricDataProfileDelete> model)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Error deleting data profile", "You are not allowed to delete data profiles."));
            }
            if (model == null || model.Count == 0)
            {
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Error deleting data profile", "The request is malformed or contains no data profiles."));
            }

            var results = new List<MetricDataProfileDeleteResult>();

            foreach(var profile in model)
            {

                if (!profile.EffectiveStartDate.HasValue || !profile.EffectiveEndDate.HasValue)
                {
                    results.Add(new MetricDataProfileDeleteResult()
                    {
                        Success = false,
                        Message = "Effective date range was not specified.",
                        EffectiveEndDate = ((DateTime)profile.EffectiveEndDate).ToShortDateString(),
                        EffectiveStartDate = ((DateTime)profile.EffectiveStartDate).ToShortDateString()
                    });

                    continue;
                }

                int recordCount = (await Company.QueryAsync<int>(@"select count(1) from metrics.dataprofile with (nolock) where 
                    assetUid = @AssetUid and effectiveDate between @EffectiveStartDate and @EffectiveEndDate", new { profile.AssetUid, profile.EffectiveStartDate, profile.EffectiveEndDate })).First();

                if (recordCount < 1)
                {
                    results.Add(new MetricDataProfileDeleteResult()
                    {
                        Success = false,
                        Message = "There were no records found for this asset and effective date range.",
                        EffectiveEndDate = ((DateTime)profile.EffectiveEndDate).ToShortDateString(),
                        EffectiveStartDate = ((DateTime)profile.EffectiveStartDate).ToShortDateString()
                    });
                }
                else
                {
                    try
                    {
                        Company.Execute(@"delete from metrics.dataprofile where assetUid = @assetUid and effectivedate between @EffectiveStartDate and @EffectiveEndDate", new { profile.AssetUid, profile.EffectiveStartDate, profile.EffectiveEndDate });

                        results.Add(new MetricDataProfileDeleteResult()
                        {
                            Success = true,
                            Message = "",
                            EffectiveEndDate = ((DateTime)profile.EffectiveEndDate).ToShortDateString(),
                            EffectiveStartDate = ((DateTime)profile.EffectiveStartDate).ToShortDateString()
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new MetricDataProfileDeleteResult()
                        {
                            Success = false,
                            Message = "An unknown error occurred when deleting the data profiles",
                            EffectiveEndDate = ((DateTime)profile.EffectiveEndDate).ToShortDateString(),
                            EffectiveStartDate = ((DateTime)profile.EffectiveStartDate).ToShortDateString()
                        });
                    }
                }
            }

            return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));

        }
    }
}
