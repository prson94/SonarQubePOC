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

        //[Route("resources/{id:int}/ownership")]
        //public ActionResult Ownership(int id)
        //{
        //    ViewData.Add("IsAdmin", (Company.CurrentResourceID == id));
        //    return PartialView(new ObjectModel { ObjectID = id, ObjectType = SystemObjects.Resource.ToString() });
        //}

        //[Route("resources/{resourceID:int}/ownership/{type}/{id:int}")]
        //public ActionResult OwnershipByType(int resourceID, string type, int id)
        //{
        //    ObjectDetail detail = null;

        //    switch (type)
        //    {
        //        case "Policy":
        //            id = 0;
        //            detail = new ObjectDetail { Description = "", Name = "Policy", PluralizedName = "Policies", ID = id, Type = type, TypeID = id };
        //            break;
        //        case "Rule":
        //            id = 0;
        //            detail = new ObjectDetail { Description = "", Name = "Rule", PluralizedName = "Rules", ID = id, Type = type, TypeID = id };
        //            break;
        //        default:
        //            //type = type + "Type";
        //            detail = Company.GetObjectDetail(type + "Type", id);
        //            break;
        //    }

        //    var resourceName = "";
        //    if (Company.CurrentResourceID == resourceID)
        //    {
        //        resourceName = "You Own";
        //    }
        //    else
        //    {
        //        var resource = Community.GetById<Resource>(resourceID);
        //        resourceName = resource.FirstName + " Owns";
        //        resource = null;
        //    }

        //    var name = detail == null ? "" : detail.PluralizedName;

        //    ViewData.Add("Title", string.Format("{0} That {1}", name, resourceName));

        //    detail = null;

        //    ViewData.Add("ObjectType", type);
        //    ViewData.Add("ObjectTypeID", id);
        //    ViewData.Add("IsAdmin", (Company.CurrentResourceID == resourceID));
        //    return PartialView("Ownership", new ObjectModel { ObjectID = resourceID, ObjectType = SystemObjects.Resource.ToString() });
        //}

        //[Route("ResponsibilityTypeObjectClaimGrid")]
        //public ActionResult ResponsibilityTypeObjectClaimGrid(SystemObjects type, int id)
        //{
        //    ViewData.Add("ObjectType", type.ToString());
        //    ViewData.Add("ObjectID", id);
        //    return PartialView();
        //}

        //[Route("ResponsibilityTypeUsageGrid")]
        //public ActionResult ResponsibilityTypeUsageGrid(int id)
        //{
        //    ViewData.Add("ID", id);
        //    return PartialView();
        //}
    }
}
