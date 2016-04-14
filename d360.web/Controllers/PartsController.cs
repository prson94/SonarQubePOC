using d360.core;
using System.Linq;
using System.Web.Mvc;
using d360.web.Models;
using d360.core.entities;
using d360.model;

namespace d360.web.Controllers
{
    [RoutePrefix("parts"), Authorize]
    public class PartsController : BaseController
    {
        #region DI
        
        public PartsController(CommunityContext community, CompanyContext company) : base(community, company) { }

        #endregion

        [Route("groups/{id:int}/ownership")]
        public ActionResult OwnershipForGroup(int id)
        {
            ViewData.Add("IsAdmin", Company.Any<ResourceGroup>(i => i.GroupID == id && i.ResourceID == id));
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = SystemObjects.Group.ToString() });
        }

        public ActionResult DisplayClaimsMatrix(SystemObjects type, int id, int responsibilityTypeID)
        {
            var sType = type.ToString();
            var model = new ClaimsMatrixDisplayModel
            {
                ResponsibilityTypeID = responsibilityTypeID,
                Items = Company.Filter<ResponsibilityTypeObjectClaim>(i => i.ObjectID == id && i.ObjectType == sType && i.ResponsibilityTypeID == responsibilityTypeID)
                .Select(i => new ClaimsMatrixEditorItemModel { Claim = i.Claim, ClaimObject = i.ClaimObject, ID = i.ID })
                .ToList()
            };
            return PartialView(model);
        }

        [Route("resources/{id:int}/ownership")]
        public ActionResult Ownership(int id)
        {
            ViewData.Add("IsAdmin", (Company.CurrentResourceID == id));
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = SystemObjects.Resource.ToString() });
        }

        [Route("resources/{resourceID:int}/ownership/{type}/{id:int}")]
        public ActionResult OwnershipByType(int resourceID, string type, int id)
        {
            ObjectDetail detail = null;

            switch (type)
            {
                case "Policy":
                    id = 0;
                    detail = new ObjectDetail { Description = "", Name = "Policy", PluralizedName = "Policies", ID = id, Type = type, TypeID = id };
                    break;
                case "Rule":
                    id = 0;
                    detail = new ObjectDetail { Description = "", Name = "Rule", PluralizedName = "Rules", ID = id, Type = type, TypeID = id };
                    break;
                default:
                    //type = type + "Type";
                    detail = Company.GetObjectDetail(type + "Type", id);
                    break;
            }

            var resourceName = "";
            if (Company.CurrentResourceID == resourceID)
            {
                resourceName = "You Own";
            }
            else
            {
                var resource = Community.GetById<Resource>(resourceID);
                resourceName = resource.FirstName + " Owns";
                resource = null;
            }

            ViewData.Add("Title", string.Format("{0} That {1}", detail.PluralizedName, resourceName));

            detail = null;

            ViewData.Add("ObjectType", type);
            ViewData.Add("ObjectTypeID", id);
            ViewData.Add("IsAdmin", (Company.CurrentResourceID == resourceID));
            return PartialView("Ownership", new ObjectModel { ObjectID = resourceID, ObjectType = SystemObjects.Resource.ToString() });
        }

        public ActionResult ResponsibilityTypeObjectClaimGrid(SystemObjects type, int id)
        {
            ViewData.Add("ObjectType", type.ToString());
            ViewData.Add("ObjectID", id);
            return PartialView();
        }

        public ActionResult ResponsibilityTypeUsageGrid(int id)
        {
            ViewData.Add("ID", id);
            return PartialView();
        }

        #region Type/ID Queries

        [Route("{type}/{id}/audit/grid")]
        public ActionResult AuditGrid(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        [Route("{type}/{id}/board")]
        public ActionResult Board(SystemObjects type, int id)
        {
            ViewData.Add("type", type);
            ViewData.Add("id", id);
            return PartialView();
        }

        [
        Route("{type}/{id}/detail"), 
        Route("{type}/{id}/detail/{context}")
        ]
        public ActionResult Detail(SystemObjects type, int id, string context = "")
        {
            if (string.IsNullOrEmpty(context)) context = string.Format("{0}form", type.ToString().ToLower());
            ViewData.Add("type", type);
            ViewData.Add("id", id);
            ViewData.Add("context", context);
            return PartialView();
        }

        [Route("Following")]//[Route("{type}/{id}/following/{type}/{id:int}")]
        public ActionResult Following(int resourceID, SystemObjects type, int id)
        {
            var detail = Company.GetObjectDetail(type.ToString(), id);
            var resourceName = "";
            if (Company.CurrentResourceID == resourceID)
            {
                resourceName = "You Are";
            }
            else
            {
                var resource = Community.GetById<Resource>(resourceID);
                resourceName = resource.FirstName + " Is";
                resource = null;
            }

            var objectName = "";

            if (detail != null)
            {
                objectName = detail.PluralizedName;
            }
            else
            {
                switch (type)
                {
                    case SystemObjects.Group:
                        objectName = "Groups";
                        break;
                    case SystemObjects.ResourceType:
                        objectName = "Users";
                        break;
                    default:
                        objectName = type.ToString();
                        break;
                }
            }

            ViewData.Add("Title", string.Format("{0} That {1} Following", objectName, resourceName));
            
            detail = null;
            ViewData.Add("ResourceID", resourceID);
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        [Route("{type}/{id}/followers")]
        public ActionResult Followers(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        [Route("{type}/{id}/groups")]
        public ActionResult Groups(SystemObjects type, int id)
        {
            return PartialView(new ObjectModel { ObjectID = id, ObjectType = type.ToString() });
        }

        [Route("{type}/{id}/reports/survey/{surveyTypeID}")]
        public ActionResult SurveyReport(SystemObjects type, int id, int surveyTypeID)
        {
            ViewData.Add("type", type);
            ViewData.Add("id", id);
            ViewData.Add("surveyTypeID", surveyTypeID);
            return PartialView();
        }

        [Route("{type}/{id}/empty")]
        public ContentResult Empty(SystemObjects type, int id)
        {
            return Content("Empty content here");
        }

        #endregion
    }
}
