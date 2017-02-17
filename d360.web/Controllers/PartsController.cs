using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.web.Models;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;

namespace d360.web.Controllers
{
    [RoutePrefix("parts"), Authorize]
    public class PartsController : BaseController
    {
        #region DI
        
        public PartsController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        [HttpGet, Route("ClaimsMatrix"), NonNullableParameters]
        public JsonNetResult ClaimsMatrix(SystemObjects type, int id, int responsibilityTypeID)
        {
            var sType = type.ToString();
            var model = new ClaimsMatrixDisplayModel
            {
                ResponsibilityTypeID = responsibilityTypeID,
                Items = Company.Filter<ResponsibilityTypeObjectClaim>(i => i.ObjectID == id && i.ObjectType == sType && i.ResponsibilityTypeID == responsibilityTypeID)
                .Select(i => new ClaimsMatrixEditorItemModel { Claim = i.Claim, ClaimObject = i.ClaimObject, ID = i.ID })
                .ToList()
            };

            return new JsonNetResult
            {
                Data = model,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }        
    }
}
