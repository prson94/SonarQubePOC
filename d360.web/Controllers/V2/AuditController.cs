using d360.core;
using Microsoft.Web.Http;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;
using Dapper;
using SpreadsheetLight;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Web.Http.Description;
using System.Collections.Generic;
using Swashbuckle.Swagger.Annotations;
using d360.web.Filters;
using d360.web.Models;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.entities.Metric;
using d360.model.helpers.filters;
using d360.model.DataAccessLayer;
using Resources;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// 
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/audit"),
        Authorize
    ]
    public class AuditController : BaseV2ApiController
    {
        public AuditController(CoreComponentSet set): base(set)
        {

        }

        /// A dictionary of Action Object with the DB value as key, and the display value as value
        private readonly Dictionary<string, string> ActionObjectDictionary = new Dictionary<string, string> {
            { "Intersect", "Relationship" },
            { "IntersectType", "RelationshipType" },
            { "Taxonomy" , "Model" },
            { "TaxonomyType" , "ModelType" },
            { "ResponsibilityTypeRelationOverrideItem" , "Responsibility Type Relation Override Item" },
        };

        /// <summary>
        /// Retrieves audit data for the given asset unique identifier.
        /// </summary>
        /// <remarks>
        /// Results can be filted using the _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
        /// *  For comparison operators you can use eq (equal), ne (not equal), gt (greater than), ge (greater than or equal), lt (less than), le (less than or equal) and ct (contains) which allows usage of (*) symbol as wildcard
        /// *  Chaining of filter expressions is done using 'and' or 'or' logical operator. IE. city eq 'Redmond' OR city ct 'Lo'.
        /// 
        /// If the requested content media type is "application/octet-stream", the response will be an Excel document with the asset audit data.
        /// </remarks>
        /// <param name="assetUid">The unique identifier of the asset.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetUid:Guid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetsAuditApiViewModel)),
            SwaggerProduces("application/json", "application/octet-stream"),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", PAGE_SIZE_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", PAGE_NUMBER_DESCRIPTION, DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by Date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered descending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", ADVANCED_FILTER_DESCRIPTION, DataType = "string", ParameterType = "query", Required = false)
        ]
        public async Task<IHttpActionResult> GetAuditAsync(Guid assetUid)
        {
            var prefix = "Audit.GetAuditAsync => ";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                bool isStreamResponse = Request?.Headers?.Accept?.Any(a => a.MediaType == "application/octet-stream") ?? false;
                int pageSizeLimit = isStreamResponse ? 200000 : 250;

                var orderBySql = "";
                var dbArgs = new DynamicParameters();
                List<string> whereStatements = new List<string>();

                string isValid = IsPageSizeAndNumValid(queryParams, pageSizeLimit);
                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, isValid)).ConfigureAwait(false);
                }

                List<DefaultFilter> fieldList = new List<DefaultFilter>
                {
                    new DefaultFilter("uid", "A.uid", SqlFieldType.Guid),
                    new DefaultFilter("name", "A.name", SqlFieldType.Text),
                    new DefaultFilter("resourceUid", "A.resourceUid", SqlFieldType.Guid),
                    new DefaultFilter("resourceName", "A.resourceName", SqlFieldType.Text),
                    new DefaultFilter("date", "A.date", SqlFieldType.DateTime),
                    new DefaultFilter("action", "A.action", SqlFieldType.Text),
                    new DefaultFilter("actionAssetUid", "A.actionAssetUid", SqlFieldType.Guid),
                    new DefaultFilter("actionAssetTypeUid", "A.actionAssetTypeUid", SqlFieldType.Guid),
                    new DefaultFilter("actionObject", "A.actionObject", SqlFieldType.Text),
                    new DefaultFilter("actionObjectTypeName", "A.actionObjectTypeName", SqlFieldType.Text),
                    new DefaultFilter("actionObjectName", "A.actionObjectName", SqlFieldType.Text),
                    new DefaultFilter("actionDescription", "A.actionDescription", SqlFieldType.Text),
                    new DefaultFilter("field", "A.field", SqlFieldType.Text),
                    new DefaultFilter("newValue", "A.newValue", SqlFieldType.Text),
                    new DefaultFilter("class", "A.class", SqlFieldType.Number),
                    new DefaultFilter("version", "isnull(A.version,0)", SqlFieldType.Number),
                    new DefaultFilter("previousValue", "A.previousValue", SqlFieldType.Text)
                };

                var orderColumn = Company.ParseOrderColumn(queryParams, fieldList, "Date");
                var orderDirection = Company.ParseOrderDirection(queryParams, "desc");
                orderBySql = $" order by {orderColumn} {orderDirection} ";

                //some actionObject values are translated using ActionObjectDictionary, so incoming filters for actionObject
                //must have the values translated back
                List<KeyValuePair<string, string>> modifiedQueryParams = new List<KeyValuePair<string, string>>();
                foreach(KeyValuePair<string, string> kp in queryParams)
                {
                    string currentValue = kp.Value;
                    if(kp.Key.ToLower(System.Globalization.CultureInfo.InvariantCulture) == "_filter" && currentValue.Contains("actionObject"))
                    {
                        List<string> operators = new List<string>
                        {
                            "eq",
                            "ne"
                        };
                        Dictionary<string, string> lookups = ActionObjectDictionary.ToDictionary(d => d.Value, d => d.Key);
                        lookups.Add("Business Asset", "Artifact");
                        lookups.Add("Technical Asset", "Artifact");

                        currentValue = lookups.SelectMany(l => operators, (l, o) => new { l, o })
                            .ToDictionary(s => $"actionObject {s.o} '{s.l.Key}'", s=> $"actionObject {s.o} '{s.l.Value}'")
                            .Aggregate(currentValue, (current, value) => current.Replace(value.Key, value.Value));
                    }
                    modifiedQueryParams.Add(new KeyValuePair<string, string> ( kp.Key, currentValue));
                }

                DynamicParameters advFilterArgs = null;
                List<string> advFilterStatements = null;
                Company.ParseAdvancedFilterQueryParameter(modifiedQueryParams, fieldList, out advFilterArgs, out advFilterStatements);
                if (advFilterArgs != null && advFilterStatements != null)
                {
                    dbArgs.AddDynamicParams(advFilterArgs);
                    whereStatements.AddRange(advFilterStatements);
                }

                bool isAssetType = false;
                AssetType assetType = null;
                if (
                    !Company.Any<Asset>(i => i.uid == assetUid) &&
                    !Company.Any<Tag>(i => i.uid == assetUid) &&
                    !Company.Any<IssueType>(i => i.uid == assetUid) &&
                    !Company.Any<IntersectType>(i => i.uid == assetUid) &&
                    !Company.Any<ResponsibilityType>(i => i.UID == assetUid) &&
                    !Company.Any<Report>(i => i.uid == assetUid) &&
                    !Company.Any<MetricAllocation>(i => i.Uid == assetUid) &&
                    !Company.Any<Predicate>(i => i.UID == assetUid))
                {
                    assetType = Company.Filter<AssetType>(i => i.uid == assetUid).SingleOrDefault();
                    if (assetType == null)
                    {
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, ApiMessages.InvalidRequest, ApiMessages.UIDNotFoundObjectAndObjectType)).ConfigureAwait(false);
                    }
                    isAssetType = true;
                }
                string baseSql = "";
                if (isAssetType)
                {
                    switch (assetType.Object)
                    {
                        case "ResourceType":
                            baseSql = GetBaseAuditQueryObject(SystemObjects.Resource, false);
                            break;
                        case "GroupType":
                            baseSql = GetBaseAuditQueryObject(SystemObjects.Group, false);
                            break;
                        case "MetricAllocation":
                            baseSql = GetBaseAuditQueryObject(SystemObjects.MetricAllocation, false);
                            break;
                        case "Predicate":
                            baseSql = GetBaseAuditQueryObject(SystemObjects.Predicate, false);
                            break;
                        default:
                            baseSql = GetBaseAuditQueryForAssetTypeUid(assetType?.Class == AssetTypeClass.Reference);
                            break;
                    }
                }
                else
                {
                    baseSql = GetBaseAuditQueryForUid();
                }

                dbArgs.Add("uid", assetUid);

                string whereSql = "";
                if (whereStatements.Any())
                {
                    whereSql = $" where {string.Join(" and ", whereStatements)}";
                }

                int pageNum = Company.ParsePageNumber(queryParams, 1);
                int pageSize = Company.ParsePageSize(queryParams);
                string offsetSql = Company.ParsePageOffsetSql(pageNum, pageSize);

                var countSql = string.Format(@"select count(1) from ({0}) A {1}", baseSql, whereSql);
                var sql = string.Format(@"select * from ({0}) A {1}", baseSql, whereSql);

                sql += " " + orderBySql + " " + offsetSql;

                var query = Company.Query<AssetAuditApiItemModel>(sql, dbArgs, ApiTimeout).ToList();

                //Translate actionObject values
                query.ForEach(r => {
                    if (new[] { "Artifact", "ArtifactType" }.Contains(r.actionObject))
                    {
                        if (r.@class == 1)
                        {
                            r.actionObject = "Business Asset";
                            r.actionDescription = r.actionDescription.Replace("Artifact", "Business Asset");
                        } else if (r.@class == 8)
                        {
                            r.actionObject = "Technical Asset";
                            r.actionDescription = r.actionDescription.Replace("Artifact", "Technical Asset");
                        }
                    } else if (ActionObjectDictionary.ContainsKey(r.actionObject))
                    {
                        r.actionObject = ActionObjectDictionary[r.actionObject];
                    }
                });

                if (isStreamResponse)
                {
                    string fileName = assetUid.ToString() + " Audit Data";
                    SLDocument document = GetExcelDocumentFromQuery(query);

                    var stream = new MemoryStream();
                    document.SaveAs(stream);
                    byte[] bytes = stream.ToArray();

                    var response = createFileResponseMessage(HttpStatusCode.OK, $"{fileName} {DateTime.Now.ToString("MMM dd yyyy")}.xlsx", bytes);
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(response)).ConfigureAwait(false);
                }
                else
                {
                    var model = new AssetsApiViewModel();
                    model.total = Company.Query<int>(countSql, dbArgs, ApiTimeout).First();
                    model.pageNum = pageNum;
                    model.pageSize = pageSize;
                    model.items = query;

                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model))).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var messages = new List<StatusCodeErrorMessage>();
                return DetermineUnhandledException(
                    ex,
                    "Error retrieving audit records",
                    messages,
                    new Dictionary<string, string> { { "Method Name", prefix } }
                );
            }
        }

        /// <summary>
        /// Gets displayname, object and objectid from Uid regardless of whether the UID is Asset, AssetType or Tag
        /// </summary>
        /// <param name="assetUid">The asset Uid</param>
        /// <returns></returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("objectdetail/{assetUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(Object)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public dynamic GetLegacyObjectDetails(Guid assetUid)
        {
            dynamic result;
            result = Company.Query<dynamic>($@"select Object,ObjectId,DisplayValue from AssetDetail where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();

            if (result == null)
            {
                result = Company.Query<dynamic>($@"select Object,ObjectId,Name as DisplayValue from AssetType where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();
            }

            if (result == null)
            {
                result = Company.Query<dynamic>($@"select 'Tag' as Object, ID as ObjectId,Value as DisplayValue from Tag where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();
            }

            if (result == null)
            {
                result = Company.Query<dynamic>($@"select 'IssueType' as Object, ID as ObjectId, Name as DisplayValue from IssueType where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();
            }

            if (result == null)
            {
                result = Company.Query<dynamic>($@"select 'IntersectType' as Object, ID as ObjectId, itn.name as DisplayValue
                    from dbo.[IntersectType] IT
                    CROSS APPLY dbo.GetIntersectTypeNames(IT.ID) ITN where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();
            }

            if (result == null)
            {
                result = Company.Query<dynamic>($@"select 'ResponsibilityType' as Object, ID as ObjectId, Name as DisplayValue from ResponsibilityType where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();
            }

            if (result == null)
            {
                result = Company.Query<dynamic>($@"select 'Report' as Object, ID as ObjectId, Name as DisplayValue from Report where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();
            }

            return result;
        }

        /// <summary>
        /// Gets lists of User, Action and ActionObject values in change log for the asset yuid to use in advanced filter lists
        /// </summary>
        /// <param name="assetUid">The asset Uid</param>
        /// <returns></returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route("filterlists/{assetUid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(Object)),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public dynamic GetFilterLists(Guid assetUid)
        {
            dynamic objectInfo = GetLegacyObjectDetails(assetUid);
            dynamic result = new System.Dynamic.ExpandoObject();
            string condition;

            AssetType assetType = Company.Filter<AssetType>(i => i.uid == assetUid).SingleOrDefault();
            if (assetType?.Class == AssetTypeClass.Reference)
            {
                condition = @"(ga.[Object] = @Object and ga.ObjectId = @ObjectId) OR (
                    ga.[Object] = 'ReferenceItem' and ga.ObjectID in 
                        (select a.objectid from[dbo].[asset] a
                        inner join [dbo].[assettype] att on(a.assettypeid = att.id)
                        where att.[Object] = @Object and att.ObjectId = @ObjectId))";
            }
            else if (new List<string> { "ResourceType", "GroupType", "MetricAllocation", "Predicate" }.Contains(objectInfo.Object))
            {
                condition = "ga.[Object] = @Object";
            }
            else
            {
                condition = "ga.[Object] = @Object and ga.ObjectId = @ObjectId";
            }

            result.resourceName = Company.Query<dynamic>($@"select distinct
	                CASE WHEN R.State = 3 THEN
		                R.FirstName + ' ' + R.LastName + ' (deleted)'
	                ELSE
		                R.FirstName + ' ' + R.LastName
	                END as val
                from reporting.global_audit ga
                inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
			    where {condition}", new { objectInfo.Object, objectInfo.ObjectId }, ApiTimeout).Select(x => x.val).ToList();

            result.action = Company.Query<dynamic>($@"select distinct ga.action as val
                from reporting.global_audit ga
			    where {condition}", new { objectInfo.Object, objectInfo.ObjectId }, ApiTimeout).Select(x => x.val).ToList();

            result.actionObject = Company.Query<dynamic>($@"select distinct
                case when ga.ActionObject like 'Artifact%' and coalesce(at.class, att.class) = 1 then 'Business Asset'
                when ga.ActionObject like 'Artifact%' and coalesce(at.class, att.class) = 8 then 'Technical Asset'
                else  ga.ActionObject end val
                from reporting.global_audit ga
                left join AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID
				left join Asset A on A.Object = ga.Object and A.ObjectID = ga.ObjectID
				left join AssetType ATT on A.AssetTypeID = att.id
                where {condition}", new { objectInfo.Object, objectInfo.ObjectId }, ApiTimeout).Select(x => {
                    return (ActionObjectDictionary.ContainsKey(x.val)) ? ActionObjectDictionary[x.val] : x.val;
                }).ToList();

            return result;
        }


        [
            HttpGet,
            Route("{type}/{uid}/auditcombined.json"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> AuditCombined(SystemObjects type, Guid uid, string sortDataField)
        {
            try
            {
                int id = Company.GetObjectId(uid, type);
                return await AuditCombined(type, id, sortDataField).ConfigureAwait(false);
            }
            catch
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [
            HttpGet,
            Route("{type}/{id:int}/auditcombined.json"),
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public async Task<IHttpActionResult> AuditCombined(SystemObjects type, int id, string sortDataField)
        {
            try
            {
                Trace.TraceInformation("Calling AuditController.AuditCombined : {0}, {1}", type.ToString(), id);
                var dbArgs = new DynamicParameters();

                var querySql = getBaseAuditQueryForId(type, id == 0);

                var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
                var sql = string.Format(@"select * from ({0}) A", querySql);

                dbArgs.Add("objType", new DbString { Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 });
                dbArgs.Add("objId", id);

                countSql = base.applyFilteringSuffix(countSql, Request);
                int total = Company.Query<int>(countSql, dbArgs).First();

                sql = base.applyFilteringSuffix(sql, Request);

                var stFieldType = sortDataField == null || sortDataField == "Date" ? "DateTime" : "string";

                sql = base.applySortSuffix(sql, Request, "Date", "desc", stFieldType);
                sql = base.applyPagingSuffix(sql, Request);

                var query = Company.Query<dynamic>(sql, dbArgs, ApiTimeout);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { total, results = query }))).ConfigureAwait(false);
            }
            catch
            {
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }
        }

        [
            Route("{type}/{uid}/download/excel/audit.xls"),
            FileDownload,
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetAuditToExcel(SystemObjects type, string uid)
        {
            Guid guid = Guid.Parse(uid);
            var objectId = Company.GetObjectId(guid, type);

            return GetAuditToExcel(type, objectId);
        }

        [
            Route("{type}/{id:int}/download/excel/audit.xls"),
            FileDownload,
            HttpGet,
            ApiExplorerSettings(IgnoreApi = true)
        ]
        public IHttpActionResult GetAuditToExcel(SystemObjects type, int id)
        {
            var querySql = getBaseAuditQueryForId(type, id == 0);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            sql = base.applyFilteringSuffix(sql, Request);
            sql = base.applySortSuffix(sql, Request, "Date", "desc", "DateTime");


            var dbArgs = new Dapper.DynamicParameters();
            dbArgs.Add("objType", new DbString { Value = type.ToString(), IsAnsi = true, IsFixedLength = true, Length = 50 });
            dbArgs.Add("objId", id);


            var query = Company.Query<dynamic>(sql, dbArgs, ApiTimeout);
            var document = GetExcelDocumentFromQuery(query);

            var stream = new MemoryStream();
            document.SaveAs(stream);

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(stream.GetBuffer())
            };
            result.Content.Headers.ContentLength = stream.Length;
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = "audit.xlsx"
            };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.ms-excel");

            return ResponseMessage(result);
        }

        private SLDocument GetExcelDocumentFromQuery(IEnumerable<dynamic> query)
        {
            var document = new SLDocument();
            document.RenameWorksheet(SLDocument.DefaultFirstSheetName, "Items");

            #region Create the list sheet

            #region Header

            document.SetCellValue(1, 1, "User");
            document.SetCellValue(1, 2, "Date");
            document.SetCellValue(1, 3, "Action");
            document.SetCellValue(1, 4, "Field");
            document.SetCellValue(1, 5, "New Value");
            document.SetCellValue(1, 6, "Previous Value");
            document.SetCellValue(1, 7, "Object");
            document.SetCellValue(1, 8, "Type");
            document.SetCellValue(1, 9, "Item");
            document.SetCellValue(1, 10, "Audit Description");
            document.SetCellValue(1, 11, "Revision");

            #endregion

            int rowIndex = 1;
            foreach (var row in query)
            {
                rowIndex++;

                document.SetCellValue(rowIndex, 1, row.resourceName);
                document.SetCellValue(rowIndex, 2, (((DateTime)row.date)));

                SLStyle style = document.CreateStyle();
                style.FormatCode = "mmm dd yyyy hh:mm:ss";
                document.SetCellStyle(rowIndex, 2, style);

                document.SetCellValue(rowIndex, 3, row.action);
                document.SetCellValue(rowIndex, 4, row.field ?? "");
                document.SetCellValue(rowIndex, 5, row.newValue ?? "");
                document.SetCellValue(rowIndex, 6, row.previousValue ?? "");
                document.SetCellValue(rowIndex, 7, row.actionObject);
                document.SetCellValue(rowIndex, 8, row.actionObjectTypeName);
                document.SetCellValue(rowIndex, 9, row.actionObjectName);
                document.SetCellValue(rowIndex, 10, row.actionDescription);
                document.SetCellValue(rowIndex, 11, row.version ?? "");
            }

            #endregion


            return document;
        }

        private string getBaseAuditQueryForId(SystemObjects type, bool auditingByType = false)
        {
            string querySql = $@"select
                ga.*,
                CASE WHEN R.State = {(int)CompanyResourceState.Deleted} THEN
                    R.FirstName + ' ' + R.LastName + ' (deleted)'
                ELSE
                    R.FirstName + ' ' + R.LastName
                END as ResourceName,
                fa.FieldName as Field, 
			    CASE WHEN ga.Action = 'Tag Consolidate' THEN
                    ga.ObjectName
			    ELSE
                    fa.Value
	            END as NewValue,
				coalesce(AT.Class, AD.AssetTypeClass) as Class,
                fa.[Version] as 'Version',
                CASE WHEN ga.Action  = 'Tag Consolidate' THEN
                    ga.ActionObjectName
				ELSE
                    (select top 1 fa_sub.value as 'value'
                    from reporting.global_fieldaudit fa_sub
                    inner join reporting.global_audit ga_sub on (fa_sub.auditid = ga_sub.id)	
                    where ga_sub.[object] = ga.[object] 
                    and ga_sub.[objectid] = ga.[objectid] 
                    and fa_sub.version = (fa.Version - 1) 
                    and fa_sub.fieldname = fa.FieldName 
                    and fa_sub.fieldtypeid = fa.FieldTypeId 
                    and ga_sub.actionObjectId=ga.actionObjectId)
				END AS 'PreviousValue'    
                from reporting.global_audit ga";

            //get all auditing by type (introduced in tagging)
            if (auditingByType)
            {
                //Class value will be 11. To reuse columns from regular query, add by cross joining a select 11
                querySql += @"
                left outer join reporting.global_fieldaudit fa on(fa.auditid = ga.id) and ga.Action != 'Removed'
                inner join[reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
                cross join (select 11 as Class) AT
                cross join (select 11 as AssetTypeClass) AD
                where ga.[Object] = @objType";
            }
            else
            {
                querySql += @"
                left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
                inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID and ga.[Object] = @objType and ga.ObjectID = @objId
				left join AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID
                left join AssetDetail AD on AD.Object = ga.Object and AD.ObjectID = ga.ObjectID";
            }

            if (type == SystemObjects.ReferenceItemType)
            {
                querySql += $@" UNION
                    select 	                            
                    ga.*,
                    case when R.State = {(int)CompanyResourceState.Deleted} then
                        R.FirstName + ' ' + R.LastName + ' (deleted)'
                    else
                        R.FirstName + ' ' + R.LastName
                    end as ResourceName,
                        fa.FieldName as Field, 
                        fa.Value as NewValue, 
                        9 as Class,
                        fa.[Version] as 'Version',	                            
                    (select top 1 fa_sub.value as 'value'			                            
                    from reporting.global_fieldaudit fa_sub
                    inner join reporting.global_audit ga_sub on ( fa_sub.auditid = ga_sub.id)	
                    where ga_sub.[object] = ga.[object] 
                        and ga_sub.[objectid] = ga.[objectid] 
                        and fa_sub.version = (fa.Version -1) 
                        and fa_sub.fieldname = fa.FieldName 
                        and fa_sub.fieldtypeid = fa.FieldTypeId 
                        and ga_sub.actionObjectId=ga.actionObjectId) as 'PreviousValue'
                    from reporting.global_audit ga 
                    left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id ) 
                    inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID 
                        and ga.[Object] = 'ReferenceItem' 
                        and ga.ObjectID in 
                            (select a.objectid from [dbo].[asset] a 
                            inner join [dbo].[assettype] att on (a.assettypeid = att.id) 
                            where att.[object] = 'ReferenceItemType' 
                                and att.ObjectID = @objId)";
            }
            return querySql;
        }

        /// <summary>
        /// Gets audit data query for an Asset UID
        /// This method only works for assets, not asset types
        /// Use the version of the method that accepts Object and ID as paramneters for that
        /// </summary>
        /// <returns>A base query with @uid</returns>
        private string GetBaseAuditQueryForUid()
        {
            //This query should generally match the one in GetBaseAuditQueryForId except UID columns are returned instead of Object/id same

            string querySql = $@"select
	            ad.uid,
	            ad.DisplayValue as name,
	            r.uid as resourceUid,
	            CASE WHEN R.State = {(int)CompanyResourceState.Deleted} THEN
		            R.FirstName + ' ' + R.LastName + ' (deleted)'
	            ELSE
		            R.FirstName + ' ' + R.LastName
	            END as resourceName,
	            ga.Date as date,
	            ga.action,
	            ActionA.uid as actionAssetUid,
	            ActionAT.uid as actionAssetTypeUid,
                case when ga.ActionObject = 'Intersect' then 'Relationship'
                     when ga.ActionObject = 'IntersectType' then 'RelationshipType'
                     else ga.ActionObject end ActionObject,
	            case when ga.ActionObjectTypeName = 'Intersect Type' then 'Relationship Type' else ga.ActionObjectTypeName end as actionObjectTypeName,
	            ga.actionObjectName,
	            ga.actionDescription,
	            fa.FieldName as Field,
	            CASE WHEN ga.Action = 'Tag Consolidate' THEN
		            ga.ObjectName
	            ELSE
		            fa.Value
	            END as NewValue,
	            coalesce(AT.Class, AD.AssetTypeClass) as Class,
	            fa.[Version] as 'Version',
	            CASE WHEN ga.Action  = 'Tag Consolidate' THEN
		            ga.ActionObjectName
	            ELSE
		            (select top 1 fa_sub.value as 'value'
		            from reporting.global_fieldaudit fa_sub
		            inner join reporting.global_audit ga_sub on (fa_sub.auditid = ga_sub.id)	
		            where ga_sub.[object] = ga.[object] 
		            and ga_sub.[objectid] = ga.[objectid] 
		            and fa_sub.version = (fa.Version - 1) 
		            and fa_sub.fieldname = fa.FieldName 
		            and fa_sub.fieldtypeid = fa.FieldTypeId 
		            and ga_sub.actionObjectId=ga.actionObjectId)
	            END AS 'PreviousValue',
                ft.[Type] as FieldType
            from reporting.global_audit ga
            left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
            inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
            left join AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID
            left join Asset ActionA on ActionA.Object = ga.ActionObject and ActionA.ObjectID = ga.ActionObjectID
            left join AssetType ActionAT on ActionA.AssetTypeID = ActionAT.ID
            left join FieldType FT on FT.ID = fa.FieldTypeID
            inner join  (
    			select uid, DisplayValue, Object, objectid, AssetTypeClass from AssetDetail where uid = @uid
    			union
                select uid, value as DisplayName, 'Tag' as Object, id as ObjectID, 11 as AssetTypeClass from Tag where uid = @uid
                union
                select uid, name as DisplayName, 'IssueType' as Object, id as ObjectID, null as AssetTypeClass from dbo.IssueType where uid = @uid
                union
                select uid, itn.name as DisplayValue, 'IntersectType' as Object, id as ObjectID, null as AssetTypeClass from dbo.[IntersectType] IT
                    CROSS APPLY dbo.GetIntersectTypeNames(IT.ID) ITN  where uid = @uid
                union
                select uid, name as DisplayName, 'ResponsibilityType' as Object, id as ObjectID, null as AssetTypeClass from dbo.ResponsibilityType where uid = @uid
                union
                select uid, name as DisplayName, 'Report' as Object, id as ObjectID, null as AssetTypeClass from dbo.[Report] where uid = @uid
                union
                select MA.uid, AT.Name as DisplayName, 'MetricAllocation' as Object, MA.ID as ObjectID, null as AssetTypeClass from metrics.Allocation MA inner join [dbo].[AssetType] AT on AT.uid = MA.AssetTypeUid where MA.uid = @uid
                union
				select uid, name as DisplayName, 'Predicate' as Object, id as ObjectID, null as AssetTypeClass from dbo.[Predicate] where uid = @uid
			) AD on AD.Object = ga.Object and AD.ObjectID = ga.ObjectID and AD.uid = @uid";

            return querySql;
        }

        private string GetBaseAuditQueryForAssetTypeUid(bool includeReferenceItem)
        {
            string querySql = $@"select
	            at.uid,
	            at.Name as name,
	            r.uid as resourceUid,
	            CASE WHEN R.State = {(int)CompanyResourceState.Deleted} THEN
		            R.FirstName + ' ' + R.LastName + ' (deleted)'
	            ELSE
		            R.FirstName + ' ' + R.LastName
	            END as resourceName,
	            ga.Date as date,
	            ga.action,
	            ActionA.uid as actionAssetUid,
	            ActionAT.uid as actionAssetTypeUid,
                case when ga.ActionObject = 'Intersect' then 'Relationship'
                     when ga.ActionObject = 'IntersectType' then 'RelationshipType'
                     else ga.ActionObject end ActionObject,
	            case when ga.ActionObjectTypeName = 'Intersect Type' then 'Relationship Type' else ga.ActionObjectTypeName end as actionObjectTypeName,
	            ga.actionObjectName,
	            ga.actionDescription,
	            fa.FieldName as Field,
	            CASE WHEN ga.Action = 'Tag Consolidate' THEN
		            ga.ObjectName
	            ELSE
		            fa.Value
	            END as NewValue,
	            AT.Class as Class,
	            fa.[Version] as 'Version',
	            CASE WHEN ga.Action  = 'Tag Consolidate' THEN
		            ga.ActionObjectName
	            ELSE
		            (select top 1 fa_sub.value as 'value'
		            from reporting.global_fieldaudit fa_sub
		            inner join reporting.global_audit ga_sub on (fa_sub.auditid = ga_sub.id)	
		            where ga_sub.[object] = ga.[object] 
		            and ga_sub.[objectid] = ga.[objectid] 
		            and fa_sub.version = (fa.Version - 1) 
		            and fa_sub.fieldname = fa.FieldName 
		            and fa_sub.fieldtypeid = fa.FieldTypeId 
		            and ga_sub.actionObjectId=ga.actionObjectId)
	            END AS 'PreviousValue'
            from reporting.global_audit ga
            left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
            inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
            inner join AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID and at.uid = @uid
            left join Asset ActionA on ActionA.Object = ga.ActionObject and ActionA.ObjectID = ga.ActionObjectID
            left join AssetType ActionAT on ActionA.AssetTypeID = ActionAT.ID            
            ";

            if (includeReferenceItem)
            {
                querySql += $@" UNION select
                    ad.uid,
	                ad.DisplayValue as name,
	                r.uid as resourceUid,
	                CASE WHEN R.State = {(int)CompanyResourceState.Deleted} THEN
                        R.FirstName + ' ' + R.LastName + ' (deleted)'
                    ELSE
                        R.FirstName + ' ' + R.LastName
                    END as resourceName,
	                ga.Date as date,
	                ga.action,
	                ActionA.uid as actionAssetUid,
	                ActionAT.uid as actionAssetTypeUid,
                     case when ga.ActionObject = 'Intersect' then 'Relationship'
                          when ga.ActionObject = 'IntersectType' then 'RelationshipType'
                          else ga.ActionObject end ActionObject,
	                case when ga.ActionObjectTypeName = 'Intersect Type' then 'Relationship Type' else ga.ActionObjectTypeName end as actionObjectTypeName,
	                ga.actionObjectName,
	                ga.actionDescription,
	                fa.FieldName as Field,
	                CASE WHEN ga.Action = 'Tag Consolidate' THEN
                        ga.ObjectName
                    ELSE
                        fa.Value
                    END as NewValue,
	                coalesce(AT.Class, AD.AssetTypeClass) as Class,
	                fa.[Version] as 'Version',
	                CASE WHEN ga.Action = 'Tag Consolidate' THEN
                        ga.ActionObjectName
                    ELSE
                        (select top 1 fa_sub.value as 'value'
                        from reporting.global_fieldaudit fa_sub
                        inner join reporting.global_audit ga_sub on (fa_sub.auditid = ga_sub.id)
                            where ga_sub.[object] = ga.[object]
                            and ga_sub.[objectid] = ga.[objectid]
                            and fa_sub.version = (fa.Version - 1)
                            and fa_sub.fieldname = fa.FieldName
                            and fa_sub.fieldtypeid = fa.FieldTypeId
                            and ga_sub.actionObjectId = ga.actionObjectId)
                    END AS 'PreviousValue'
                from reporting.global_audit ga
                left outer join reporting.global_fieldaudit fa on(fa.auditid = ga.id)
                inner join[reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
                    and ga.[Object] = 'ReferenceItem'
                    and ga.ObjectID in 
                        (select a.objectid from[dbo].[asset] a
                        inner join[dbo].[assettype] att on(a.assettypeid = att.id)
                        where att.uid = @uid)
                left join AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID
                left join Asset ActionA on ActionA.Object = ga.ActionObject and ActionA.ObjectID = ga.ActionObjectID
                left join AssetType ActionAT on ActionA.AssetTypeID = ActionAT.ID
                left join AssetDetail AD on AD.Object = ga.Object and AD.ObjectID = ga.ObjectID";
            }

            return querySql;
        }

        private string GetBaseAuditQueryObject(SystemObjects systemObject, bool includeTypeAudits)
        {
            string objectTypeName = systemObject.ToString() + "Type";
            string querySql = $@"select
	            at.uid,
	            at.Name as name,
	            r.uid as resourceUid,
	            CASE WHEN R.State = {(int)CompanyResourceState.Deleted} THEN
		            R.FirstName + ' ' + R.LastName + ' (deleted)'
	            ELSE
		            R.FirstName + ' ' + R.LastName
	            END as resourceName,
	            ga.Date as date,
	            ga.action,
	            ActionA.uid as actionAssetUid,
	            ActionAT.uid as actionAssetTypeUid,
                ga.ActionObject,
	            ga.ActionObjectTypeName as actionObjectTypeName,
	            ga.actionObjectName,
	            ga.actionDescription,
	            fa.FieldName as Field,
	            fa.Value as NewValue,
	            AT.Class as Class,
	            fa.[Version] as 'Version',
	            fa.PreviousValue
            from reporting.global_audit ga
            left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
            inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
            left join AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID
            left join Asset ActionA on ActionA.Object = ga.ActionObject and ActionA.ObjectID = ga.ActionObjectID
            left join AssetType ActionAT on ActionA.AssetTypeID = ActionAT.ID            
			where ga.Object = '{systemObject.ToString()}'
            ";

            if (includeTypeAudits)
            {
                querySql += $@"union
select
	            at.uid,
	            at.Name as name,
	            r.uid as resourceUid,
	            CASE WHEN R.State = {(int)CompanyResourceState.Deleted} THEN
		            R.FirstName + ' ' + R.LastName + ' (deleted)'
	            ELSE
		            R.FirstName + ' ' + R.LastName
	            END as resourceName,
	            ga.Date as date,
	            ga.action,
	            ActionA.uid as actionAssetUid,
	            ActionAT.uid as actionAssetTypeUid,
                ga.ActionObject,
	            ga.ActionObjectTypeName as actionObjectTypeName,
	            ga.actionObjectName,
	            ga.actionDescription,
	            fa.FieldName as Field,
	            fa.Value as NewValue,
	            AT.Class as Class,
	            fa.[Version] as 'Version',
	            fa.PreviousValue
            from reporting.global_audit ga
            left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
            inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
            left join AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID
            left join Asset ActionA on ActionA.Object = ga.ActionObject and ActionA.ObjectID = ga.ActionObjectID
            left join AssetType ActionAT on ActionA.AssetTypeID = ActionAT.ID            
			where ga.Object = '{objectTypeName}'";
            }

            return querySql;
        }


        public string IsPageSizeAndNumValid(IEnumerable<KeyValuePair<string, string>> queryParams, int pageSizeLimit = 250)
        {
            var parameters = queryParams.ToList();
            long pageSize = 0;
            long pageNum = 0;

            if (parameters.Any(q => q.Key == "_pageSize"))
            {
                var _pageSize = queryParams.ToList().FirstOrDefault(q => q.Key == "_pageSize").Value;
                if (_pageSize.Length > 10)
                {
                    return string.Format(ApiMessages.InvalidValueMessage, ApiMessages.PageSizeString);
                }
                if (long.TryParse(_pageSize, out pageSize))
                {
                    if (pageSize > pageSizeLimit)
                    {
                        return string.Format(ApiMessages.InvalidNumberTooLarge, ApiMessages.PageSizeString);
                    }
                    if (pageSize <= 0)
                    {
                        return string.Format(ApiMessages.MinLengthCheckGTZero, ApiMessages.PageSizeString);
                    }
                }
                else
                {
                    return string.Format(ApiMessages.NumberValueMessage, ApiMessages.PageSizeString);
                }
            }

            if (parameters.Any(q => q.Key == "_pageNum"))
            {
                var _pageNum = queryParams.ToList().FirstOrDefault(q => q.Key == "_pageNum").Value;
                if (_pageNum.Length > 10)
                {
                    return string.Format(ApiMessages.InvalidValueMessage, ApiMessages.PageNumString);
                }
                if (long.TryParse(_pageNum, out pageNum))
                {
                    if (pageNum > 100000)
                    {
                        return string.Format(ApiMessages.InvalidNumberTooLarge, ApiMessages.PageNumString);
                    }
                    if (pageNum <= 0)
                    {
                        return string.Format(ApiMessages.MinLengthCheckGTZero, ApiMessages.PageNumString);
                    }
                }
                else
                {
                    return string.Format(ApiMessages.NumberValueMessage, ApiMessages.PageNumString);
                }
            }

            return "";
        }
    }
}
