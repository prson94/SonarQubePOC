using d360.core.entities;
using d360.core.enums;
using d360.extensions;
using d360.model;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [ 
        ApiVersion("2.0"), 
        RoutePrefix("api/v{version:apiVersion}/relationships"), Authorize]
    public class RelationshipsController : BaseApiController
    {
        #region DI

        IQueueSource QueueSource;

        public RelationshipsController(CommunityContext community, CompanyContext company, IQueueSource queueSource)
            : base(community, company)
        {
            QueueSource = queueSource;
        }

        #endregion

        #region utils

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

        #endregion

        /// <summary>
        /// GET a list of relationship types.
        /// </summary>
        /// <returns></returns>
        [HttpGet, MapToApiVersion("2.0"), Route("types")]
        public async Task<HttpResponseMessage> GetRelationshipTypesAsync()
        {
            var prefix = "Relationships.GetRelationshipTypesAsync => ";
            var errorMessage = "";

            try
            {
                var types = await Company.QueryAsync<IntersectTypeApiViewModel>(@"
select	I.Uid,
		P.Name as PredicateName,
		P.Inverse as PredicateInverse,
		P.[Type] as PredicateTypeID,
		I.SubjectUid,
		S.Class as SubjectClassID,
		case 
			when I.SubjectUid = '0000000A-0000-0000-0000-000000000009' then 'Reference List' 
			when I.SubjectUid = '00000001-0000-0000-0000-a00000000011' then 'User'
			when I.SubjectUid = '00000001-0000-0000-0000-a00000000012' then 'Group'
			else S.Name 
		end as SubjectTypeName,
		I.ObjectUid,
		O.Class as ObjectClassID,
		case 
			when I.ObjectUid = '0000000A-0000-0000-0000-000000000009' then 'Reference List' 
			when I.ObjectUid = '00000001-0000-0000-0000-a00000000011' then 'User'
			when I.ObjectUid = '00000001-0000-0000-0000-a00000000012' then 'Group'
			else O.Name 
		end as ObjectTypeName
from	IntersectType I
		left join [Predicate] P on P.ID = I.PredicateID
		left join AssetType S on S.uid = I.SubjectUid
		left join AssetType O on O.uid = I.ObjectUid
where	coalesce(S.uid, I.SubjectUid) is not null
		and coalesce(O.uid, I.ObjectUid) is not null");

                return Request.CreateResponse(HttpStatusCode.OK, types);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        #region Bulk Relationships

        /// <summary>
        /// Takes a given set of relationships and bulk inserts/updates them.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [HttpPost, MapToApiVersion("2.0"), Route("{uid}")]
        public async Task<IHttpActionResult> PostBulkRelationshipsAsync(Guid uid)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add/update relationships of this type.")));

            var prefix = "Assets.PostBulkRelationshipsAsync => ";
            var errorMessage = "";

            try
            {
                var intersectType = Company.Filter<IntersectType>(i => i.uid == uid).SingleOrDefault();

                if (intersectType == null)
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Relationship Type with UID {uid} could not be found.")));

                var import = readRequestJsonContent<BulkAssetImport>(Request).Result;

                var results = (Company.Database.Connection as SqlConnection).BulkRelationshipsImport(
                    QueueSource, 
                    Company.CurrentCompanyDomain, Company.CurrentCompanyID, Company.CurrentResourceID, 
                    intersectType, 
                    import);

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

    }
}
