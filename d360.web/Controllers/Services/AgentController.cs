using System.Linq;
using System.Web.Http;
using d360.core.entities;
using System.Net;
using System.Net.Http;
using d360.extensions;
using d360.model;
using d360.core.entities.Plugins;
using d360.web.Models.Attributes;
using d360.core;

namespace d360.web.Controllers.Services
{
    [RoutePrefix("services/agent"), Authorize]
    public class AgentController : BaseApiController
    {
        #region DI

        IStorageProvider Storage;

        public AgentController(
            CommunityContext community,
            CompanyContext company, 
            IStorageProvider storage): base(community, company)
        {
            Storage = storage;
        }

        #endregion

        /// <summary>
        /// Get a list of packages.
        /// </summary>
        /// <returns>A list of packages.</returns>
        [Route("packages")]
        public IQueryable<Package> GetPackages()
        {
            var companyID = Company.CurrentCompanyID;
            return Community.Filter<Package>(i => i.Companies.Any(c => c.ID == companyID), i => i.PackageContents);
        }

        /// <summary>
        /// Gets the ZIP file containing the contents outlined.
        /// </summary>
        /// <param name="id">The package ID</param>
        /// <returns>Artifact</returns>
        [Route("packages/{id}")]
        public HttpResponseMessage GetPackageContents(int id)
        {
            var companyID = Company.CurrentCompanyID;

            if (!Company.CurrentResourceIsAdmin)
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to see the package contents.");

            var package = Community.Filter<Package>(i => i.Companies.Any(c => c.ID == companyID) && i.ID == id, i => i.PackageContents).SingleOrDefault();
                
            if (package == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            var uri = Storage.GetFileSecureUrl("agent-packages", string.Format("{0}.zip", package.ID));
            //var stream = Storage.GetFile("agent-packages", string.Format("{0}.zip", package.ID));

            HttpResponseMessage result = Request.CreateResponse(HttpStatusCode.OK, new { Uri = uri });//new StreamContent(stream));
            //result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return result;
        }
    }
}
