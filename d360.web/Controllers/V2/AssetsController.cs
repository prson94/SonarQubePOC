using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using Dapper;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/assets"),
        Authorize
    ]
    public class AssetsController : BaseV2ApiController
    {
        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;

        public AssetsController(ICommunityContext community, ICompanyContext company, IStorageProvider storage, IQueueSource queueSource)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
        }

        #endregion

        #region utils
                
        private string getFieldDataType(FieldType field)
        {
            switch (field.Type)
            {
                case "Date":
                case "DateTime":
                    return "datetime";
                case "Number":
                    return "bigint";
                case "Decimal":
                    return "float";
                case "Boolean":
                    return "bit";
                default:
                    return "";
            }
        }

        private void getFieldSql(List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> fieldJoins, List<string> fieldColumns)
        {
            fieldTypes.ForEach(f =>
            {
                var defaultVal = f.DefaultFormattedValue;
                var joinPrefix = "left";
                var tableAlias = $"F{f.ID}";
                var columnName = f.Name;
                var valueColumn = "FormattedValue";
                var fieldDataType = getFieldDataType(f);

                if (f.Type == "Link")
                    valueColumn = "Value";

                if (f.Type == "FieldFromRelationship")
                {
                    if (!f.LookupObjectFieldTypeID.HasValue || !f.LookupObjectID.HasValue)
                        return;

                    var relatedField = Company.GetById<FieldType>((int)f.LookupObjectFieldTypeID);
                    if (relatedField == null)
                        return;

                }

                if (f.IsRequired && string.IsNullOrEmpty(f.DefaultValue))
                {
                    joinPrefix = "left";
                    if (!string.IsNullOrEmpty(fieldDataType))
                    {
                        if (fieldDataType == "bit")
                            fieldColumns.Add($"cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
                        else
                            fieldColumns.Add($"cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
                    }
                    else
                        fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
                }
                else
                {
                    if (!string.IsNullOrEmpty(f.DefaultValue))
                    {
                        if (!string.IsNullOrEmpty(fieldDataType))
                        {
                            if (fieldDataType == "bit")
                                fieldColumns.Add($"coalesce(cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
                            else
                                fieldColumns.Add($"coalesce(cast({tableAlias}.{valueColumn} as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
                        }
                        else
                            fieldColumns.Add($"coalesce({tableAlias}.{valueColumn}, @defaultValue{tableAlias}) as [{columnName}]");

                        dbArgs.Add($"@defaultValue{tableAlias}", defaultVal);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(fieldDataType))
                        {
                            if (fieldDataType == "bit")
                                fieldColumns.Add($"cast(case when {tableAlias}.{valueColumn} = 'true' then 1 else 0 end as {fieldDataType}) as [{columnName}]");
                            else
                                fieldColumns.Add($"cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
                        }
                        else
                            fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");

                    }

                }

                if (f.Type == "FieldFromRelationship")
                {
                    fieldJoins.Add($@"outer apply (
                        select top 1 
                            F.[Value], 
                            F.FormattedValue 
                        from [Intersect] I
                        inner join Asset R on R.[Object] = I.[Object] and R.ObjectID = I.ObjectID
                        inner join Field F on F.FieldTypeID = {f.LookupObjectFieldTypeID} and F.AssetID = R.ID
                        where I.[Subject] = A.Object and I.SubjectID = A.ObjectID and I.IntersectTypeID = {f.LookupObjectID}
                    ) {tableAlias}");

                }
                else
                {
                    fieldJoins.Add($"{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.[ObjectType] = A.[Object] and {tableAlias}.[ObjectID] = A.[ObjectID]");
                }
            });
        }

        private void getQueryParamsSql(AssetsApiViewModel model, AssetType assetType, List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> whereStatements, List<string> pagingSql, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if (queryParams != null)
            {

                var orderBySql = "";
                var offsetSql = "";
                var pageNum = -1;
                var pageSize = -1;

                //add base sort if none is specified
                if (!queryParams.Any(p => p.Key == "_order"))
                {
                    orderBySql = "order by A.ID";
                }

                queryParams
                    .ToList()
                    .ForEach(q =>
                    {
                        var key = q.Key.ToLower();

                        if (key.StartsWith("_"))
                        {
                            if (key == "_order")
                            {
                                if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "name")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "FA.Name";
                                }
                                else if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "sourceid")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "FA.SourceID";
                                }
                                else if (assetType.Object == "FusionAttributeType" && q.Value.ToLower() == "textpath")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "FA.TextPath";
                                }
                                else if (assetType.Object == "ReferenceItemType" && q.Value.ToLower() == "code")
                                {
                                    orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "RI.Code";
                                }
                                else
                                {
                                    var field = fieldTypes.FirstOrDefault(f => f.Name.ToLower() == q.Value.ToLower());
                                    var valueColumn = "FormattedValue";
                                    var fieldDataType = getFieldDataType(field);
                                    if (field.Type == "Link") valueColumn = "Value";

                                    if (field == null)
                                    {
                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + "A.ID";
                                        return;
                                    }

                                    if (!string.IsNullOrEmpty(fieldDataType))
                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"cast(F{field.ID}.{valueColumn} as {fieldDataType})";
                                    else
                                        orderBySql += (string.IsNullOrEmpty(orderBySql) ? "order by " : ", ") + $"F{field.ID}.{valueColumn}";
                                }
                            }
                            else if (key == "_pagenum")
                            {
                                if (int.TryParse(q.Value, out pageNum))
                                {
                                    if (pageNum < 1) pageNum = 1;
                                }
                            }
                            else if (key == "_pagesize")
                            {
                                if (int.TryParse(q.Value, out pageSize))
                                {
                                    if (pageSize < 1) pageSize = 1;
                                }
                            }
                        }
                        else
                        {
                            if (assetType.Object == "FusionAttributeType" && key == "name")
                            {
                                whereStatements.Add($"FA.[Name] = @faName");
                                dbArgs.Add($"@faName", q.Value);
                            }
                            else if (assetType.Object == "FusionAttributeType" && key == "sourceid")
                            {
                                whereStatements.Add($"FA.[SourceID] = @sourceID");
                                dbArgs.Add($"@sourceID", q.Value);
                            }
                            else if (assetType.Object == "FusionAttributeType" && key == "textpath")
                            {
                                whereStatements.Add($"FA.[TextPath] = @textpath");
                                dbArgs.Add($"@textpath", q.Value);
                            }
                            else if (assetType.Object == "ReferenceItemType" && key == "code")
                            {
                                whereStatements.Add($"RI.[Code] = @code");
                                dbArgs.Add($"@code", q.Value);
                            }
                            else
                            {
                                var field = fieldTypes.Find(f => f.Name.ToLower() == key);

                                if (field != null)
                                {
                                    var tableAlias = $"F{field.ID}";
                                    whereStatements.Add($"{tableAlias}.FormattedValue = @field{field.ID}");
                                    dbArgs.Add($"@field{field.ID}", q.Value);
                                }
                            }
                        }
                    });

                pagingSql.Add(orderBySql);

                if (pageSize > 0 || pageNum > 0)
                {
                    if (pageSize < 1) pageSize = 1;
                    if (pageNum < 1) pageNum = 1;

                    model.pageSize = pageSize;
                    model.pageNum = pageNum;

                    offsetSql = $"offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
                    pagingSql.Add(offsetSql);
                }

            }
        }

        #endregion

        /// <summary>
        /// Retrieves a list of all asset types classes.
        /// </summary>
        /// <returns>Returns a list of asset type classes.</returns>
        [
            HttpGet,
            Route("classes"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type classes.", typeof(List<AssetTypeClassInfo>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public HttpResponseMessage GetAssetTypeClassesAsync()
        {
            var prefix = "Assets.GetAssetTypeClassesAsync => ";
            var errorMessage = "";
            
            try
            {
                var classes = AssetTypeClass.Glossary.GetAsList();
                return Request.CreateResponse(HttpStatusCode.OK, classes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// GET a list of asset types.
        /// </summary>
        /// <returns></returns>
        [
            HttpGet,
            Route("types"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerParameter("_classID", "The Class ID of an asset type.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset types.", typeof(List<AssetTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetTypesAsync()
        {
            var prefix = "Assets.GetAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var dbArgs = new DynamicParameters();
                string condition = "";
                if (queryParams.ToList().Any(k => k.Key.ToLower() == "_classid"))
                {
                    var Id = Int32.Parse(queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_classid").Value);
                    dbArgs.Add("@Id", Id);
                    condition = "and A.[Class]=@Id";
                }

                var sql = $@"
                        SELECT      A.[Name]
                                    ,A.[Description]
                                    ,A.[Class] as ClassID
                                    ,A.[Notes]
                                    ,A.[uid],
                                    P.[Path]
                        FROM        AssetType A
                                    cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
                        where       A.[State] = 1
                        {condition}
                        order by    P.[Path]
                        ";
                var assetTypes = await Company.QueryAsync<AssetTypeApiViewModel>(sql, dbArgs);

                return Request.CreateResponse(HttpStatusCode.OK, assetTypes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix }
                });

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }



        private async Task<AssetsApiViewModel> GetAssets(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var assetTypeID = 0;
            var includeRelationships = false;

            var assetType = Company.AssetTypes.FirstOrDefault(t => t.uid == uid);
            if (assetType == null)
                throw new Exception("not found");

            assetTypeID = assetType.ID;

            var fieldTypes = Company.FieldTypes.Where(f => f.AssetTypeID == assetTypeID).ToList();

            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_predicateuid"))
                includeRelationships = true;

            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();
            List<string> whereStatements = new List<string>();
            List<string> pagingSql = new List<string>();
            
            var dbArgs = new DynamicParameters();
            var model = new AssetsApiViewModel();

            dbArgs.Add("@uid", uid.ToString());
            fieldJoins.Add("inner join AssetType T on T.ID = A.AssetTypeID and T.UID = @uid");

            List<string> countJoins = new List<string>(fieldJoins);

            if (includeRelationships)
            {
                var subjectAlias = "B";
                var objectAlias = "A";
                string relatedAssetUIDString = "";
                Guid relatedAssetUID;

                var predicateUID = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_predicateuid").Value;
                var intersectJoin = "";
                var reverseIntersectJoin = "";
                var relatedAssetSql = "";
                bool includeBoth = false;


                if (queryParams.ToList().Any(q => q.Key.ToLower() == "_objectuid"))
                {
                    relatedAssetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_objectuid").Value;
                    if (Guid.TryParse(relatedAssetUIDString, out relatedAssetUID))
                    {
                        dbArgs.Add("@relatedAssetUid", relatedAssetUID);
                        relatedAssetSql = $"where {subjectAlias}.[UID] = @relatedAssetUid";
                    }
                    intersectJoin = $"I.[Subject] = {objectAlias}.[Object] and I.SubjectID = {objectAlias}.ObjectID and I.[Object] = {subjectAlias}.[Object] and I.ObjectID = {subjectAlias}.ObjectID";

                }
                else if (queryParams.ToList().Any(q => q.Key.ToLower() == "_subjectuid"))
                {
                    relatedAssetUIDString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_subjectuid").Value;
                    if (Guid.TryParse(relatedAssetUIDString, out relatedAssetUID))
                    {
                        dbArgs.Add("@relatedAssetUid", relatedAssetUID);
                        relatedAssetSql = $"where {subjectAlias}.[UID] = @relatedAssetUid";
                    }
                    intersectJoin = $"I.[Subject] = {subjectAlias}.[Object] and I.SubjectID = {subjectAlias}.ObjectID and I.[Object] = {objectAlias}.[Object] and I.ObjectID = {objectAlias}.ObjectID";
                }
                else
                {
                    //subject and object not specified
                    includeBoth = true;
                    intersectJoin = $"I.[Subject] = {objectAlias}.[Object] and I.SubjectID = {objectAlias}.ObjectID and I.[Object] = {subjectAlias}.[Object] and I.ObjectID = {subjectAlias}.ObjectID";
                    reverseIntersectJoin = $"I.[Subject] = {subjectAlias}.[Object] and I.SubjectID = {subjectAlias}.ObjectID and I.[Object] = {objectAlias}.[Object] and I.ObjectID = {objectAlias}.ObjectID";
                }

                var innerSql = $@"
                            select 
                                B.[UID] as AssetUid, 
                                BD.DisplayValue,
                                TB.[Name] as TypeName,
                                P.[UID] as PredicateUid
                            from Asset B
                            inner join AssetType TB on TB.ID = B.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(B.ID) BD
                            inner join [Intersect] I on {intersectJoin}
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID
                            inner join [Predicate] P on P.ID = IT.PredicateID and P.[UID] = @predicateUid
                            {relatedAssetSql}";

                if (includeBoth)
                {
                    var reverseInnerSql = $@"
                            select 
                                B.[UID] as AssetUid, 
                                BD.DisplayValue,
                                TB.[Name] as TypeName,
                                P.[UID] as PredicateUid
                            from Asset B
                            inner join AssetType TB on TB.ID = B.AssetTypeID
                            cross apply dbo.GetAssetDisplayValueById(B.ID) BD
                            inner join [Intersect] I on {reverseIntersectJoin}
                            inner join IntersectType IT on IT.ID = I.IntersectTypeID
                            inner join [Predicate] P on P.ID = IT.PredicateID and P.[UID] = @predicateUid";

                    innerSql = $@"select * from (
                        {innerSql}
                        union all
                        {reverseInnerSql}) RI";
                }

                var joinSql = $@"
                    cross apply (
                        select (
                            {innerSql}
                            for json path
                        ) as Relationships
                    ) R";


                fieldColumns.Add("R.Relationships");
                dbArgs.Add("@predicateUid", predicateUID);

                fieldJoins.Add(joinSql);
            }

            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);

            if (includeRelationships)
                whereStatements.Add("R.Relationships is not null");

            if (!Company.CurrentResourceIsAdmin)
            {
                whereStatements.Add($"A.ID not in ({Company.GetNoReadSqlStatement()})");
                whereStatements.Add($"A.AssetTypeID not in ({Company.GetAssetTypeNoReadSqlStatement()})");
            }

            getQueryParamsSql(model, assetType, fieldTypes, dbArgs, whereStatements, pagingSql, queryParams);

            var whereSql = "";
            if (whereStatements.Any())
                whereSql = $"where {string.Join(" and ", whereStatements)}";

            var fieldsSql = "";
            if (fieldColumns.Any())
                fieldsSql = $",\n {string.Join(",\n", fieldColumns)}";


            var countSql = $@"
                select
                    count(*)
                from Asset A
                {(assetType.Object == "ReferenceItemType" ? " inner join ReferenceItem RI on RI.ID = A.ObjectID" : "")} 
                {(assetType.Object == "FusionAttributeType" ? " inner join FusionAttribute FA on FA.ID = A.ObjectID" : "")} 
                {string.Join("\n", string.IsNullOrWhiteSpace(whereSql) ? countJoins : fieldJoins)}
                {whereSql}";

            var sql = $@"
                select
                    A.ID as AssetId,
                    A.[UID] as [AssetUid],
                    A.AssetTypeId,
                    T.[UID] as AssetTypeUid,
                    A.UpdatedOn,
                    A.CreatedOn
                    {(assetType.Object == "ReferenceItemType" ? " , RI.Code" : "")} 
                    {(assetType.Object == "FusionAttributeType" ? " , FA.SourceID, FA.Name, FA.TextPath" : "")} 
                    {fieldsSql}
                from Asset A
                {(assetType.Object == "ReferenceItemType" ? " inner join ReferenceItem RI on RI.ID = A.ObjectID" : "")} 
                {(assetType.Object == "FusionAttributeType" ? " inner join FusionAttribute FA on FA.ID = A.ObjectID" : "")} 
                {string.Join("\n", fieldJoins)}
                {whereSql}
                {string.Join("\n", pagingSql)}
            ";

            var countResults = await Company.QueryAsync<int>(countSql, dbArgs);
            var count = countResults.First();

            var results = await Company.QueryAsync<dynamic>(sql, dbArgs);

            if (includeRelationships)
            {
                foreach (var result in results)
                {
                    result.Relationships = JsonConvert.DeserializeObject(result.Relationships);
                }
            }

            model.items = results;
            model.total = count;

            return model;
        }


        /// <summary>
        /// Retrieves assets for the given asset type unique identifier.
        /// </summary>
        /// <remarks>
        /// In addition to the below query parameters a field name for the asset type can be specified to filter by exact match. For example MyCustomField=someExactValue.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{assetTypeUid:Guid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetsApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerParameter("_pageSize", "The number of results to return per page. The default value is 200.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_pageNum", "The page number to return results for.", DataType = "integer", ParameterType = "query", Required = false),
            SwaggerParameter("_order", "The name of the field to order results by, ascending. By default the results are ordered by AssetId.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_predicateUid", "The Uid of a predicate type to return relationships for. If specified the results will include relationships of this predicate type. Assets without this type of relationship defined will be omitted.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_subjectUid", "The Uid of the subject side of a relationship to filter by in addition to filtering by predicate type. _predicateUid is required.", DataType = "string", ParameterType = "query", Required = false),
        ]
        public async Task<IHttpActionResult> GetAssetsAsync(Guid assetTypeUid)
        {
            var prefix = "Assets.GetAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var results = await GetAssets(assetTypeUid, queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }

        }

        /// <summary>
        /// Get field types for the given asset type Uid
        /// </summary>
        /// <param name="assetTypeUid">The Uid of the asset type</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("fields/{assetTypeUid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(AssetsApiViewModel)),
            SwaggerResponse(HttpStatusCode.BadRequest, "An error to indicate that your request to retrieve this asset is invalid, possibly due to an incorrectly formatted identifier (uid).", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAssetsTypeFieldsAsync(Guid assetTypeUid)
        {
            var prefix = "Assets.GetAssetsTypeFieldsAsync => ";
            var errorMessage = "";

            try
            {
                var assetTypeID = 0;
                assetTypeID = Company.AssetTypes.FirstOrDefault(t => t.uid == assetTypeUid)?.ID ?? 0;
                //Use same output format as FieldsController._FieldTypesByObject to preserve compatability
                var fieldTypes = Company.FieldTypes.Where(f => f.AssetTypeID == assetTypeID).Select(i => new {
                    i.FriendlyName,
                    i.Category,
                    i.DisplayDescription,
                    i.FormDescription,
                    i.ID,
                    i.IsListable,
                    i.IsRequired,
                    i.ColumnOrder,
                    i.SortOrder,
                    ObjectType = i.Object,
                    i.ObjectID,
                    i.Type
                }).ToList();

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, fieldTypes)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Adds a given set of assets based on the specific asset type unique identifier. Use this endpoint if you want to process under 250 items and need immediate results.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("{assetTypeUid:Guid}"),
            SwaggerRequestExample(typeof(AssetInsert), typeof(AssetInsertsExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostAssetsAsync(Guid assetTypeUid, List<AssetInsert> assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to add assets of this type."));

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == assetTypeUid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetInsert>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (assets.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} assets in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items."));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PostAssets { AssetTypeUid = assetTypeUid });

                Company.Add(execution);

                List<DatabaseBulkAssetResult> results = null;
                try
                {
                    results = Company.ImportAssets(execution, assetType, assets, true);

                    // Close execution record.
                    execution.Processed = results.Count;
                    execution.Error = results.Count(i => !i.Success);
                    execution.CompletedOn = DateTime.UtcNow;
                    Company.Update(execution);
                }
                catch (Exception ex)
                {
                    execution.ErrorMessage = ex.GetFullExceptionData(false);
                    execution.CompletedOn = DateTime.UtcNow;
                    Company.Update(execution);
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates a given set of assets based on the specific asset type unique identifier. Use this endpoint if you want to process under 250 items and need immediate results.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("{assetTypeUid:Guid}"),
            SwaggerRequestExample(typeof(AssetUpdate), typeof(AssetUpdatesExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutAssetsAsync(Guid assetTypeUid, List<AssetUpdate> assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to update assets of this type."));

            var prefix = "Assets.PutAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == assetTypeUid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetUpdate>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (assets.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} assets in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items."));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PutAssets { AssetTypeUid = assetTypeUid });

                Company.Add(execution);

                List<DatabaseBulkAssetResult> results = null;
                try
                {
                    results = Company.ImportAssets(execution, assetType, assets, false);

                    // Close execution record.
                    execution.Processed = results.Count;
                    execution.Error = results.Count(i => !i.Success);
                    execution.CompletedOn = DateTime.UtcNow;
                    Company.Update(execution);
                }
                catch (Exception ex)
                {
                    execution.ErrorMessage = ex.GetFullExceptionData(false);
                    execution.CompletedOn = DateTime.UtcNow;
                    Company.Update(execution);
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Removes a given set of assets based on the specific asset type unique identifier. Use this endpoint if you want to process under 250 items and need immediate results.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("{assetTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteAssetsAsync(Guid assetTypeUid, AssetDeletes assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to remove assets of this type."));

            var prefix = "Assets.DeleteAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == assetTypeUid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetDeletes>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                if (assets.Count > MAX_SYNCHRONOUS_API_ITEM_COUNT)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", $"You may only provide a maximum of {MAX_SYNCHRONOUS_API_ITEM_COUNT} assets in this request. Please call the BATCH API to submit more than {MAX_SYNCHRONOUS_API_ITEM_COUNT} items."));

                var execution = getApiExecution(assets.Count, new ApiExecutionFields_DeleteAssets { AssetTypeUid = assetTypeUid });

                Company.Add(execution);

                List<DatabaseBulkAssetResult> results = null;
                try
                {
                    results = Company.RemoveAssets(execution, assetType, assets);

                    // Close execution record.
                    execution.Processed = results.Count;
                    execution.Error = results.Count(i => !i.Success);
                    execution.CompletedOn = DateTime.UtcNow;
                    Company.Update(execution);
                }
                catch (Exception ex)
                {
                    execution.ErrorMessage = ex.GetFullExceptionData(false);
                    execution.CompletedOn = DateTime.UtcNow;
                    Company.Update(execution);
                }

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        #region Batch

        /// <summary>
        /// Adds a given set of assets based on the specific asset type unique identifier. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("batch/{assetTypeUid:Guid}"),
            SwaggerRequestExample(typeof(AssetInsert), typeof(AssetInsertsExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostBulkAssetsAsync(Guid assetTypeUid, List<AssetInsert> assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to add assets of this type."));

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == assetTypeUid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetInsert>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                var executionInfo = new ApiExecutionInfo
                {
                    CompanyID = Company.CurrentCompanyID,
                    CompanyDomainPrefix = Company.CurrentCompanyDomain,
                    ExecutionID = Guid.NewGuid(),
                    ResourceID = Company.CurrentResourceID,
                    Action = ApiExecutionAction.PostAssets
                };

                // Save to storage container.
                Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

                // Save to queue.
                await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

                // Save to the database.
                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PostAssets { AssetTypeUid = assetTypeUid });
                execution.ExecutionID = executionInfo.ExecutionID;

                Company.Add(execution);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            }
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates a given set of assets based on the specific asset type Uid. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("batch/{assetTypeUid:Guid}"),
            SwaggerRequestExample(typeof(AssetUpdate), typeof(AssetUpdatesExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutBulkAssetsAsync(Guid assetTypeUid, List<AssetUpdate> assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to update assets of this type."));

            var prefix = "Assets.PutBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == assetTypeUid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<List<AssetUpdate>>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                var executionInfo = new ApiExecutionInfo
                {
                    CompanyID = Company.CurrentCompanyID,
                    CompanyDomainPrefix = Company.CurrentCompanyDomain,
                    ExecutionID = Guid.NewGuid(),
                    ResourceID = Company.CurrentResourceID,
                    Action = ApiExecutionAction.PutAssets
                };

                // Save to storage container.
                //Storage.CreateFolder(executionInfo.StorageFolder);
                Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

                // Save to queue.
                await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);


                // Save to the database.
                var execution = getApiExecution(assets.Count, new ApiExecutionFields_PutAssets { AssetTypeUid = assetTypeUid });
                execution.ExecutionID = executionInfo.ExecutionID;
                Company.Add(execution);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            }
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Removes a given set of assets based on the specific asset type Uid. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <remarks>
        /// When using the ExecutionItemUid, keep in mind:
        /// * ExecutionItemUid is optional.
        /// * If you do not wish to provide an ExecutionItemUid, remove the entire line, including the preceding comma (, "ExecutionItemUid": "00000000-0000-0000-0000-000000000000").
        /// * If you provide ExecutionItemUids, values must be a unique across the entire request body.
        /// * You do not have to provide ExecutionItemUid values for all entries in a request.
        /// * ExecutionItemUid values, if provided, are returned in the response to allow you to correlate success / failure per item.
        /// </remarks>
        /// <param name="assetTypeUid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpDelete,
            Route("batch/{assetTypeUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution's unique identifier to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your asset was not found.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to add assets of this type.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> DeleteBulkAssetsAsync(Guid assetTypeUid, AssetDeletes assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to remove assets of this type."));

            var prefix = "Assets.DeleteBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == assetTypeUid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with Uid {assetTypeUid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetDeletes>(Request).Result;

                if (assets == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a valid JSON structure for this request."));

                var executionInfo = new ApiExecutionInfo
                {
                    CompanyID = Company.CurrentCompanyID,
                    CompanyDomainPrefix = Company.CurrentCompanyDomain,
                    ExecutionID = Guid.NewGuid(),
                    ResourceID = Company.CurrentResourceID,
                    Action = ApiExecutionAction.DeleteAssets
                };

                // Save to storage container.
                Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

                // Save to queue.
                await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

                // Save to the database.
                var execution = getApiExecution(assets.Count, new ApiExecutionFields_DeleteAssets { AssetTypeUid = assetTypeUid });
                execution.ExecutionID = executionInfo.ExecutionID;
                Company.Add(execution);

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            new ApiExecutionRecievedResponse
                            {
                                ExecutionID = executionInfo.ExecutionID,
                                Message = "Now processing request. Please check back with this ExecutionID for status.",
                                Uri = $"{Request.RequestUri.Scheme}://{Request.RequestUri.Host}/api/v2/assets/executions/{executionInfo.ExecutionID}/status"
                            }
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "AssetTypeUid", assetTypeUid.ToString() },
                    { "AssetCount", $"{((assets != null) ? assets.Count : 0)}" }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// GETs the status of an execution record, including the results for the execution.
        /// </summary>
        /// <param name="executionUid">The execution's unique identifier to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("executions/{executionUid:Guid}/status"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of assets.", typeof(ApiExecutionStatusModel)),
            SwaggerResponse(HttpStatusCode.NotFound, "An error to indicate that your status was not found.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid executionUid)
        {
            var prefix = "Assets.GetExecutionStatus => ";
            var errorMessage = "";

            try
            {
                var dbExecutionItem = Company.Filter<ApiExecution>(i => i.ExecutionID == executionUid).SingleOrDefault();

                if (dbExecutionItem == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Execution unique identifier not found."));
                }

                var info = new ApiExecutionInfo { CompanyID = Company.CurrentCompanyID, ExecutionID = executionUid };

                List<DatabaseBulkAssetResult> results = null;
                try
                {
                    var resultsJson = Storage.GetFileContentsAsString(info.StorageFolder, info.ResponseFileName);
                    results = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(resultsJson);
                }
                catch
                {
                }

                var statusModel = new ApiExecutionStatusModel
                {
                    CompletedOn = dbExecutionItem.CompletedOn,
                    Error = dbExecutionItem.Error,
                    Fields = Newtonsoft.Json.Linq.JObject.Parse(dbExecutionItem.Fields),
                    Processed = dbExecutionItem.Processed,
                    StartedOn = dbExecutionItem.StartedOn,
                    Total = dbExecutionItem.Total,
                    Results = results
                };

                return await Task.FromResult<IHttpActionResult>(
                    ResponseMessage(
                        Request.CreateResponse(
                            HttpStatusCode.OK,
                            statusModel
                        )
                    )
                );
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                SendException(ex, new Dictionary<string, string>() {
                    { "Endpoint Method", prefix },
                    { "ExecutionUid", executionUid.ToString() }
                });

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        #endregion
    }
}
