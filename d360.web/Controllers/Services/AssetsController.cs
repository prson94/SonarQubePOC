using d360.core.entities;
using d360.model;
using d360.core;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using d360.core.exceptions;
using d360.core.enums;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Data.SqlClient;
using d360.extensions;
using d360.core.queue;
using d360.core.enums.Workflow;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [RoutePrefix("services/assets"), Authorize]
    public class AssetsController : BaseApiController
    {
        #region DI

        IQueueSource QueueSource;

        public AssetsController(CommunityContext community, CompanyContext company, IQueueSource queueSource)
            : base(community, company)
        {
            QueueSource = queueSource;
        }

        #endregion

        #region Bulk Endpoints

        private async Task<T> readRequestJsonContent<T>(HttpRequestMessage request)
        {
            string json = "";

            if (request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MultipartMemoryStreamProvider();
                await request.Content.ReadAsMultipartAsync(streamProvider);

                json = await streamProvider.Contents.Single().ReadAsStringAsync();
            }
            else
            {
                json = await request.Content.ReadAsStringAsync();
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        /// <summary>
        /// Takes a given set of assets and bulk inserts/updates them.
        /// </summary>
        /// <param name="ot">The Object Type of the asset type.</param>
        /// <param name="otid">The Object Type ID of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, Route("{ot}/{otid:int}/bulk")]
        public async Task<HttpResponseMessage> PostBulkAssetsAsync(SystemObjects ot, int otid)
        {
            if (!Company.HasPermission(ot, otid, Claim.Update, ClaimObject.Root))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update assets of this type.");

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            //string json = "";

            //if (Request.Content.IsMimeMultipartContent())
            //{
            //    var streamProvider = new MultipartMemoryStreamProvider();
            //    await Request.Content.ReadAsMultipartAsync(streamProvider);

            //    json = await streamProvider.Contents.Single().ReadAsStringAsync();
            //}
            //else
            //{
            //    json = await Request.Content.ReadAsStringAsync();
            //}

            try
            {
                var sType = ot.ToString();
                var assetType = Company.Filter<AssetType>(i => i.Object == sType && i.ObjectID == otid).SingleOrDefault();

                if (assetType == null)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Asset Type with Object {sType} and ObjectID {otid} could not be found.");

                var import = readRequestJsonContent<BulkAssetImport>(Request).Result;
                //var import = JsonConvert.DeserializeObject<BulkAssetImport>(json);

                var results = Company.BulkAssetsImport(ot, otid, import);

                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage);
            }
            //finally
            //{
            //    json = null;
            //}
        }

        /// <summary>
        /// Takes a given set of relationships and bulk inserts/updates them.
        /// </summary>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, Route("relationships/bulk")]
        public async Task<HttpResponseMessage> PostBulkAssetRelationshipsAsync()
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update relationships via bulk asset manager.");

            var prefix = "Assets.PostBulkAssetRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                var import = readRequestJsonContent<BulkRelationshipImport>(Request).Result;
                var retResults = Company.BulkRelationshipsImport(import);
                return Request.CreateResponse(HttpStatusCode.OK, retResults);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Takes a given set of relationships and bulk inserts/updates them.
        /// </summary>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, Route("ownership/bulk")]
        public async Task<HttpResponseMessage> PostBulkAssetOwnersAsync()
        {
            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update ownership via bulk asset manager.");

            var prefix = "Assets.PostBulkAssetOwnersAsync => ";
            var errorMessage = "";

            try
            {
                var import = readRequestJsonContent<BulkOwnerImport>(Request).Result;
                var retResults = Company.BulkOwnersImport(import);
                return Request.CreateResponse(HttpStatusCode.OK, retResults);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        #endregion
    }
}
