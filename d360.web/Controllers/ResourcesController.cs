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
using Resources;

namespace d360.web.Models
{
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
            foreach (var item in query)
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

        #region Resources

        private string getDynamicFieldSimpleFilter(string[] fixedColumns, SystemObjects type, int typeID, string filterExp, Dapper.DynamicParameters dbArgs, List<FieldType> fields = null)
        {
            if (string.IsNullOrEmpty(filterExp)) return "";

            if (fields == null)
            {
                fields = Company.Filter<FieldType>(i => i.Object == type.ToString() && i.ObjectID == typeID && i.IsListable).OrderBy(i => i.ColumnOrder).ToList();
            }

            StringBuilder sb = new StringBuilder();

            foreach (var column in fixedColumns)
            {
                if (sb.Length != 0) sb.Append(" or ");

                sb.Append($"({column} like  '%'+ @simpleFilter + '%')");
            }

            foreach (var field in fields)
            {
                if (sb.Length != 0) sb.Append(" or ");
                if (!string.IsNullOrEmpty(field.DefaultFormattedValue))
                    sb.Append($"(coalesce(Field{field.ID}_T.FormattedValue, '{field.DefaultFormattedValue}') like @simpleFilter )");
                else
                    sb.Append($"(Field{field.ID}_T.Value like  '%'+ @simpleFilter + '%')");

            }

            var val = new Dapper.DbString { Value = filterExp.Replace('*', '%').Replace('?', '_'), Length = 200 };

            dbArgs.Add("simpleFilter", val);

            return $"({sb.ToString()})";
        }


        [HttpGet, Route("{typeId:int}/lazy")]
        public JsonNetResult GetResourcesLazy(int typeId, int pagenum, int pagesize, string sortDataField, string sortOrder, string simpleFilter)
        {
            try
            {
                var settings = Community.GetCompanySettings();
                //check that current user is an admin or the company settings allow users to be listed
                if (!Company.CurrentResourceIsAdmin && (settings["ShowResources"] ?? "").ToUpper() != "TRUE")
                {
                    Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return null;
                }

                var joins = "";
                var columns = "";
                getDynamicFieldJoinStatements(typeId, "Resource", out joins, out columns, false, false);

                var hideData3SixtySql = string.Empty;
                if (HideData3SixtyUsers())
                {
                    hideData3SixtySql += " and (Email not like '%@data3sixty.com' and Email not like '%@infogix.com')";
                }

                var querySql = $@"
                    select  A.FirstName,
		                    A.LastName,
                            A.Email,
		                    A.LastLoggedInOn,
                            A.[State],
                            A.IsAdministrator,
                            {columns}
		                    A.ID,
                            A.ID as ResourceID,
                            A.FirstName + ' ' + A.LastName as FullName 
                    from    (
                            select	FirstName,
		                            LastName,
                                    Email,
		                            LastLoggedInOn,
                                    case State when 1 then 'Active' else 'Inactive' end as [State],
                                    case IsAdministrator when 1 then 'True' else 'False' end as [IsAdministrator],
                                    ResourceID as ID
                            from	reporting.Global_Resource
                                    where State <> @excludeStatus {hideData3SixtySql}
                            ) A 
                            {joins}";

                var countSql = string.Empty;
                var sql = string.Empty;

                var dbArgs = new DynamicParameters();
                dbArgs.Add("excludeStatus", CompanyResourceState.Deleted);

                if (!string.IsNullOrEmpty(simpleFilter))
                {
                    string[] fixedColumns = { "FirstName", "LastName", "Email", "IsAdministrator", "LastLoggedInOn", "State" };
                    var filter = getDynamicFieldSimpleFilter(fixedColumns, SystemObjects.ResourceType, typeId, simpleFilter, dbArgs, null);
                    countSql = string.Format(@"select count(1) from ({0} where {1}) AA", querySql, filter);
                    sql = string.Format(@"select * from ({0} where {1} ) AA ", querySql, filter);
                }
                else
                {
                    countSql = string.Format(@"select count(1) from ({0}) AA", querySql);
                    sql = string.Format(@"select * from ({0}) AA", querySql);
                    countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
                    sql = applyFilteringSuffixBind(sql, Request, dbArgs);
                }

                int total = Company.Query<int>(countSql, dbArgs).First();

                sql = applySortSuffix(sql, sortDataField, sortOrder, "FirstName", "asc", sortFieldType: "string");
                sql = applyPagingSuffix(sql, pagenum, pagesize);

                var query = Company.Query<dynamic>(sql, dbArgs);

                return new JsonNetResult { Data = new { total, results = query }, Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (System.Exception ex)
            {
                return jsonNetException(ex);
            }
        }


        [HttpGet, Route("{typeID:int}/lazy/excel")]
        public FileResult GetResourcesExcel(int typeId, string sortDataField, string sortOrder, string simpleFilter)
        {

            var settings = Community.GetCompanySettings();
            //check that current user is an admin or the company settings allow users to be listed
            if (!Company.CurrentResourceIsAdmin && (settings["ShowResources"] ?? "").ToUpper() != "TRUE")
            {
                Response.StatusCode = (int)HttpStatusCode.Forbidden;
                return null;
            }

            var joins = "";
            var columns = "";
            var hideData3SixtySql = string.Empty;
            if (HideData3SixtyUsers())
            {
                hideData3SixtySql += " and (Email not like '%@data3sixty.com' and Email not like '%@infogix.com')";
            }
            getDynamicFieldJoinStatements(typeId, "Resource", out joins, out columns, false, false);

            var querySql = $@"
                    select  A.FirstName,
		                    A.LastName,
                            A.Email,
		                    A.LastLoggedInOn,
                            A.[State],
                            A.IsAdministrator,
                            {columns}
		                    A.ID,
                            A.ID as ResourceID,
                            A.FirstName + ' ' + A.LastName as FullName 
                    from    (
                            select	FirstName,
		                            LastName,
                                    Email,
		                            LastLoggedInOn,
                                    case State when 1 then 'Active' else 'Inactive' end as [State],
                                    case IsAdministrator when 1 then 'True' else 'False' end as [IsAdministrator],
                                    ResourceID as ID
                            from	reporting.Global_Resource
                                    where State <> @excludeStatus {hideData3SixtySql}
                            ) A 
                            {joins}";



            if (HideData3SixtyUsers())
            {
                querySql += " where (A.Email not like '%@data3sixty.com' and A.Email not like '%@infogix.com')";
            }

            var sql = string.Format(@"select * from ({0}) AA", querySql);

            var dbArgs = new DynamicParameters();
            dbArgs.Add("excludeStatus", CompanyResourceState.Deleted);

            if (!string.IsNullOrEmpty(simpleFilter))
            {
                string[] fixedColumns = { "FirstName", "LastName", "Email", "IsAdministrator", "LastLoggedInOn", "State" };
                var filter = getDynamicFieldSimpleFilter(fixedColumns, SystemObjects.ResourceType, typeId, simpleFilter, dbArgs, null);
                sql = string.Format(@"select * from ({0} where {1} ) AA ", querySql, filter);
            }
            else
            {
                sql = applyFilteringSuffixBind(sql, Request, dbArgs);
            }

            sql = applySortSuffix(sql, sortDataField, sortOrder, "FirstName", "asc", sortFieldType: "string");


            var query = Company.Query<dynamic>(sql, dbArgs);

            var items = Company.Filter<FieldType>(i => i.Object == SystemObjects.ResourceType.ToString() && i.ObjectID == typeId && i.IsListable).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();

            var fields = new List<GridColumn>();
            fields.Add(new GridColumn { text = d360.core.resources.Fields.FirstName_Name, datafield = "FirstName", columntype = "string" });
            fields.Add(new GridColumn { text = d360.core.resources.Fields.LastName_Name, datafield = "LastName", columntype = "string" });
            fields.Add(new GridColumn { text = d360.core.resources.Fields.Email_Name, datafield = "Email", columntype = "string" });
            items.ForEach(
                i =>
                {
                    fields.Add(new GridColumn { text = i.Name, datafield = $"Field{i.ID}", columntype = getGridFieldTypeForColumn(i) });
                }
                );
            fields.Add(new GridColumn { text = d360.core.resources.Fields.LastLoggedInOn_Name, datafield = "LastLoggedInOn", columntype = "date" });
            fields.Add(new GridColumn { text = "Administrator?", datafield = "IsAdministrator", columntype = "bool" });
            fields.Add(new GridColumn { text = d360.core.resources.Fields.Status_Name, datafield = "State", columntype = "string" });

            var document = new SLDocument();
            document.AddWorksheet("Users");

            #region Header

            int colIndex = 1;
            int rowIndex = 1;
            foreach (var f in fields)
            {
                document.SetCellValue(rowIndex, colIndex, f.text);
                colIndex++;
            }

            #endregion

            #region Detail Records
            foreach (var row in query)
            {
                rowIndex++;
                colIndex = 1;
                int i = 0;
                foreach (var f in fields)
                {
                    var val = (((row as IDictionary<string, object>)[$"{f.datafield}"]) ?? "").ToString();
                    SetCellValue(document, rowIndex, colIndex, f.columntype, val);
                    colIndex++;
                }
            }



            #endregion

            var stream = new MemoryStream();
            document.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.ms-excel", $"Users {System.DateTime.Now.ToShortDateString()}.xlsx");
        }

        private string getGridFieldTypeForColumn(FieldType item)
        {
            string fieldType = "string";

            switch (item.Type)
            {
                case "Date":
                    fieldType = "date";
                    break;
                case "DateTime":
                    fieldType = "date";
                    break;
                case "Number":
                    fieldType = "number";
                    break;
                case "Decimal":
                    fieldType = "number";
                    break;
                case "Boolean":
                    fieldType = "bool";
                    break;
                case "Html":
                    fieldType = "html";
                    break;
                case "Link":
                    fieldType = "html";
                    break;
            }

            return fieldType;
        }
        private void SetCellValue(SLDocument document, int rowIndex, int colIndex, string dataType, object value)
        {
            var valueString = value?.ToString() ?? "";
            switch (dataType.ToUpper())
            {
                case "DECIMAL":
                    double dVal = 0;
                    if (double.TryParse(valueString, out dVal))
                        document.SetCellValue(rowIndex, colIndex, dVal);
                    else
                        document.SetCellValue(rowIndex, colIndex, valueString);
                    break;
                case "NUMBER":
                    int intVal = 0;
                    if (int.TryParse(valueString, out intVal))
                        document.SetCellValue(rowIndex, colIndex, intVal);
                    else
                        document.SetCellValue(rowIndex, colIndex, valueString);
                    break;
                case "DATE":
                    if (DateTime.TryParse((value ?? "").ToString(), out DateTime dateVal))
                    {
                        document.SetCellValue(rowIndex, colIndex, dateVal);

                        SLStyle style = document.CreateStyle();
                        style.FormatCode = "m/d/yyyy";
                        document.SetCellStyle(rowIndex, colIndex, style);
                    }
                    break;
                default:
                    var doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(value + "");
                    var txt = HtmlAgilityPack.HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
                    if (txt.StartsWith("="))
                        txt = "'" + txt;
                    document.SetCellValue(rowIndex, colIndex, txt);
                    break;
            }
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

            return Content(sb.ToString(), "text/html");
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
            foreach (var val in fieldValues)
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

            if (!voters.Any())
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
                    var resource = Company.Filter<GlobalReportingResource>(i => i.ResourceID == id).FirstOrDefault();
                    if (resource != null)
                    {
                        var fields = Company.Filter<FieldWithRelation>(i => i.ObjectType == "Resource" && i.ObjectID == id).ToDictionary(k => k.Name, var => var.FormattedValue);
                        fields.Add("Name", resource.FullName);
                        fields.Add("FirstName", resource.FirstName);
                        fields.Add("LastName", resource.LastName);
                        fields.Add("LastLoggedInOn", resource.LastLoggedInOn.HasValue ? resource.LastLoggedInOn.Value.ToString("MM/dd/yyyy HH:mm:ss") : "Never");
                        fields.Add("Email", resource.Email);
                        fields.Add("Status", resource.State.ToString());
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

            var items = list
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
                string uid = "";
                ObjectDetail det = null;

                if (Company.HasAssetDefaultReadPermission(objectType, objectID))
                {
                    det = Company.GetObjectDetail(objectType, objectID);

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
                    uid = det?.UID?.ToString() ?? "";

                    if (objectType == "TaxonomyType")
                    {
                        var desSql = @"Select Description from AssetType where objectid =@id and object= @ty";
                        desc = Company.Query<string>(desSql, new { ty = objectType, id = objectID }).FirstOrDefault();
                        typeName = "Model Type";
                    }
                    else if (objectType == "Fusion")
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
                    else if (objectType == "Predicate")
                    {
                        typeName = "Predicate";
                        var predicate = Company.GetById<Predicate>(objectID);

                        dispName = predicate == null ? "" : ($"{predicate.Name} / {predicate.Inverse}");
                        uid = predicate?.UID.ToString();
                    }
                }

                return Json(
                    new
                    {
                        ShowTooltip = show,
                        AssetID = (det != null ? det.AssetID : -1),
                        UID = string.IsNullOrEmpty(uid) ? null : uid,
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

        [Route("MyApiCredentials")]
        public JsonNetResult MyApiCredentials()
        {
            if (!Company.CurrentResourceIsAdmin && !this.ShowAllUsersAPIKey())
                return jsonNetException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

            var resource = Community.GetById<Resource>(Community.CurrentResourceID);

            return new JsonNetResult
            {
                Data = new
                {
                    PublicKey = resource.APIPublicKey,
                    PrivateKey = resource.APIPrivateKey
                },
                Formatting = Newtonsoft.Json.Formatting.None

            };
        }

        #endregion
    }
}
