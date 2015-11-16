using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using d360.model;
using d360.web.Models;
using d360.core;
using d360.core.exceptions;
using System.Xml.Linq;
using d360.core.entities;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using d360.core.enums;

namespace d360.web.Models
{
    public class QuestionTypeModel
    { 
        public string Name { get; set; }
        public string Description { get; set; }
        public int ResponseTypeID { get; set; }
        public int SurveyTypeID { get; set; }
        public int QuestionTypeID { get; set; }
        public List<ResponseType> ResponseTypes { get; set; }
    }
}

namespace d360.web.Controllers
{
    [RoutePrefix("resources"), Authorize]
    public class ResourcesController : BaseController
    {
        #region DI

        public ResourcesController(CommunityContext community, CompanyContext company)
            : base(community, company)
        { }

        #endregion

        #region Actions

        [Route("surveys/{typeID:int}/{id:int}")]
        public ActionResult Survey(int typeID, int id)
        {
            return View();
        }

        [Route("image/{id:int}")]
        public ActionResult MyImage(int id, int size = 150)
        {
            var resource = Community.GetById<Resource>(id);

            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.  
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(resource.Email));

            // Create a new Stringbuilder to collect the bytes  
            // and create a string.  
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string.  
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return Redirect(string.Format("https://secure.gravatar.com/avatar/{0}?s={1}", sBuilder.ToString(), size));
        }

        [Route("image/me")]
        public ActionResult ResourceImage(int size = 150)
        {
            var resource = Community.GetById<Resource>(Company.CurrentResourceID);

            MD5 md5Hasher = MD5.Create();

            // Convert the input string to a byte array and compute the hash.  
            byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(resource.Email));

            // Create a new Stringbuilder to collect the bytes  
            // and create a string.  
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string.  
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            return Redirect(string.Format("https://secure.gravatar.com/avatar/{0}?s={1}", sBuilder.ToString(), size));
        }

        #endregion

        #region Partials

        [Route("templates/email")]
        public ActionResult _EmailTemplates()
        {
            return PartialView();
        }

        //[Route("lookups/{id:int}/allocations")]
        //public ActionResult _LookupAllocations(int id)
        //{
        //    ViewData.Add("ID", id);
        //    return PartialView();
        //}

        //[Route("lookups/{id:int}/items")]
        //public ActionResult _LookupItems(int id)
        //{
        //    ViewData.Add("ID", id);

        //    var sType = SystemObjects.LookupType.ToString();
        //    var fields = Company.Filter<FieldTypeWithRelation>(i => i.Object == sType && i.ObjectID == id).ToList();
        //    fields.Insert(0, new FieldTypeWithRelation { FriendlyName = "ID", Name = "ID", Type = DataType.Number.ToString() });
        //    return PartialView(fields);
        //}

        string buttonHtml(string buttonType, string context, string uri, string icon, string title, string method = "")
        {
            string methodAttribute = string.IsNullOrEmpty(method) ? "" : string.Format(" data-method='{0}'", method);
            return string.Format("<button type='button' data-{0} data-context='{1}' data-uri='{2}'{5} class='btn btn-default' title='{4}'><i class='fa fa-{3}'></i></button>", buttonType, context, uri, icon, title, methodAttribute);
        }

        [Route("{type}/{id:int}/flags")]
        public ContentResult RenderFlagsTooltip(SystemObjects type, int id)
        {
            string html = "The red Flag comment thread will appear here in the coming iteration.";

            return Content(html, "text/html");
        }

        [Route("{type}/{id:int}/tools")]
        public ActionResult RenderToolsTooltip(SystemObjects type, int id, string templateAction)
        {
            var toolbar = new ContextToolbar { ToolbarSuffix = string.Format("{0}{1}", type.ToString(), id) };

            var common = new ContextToolbarItem { Title = "Common Actions" };

            switch (type)
            {
                case SystemObjects.Intersect:
                    if (Company.HasPermission(type, id, Claim.Create, ClaimObject.Relationship))
                    {
                        var intersect = Company.GetById<Intersect>(id, i => i.IntersectType);
                        if (intersect != null)
                        {
                            //if (intersect.IntersectType.AllowSourcing)
                            //{
                                common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionResponsibility, Icon = "plus", Title = "Select source", Type = "local", Uri = "/form/AddSourcingResponsibility?type=Intersect&id=" + id });
                            //}

                            var add = new ContextToolbarItem { Context = "null", Icon = "", Title = "Associate Child Items", Type = "local", Uri = "#" };
                            var types = Company.Query<AllowedIntersectionType>("GetAllowedIntersectionTypesByIntersect @intersectID", new { intersectID = id }).ToList();
                            foreach (var t in types)
                            {
                                add.Items.Add(
                                    new ContextToolbarItem
                                    {
                                        Context = ContextList.ActionRelate,
                                        Icon = "plus",
                                        Title = t.TargetName,
                                        Type = "local",
                                        Uri = "/Relations/AddRelationship?source=Intersect&sourceID=" + t.ParentIntersectID + "&intersectTypeID=" + t.IntersectTypeID + "&target=" + t.TargetType + "&targetID=" + t.TargetTypeID
                                    });
                            }
                            if (add.Items.Count > 0)
                            {
                                toolbar.Items.Add(add);
                            }
                        }
                        intersect = null;
                    }
                    if (Company.HasPermission(type, id, Claim.Update, ClaimObject.Relationship))
                        common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionEditRelate, Icon = "pencil", Title = "Edit relationship", Type = "local", Uri = "/relations/EditRelationship?id=" + id });
                    if (Company.HasPermission(type, id, Claim.Delete, ClaimObject.Relationship))
                        common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionUnrelate, Icon = "trash-o", Method = "DELETE", Title = "Remove relationship", Type = "command", Uri = "/api/relationships/" + id });
                    break;
                case SystemObjects.Responsibility:
                    if (Company.HasPermission(type, id, Claim.Update, ClaimObject.Governance))
                        common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionResponsibility, Icon = "pencil", Title = "Edit responsibility", Type = "local", Uri = "/form/EditSourcingResponsibility?id=" + id });
                    if (Company.HasPermission(type, id, Claim.Delete, ClaimObject.Governance))
                        common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionResponsibility, Icon = "trash-o", Title = "Remove responsibility", Type = "local", Uri = "/form/DeleteResponsibility?&id=" + id });
                    if (Company.HasPermission(type, id, Claim.Update, ClaimObject.Governance))
                        common.Items.Add(new ContextToolbarItem { Context = ContextList.ResponsibilityTransformation, Icon = "plus", Title = "Add transformation", Type = "local", Uri = "/form/AddResponsibilityTransformation?responsibilityID=" + id });
                        break;
            }

            if (common.Items.Count > 0)
            {
                toolbar.Items.Add(common);
            }

            return PartialView("RowCommandTooltip", toolbar); //Content(toolsHtml + html, "text/html");
        }

        [Route("{type}/{id:int}/templates/email/{templateAction}")]
        public ContentResult RenderEmail(SystemObjects type, int id, string templateAction)
        {
            string html = Company.RenderEmail(templateAction, type, id);
            return Content(html, "text/html");
        }

        [Route("complexvalue/{id:int}/{attribute:int}/templates/tooltip/preview")]
        public ContentResult RenderComplexValueTooltip(int id, int attribute)
        {
            //get values for attribute and all attributes that have this as a parent for specified element
            StringBuilder sb = new StringBuilder();

            int maxIterations = 50;
            var firstValue = Company.Filter<AttributeDetail>(i => i.AttributeTypeID == attribute && i.ObjectID == id && i.ObjectType == "Intersect").Select(i => new { name = i.Name, value = i.FormattedValue, ID = i.ID }).FirstOrDefault();

            if (firstValue != null)
            {
                sb.Append("<b>" + firstValue.name + ":</b> ");
                sb.Append(firstValue.value);
                sb.Append("<br>");
                Queue<int> attributeIDQueue = new Queue<int>();

                attributeIDQueue.Enqueue(firstValue.ID);
                int level = 1;

                while (attributeIDQueue.Count > 0 && maxIterations > 0)
                {
                    int currentID = attributeIDQueue.Dequeue();
                    var valuePairs = Company.Filter<AttributeDetail>(i => i.ParentID == currentID && i.ObjectID == id).Select(i => new { name = i.Name, value = i.FormattedValue, ID = i.ID }).ToList();

                    //add each of the values to the html output
                    //enqueue unique attribute ids
                    foreach (var item in valuePairs)
                    {                        
                        sb.Append(string.Concat(Enumerable.Repeat("&nbsp;", (level * 3))));
                        sb.Append("<b>" + item.name + ":</b> ");
                        sb.Append(item.value);
                        sb.Append("<br>");

                        if (!attributeIDQueue.Contains(item.ID)) attributeIDQueue.Enqueue(item.ID);
                    }
                    level++; //used for spaces
                    maxIterations--; // for christ sakes Jim this is a tooltip, do you really think it makes sense to show a tooltip bigger than the screen?
                }
            }

            return Content(sb.ToString(),"text/html");
        }

        [Route("{type}/{id:int}/templates/tooltip/{templateAction}")]
        public ContentResult _RenderTooltip(SystemObjects type, int id, string templateAction)
        {
            string html = Company.RenderTooltip(templateAction, type, id);

            if (type == SystemObjects.Resource)
            {
                // Need to do extra processing here as the tooltip cannot populate from the company database.
                var resource = Community.Resources.SingleOrDefault(i => i.ID == id);
                if (resource != null)
                {
                    var fields = Company.Filter<FieldWithRelation>(i => i.ObjectType == "Resource" && i.ObjectID == id).ToDictionary(k => k.Name, var => var.FormattedValue);
                    fields.Add("Name", resource.FormatDisplayName());
                    fields.Add("FirstName", resource.FirstName);
                    fields.Add("LastName", resource.LastName);
                    fields.Add("DateLastLoggedIn", resource.DateLastLoggedIn.HasValue ? resource.DateLastLoggedIn.Value.ToString("MM/dd/yyyy HH:mm:ss") : "Never");
                    fields.Add("Email", resource.Email);
                    fields.Add("Status", resource.Status);
                    html = html.ReplaceTokenWithValues(fields);
                }
            }
            
            return Content(html, "text/html");
        }

        [Route("responsetypes/{id:int}/options")]
        public ActionResult _ResponseTypeOptions(int id)
        {
            ViewData.Add("id", id);
            return PartialView();
        }

        [Route("surveys/{id:int}/entries")]
        public ActionResult _SurveyTypeEntries(int id)
        {
            ViewData.Add("id", id);
            return PartialView();
        }

        [Route("surveys/{id:int}/questions")]
        public ActionResult _SurveyTypeQuestions(int id)
        {
            ViewData.Add("id", id);
            return PartialView();
        }

        [Route("templates/tooltip")]
        public ActionResult _TooltipTemplates()
        {
            return PartialView();
        }

        #endregion

        #region Json

        public JsonResult _GroupsByResourceID(int id)
        {
            return Json(
                Company.Filter<ResourceGroup>(i => i.ResourceID == id)
                    .Select(i => i.Group)
                    .Select(i => new
                    {
                        i.ID,
                        i.Name
                    }), 
                JsonRequestBehavior.AllowGet
            );
        }

        public JsonResult _Lookups(string sortDataField, string sortOrder, int pagenum = 0, int pagesize = 10)
        {
            var list = Company.Table<LookupType>();

            var total = list.Count();

            #region Sort Processing

            if (string.IsNullOrEmpty(sortDataField))
            {
                sortDataField = "Name";
                sortOrder = "asc";
            }

            switch (sortDataField)
            {
                default:
                    list = list.OrderBy(sortDataField + " " + sortOrder).AsQueryable();
                    break;
            }

            #endregion

            #region Filter Processing

            var query = Request.QueryString;

            int filterscount = 0;

            if (int.TryParse(query["filterscount"], out filterscount))
            {
                for (int i = 0; i < filterscount; i++)
                {
                    var fField = query["filterdatafield" + i];
                    var fCondition = query["filtercondition" + i];
                    var fValue = query["filtervalue" + i];

                    switch (fCondition)
                    {
                        case "CONTAINS":
                            switch (fField)
                            {
                                case "Name":
                                    list = list.Where(f => f.Name.Contains(fValue));
                                    break;
                            }
                            break;
                        case "DOES_NOT_CONTAIN":
                            switch (fField)
                            {
                                case "Name":
                                    list = list.Where(f => !f.Name.Contains(fValue));
                                    break;
                            }
                            break;
                        case "EQUAL":
                            switch (fField)
                            {
                                case "Name":
                                    list = list.Where(f => f.Name == fValue);
                                    break;
                                //case "System":
                                //    list = list.Where(f => f.System.Contains(fValue));
                                //    break;

                            }
                            break;
                        case "NOT_EQUAL":
                            switch (fField)
                            {
                                case "Name":
                                    list = list.Where(f => f.Name != fValue);
                                    break;
                            }
                            break;
                        case "STARTS_WITH":
                            switch (fField)
                            {
                                case "Name":
                                    list = list.Where(f => f.Name.StartsWith(fValue));
                                    break;
                            }
                            break;
                        case "ENDS_WITH":
                            switch (fField)
                            {
                                case "Name":
                                    list = list.Where(f => f.Name.EndsWith(fValue));
                                    break;
                            }
                            break;
                    }
                }
            }

            #endregion

            var items = list//.Skip(pagenum * pagesize)
                //.Take(pagesize)
                .Select(i => new
                {
                    i.ID,
                    i.Name,
                    ItemCount = i.Lookups.Count
                })
                .ToList();

            var o = new { total, results = items };

            return Json(o, JsonRequestBehavior.AllowGet);
        }

        [Route("lookups/{typeID:int}/items.json")]
        public JsonResult GetLookupItems(int typeID)
        {
            return Json(Company.GetLookupItemsAsDictionary(typeID), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateFollowStatus(SystemObjects type, int id)
        {
            try
            {
                bool status = Company.UpdateFollowStatus(type, id, null);
                return Json(new { title = "Success!", message = string.Format("You are {0} following this item.", (status) ? "now" : "no longer"), type = "notification" });
            }
            catch (Exception ex)
            {
                return Json(new { title = "Error Occurred!", message = ex.Message, type = "error" });
            }
        }

        #endregion
    }
}
