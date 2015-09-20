using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using d360.core.entities;
using d360.core;
using d360.core.entities.Views;
using d360.services.interfaces;
using d360.extensions;
using System.Net;
using AttributeRouting.Web.Mvc;
using AttributeRouting;

namespace d360.web.Controllers
{
    [RoutePrefix("statusupdates")]
    public class StatusUpdatesController : BaseController
    {
        IResourceService ResourceService;

        public StatusUpdatesController(IResourceService resourceService)
        {
            ResourceService = resourceService;
        }

        [HttpPost]
        public JsonResult UpdateMyStatus(FormCollection form)
        {
            var description = form["description"];
            var post = new Comment { Body = description, ObjectID = 0, ObjectType = "Resource" };
            var list = ResourceService.AddComment(post).ToList();
            Response.StatusCode = (int)HttpStatusCode.Created;
            return Json(list);
        }

        [HttpPost]
        public JsonResult Create(SystemObjects type, int id, FormCollection form)
        {
            var description = form["description"];
            var post = new Comment { Body = description, ObjectID = id, ObjectType = type.ToString() };
            var list = ResourceService.AddComment(post);
            Response.StatusCode = (int)HttpStatusCode.Created;
            return Json(list);
        }

        public ActionResult ByType(string type, int id)
        {
            ViewData.Add("Followed", "false");
            ViewData.Add("Type", type);
            ViewData.Add("ID", id);
            return PartialView("StatusUpdates");
        }

        public ActionResult Followed()
        {
            ViewData.Add("Followed", "true");
            ViewData.Add("Question", "What are you working on?");
            ViewData.Add("Type", "Resource");
            ViewData.Add("ID", 0);//SystemValueProvider.CurrentResourceID);
            return PartialView("StatusUpdates");
        }

        public JsonResult Comments(int id, int skip = 0)
        {
            var list = new List<string>();// ResourceService.GetCommentsByStatusUpdate(id, skip).OrderByDescending(i => i.DateCreated);
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Comments(int id, FormCollection form)
        {
            var o = new Comment { ParentID = id, Body = form["description"] };
            var v = ResourceService.AddComment(o);
            return Json(v, JsonRequestBehavior.AllowGet);
        }


        public JsonResult GetByPublicGroups(int skip = 0)
        {
            var list = new List<string>(); //ResourceService.GetCommentDetailsByType(SystemObjects.Group, skip);
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetByFollowed(int skip = 0)
        {
            var list = ResourceService.GetCommentDetailsByFollower(ResourceService.CurrentResourceID, skip, 200);
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetByType(SystemObjects type, int id, int skip = 0)
        {
            var list = ResourceService.GetCommentDetailsByType(type, id, skip, 200).ToList();
            var items = list.Select(i => new { i.DateCreated, i.Body, i.ResourceEmail, i.ID, i.ResourceName, i.ObjectID, i.ObjectType, i.ObjectUrl, i.CreatingResourceID });
            return Json(items, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        [GET("StatusUpdateCategoriesTree_Followed")]
        public ActionResult CommentCategoriesTree_Followed()
        {
            var entries = new List<CommentCategory>();// VerboseStatusUpdateCategories();

            var list = ResourceService.GetCategoriesForComments().ToList();

            foreach (var g in list.GroupBy(i => i.Category).OrderBy(i => i.Key))
            {
                var f = g.First();
                var p = new CommentCategory { Category = f.Category, Count = g.Sum(i => i.Count), Name = f.Category, ObjectID = 0, ObjectType = g.Key };
                p.Items = g.ToList();
                entries.Add(p);
            }

            return PartialView("CommentCategoriesTree", entries);
        }
    }
}
