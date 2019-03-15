using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.model;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [ 
        ApiVersion("1.0"),
        ApiExplorerSettings(IgnoreApi = true),
        RoutePrefix("services/deprecated/assets"), 
        Authorize 
    ]
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

        #region Bulk Assets

        /// <summary>
        /// Takes a given set of assets and bulk inserts/updates them.
        /// </summary>
        /// <param name="ot">The Object Type of the asset type.</param>
        /// <param name="otid">The Object Type ID of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, Route("{ot}/{otid:int}/bulk"), SwaggerResponse(HttpStatusCode.OK, "A list of results based on the import, which may contain errors encountered for each item.", typeof(List<AssetImportResult>))]
        public async Task<IHttpActionResult> PostBulkAssetsAsync(SystemObjects ot, int otid)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update assets of this type.")));

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";
            
            try
            {
                var sType = ot.ToString();

                if(!await Company.Database.Connection.QuerySingleAsync<bool>(
                    @"begin
	                        if exists(select 1 from assettype where [object] = @obj and [objectid] = @objId)
	                        begin
		                        select 1
	                        end
	                        else
	                        begin
		                        select 0
	                        end
                        end"
                    ,new { obj = sType, objId = otid })){
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Asset Type with Object {sType} and ObjectID {otid} could not be found.")));
                }
                
                var import = readRequestJsonContent<List<Dictionary<string, string>>>(Request).Result;
                
                var results = await ( (Company.Database.Connection as SqlConnection).BulkAssetsImport(Company.CurrentResourceID, ot, otid, import));

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

               return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }            
        }
        
        #endregion

        /// <summary>
        /// Takes a given set of relationships and bulk inserts/updates them.
        /// </summary>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, Route("relationships/bulk"), SwaggerResponse(HttpStatusCode.OK, "A list of results based on the import, which may contain errors encountered for each item.", typeof(List<DatabaseBulkRelationshipResult>))]
        public async Task<IHttpActionResult> PostBulkAssetRelationshipsAsync()
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update relationships via bulk asset manager.")));

            var prefix = "Assets.PostBulkAssetRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                var import = readRequestJsonContent<List<RelationshipImportRequest>>(Request).Result;
                var retResults = (Company.Database.Connection as SqlConnection).BulkRelationshipsImport(Company.CurrentResourceID, import);
               return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, retResults)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }

        /// <summary>
        /// Takes a given set of relationships and bulk inserts/updates them.
        /// </summary>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, Route("ownership/bulk"), SwaggerResponse(HttpStatusCode.OK, "A list of results based on the import, which may contain errors encountered for each item.", typeof(List<OwnerImportRequest>))]
        public async Task<IHttpActionResult> PostBulkAssetOwnersAsync()
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update ownership via bulk asset manager.")));

            var prefix = "Assets.PostBulkAssetOwnersAsync => ";
            var errorMessage = "";

            try
            {
                var import = readRequestJsonContent<BulkOwnerImport>(Request).Result;
                var retResults = (Company.Database.Connection as SqlConnection).BulkOwnersImport(Company.CurrentResourceID, import);
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, retResults)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

              return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, errorMessage)));
            }
        }

        #endregion
    }
}
