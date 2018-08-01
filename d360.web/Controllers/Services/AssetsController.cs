using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

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

        #region Type Endpoints

        [HttpGet, Route("classes")]
        public async Task<HttpResponseMessage> GetAssetTypeClassesAsync()
        {
            var prefix = "Assets.GetAssetTypeClassesAsync => ";
            var errorMessage = "";

            try
            {
                var classes = AssetTypeClass.Glossary.GetAsList();
                return Request.CreateResponse(HttpStatusCode.OK, classes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        [HttpGet, Route("types")]
        public async Task<HttpResponseMessage> GetAssetTypesAsync()
        {
            var prefix = "Assets.GetAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                var assetTypes = await Company.QueryAsync<AssetTypeApiViewModel>(@"
SELECT		A.[Name]
			,A.[Description]
			,A.[Class] as ClassID
			,A.[Notes]
			,A.[uid],
			P.[Path]
FROM		AssetType A
			cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
where		A.[State] = 1
order by	P.[Path]
");

                return Request.CreateResponse(HttpStatusCode.OK, assetTypes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        [HttpPost, Route("types")]
        public async Task<HttpResponseMessage> AddAssetTypeAsync()
        {
            var prefix = "Assets.AddAssetTypeAsync => ";
            var errorMessage = "";

            try
            {
                var assetTypes = await Company.QueryAsync<AssetTypeApiViewModel>(@"
SELECT		A.[Name]
			,A.[Description]
			,A.[Class] as ClassID
			,A.[Notes]
			,A.[uid],
			P.[Path]
FROM		AssetType A
			cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
where		A.[State] = 1
order by	P.[Path]
");

                return Request.CreateResponse(HttpStatusCode.OK, assetTypes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
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
        public async Task<IHttpActionResult> PostBulkAssetsAsync(SystemObjects ot, int otid)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update assets of this type.")));

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";
            
            try
            {
                var sType = ot.ToString();
                var assetType = Company.Filter<AssetType>(i => i.Object == sType && i.ObjectID == otid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Asset Type with Object {sType} and ObjectID {otid} could not be found.")));

                var import = readRequestJsonContent<BulkAssetImport>(Request).Result;
                
                var results = (Company.Database.Connection as SqlConnection).BulkAssetsImport(Company.CurrentResourceID, ot, otid, import);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
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
        [HttpPost, Route("relationships/bulk")]
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
        [HttpPost, Route("ownership/bulk")]
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
