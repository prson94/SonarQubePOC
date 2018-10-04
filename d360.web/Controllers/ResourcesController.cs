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
using d360.web.Models.Attributes;
using d360.web.Filters;
using Dapper;

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

    public class TooltipFieldLevelPathModel
    {
        public string Path { get; set; }
        public string LevelName { get; set; }
        public string Url { get; set; }
        public int Level { get; set; }
    }

    public class FieldTooltipValueModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
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

        [HttpGet, ValidateContracts(Ignore = true), Route("image/{id:int}")]
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
            
            return new RedirectResult($"https://secure.gravatar.com/avatar/{sBuilder.ToString()}?s={size}&d=mm");
        }

        [HttpGet, Route("image/me")]
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

        [HttpGet, Route("{resourceID:int}/following/{type}/{id:int}.xlsx")]
        public FileResult ExportFollowsByResourceByType(int resourceID, string type, int id)
        {
            var document = new SLDocument();
            document.AddWorksheet("Items");

            string sql = @"
select	TextPath as [Path],
		A.ID as AssetID
from	FollowDetail F
		inner join Asset A on A.Object = F.ObjectType and A.ObjectID = F.ObjectID and F.ResourceID = @r
		and F.[Type] = @type
		and F.TypeID = @id";

            var query = Company.Query<dynamic>(sql, new { r = resourceID, type, id });

            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 0, "Asset ID");
            document.SetCellValue(1, 1, "Asset Path");

            #endregion

            int r = 1;
            foreach (var item in query)
            {
                r++;
                document.SetCellValue(r, 0, item.AssetID);
                document.SetCellValue(r, 1, item.Path);
            }

            query = null;

            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"Followed Items as of {DateTime.Now.ToShortDateString()}.xlsx");
        }

        [HttpGet, Route("{resourceID:int}/ownership/{type}/{id:int}.xlsx")]
        public FileResult ExportResponsibilitiesByResourceByType(int resourceID, string type, int id, int? responsibilityTypeId = null)
        {
            var document = new SLDocument();
            document.AddWorksheet("Items");

            string sql = $@"
        select 
			RD.ResponsibilityTypeName as ResponsibilityType,
		    A.ID as AssetID,
            TP.TextPath as [Path],
		    case RD.SecurityAsset
			    when 'G' then 'Via Group'
			    when 'O' then 'Via Organization'
			    else ''
		    end as Via
		from 
		    ResponsibilityDetail RD 
		    inner join AssetType T on T.ObjectID = RD.TypeID and T.Object = RD.Type and T.Object = @type and T.ObjectID = @id
		    inner join Asset A on A.AssetTypeID = T.ID
            cross apply [dbo].GetAssetTextPathById(A.ID, ' / ') TP
		where {(responsibilityTypeId.HasValue && responsibilityTypeId > 0 ? " ResponsibilityTypeID = @responsibilityTypeId and " : "")} 
            ResourceID = @resourceID and AssetID = 0 and ApplyToType = 1 and RD.IsVisible = 1
		
		union all

		select	
		        RD.ResponsibilityTypeName as ResponsibilityType,
		        RD.AssetID as AssetID,
                TP.TextPath as [Path],
		        case RD.SecurityAsset
			        when 'G' then 'Via Group'
			        when 'O' then 'Via Organization'
			        else ''
		        end as Via
        from	ResponsibilityDetail RD
                cross apply [dbo].GetAssetTextPathById(RD.AssetID, ' / ') TP
		        inner join AssetType T on T.Object = RD.Type and T.ObjectID = RD.TypeID and RD.ResourceID = @resourceID and T.Object = @type and T.ObjectID = @id
        where  {(responsibilityTypeId.HasValue && responsibilityTypeId > 0 ? " ResponsibilityTypeID = @responsibilityTypeId and " : "")} 
            RD.AssetID != 0 and RD.ApplyToType = 0 and RD.IsVisible = 1";

            var query = Company.Query<dynamic>(sql, new { resourceID, type = new DbString { Value = type, IsFixedLength = true, Length = 20, IsAnsi = true }, id, responsibilityTypeId });

            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "Role");
            document.SetCellValue(1, 2, "Asset ID");
            document.SetCellValue(1, 3, "Name");
            document.SetCellValue(1, 4, "Via");

            #endregion

            int r = 1;
            foreach(var item in query)
            {
                r++;
                document.SetCellValue(r, 1, item.ResponsibilityType);
                document.SetCellValue(r, 2, item.AssetID);
                document.SetCellValue(r, 3, item.Path);
                document.SetCellValue(r, 4, item.Via);
            }

            query = null;

            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"Owned Items as of {DateTime.Now.ToShortDateString()}.xlsx");
        }

        #endregion

        #region Partials

        string buttonHtml(string buttonType, string context, string uri, string icon, string title, string method = "")
        {
            string methodAttribute = string.IsNullOrEmpty(method) ? "" : string.Format(" data-method='{0}'", method);
            return string.Format("<button type='button' data-{0} data-context='{1}' data-uri='{2}'{5} class='btn btn-default' title='{4}'><i class='fa fa-{3}'></i></button>", buttonType, context, uri, icon, title, methodAttribute);
        }

        [HttpGet, Route("complexvalue/{id:int}/{attribute:int}/templates/tooltip/preview")]
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

        [HttpGet, Route("fieldvalues/{id:int}/{attribute:int}/templates/tooltip/preview")]
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

        [HttpGet, Route("Comment/Votes/{commentId:int}/templates/tooltip/{voteAction}")]
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

        [HttpGet, Route("{type}/{itemid}/templates/tooltip/{templateAction}")]
        public ContentResult _RenderTooltip(SystemObjects type, string itemid, string templateAction)
        {
            string html = "";

            if (type != SystemObjects.WorkflowTypeRelation)
            {
                int id = int.Parse(itemid);

                html = Company.RenderTooltip(templateAction, type, id);
                
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
                    else html += "<b>Assigned To:</b><br>";
                    html += user;
                }
            }

            //replace angular special characters
            html = (html ?? "").Replace('{', '(').Replace('}', ')');

            return Content(html, "text/html");
        }

        #endregion

        #region Json

        [HttpGet, Route("HelpResources")]
        public JsonNetResult GetHelpResources()
        {
            var showDefaultVideoSetting = Community.Filter<CompanySetting>(i => i.CompanyID == Community.CurrentCompanyID && i.SettingID == 35).FirstOrDefault();

            var showDefaultVideo = (showDefaultVideoSetting != null) ? bool.Parse(showDefaultVideoSetting.Value) : true;

            var resources = Community
                .Filter<CompanyHelpResource>(
                    i => i.CompanyID == Community.CurrentCompanyID || (showDefaultVideo && i.CompanyID == 0),
                    i => i.HelpResource)
                .OrderBy(i => i.HelpResource.Type)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.HelpResource)
                .ToList();

            return new JsonNetResult
            {
                Data = resources,
                Formatting = Formatting.None
            };
        }

        [HttpGet, Route("_GroupsByResourceID"), NonNullableParameters]
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

        [HttpGet, Route("_Lookups"), NonNullableParameters]
        public JsonResult _Lookups(string sortDataField, string sortOrder, int pagenum = 0, int pagesize = 10)
        {
            if (!Company.CurrentResourceIsAdmin)
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }
            
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

        [HttpGet, Route("lookups/{typeID:int}/items.json")]
        public JsonResult GetLookupItems(int typeID)
        {
            return Json(Company.GetLookupItemsAsDictionary(typeID), JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost, Route("UpdateFollowStatus"), NonNullableParameters, AjaxValidateAntiForgeryToken]
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

        [HttpGet, Route("TooltipData/{objectType}/{objectID:int}")]
        public JsonResult GetTooltipData(int objectID, string objectType)
        {
            try
            {
                bool show = false;
                List<FieldTooltipValueModel> res = new List<FieldTooltipValueModel>();
                List<TooltipFieldLevelPathModel> levels = new List<TooltipFieldLevelPathModel>();
                string desc = "";
                string dispName = "";
                string typeName = "";
                ObjectDetail det = null;

                if (Company.HasAssetDefaultReadPermission(objectType, objectID))
                {
                    det = Company.GetObjectDetail(objectType, objectID);

                    if (det == null)
                        throw new NotFoundException(objectType);

                    show = true;

                    var sql = @"select 
                                f.FormattedValue as [Value],
	                            ft.FriendlyName as Name
                            from
                                fieldtype ft

                                inner
                            join field f on (ft.id = f.fieldtypeid and f.[objecttype] = @ty and f.objectid = @obj and ft.Name != 'Description')";

                    res = Company.Query<FieldTooltipValueModel>(sql, new { ty = objectType, obj = objectID }).ToList();

                    var descSql = @"select 
                                f.FormattedValue as [Value]	                            
                            from
                                fieldtype ft

                                inner
                            join field f on (ft.id = f.fieldtypeid and f.[objecttype] = @ty and f.objectid = @obj and ft.Name = 'Description')";

                    desc = Company.Query<string>(descSql, new { ty = objectType, obj = objectID }).FirstOrDefault();

                     dispName = det != null ? det.Name : "";
                     typeName = det != null ? det.TypeName : "";

                    if (objectType == "TaxonomyType")
                    {
                        var desSql = @"Select Description from AssetType where objectid =@id and object= @ty";
                        desc = Company.Query<string>(desSql, new { ty = objectType, id = objectID }).FirstOrDefault();
                        typeName = "Model Type";
                    }
                    else  if (objectType == "Fusion")
                    {
                        var fusionSql = @"select f.name from fusion f 
                                            where f.id=@id";
                        var fusionName = Company.Query<string>(fusionSql, new { id = objectID }).FirstOrDefault();
                        dispName = fusionName;
                    }
                    else if ((objectType == "Artifact") || (objectType == "Taxonomy") || (objectType == "FusionAttribute"))
                    {
                        string query = string.Format("[dbo].[GetAssetHierarchy] {0}, '{1}'", objectID, objectType);
                        levels = Company.Query<TooltipFieldLevelPathModel>(query).ToList<TooltipFieldLevelPathModel>();
                    }
                    else if (objectType == "ResponsibilityType")
                    {
                        typeName = "Responsibility Type";
                        
                        var responbility = Company.GetById<ResponsibilityType>(objectID);
                        dispName = responbility?.Name;
                        desc = responbility?.Description;
                    }
                }

                return Json(
                    new
                    {
                        ShowTooltip = show,
                        AssetID = (det != null ? det.AssetID : -1),
                        UID = (det != null ? det.UID.ToString() : null),
                        DisplayName = dispName,
                        TypeName = typeName,
                        Url = ((det != null && det.Url != null) ? $"/{det.Url}" : ""),
                        Levels = levels,
                        FieldValues = res,
                        Description = desc
                    },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { title = "Error Occurred!", message = ex.Message, type = "error" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet, Route("LookupTooltipData/{objectType}/{objectID:int}")]
        public JsonResult GetLookupTooltipData(int objectID, SystemObjects objectType)
        {
            try
            {
                var html = Company.RenderTooltip("LookupPreview", objectType, objectID);


                return Json(new { html = html }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { title = "Error Occurred!", message = ex.Message, type = "error" }, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion
    }
}
