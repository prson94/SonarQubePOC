using d360.core;
using Microsoft.Web.Http;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;
using Dapper;
using Resources;
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
using d360.model.helpers;
using d360.core.enums;

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
        public AuditController(ICommunityContext community, ICompanyContext company) : base(community, company)
        {

        }

        /// <summary>
        /// Retrieves audit data for the given asset unique identifier.
        /// </summary>
        /// <remarks>
        /// In addition to the below query parameters a field name for the asset type can be specified to filter by exact match. For example MyCustomField=someExactValue. TODO
        /// *  If you use the object asset type Uid as the assetTypeUid value, only use of the subjectUid filter is supported.
        /// *  If you use the subject asset type Uid as the assetTypeUid value, only use of the objectUid filter is supported.
        /// *  If you use either the subjectUid or objectUid filter, the predicateUid must be included in the request. 
        /// *  If you do not include the predicateUid, any values given in the subjectUid or objectUid field are ignored.
        /// 
        /// Advanced filtering is done using _filter parameter and filter expressions are specified using field name, operator and value. For example city eq 'Redmond'.
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
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occurred while processing this request.", typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200. Maximum is 250.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by Date.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_direction", "Specify sort direction. Use 'asc' for ascending, or 'desc' as descending. By default the results are ordered descending.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_filter", "The filter expression used to filter assets by all listable and non-listable fields. Asterisk (*) symbol can be used as a wild card character to match any character.", DataType = "string", ParameterType = "query", Required = false)
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
                var orderDirection = "desc";
                var dbArgs = new DynamicParameters();
                List<string> whereStatements = new List<string>();
                int pageNum = 1;
                int pageSize = 200;

                string isValid = IsPageSizeAndNumValid(queryParams, pageSizeLimit);
                if (!string.IsNullOrEmpty(isValid))
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", isValid));
                }

                if (queryParams.Any(x => x.Key.Equals("_direction", StringComparison.OrdinalIgnoreCase)))
                {
                    string[] allowedDirections = new string[] { "asc", "desc" };
                    string order = queryParams.FirstOrDefault(x => x.Key.Equals("_direction", StringComparison.OrdinalIgnoreCase)).Value;

                    if(allowedDirections.Contains(order.Trim().ToLower()))
                        orderDirection = order;
                    else
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Invalid direction by passed in the request"));
                }

                List<DefaultFilter> fieldList = new List<DefaultFilter>
            {
                new DefaultFilter("uid", "A.uid", SqlFieldType.Text),
                new DefaultFilter("name", "A.name", SqlFieldType.Text),
                new DefaultFilter("resourceUid", "A.resourceUid", SqlFieldType.Text),
                new DefaultFilter("resourceName", "A.resourceName", SqlFieldType.Text),
                new DefaultFilter("date", "A.date", SqlFieldType.DateTime),
                new DefaultFilter("action", "A.action", SqlFieldType.Text),
                new DefaultFilter("actionAssetUid", "A.actionAssetUid", SqlFieldType.Text),
                new DefaultFilter("actionAssetTypeUid", "A.actionAssetTypeUid", SqlFieldType.Text),
                new DefaultFilter("actionObject", "A.actionObject", SqlFieldType.Text),
                new DefaultFilter("actionObjectTypeName", "A.actionObjectTypeName", SqlFieldType.Text),
                new DefaultFilter("actionObjectName", "A.actionObjectName", SqlFieldType.Text),
                new DefaultFilter("actionDescription", "A.actionDescription", SqlFieldType.Text),
                new DefaultFilter("field", "A.field", SqlFieldType.Text),
                new DefaultFilter("newValue", "A.newValue", SqlFieldType.Text),
                new DefaultFilter("class", "A.class", SqlFieldType.Number),
                new DefaultFilter("version", "A.version", SqlFieldType.Number),
                new DefaultFilter("previousValue", "A.previousValue", SqlFieldType.Text)
            };

                if (queryParams.Any(x => x.Key.Equals("_order", StringComparison.OrdinalIgnoreCase)))
                {
                    var orderByCol = queryParams.FirstOrDefault(p => p.Key == "_order").Value;
                    if (!fieldList.Any(i => i.ApiName.Equals(orderByCol, StringComparison.OrdinalIgnoreCase)))
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Bad request submitted", "Invalid order by passed in the request"));

                    orderBySql = $" order by {orderByCol} {orderDirection} ";

                }
                else
                {
                    orderBySql = $" order by Date {orderDirection} ";
                }

                if (queryParams.Any(x => x.Key.Equals("_filter", StringComparison.OrdinalIgnoreCase)))
                {
                    var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_filter").Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var filterExpressionParser = new FilterExpressionParser(Company, FilterExpressionParseType.CustomFields, false);
                        filterExpressionParser.OverrideAllowedDefaultFields(fieldList);
                        Dictionary<string, object> sqlParams = new Dictionary<string, object>();
                        List<int> filteredFields = new List<int>();
                        whereStatements.Add("(" + filterExpressionParser.Parse(value, out sqlParams, out filteredFields) + ")");

                        foreach (var item in sqlParams)
                        {
                            dbArgs.Add(item.Key, item.Value);
                        }
                    }
                }

                bool isAssetType = false;
                AssetType assetType = null;
                if (
                    !Company.Any<Asset>(i => i.uid == assetUid) &&
                    !Company.Any<Tag>(i => i.uid == assetUid) &&
                    !Company.Any<IssueType>(i => i.uid == assetUid) &&
                    !Company.Any<IntersectType>(i => i.uid == assetUid) &&
                    !Company.Any<ResponsibilityType>(i => i.UID == assetUid))
                {
                    assetType = Company.Filter<AssetType>(i => i.uid == assetUid).SingleOrDefault();
                    if(assetType == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Asset, Asset Type, Tag, Workflow Type, RelationshipType or Responsibility Type not found for UID"));
                    isAssetType = true;
                }

                string baseSql = isAssetType ? GetBaseAuditQueryForAssetTypeUid(assetType?.Class == AssetTypeClass.Reference) : GetBaseAuditQueryForUid();
                dbArgs.Add("uid", assetUid);

                string whereSql = "";
                if (whereStatements.Any())
                    whereSql = $" where {string.Join(" and ", whereStatements)}";

                string offsetSql = "";
                if (queryParams.Any(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)))
                    if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageNum", StringComparison.OrdinalIgnoreCase)).Value, out pageNum))
                        if (pageNum < 1) pageNum = 1;

                if (queryParams.Any(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)))
                    if (int.TryParse(queryParams.FirstOrDefault(x => x.Key.Equals("_pageSize", StringComparison.OrdinalIgnoreCase)).Value, out pageSize))
                        if (pageSize < 1) pageSize = 1;

                if (pageSize > 0 || pageNum > 0)
                {
                    if (pageSize < 1) pageSize = 1;
                    if (pageNum < 1) pageNum = 1;
                    if (pageSize > pageSizeLimit) pageSize = pageSizeLimit;
                    if (pageNum > 10000) pageNum = 10000;
                    offsetSql = $" offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only ";
                }

                var countSql = string.Format(@"select count(1) from ({0}) A {1}", baseSql, whereSql);
                var sql = string.Format(@"select * from ({0}) A {1}", baseSql, whereSql);

                sql += " " + orderBySql + " " + offsetSql;

                var query = Company.Query<AssetAuditApiItemModel>(sql, dbArgs, ApiTimeout);

                if (isStreamResponse)
                {
                    string fileName = assetUid.ToString() + " Audit Data";
                    SLDocument document = GetExcelDocumentFromQuery(query);

                    var stream = new MemoryStream();
                    document.SaveAs(stream);
                    byte[] bytes = stream.ToArray();

                    var response = createFileResponseMessage(HttpStatusCode.OK, $"{fileName} {DateTime.Now.ToString("MMM dd yyyy")}.xlsx", bytes);
                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(response));
                }
                else
                {
                    var model = new AssetsApiViewModel();
                    model.total = Company.Query<int>(countSql, dbArgs, ApiTimeout).First();
                    model.pageNum = pageNum;
                    model.pageSize = pageSize;
                    model.items = query;

                    return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, model)));
                }
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Internal Server Error", errorMessage));
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
                result = Company.Query<dynamic>($@"select Object,ObjectId,Name as DisplayValue from AssetType where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();

            if (result == null)
                result = Company.Query<dynamic>($@"select 'Tag' as Object, ID as ObjectId,Value as DisplayValue from Tag where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();

            if (result == null)
                result = Company.Query<dynamic>($@"select 'IssueType' as Object, ID as ObjectId, Name as DisplayValue from IssueType where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();

            if (result == null)
                result = Company.Query<dynamic>($@"select 'IntersectType' as Object, ID as ObjectId, itn.name as DisplayValue
                    from dbo.[IntersectType] IT
                    CROSS APPLY dbo.GetIntersectTypeNames(IT.ID) ITN where uid = @assetUid", new { assetUid }, ApiTimeout).FirstOrDefault();

            
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
                return await AuditCombined(type, id, sortDataField);
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

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, new { total, results = query })));
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
            string querySql = @"select
                ga.*,
                CASE WHEN R.State = 1 THEN
                    R.FirstName + ' ' + R.LastName
                ELSE
                    R.FirstName + ' ' + R.LastName + ' (deleted)'
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
            if (auditingByType) {
                //Class value will be 11. To reuse columns from regular query, add by cross joining a select 11
                querySql += @"
                left outer join reporting.global_fieldaudit fa on(fa.auditid = ga.id) and ga.Action != 'Removed'
                inner join[reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
                cross join (select 11 as Class) AT
                cross join (select 11 as AssetTypeClass) AD
                where ga.[Object] = @objType";
            } else
            {
                querySql += @"
                left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
                inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID and ga.[Object] = @objType and ga.ObjectID = @objId
				left join AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID
                left join AssetDetail AD on AD.Object = ga.Object and AD.ObjectID = ga.ObjectID";
            }

            if (type.ToString() == "FusionType")
            {
                //Gets the Fusion audit for the fusion type
                querySql += @" UNION 
                        select 	                            
                        ga.*,
                        case when R.State = 1 then
                            R.FirstName + ' ' + R.LastName
                        else
                            R.FirstName + ' ' + R.LastName + ' (deleted)'
                        end as ResourceName, 
                            fa.FieldName as Field, 
                            fa.Value as NewValue, 
                            3 as Class,
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
                    inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID and ga.[Object] = 'Fusion' 
                    and ga.ObjectID in ( select Id from Fusion where fusiontypeid = @objId)";
            }

            if (type == SystemObjects.ReferenceItemType)
            {
                querySql += @" UNION
                    select 	                            
                    ga.*,
                    case when R.State = 1 then
                        R.FirstName + ' ' + R.LastName
                    else
                        R.FirstName + ' ' + R.LastName + ' (deleted)'
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
        /// <returns>A bease quert with @uid</returns>
        private string GetBaseAuditQueryForUid()
        {
            //This query should generally match the one in GetBaseAuditQueryForId except UID columns are returned instead of Object/id same

            string querySql = @"select
	            ad.uid,
	            ad.DisplayValue as name,
	            r.uid as resourceUid,
	            CASE WHEN R.State = 1 THEN
		            R.FirstName + ' ' + R.LastName
	            ELSE
		            R.FirstName + ' ' + R.LastName + ' (deleted)'
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
            from reporting.global_audit ga
            left outer join reporting.global_fieldaudit fa on ( fa.auditid = ga.id) 
            inner join [reporting].[Global_Resource] R on R.ResourceID = ga.ResourceID
            left join AssetType AT on AT.Object = ga.Object and AT.ObjectID = ga.ObjectID
            left join Asset ActionA on ActionA.Object = ga.ActionObject and ActionA.ObjectID = ga.ActionObjectID
            left join AssetType ActionAT on ActionA.AssetTypeID = ActionAT.ID
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
			) AD on AD.Object = ga.Object and AD.ObjectID = ga.ObjectID and AD.uid = @uid";

            return querySql;
        }

        private string GetBaseAuditQueryForAssetTypeUid(bool includeReferenceItem)
        {
            string querySql = @"select
	            at.uid,
	            at.Name as name,
	            r.uid as resourceUid,
	            CASE WHEN R.State = 1 THEN
		            R.FirstName + ' ' + R.LastName
	            ELSE
		            R.FirstName + ' ' + R.LastName + ' (deleted)'
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
            left join AssetType ActionAT on ActionA.AssetTypeID = ActionAT.ID";

            if(includeReferenceItem)
            {
                querySql += @" UNION select
                    ad.uid,
	                ad.DisplayValue as name,
	                r.uid as resourceUid,
	                CASE WHEN R.State = 1 THEN
                        R.FirstName + ' ' + R.LastName
                    ELSE
                        R.FirstName + ' ' + R.LastName + ' (deleted)'
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

        public string IsPageSizeAndNumValid(IEnumerable<KeyValuePair<string, string>> queryParams, int pageSizeLimit = 250)
        {
            var parameters = queryParams.ToList();
            long pageSize = 0;
            long pageNum = 0;

            if (parameters.Any(q => q.Key == "_pageSize"))
            {
                var _pageSize = queryParams.ToList().FirstOrDefault(q => q.Key == "_pageSize").Value;
                if (_pageSize.Length > 10)
                    return "Invalid pageSize value provided.";
                if (long.TryParse(_pageSize, out pageSize))
                {
                    if (pageSize > pageSizeLimit) return "Invalid pageSize value provided. Number is too large";
                    if (pageSize <= 0) return "Invalid pageSize value provided. Value must be greater than 0";
                }
                else
                    return "Invalid pageSize value provided. Must be a numeric value";
            }

            if (parameters.Any(q => q.Key == "_pageNum"))
            {
                var _pageNum = queryParams.ToList().FirstOrDefault(q => q.Key == "_pageNum").Value;
                if (_pageNum.Length > 10)
                    return "Invalid pageNum value provided.";
                if (long.TryParse(_pageNum, out pageNum))
                {
                    if (pageNum > 10000) return "Invalid pageNum value provided. Number is too large";
                    if (pageNum <= 0) return "Invalid pageNum value provided. Value must be greater than 0";
                }
                else
                    return "Invalid pageNum value provided. Must be a numeric value.";
            }

            return "";
        }
    }
}
