using d360.core.entities;
using d360.model;
using d360.core;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Web.Http;

namespace d360.web.Controllers.Services
{
    [ApiVersion("1.0"), ApiExplorerSettings(IgnoreApi = true), RoutePrefix("services/deprecated/relationships"), Name("Relationships"), Authorize]
    public class RelationshipsController : BaseApiController
    {
        public RelationshipsController(ICommunityContext community, ICompanyContext company)
            : base(community, company)
        {
        }
        
        /// <summary>
        /// Allows for OData filtering on relationships types.
        /// </summary>
        /// <returns>A list of relationships types present in the system.</returns>
        [Route(""), HttpGet, ApiExplorerSettings(IgnoreApi = true)]
        public IQueryable<IntersectType> GetIntersectTypes()
        {
            return Company.Table<IntersectType>();
        }
    }
}
