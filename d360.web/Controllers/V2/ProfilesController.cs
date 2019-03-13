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
        /// 
        /// </summary>
        /// <param name="model"></param>
        /// <returns>An HTTP status code with an appropriate status message.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Created, "", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(ConfirmResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.BadRequest, "", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> InsertDataProfile(List<MetricDataProfile> model)
        {
            foreach(var profile in model)
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
                        //error message already exists
                    }
                    else
                    {
                        Company.Add(profile);
                        Company.SaveChanges();
                    }
                }
                else
                {
                    //no asset error message
                }

            }
            //if (!Company.CurrentResourceIsAdmin)
            //{
            //    return errorMessageResponse(HttpStatusCode.Unauthorized, "Error updating metric", "You are not allowed to update this metric.");
            //}

            //if (model == null)
            //{
            //    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "You are have provided a null metric.");
            //}

            //if (string.IsNullOrEmpty(model.Name))
            //{
            //    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "You are have provided an invalid name.");
            //}

            //if (model.Weight == 0)
            //{
            //    return errorMessageResponse(HttpStatusCode.BadRequest, "Error updating metric", "You must supply a weight greater than 0.");
            //}
            return null;

        }
    }
}
