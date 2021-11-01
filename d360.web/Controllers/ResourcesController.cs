using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using d360.model;
using d360.web.Models;
using d360.core;
using System.Xml.Linq;
using d360.core.entities;
using System.Text;
using Newtonsoft.Json;
using d360.core.enums;
using d360.core.entities.Views;
using d360.web.Models.Attributes;
using d360.web.Filters;
using Dapper;
using System.Threading.Tasks;
using d360.model.DataAccessLayer;
using d360.utils.excel;
using d360.core.resources;

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
        public string Type { get; set; }
        public string Description { get; set; }
        public List<string> Values { get; set; }
    }
}


namespace d360.web.Controllers
{
    [RoutePrefix("resources"), Authorize]
    public class ResourcesController : BaseController
    {
        #region DI

        public ResourcesController(CoreComponentSet set): base(set)
        { }

        #endregion

        #region Exports

        [HttpGet, Route("{resourceID:int}/following/{type}/{id:int}.xlsx")]
        public FileResult ExportFollowsByResourceByType(int resourceID, string type, int id)
        {
            string sql = @"
select	TextPath as [Path],
		A.ID as AssetID
from	FollowDetail F
		inner join Asset A on A.Object = F.ObjectType and A.ObjectID = F.ObjectID and F.ResourceID = @r
		and F.[Type] = @type
		and F.TypeID = @id";

            var query = Company.Query<dynamic>(sql, new { r = resourceID, type, id });

            var document = new ExcelDocument(string.Format(ExcelExports.FollowedResources_DocumentName, DateTime.Now))
            {
                new ExcelSheet(ExcelExports.Common_ItemsSheetName)
                {
                    HeaderRows = {
                        new ExcelRow()
                        {
                            ExcelExports.FollowedResources_AssetID,
                            ExcelExports.FollowedResources_AssetPath
                        }
                    },

                    ValueRows = query.Select(row => new ExcelRow
                    {
                        row.AssetID,
                        row.Path
                    }).ToList()
                }
            };

            return ExcelDocumentAsFile(document);
        }

        [HttpGet, Route("{resourceID:int}/ownership/{type}/{id:int}.xlsx")]
        public FileResult ExportResponsibilitiesByResourceByType(int resourceID, string type, int id, int? responsibilityTypeId = null, Guid? responsibilityTypeUid = null)
        {
            if (!responsibilityTypeId.HasValue && responsibilityTypeUid.HasValue && responsibilityTypeUid != Guid.Empty)
            {
                responsibilityTypeId = Company.ResponsibilityTypes.Where(t => t.UID == responsibilityTypeUid).Select(t => t.ID).FirstOrDefault();
            }

            string sql = $@"
        select 
			RD.ResponsibilityTypeName as ResponsibilityType,
		    A.ID as AssetID,
            TP.TextPath as [Path],
		    case RD.SecurityAsset
			    when 'G' then 'Via Group'
			    when 'O' then 'Via Organization'
			    else ''
		    end as ViaType,
            A.Uid as UID,
            RD.SecurityAssetName as Via
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
		        end as ViaType,
                A.Uid as UID,
                RD.SecurityAssetName as Via
        from	ResponsibilityDetail RD
                cross apply [dbo].GetAssetTextPathById(RD.AssetID, ' / ') TP
                inner join Asset A on A.ID = RD.AssetID
		        inner join AssetType T on T.Object = RD.Type and T.ObjectID = RD.TypeID and RD.ResourceID = @resourceID and T.Object = @type and T.ObjectID = @id
        where  {(responsibilityTypeId.HasValue && responsibilityTypeId > 0 ? " ResponsibilityTypeID = @responsibilityTypeId and " : "")} 
            RD.AssetID != 0 and RD.ApplyToType = 0 and RD.IsVisible = 1";

            var query = Company.Query<dynamic>(sql, new { resourceID, type = new DbString { Value = type, IsFixedLength = true, Length = 20, IsAnsi = true }, id, responsibilityTypeId });

            var document = new ExcelDocument(string.Format(ExcelExports.OwnedResources_DocumentName, DateTime.Now))
            {
                new ExcelSheet(ExcelExports.Common_ItemsSheetName)
                {
                    HeaderRows = {
                        new ExcelRow()
                        {
                            ExcelExports.OwnedResources_Role,
                            ExcelExports.OwnedResources_Name,
                            ExcelExports.OwnedResources_Via,
                            ExcelExports.OwnedResources_ViaType,
                            ExcelExports.OwnedResources_AssetUID,
                            ExcelExports.OwnedResources_AssetID
                        }
                    },

                    ValueRows = query.Select(row => new ExcelRow
                    {
                        row.ResponsibilityType,
                        row.Path,
                        row.Via,
                        row.ViaType,
                        row.UID.ToString(),
                        row.AssetID
                    }).ToList()
                }
            };

            return ExcelDocumentAsFile(document);
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

        #endregion

        #region Partials


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
        public async Task<JsonNetResult> GetHelpResources()
        {
            var sql = "select ID, uid, Name, Description, Url, SortIndex from helpresource order by sortindex";
            var resources = await Company.QueryAsync<HelpResource>(sql);

            return new JsonNetResult
            {
                Data = resources,
                Formatting = Formatting.None
            };
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

        [HttpGet, Route("TooltipData/{objectType}/{objectID}")]
        public JsonResult GetTooltipData(SystemObjects objectType, string objectID)
        {
            try
            {
                Guid uid = Guid.Parse(objectID);
                int objectId = Company.GetObjectId(uid, objectType);
                return GetTooltipData(objectId, objectType.ToString());
            }
            catch (Exception ex)
            {
                return Json(new { title = "Error Occurred!", message = ex.Message, type = "error" }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet, Route("tooltipdatabyuid/{uid}")]
        public JsonResult GetTooltipDataByUid(Guid uid)
        {
            var queryParams = Request.QueryString;

            var objectType = queryParams.Get("objecttype");
            if (objectType != null)
            {
                if (objectType == "Allocation")
                {
                    var alloc = Company.MetricAllocations.FirstOrDefault(x => x.Uid == uid);
                    return Json(
                    new
                    {
                        ShowTooltip = true,
                        AssetID = -1,
                        UID = alloc.Uid,
                        DisplayName = "",
                        TypeName = "",
                        Url = "",
                        Description = ""

                    },
                    JsonRequestBehavior.AllowGet);
                }

                if (objectType == "ResponsibilityType")
                {
                    var responbility = Company.ResponsibilityTypes.FirstOrDefault(x => x.UID == uid);
                    return Json(
                    new
                    {
                        ShowTooltip = true,
                        AssetID = -1,
                        UID = uid,
                        DisplayName = responbility?.Name,
                        TypeName = "Responsibility Type",
                        Url = "",
                        Description = responbility?.Description

                    },
                    JsonRequestBehavior.AllowGet);
                }

            }


            var asset = Company.Assets.FirstOrDefault(x => x.uid == uid);
            if (asset != null)
                return GetTooltipData(asset.ObjectID, asset.Object);

            var assetType = Company.AssetTypes.FirstOrDefault(x => x.uid == uid);
            if (assetType != null)
                return GetTooltipData(assetType.ObjectID, assetType.Object, assetType.Class);

            else return Json(
                    new
                    {
                        ShowTooltip = false,
                        AssetID = -1,
                        UID = "",
                        DisplayName = "",
                        TypeName = "",
                        Url = "",
                        Description = ""

                    },
                    JsonRequestBehavior.AllowGet);
        }

        [HttpGet, Route("TooltipData/{objectType}/{objectID:int}")]
        public JsonResult GetTooltipData(int objectID, string objectType, AssetTypeClass? assetTypeClass = null)
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
                string workflowTypeUid = "";
                string workflowVersionUid = "";

                if (Company.HasAssetPermission(objectType, objectID, Permission.ReadAsset))
                {
                    det = Company.GetObjectDetail(objectType, objectID);

                    show = true;

                    if (objectType == "Issue")
                    {
                        //For Tooltip data for Issues, we want multivalue fields separated out in an array of each separate value
                        //for use on the workflow monitor page 
                        //We'll maintain the compound comma separated value in "Value" for compatability with other pages
                        var sql = @"select ft.objectId as IssueId,
                               f.FormattedValue as [Value],
                               ft.FriendlyName as Name,
                               ft.[Type] as [Type],
                               ft.AllowMultipleValues,
                               f.Value as OriginalValue,
                               ft.ID as FieldTypeID,
                               ft.[DisplayDescription] as Description
                            from
                                fieldtype ft

                                inner
                            join field f on (ft.id = f.fieldtypeid and f.[objecttype] = @ty and f.objectid = @obj and ft.Name != 'Description')";
                        int issueId = 0;

                        List<dynamic> issueRes = Company.Query<dynamic>(sql, new { ty = objectType, obj = objectID }).ToList();
                        issueRes.ForEach((item) =>
                        {
                            issueId = item.IssueId;
                            FieldTooltipValueModel resItem = new FieldTooltipValueModel { Name = item.Name, Value = item.Value, Type = item.Type, Description = item.Description };
                            if (item.AllowMultipleValues)
                            {
                                var items = ((item.OriginalValue != null) ? item.OriginalValue.Split(',') : new string[] { });
                                var itemIds = new List<long>();

                                foreach (var iditem in items)
                                {
                                    if (long.TryParse(iditem, out long listId)) itemIds.Add(listId);
                                }
                                //If we only have one value, then we have no reason to perform an additional lookup. item.Value will suffice
                                if (itemIds.Count > 1)
                                {
                                    resItem.Values = Company.Query<string>(@"select Text from fieldlookupvalue where fieldtypeid = @fId and value in @vals order by Text", new { fId = item.FieldTypeID, vals = itemIds }).ToList();

                                }
                            }
                            res.Add(resItem);
                        });
                        var fieldTypes = Company.Filter<FieldType>(i => i.Object == "IssueType" && i.ObjectID == issueId && i.IsDisplayable && i.Name != "Description" && i.ShowIfEmpty).OrderBy(i => i.ColumnOrder).ToList();
                        var f = fieldTypes.Where(x => !res.Any(y => y.Name == x.FriendlyName)).ToList();
                        f.ForEach(x =>
                        {
                            res.Add(new FieldTooltipValueModel { Name = x.FriendlyName, Value = " ", Type = x.Type });
                        });


                    }
                    else if (det != null)
                    {

                        var sql = @"
with FieldValueDet as (
select  FT.ColumnOrder,
        COALESCE(Color.value,Counter.Val,FormattedValue,' ') as Value,
	    F.FriendlyName as Name,
        case 
		    when Color.value is not null then 'Color' 
		    else F.[Type] 
		end as 'Type'
from    FieldDetail  F
inner join fieldType FT on FT.ID = F.FieldTypeID
outer apply(
        select value = (
             SELECT
			 CASE
				WHEN (FT.AllowMultipleValues = 0) THEN COALESCE(Fi.FormattedValue, ADV.DisplayValue, AC.Code)
				ELSE COALESCE(ADV.DisplayValue, AC.Code)
			 END as name,
             COALESCE(JSON_VALUE(ACJ.ColorJSON, '$.Value'), '{{emptycolor}}') as color
             FROM field fi
             cross apply STRING_SPLIT(F.Value, ',') SPFfi
             inner join Asset AC on AC.Object = FT.LookupObjectType and AC.ObjectID = try_cast(SPFfi.value as int)
             cross apply dbo.GetAssetColorJsonByColor(AC.Color) ACJ
             cross apply GetAssetDisplayValueByID(AC.ID) ADV
             where FieldTypeID = F.fieldTypeID and fi.AssetID = F.AssetID and FT.Type = 'Lookup'
			for json path)
		)Color(value)
outer apply(
select top 1 ISNULL(FT.CounterPrefix,'') + cast(fcv.value as nvarchar(20) )
                from fieldcountervalue fcv
				inner join Asset A on a.Object =@o and A.ObjectID=@oid
                where fcv.AssetId=a.ID and fcv.FieldTypeId=ft.id
)Counter(val)
where   F.[Object]= @o and F.ObjectID = @oid and F.[Name] != 'Description' and F.[Type] not in ('JsonElement', 'Score', 'Path')
union
select  FT.ColumnOrder,
        COALESCE([Path].value,' ') as Value,
	    F.FriendlyName as Name,
		F.[Type]
from    FieldDetail  F
inner join fieldType FT on FT.ID = F.FieldTypeID
outer apply (
	select graph.GetPathByAssetId(F.AssetID, ' <i class=""fa fa-angle-right""></i> ', ' / ') value)[Path](value)
where   F.[Object]= @o and F.ObjectID = @oid and F.[Type] ='Path'
union
select	RT.ColumnOrder,
		p.[Value],
		RT.FriendlyName as [Name],
        RT.[Type]
from	FieldType RT 
		cross apply openjson(RT.Definition) with (FieldTypeID int '$.FieldTypeID', [Path] nvarchar(250) '$.Path', DataType varchar(50) '$.DataType') D
		inner join Field F on  F.ObjectType = @o and F.ObjectID = @oid and F.FieldTypeID = D.FieldTypeID and RT.[Type] = 'JsonElement'
		inner join FieldJsonProperty P on P.FieldID = F.ID and P.[Path] = D.[Path] 
where   RT.Object = @type and RT.ObjectID = @typeID
union
select	RT.ColumnOrder,
		S.FormattedValue as [Value],
		RT.FriendlyName as [Name],
        RT.[Type] 
from	FieldType RT
		inner join Asset A on A.[Object] = @o and A.ObjectID = @oid
		outer apply dbo.GetAssetScoreById(A.ID, RT.ScoreType) S
where	RT.[Object] = @type and RT.ObjectID = @typeID and RT.[Type] = 'Score'
		and (S.[Value] is not null or RT.ShowIfEmpty = 1)
)
Select [Value],[Name],[Type]
from FieldValueDet
Order by ColumnOrder,Name
";

                        res = Company.Query<FieldTooltipValueModel>(sql, new { oid = objectID, o = objectType, type = det.Type, typeID = det.TypeID }).ToList();


                    }

                    var descSql = @"select 
	                                    ISNULL(FormattedValue,' ') as Value,
	                                    FriendlyName as Name,
                                        [Type]
	                                    from dbo.FieldDetail 
		                                    where objectid = @oid and [object]= @o and [Name] = 'Description'";

                    desc = Company.Query<string>(descSql, new { oid = objectID, o = objectType, }).FirstOrDefault();

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
                    else if ((objectType == "Artifact") || (objectType == "Taxonomy") || (objectType == "FusionAttribute") || (objectType == "Policy"))
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
                    else if (objectType == "ResponsibilityTypeRelationRule")
                    {
                        var responbilityReleationRule = Company.GetById<ResponsibilityTypeRelationRule>(objectID);
                        uid = responbilityReleationRule?.UID.ToString();
                    }
                    else if (objectType == "WorkflowVersion")
                    {
                        var worflowSql = @"	Select t.uid as TypeUID,v.uid as VersionUID from workflow.[type] as t
	                                    inner join  [workflow].[Version] as v on
	                                    t.id = v.TypeId
	                                    where v.id=@id";
                        var workflow = Company.Query<dynamic>(worflowSql, new { id = objectID }).FirstOrDefault();
                        workflowTypeUid = workflow.TypeUID.ToString();
                        workflowVersionUid = workflow.VersionUID.ToString();
                        det = null;
                        uid = null;
                        dispName = null;
                        typeName = null;

                    }
                    else if (objectType == "QuestionType")
                    {
                        var sql = @"select Name, Description, Uid 
                                    from questiontype
                                    where ID = @id";
                        var qType = Company.Query<dynamic>(sql, new { id = objectID }).FirstOrDefault();
                        typeName = "QuestionType";
                        uid = qType.Uid.ToString();
                        desc = qType.Description ?? "";
                        dispName = qType.Name.ToString();
                    }
                    else if (objectType == "Tag")
                    {
                        var tag = Company.Tags.FirstOrDefault(x => x.ID == objectID);
                        int useCount = Company.AssetTags.Count(x => x.TagID == tag.ID);
                        uid = tag.uid.ToString();
                        res.Add(new FieldTooltipValueModel() { Name = "Use count", Value = useCount.ToString() });
                    }
                    else if (objectType == "Task")
                    {
                        if (det.UID.HasValue)
                            det.Url = Company.GetDiagramUrlForDiagramAsset(det.UID.Value);

                        if (det.UID.HasValue)
                            levels = GetFieldLevelPathFromAssetNodeSegment(det.UID ?? Guid.Empty);
                    }
                    else if (objectType == "ConnectorLabel")
                    {
                        var connectorLabel = Company.ConnectorLabels.FirstOrDefault(x => x.ID == objectID);
                        int useCount = Company.Query<int>(@"select count(*) from processexpandeddata where labeluid = @uid", new { connectorLabel.uid }).First();
                        uid = connectorLabel.uid.ToString();
                        res.Add(new FieldTooltipValueModel() { Name = "Use count", Value = useCount.ToString() });
                        dispName = connectorLabel.Value;
                        typeName = "Connector Label";
                    }


                    var tagFieldType = det == null ? null : Company.FieldTypes.Where(x => x.Object == det.Type && x.ObjectID == det.TypeID && x.Type == "Tag").Select(x => new { x.ID, x.ShowIfEmpty, x.FriendlyName }).FirstOrDefault();
                    if (tagFieldType != null)
                    {
                        string assetTagSql = @"select T.Value, T.uid from Asset A
                                                 inner join AssetTag AT on AT.AssetId = A.Id
                                                 inner join Tag T on AT.TagId = T.Id
                                                where Object = @object and ObjectID = @objectID";
                        var tags = Company.Query<dynamic>(assetTagSql, new { @object = objectType, objectID }).ToList();


                        var tagTooltip = new FieldTooltipValueModel()
                        {
                            Name = tagFieldType.FriendlyName,
                            Type = "Tag",
                            Value = JsonConvert.SerializeObject(tags)
                        };


                        if (tags.Count > 0)
                        {
                            if (!tagFieldType.ShowIfEmpty)
                            {
                                res.Add(tagTooltip);
                            }
                            else
                            {
                                int idx = res.FindIndex(x => x.Type == "Tag");
                                res[idx] = tagTooltip;
                            }
                        }
                    }
                    var colorDataString = @"select ACJ.ColorJSON from Asset A cross apply dbo.GetAssetColorJsonByColor(A.Color) ACJ where ID = @assetID ";
                    var colorData = Company.Query<string>(colorDataString, new { @assetID = (det != null ? det.AssetID : -1) }).FirstOrDefault();
                    if (colorData != null)
                    {
                        var color = new FieldTooltipValueModel()
                        {
                            Name = "Color",
                            Type = "Color",
                            Value = colorData
                        };
                        res.Add(color);
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
                        Description = desc,
                        WorkflowTypeUID = workflowTypeUid,
                        WorkflowVersionUID = workflowVersionUid,
                        Class = assetTypeClass

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

        private List<TooltipFieldLevelPathModel> GetFieldLevelPathFromAssetNodeSegment(Guid uid)
        {
            List<TooltipFieldLevelPathModel> levels = new List<TooltipFieldLevelPathModel>();
            string segments = Company.Query<string>($@"SELECT Segments FROM graph.AssetNode WHERE Uid = @assetUid", new { assetUid = uid }).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(segments) && segments.IndexOf('<') >= 0)
            {
                XElement segmentXML = XElement.Parse(segments);
                List<XElement> segmentList = segmentXML.Descendants("segment").OrderBy(order => order.Attribute("level").Value).ThenBy(x => x.Attribute("position").Value).ToList();
                int currentlevel = 1;
                int level = 0;
                int position = 0;
                int assetTypeId = -1;
                List<string> elementPath = new List<string>();

                foreach (XElement element in segmentList)
                {
                    if (int.TryParse(element.Attribute("level").Value, out level))
                    {
                        if (int.TryParse(element.Attribute("position").Value, out position))
                        {
                            if (level != currentlevel)
                            {
                                levels.Add(new TooltipFieldLevelPathModel()
                                {
                                    Level = currentlevel,
                                    LevelName = Company.AssetTypes.Where(d => d.ID == assetTypeId).SingleOrDefault().Name,
                                    Path = string.Join("/", elementPath.ToArray())
                                });
                                currentlevel = level;
                                elementPath = new List<string>();
                            }
                            elementPath.Add(element.Value);
                            int.TryParse(element.Attribute("assetTypeId").Value, out assetTypeId);
                        }
                    }
                }
                //capture the last element path
                if (elementPath.Any())
                {
                    levels.Add(new TooltipFieldLevelPathModel()
                    {
                        Level = currentlevel,
                        LevelName = Company.AssetTypes.Where(d => d.ID == assetTypeId).SingleOrDefault().Name,
                        Path = string.Join("/", elementPath.ToArray())
                    });
                }
            }
            return levels;
        }

        #endregion
    }
}
