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
using d360.core.entities.Views;
using SpreadsheetLight;
using System.IO;

namespace d360.web.Models
{
    public class QuestionTypeModel
    { 
        public string Name { get; set; }
        public string Description { get; set; }
        public int ResponseTypeID { get; set; }
        public int SurveyTypeID { get; set; }
        public int QuestionTypeID { get; set; }
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

            return Redirect(string.Format("https://secure.gravatar.com/avatar/{0}?s={1}&d=mm", sBuilder.ToString(), size));
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

        #region Exports

        string getSqlStatement(int resourceID, string type, int id, bool follow)
        {
            var joins = "";
            var columns = "";
            string sql = "";

            SystemObjects enumValueValidation;
            if (Enum.TryParse<SystemObjects>(type, out enumValueValidation))
            {
                getDynamicFieldJoinStatements(id, type.Replace("Type", ""), out joins, out columns, false, true);
                string followOrOwnSql = "";
                string lastColumn = (follow) ? "FD.OpenEventCount" : "FD.[Role], FD.ContextItems as [Context]";

                switch (type)
                {
                    case "Artifact":
                    case "ArtifactType":
                        #region
                        followOrOwnSql = (follow) ?
                            $"inner join FollowDetail FD on FD.ResourceID = {resourceID} and FD.Type = '{type.Replace("'", "''")}' and FD.TypeID = {id} and FD.ObjectID = A.ID" :
                            $"inner join ResponsibilityDetailForResource FD on FD.ResponsibleObjectType = 'Resource' and FD.ResponsibleObjectID = {resourceID} and FD.ObjectType = '{type.Replace("'", "''")}' and FD.ObjectTypeID = {id} and FD.ObjectID = A.ID";

                        sql = $@"
select	A.ID,
		A.Name,
		A.Description,
		A.TextPath,
		A.Status,
		V.Name as SubjectArea,
        {columns}
        FD.CurrentScore,
        {lastColumn}
from	Artifact A 
        {followOrOwnSql} 
        inner join TaxonomyType V on V.ID = A.TaxonomyTypeID and A.ArtifactTypeID = {id} 
        {joins}";
                        break;
                    #endregion
                    case "Domain":
                    case "DomainType":
                        #region
                        followOrOwnSql = (follow) ?
                            $"inner join FollowDetail FD on FD.ResourceID = {resourceID} and FD.Type = '{type.Replace("'", "''")}' and FD.TypeID = {id} and FD.ObjectID = A.ID" :
                            $"inner join ResponsibilityDetailForResource FD on FD.ResponsibleObjectType = 'Resource' and FD.ResponsibleObjectID = {resourceID} and FD.ObjectType = '{type.Replace("'", "''")}' and FD.ObjectTypeID = {id} and FD.ObjectID = A.ID";

                        sql = $@"
select	A.ID,
		A.Name,
		A.Description,
		A.Code,
        {columns}
        FD.CurrentScore,
        {lastColumn}
from	Domain A 
        {followOrOwnSql} and A.DomainTypeID = {id} 
        {joins}";
                        break;
                    #endregion
                    case "Taxonomy":
                    case "TaxonomyType":
                        #region
                        followOrOwnSql = (follow) ?
                            $"inner join FollowDetail FD on FD.ResourceID = {resourceID} and FD.Type = '{type.Replace("'", "''")}' and FD.TypeID = {id} and FD.ObjectID = A.ID" :
                            $"inner join ResponsibilityDetailForResource FD on FD.ResponsibleObjectType = 'Resource' and FD.ResponsibleObjectID = {resourceID} and FD.ObjectType = '{type.Replace("'", "''")}' and FD.ObjectTypeID = {id} and FD.ObjectID = A.ID";

                        sql = $@"
select	A.ID,
		A.Name,
		A.Description,
		A.TextPath,
        {columns}
        FD.CurrentScore,
        {lastColumn}
from	Taxonomy A 
        {followOrOwnSql} and A.TaxonomyTypeID = {id} 
        {joins}";
                        break;
                    #endregion
                    default:
                        #region
                        sql = (follow) ? 
                            $"select  Name, TextPath, Description, TypeName, CurrentScore, OpenEventCount from FollowDetail where ResourceID = {resourceID} and Type = '{type.Replace("'", "''")}' and TypeID = {id}" :
                            $"select ObjectName as Name, ObjectTypeName as Type, [Role], ContextItems as [Context], CurrentScore from ResponsibilityDetailForResource FD where ResponsibleObjectType = 'Resource' and ResponsibleObjectID = {resourceID} and ObjectType = '{type.Replace("'", "''")}' and ObjectTypeID = {id}";
                        break;
                        #endregion
                }
            }

            return sql;
        }

        [Route("{resourceID:int}/following/{type}/{id:int}.xlsx")]
        public FileResult ExportFollowsByResourceByType(int resourceID, string type, int id)
        {
            var document = new SLDocument();
            document.AddWorksheet("Items");

            string sql = getSqlStatement(resourceID, type, id, true);

            if (!string.IsNullOrEmpty(sql))
            {
                // The data reader.
                var query = Company.Read(sql);
                var metafields = query.GetSchemaTable();

                #region Create the list sheet

                #region Header

                for (int i = 0; i < metafields.Rows.Count; i++)
                {
                    document.SetCellValue(1, i, (string)metafields.Rows[i]["ColumnName"]);
                }

                #endregion

                int r = 1;
                while (query.Read())
                {
                    r++;
                    for (int i = 0; i < metafields.Rows.Count; i++)
                    {
                        document.SetCellValue(r, i, query[i].ToString());
                    }
                }

                metafields = null;
                query.Dispose();

                #endregion
            }
            else
            {
                document.SetCellValue(1, 1, "Invalid value for type parameter. Please check your URI.");
            }

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"Followed Items as of {DateTime.Now.ToShortDateString()}.xlsx");
        }

        [Route("{resourceID:int}/ownership/{type}/{id:int}.xlsx")]
        public FileResult ExportResponsibilitiesByResourceByType(int resourceID, string type, int id)
        {
            var document = new SLDocument();
            document.AddWorksheet("Items");

            string sql = getSqlStatement(resourceID, type, id, false);

            if (!string.IsNullOrEmpty(sql))
            {
                // The data reader.
                var query = Company.Read(sql);
                var metafields = query.GetSchemaTable();

                #region Create the list sheet

                #region Header

                for (int i = 0; i < metafields.Rows.Count; i++)
                {
                    document.SetCellValue(1, i, (string)metafields.Rows[i]["ColumnName"]);
                }

                #endregion

                int r = 1;
                while (query.Read())
                {
                    r++;
                    for (int i = 0; i < metafields.Rows.Count; i++)
                    {
                        document.SetCellValue(r, i, query[i].ToString());
                    }
                }

                metafields = null;
                query.Dispose();

                #endregion
            }
            else
            {
                document.SetCellValue(1, 1, "Invalid value for type parameter. Please check your URI.");
            }

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"Owned Items as of {DateTime.Now.ToShortDateString()}.xlsx");
        }

        #endregion

        #region Partials

        [Route("templates/email")]
        public ActionResult _EmailTemplates()
        {
            return PartialView();
        }

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

        //[Route("{type}/{id:int}/tools")]
        //public ActionResult RenderToolsTooltip(SystemObjects type, int id, string templateAction)
        //{
        //    var toolbar = new ContextToolbar { ToolbarSuffix = string.Format("{0}{1}", type.ToString(), id) };

        //    var common = new ContextToolbarItem { Title = "Common Actions" };

        //    switch (type)
        //    {
        //        case SystemObjects.Intersect:
        //            if (Company.HasPermission(type, id, Claim.Create, ClaimObject.Relationship))
        //            {
        //                var intersect = Company.GetById<Intersect>(id, i => i.IntersectType);
        //                if (intersect != null)
        //                {
        //                    //if (intersect.IntersectType.AllowSourcing)
        //                    //{
        //                    //    common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionResponsibility, Icon = "plus", Title = "Select source", Type = "local", Uri = "/form/AddSourcingResponsibility?type=Intersect&id=" + id });
        //                    //}

        //                    var add = new ContextToolbarItem { Context = "null", Icon = "", Title = "Associate Child Items", Type = "local", Uri = "#" };
        //                    var types = Company.Query<AllowedIntersectionType>("GetAllowedIntersectionTypesByIntersect @intersectID", new { intersectID = id }).ToList();
        //                    foreach (var t in types)
        //                    {
        //                        add.Items.Add(
        //                            new ContextToolbarItem
        //                            {
        //                                Context = ContextList.ActionRelate,
        //                                Icon = "plus",
        //                                Title = t.TargetName,
        //                                Type = "local",
        //                                Uri = "/Relations/AddRelationship?source=Intersect&sourceID=" + t.ParentIntersectID + "&intersectTypeID=" + t.IntersectTypeID + "&target=" + t.TargetType + "&targetID=" + t.TargetTypeID
        //                            });
        //                    }
        //                    if (add.Items.Count > 0)
        //                    {
        //                        toolbar.Items.Add(add);
        //                    }
        //                }
        //                intersect = null;
        //            }
        //            if (Company.HasPermission(type, id, Claim.Update, ClaimObject.Relationship))
        //                common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionEditRelate, Icon = "pencil", Title = "Edit relationship", Type = "local", Uri = "/relations/EditRelationship?id=" + id });
        //            if (Company.HasPermission(type, id, Claim.Delete, ClaimObject.Relationship))
        //                common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionUnrelate, Icon = "trash-o", Method = "DELETE", Title = "Remove relationship", Type = "command", Uri = "/api/relationships/" + id });
        //            break;
        //        case SystemObjects.Responsibility:
        //            if (Company.HasPermission(type, id, Claim.Update, ClaimObject.Governance))
        //                common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionResponsibility, Icon = "pencil", Title = "Edit responsibility", Type = "local", Uri = "/form/EditSourcingResponsibility?id=" + id });
        //            if (Company.HasPermission(type, id, Claim.Delete, ClaimObject.Governance))
        //                common.Items.Add(new ContextToolbarItem { Context = ContextList.ActionResponsibility, Icon = "trash-o", Title = "Remove responsibility", Type = "local", Uri = "/form/DeleteResponsibility?&id=" + id });
        //            if (Company.HasPermission(type, id, Claim.Update, ClaimObject.Governance))
        //                common.Items.Add(new ContextToolbarItem { Context = ContextList.ResponsibilityTransformation, Icon = "plus", Title = "Add transformation", Type = "local", Uri = "/form/AddResponsibilityTransformation?responsibilityID=" + id });
        //                break;
        //    }

        //    if (common.Items.Count > 0)
        //    {
        //        toolbar.Items.Add(common);
        //    }

        //    return PartialView("RowCommandTooltip", toolbar); //Content(toolsHtml + html, "text/html");
        //}

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

        [Route("fieldvalues/{id:int}/{attribute:int}/templates/tooltip/preview")]
        public ContentResult RenderMultiFieldValueTooltip(int id, int attribute)
        {
            //get values for attribute and all attributes that have this as a parent for specified element
            StringBuilder sb = new StringBuilder();

            var fieldValues = Company.Query<dynamic>(@"
select	A.ID,
        FT.FriendlyName as Name,
		F.FormattedValue as Value
from	AttributeDetail A
		inner join Field F on F.ObjectID = A.ID and A.ObjectType = 'Intersect' and A.ObjectID = @id
		inner join FieldType FT on FT.Object = 'AttributeType' and FT.ObjectID = @attribute
order by A.ID, FT.SortOrder", new { id, attribute });

            int PreviousObjectID = -1;
            foreach(var val in fieldValues)
            {
                if (PreviousObjectID != val.ID && PreviousObjectID > 0) sb.Append("<div class='separator'>&nbsp;</div>");

                sb.Append("<b>" + val.Name + ":</b> ");
                sb.Append(val.Value);
                sb.Append("<br>");

                PreviousObjectID = val.ID;
            }

            return Content(sb.ToString(), "text/html");
        }

        [Route("Comment/Votes/{commentId:int}/templates/tooltip/{voteAction}")]
        public ContentResult _RenderCommentVoteTooltip(int commentId, string voteAction)
        {            
            var voteDirection = (voteAction ?? string.Empty).ToUpper() == "UP" ? 1 : -1;

            var voters = Company.Query<string>(@"   select 
	                                                    r.firstname + ' ' + r.lastname
                                                    from [dbo].[CommentVote] cv
	                                                    inner join [reporting].[global_resource] r on (r.resourceid = cv.resourceid)
                                                    where
	                                                    cv.commentid = @id
		                                                    and
	                                                    cv.vote = @vote", new { id = commentId, vote = voteDirection });

            if(!voters.Any())
                return Content("", "text/html");

            StringBuilder sb = new StringBuilder();

            foreach (var user in voters)
            {
                sb.Append(user);
                sb.Append("<br>");
            }

            return Content(sb.ToString(), "text/html");
        }

        [Route("{type}/{itemid:int}/templates/tooltip/{templateAction}")]
        public ContentResult _RenderTooltip(SystemObjects type, string itemid, string templateAction, bool isNg = false)
        {
            string html = "";

            if (type != SystemObjects.WorkflowTypeRelation)
            {
                int id = int.Parse(itemid);

                html = Company.RenderTooltip(templateAction, type, id, isNg);
                
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
            }
            else
            {
                //select resourceid from WorkflowResource  where workflowid = <> and iscomplete = 0
                var users = Company.Query<string>(@"select 
	                                                    R.FirstName + ' ' + R.LastName
                                                    from 
	                                                    WorkflowResource  wr
	                                                    inner join reporting.Global_Resource R on R.ResourceID = wr.ResourceID
                                                    where 
	                                                    workflowid = @wid 
		                                                    and 
	                                                    iscomplete = 0", new { wid = itemid });
                foreach (var user in users)
                {
                    if (!string.IsNullOrEmpty(html)) html += "<br>";
                    html += user;
                }
            }
            
            return Content(html, "text/html");
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


        [Route("referenceItems/{typeID:int}/items.json")]
        public JsonResult GetReferenceItems(int typeID)
        {
            return Json(Company.GetReferenceItemsAsDictionary(typeID), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult UpdateFollowStatus(SystemObjects type, int id, bool includeChildren = false)
        {
            try
            {
                var sType = type.ToString();
                var f = Company.Filter<FollowDetail>(i => i.ObjectID == id && i.ObjectType == sType && i.ResourceID == Company.CurrentResourceID).FirstOrDefault();
                if (f != null)
                {
                    if (!f.HardFollow)
                    {
                        return Json(new { title = "Error!", message = $"You are currently following this item's parent.  You may not unfollow this item.", type = "error" });
                    }
                }
                bool status = Company.UpdateFollowStatus(type, id, null, includeChildren);
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
