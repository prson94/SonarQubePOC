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
        /// <param name="Class">Allows for filtering the Asset type's by Class.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("types"),
            SwaggerConsumes("application/json", "application/xml"), SwaggerProduces("application/json", "application/xml"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset types.", typeof(List<AssetTypeApiViewModel>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<HttpResponseMessage> GetAssetTypesAsync(core.enums.AssetTypeClass? Class = null)
        {
            var prefix = "Assets.GetAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                var dbArgs = new DynamicParameters();
                string condition = "";
                if (Class.HasValue)
                {
                    var Id = (int)Class;
                    dbArgs.Add("@Id", Id.ToString());
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
        /// Add an asset type based on Asset Type Class
        /// </summary>
        /// <remarks>
        /// This endpoint can add the following asset type class
        /// Glossary,Model,Organization,Policy,Reference,Rule
        /// </remarks>
        /// <param name="model">Asset Type</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route(""),
            SwaggerRequestExample(typeof(AssetTypeInsert), typeof(AssetTypeInsertExample)),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "Newly asset type Uid and success / failure message.", typeof(AssetTypeSuccess)),
            SwaggerResponse(HttpStatusCode.NotFound, "Asset Type not found based on Uid provided.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "You are not allowed to create an asset type", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostAssetTypeAsync(AssetTypeInsert model)
        {
            

            var prefix = "Assets.PostAssetTypeAsync => ";
            var errorMessage = "";
            try
            {


                var parentType = SystemObjects.ArtifactType;
                var isNamePartOfKey = true;
                var nameFriendlyName = "Name";
                AssetType assetType = null;
                AssetType parentAssetType = null;
                Predicate predicate = null;

                #region Validation

                List<AssetTypeClass> predicateClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Reference };
                List<AssetTypeClass> parentAssetTypeClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Reference };

                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not authorized to perform this action."));

              
                if (!Enum.TryParse<AssetTypeClass>(model.Class, out AssetTypeClass typeClass))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "No valid Class provided.Please check your request and try again."));
                model.AssetTypeClass = typeClass;

                List<AssetTypeClass> supportedClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Model, AssetTypeClass.Organization, AssetTypeClass.Policy, AssetTypeClass.Reference, AssetTypeClass.Rule };
                if (!supportedClass.Contains(model.AssetTypeClass))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Not supported class type"));


                if (string.IsNullOrEmpty(model.Name.Trim()))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "No valid Name provided.Please check your request and try again."));

                if (model.ParentUid != Guid.Empty)
                {
                    parentAssetType = Company.Filter<AssetType>(x => x.uid == model.ParentUid).SingleOrDefault();
                    if (parentAssetType == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Not valid ParentUid provided.Please check your request and try again."));
                    else if (parentAssetType.Object != this.GetSystemObjects(model.AssetTypeClass).ToString())
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Not valid ParentUid provided.Please check your request and try again."));
                    else if (!parentAssetTypeClass.Contains(model.AssetTypeClass))
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Not valid ParentUid for the Class.Please check your request and try again."));
                }


                if (model.Hierarchy != null && model.Hierarchy.PredicateUid != Guid.Empty)
                {
                    predicate = Company.Filter<Predicate>(x => x.UID == model.Hierarchy.PredicateUid).SingleOrDefault();
                    if (predicate == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a proper predicate based on its asset type class"));
                }



                if (parentAssetType != null && predicate == null && predicateClass.Contains(model.AssetTypeClass))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a proper predicate based on its asset type class"));
                else if (parentAssetType == null && predicate != null && parentAssetTypeClass.Contains(model.AssetTypeClass))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Asset Type not found based on Uid provided"));
                else if (parentAssetType != null && predicate != null && (model.AssetTypeClass == AssetTypeClass.Glossary || model.AssetTypeClass == AssetTypeClass.Reference) && predicate.ID != 4 && predicate.ID != 9)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a proper predicate based on its asset type class"));
                else if (parentAssetType != null && predicate != null && (model.AssetTypeClass == AssetTypeClass.Model || model.AssetTypeClass == AssetTypeClass.Policy) && (predicate.ID != 8))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a proper predicate based on its asset type class"));
                


                if (!this.IsValidDisplayFormat(0, model.DisplayFormat))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Display Format contains invalid field references."));


                #endregion

                switch (model.AssetTypeClass)
                {
                    case AssetTypeClass.Glossary:
                        #region
                        var a = new ArtifactType
                        {
                            Name = model.Name,
                            DisplayFormat = model.DisplayFormat,
                            Description = model.Description,
                            CanOwnFusion = false
                        };
                        Company.Add(a);
                        parentType = SystemObjects.ArtifactType;
                        model.ObjectID = a.ID;
                        model.Object = SystemObjects.ArtifactType.ToString();

                        #endregion
                        break;
                    case AssetTypeClass.Organization:
                        #region
                        var org = new OrganizationType
                        {
                            Name = model.Name,
                            Description = model.Description,
                            DisplayFormat = model.DisplayFormat
                        };
                        var existing = Company.Filter<OrganizationType>(o => o.Name == org.Name && o.State == State.Active).FirstOrDefault();
                        if (existing != null)
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Wrong Name", "There is already an organization type with that name."));
                        Company.Add(org);
                        parentType = SystemObjects.OrganizationType;
                        model.ObjectID = org.ID;
                        model.Object = SystemObjects.OrganizationType.ToString();
                        #endregion
                        break;
                    case AssetTypeClass.Policy:
                        #region
                        var p = new PolicyType
                        {
                            Name = model.Name,
                            DisplayFormat = model.DisplayFormat,
                            Description = model.Description,
                            MaximumDepth = model.Hierarchy.MaximumDepth,
                        };
                        Company.Add(p);
                        parentType = SystemObjects.PolicyType;
                        model.ObjectID = p.ID;
                        model.Object = SystemObjects.PolicyType.ToString();
                        #endregion
                        break;
                    case AssetTypeClass.Model:
                        #region
                        var t = new TaxonomyType
                        {
                            Name = model.Name,
                            DisplayFormat = model.DisplayFormat,
                            Description = model.Description,
                            MaximumDepth = model.Hierarchy.MaximumDepth,
                        };

                        if (t.MaximumDepth <= 0 || t.MaximumDepth > 10)
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Maximum Depth", "Invalid Maximum Depth,Model level specified must be a value between 1 and 10."));


                        Company.Add(t);
                        assetType = Company.Filter<AssetType>(x => x.ObjectID == t.ID && x.Object == "TaxonomyType").SingleOrDefault();
                        if (assetType == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Type", "Asset Not Found."));
                        for (int i = 1; i <= t.MaximumDepth; i++)
                        {
                            Company.Set<AssetTypeLevel>().Add(new AssetTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), AssetTypeID = assetType.ID });
                        }
                        Company.SaveChanges();

                        parentType = SystemObjects.TaxonomyType;
                        model.ObjectID = t.ID;
                        model.Object = SystemObjects.TaxonomyType.ToString();
                        #endregion
                        break;
                    case AssetTypeClass.Reference:
                        #region
                        var rt = new ReferenceItemType
                        {
                            Name = model.Name,
                            DisplayFormat = model.DisplayFormat,
                            Description = model.Description,
                            SourceNotes = model.Notes
                        };
                        isNamePartOfKey = false;
                        nameFriendlyName = "Long Description";
                        Company.Add(rt);
                        parentType = SystemObjects.ReferenceItemType;
                        model.ObjectID = rt.ID;
                        model.Object = SystemObjects.ReferenceItemType.ToString();
                        #endregion
                        break;
                    case AssetTypeClass.Rule:
                        #region
                        var r = new RuleType
                        {
                            Name = model.Name,
                            DisplayFormat = model.DisplayFormat,
                            Description = model.Description
                        };
                        Company.Add(r);
                        parentType = SystemObjects.Rule;
                        model.ObjectID = r.ID;
                        model.Object = SystemObjects.RuleType.ToString();
                        #endregion
                        break;
                }


                if (predicate != null)
                {
                    var intersectType = new IntersectType
                    {
                        Subject = parentType.ToString(),
                        SubjectID = (parentAssetType != null) ? parentAssetType.ObjectID : model.ObjectID,
                        SubjectCardinality = Cardinality.One,
                        Object = model.Object,
                        ObjectID = model.ObjectID,
                        ObjectCardinality = Cardinality.Many,
                        PredicateID = predicate.ID
                    };
                    Company.Add(intersectType);
                }

                //upsertObjectStyle(model.AssetType.Object, model.AssetType.ObjectID, model.IconForeColor, model.IconBackColor, model.AssetType.Name);


                if (model.ObjectID > 0)
                {
                    if (model.AssetTypeClass != AssetTypeClass.FusionAttribute && model.AssetTypeClass != AssetTypeClass.Organization)
                    {
                        Company.Add(new FieldType
                        {
                            ObjectID = model.ObjectID,
                            Object = model.Object,
                            IsListable = true,
                            IsRequired = true,
                            IsEditable = true,
                            FriendlyName = nameFriendlyName,
                            Name = "Name",
                            MaximumLength = 500,
                            MinimumLength = 1,
                            SortOrder = 1,
                            Type = DataType.Text.ToString(),
                            IsDisplayable = true,
                            IsPartOfKey = isNamePartOfKey
                        });
                    }
                }

                assetType = Company.Filter<AssetType>(x => x.ObjectID == model.ObjectID && x.Object == model.Object).SingleOrDefault();
                if (assetType == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Type", "Asset Not Found."));

                var result = new AssetTypeSuccess { Uid = assetType.uid, Message = "Asset Type is created", Success = true };

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        private bool IsValidDisplayFormat(int assetTypeId,string displayFormat)
        {
            List<string> fieldNames;
            if (assetTypeId == 0)
                fieldNames = new List<string> { "name" };
            else
                fieldNames = Company.Filter<FieldType>(x => x.AssetTypeID == assetTypeId).Select(x => x.FriendlyName.ToLower()).ToList();

            displayFormat = displayFormat.Replace("}{", "} {");
            var displayFieldNames = displayFormat.Split().Where(x => x.StartsWith("{") && x.EndsWith("}"))
                    .Select(x => x.ToLower().Replace("{", string.Empty).Replace("}", string.Empty))
                    .ToList();
            return !displayFieldNames.Except(fieldNames).Any();
        }
        private SystemObjects GetSystemObjects(AssetTypeClass assetTypeClass)
        {
            switch (assetTypeClass)
            {
                case AssetTypeClass.Glossary:
                    return SystemObjects.ArtifactType;
                case AssetTypeClass.Organization:
                    return SystemObjects.OrganizationType;
                case AssetTypeClass.Policy:
                    return SystemObjects.PolicyType;
                 case AssetTypeClass.Reference:
                    return SystemObjects.ReferenceItemType;
                case AssetTypeClass.Rule:
                    return SystemObjects.RuleType;
                case AssetTypeClass.Model:
                    return SystemObjects.TaxonomyType;
       
            }
            return SystemObjects.ArtifactType;//default
        }
        /// <summary>
        /// Updates an asset type based on the specific asset type unique identifier.
        /// </summary>
        /// <remarks>
        /// This endpoint can update the following asset type class
        /// Glossary,Model,Organization,Policy,Reference,Rule
        /// </remarks>
        /// <param name="model"></param>
        /// <returns></returns>
        [
        HttpPut,
    Route(""),
    SwaggerRequestExample(typeof(AssetTypeInsert), typeof(AssetTypeInsertExample)),
    SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
    SwaggerResponse(HttpStatusCode.OK, "Update asset type and success / failure message.", typeof(AssetTypeSuccess)),
    SwaggerResponse(HttpStatusCode.NotFound, "Asset Type not found based on Uid provided.", typeof(ErrorResponse)),
    SwaggerResponse(HttpStatusCode.BadRequest, "Assets already exist with assigned parents. You may not change the parent of this asset type.", typeof(ErrorResponse)),
    SwaggerResponse(HttpStatusCode.BadRequest, "You have not provided a proper predicate based on its asset type class.", typeof(ErrorResponse)),
    SwaggerResponse(HttpStatusCode.BadRequest, "Display Format contains invalid field references.", typeof(ErrorResponse)),
    SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse)),
    SwaggerResponse(HttpStatusCode.Unauthorized, "You are not authorized to perform this action.", typeof(ErrorResponse))
]
        public async Task<IHttpActionResult> PutAssetTypeAsync(AssetTypeInsert model)
        {
            var prefix = "Assets.PutAssetTypeAsync => ";
            var errorMessage = "";
            try
            {
               
               // var parentType = SystemObjects.ArtifactType;
                bool shouldRemoveOldRelationshipType = false;
                bool shouldRemoveExistingParentChildRelationshipType = false;

                AssetType assetType = null;
                AssetType parentAssetType = null;
                Predicate predicate = null;

                #region Validation
                List<AssetTypeClass> predicateClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Reference };
                List<AssetTypeClass> parentAssetTypeClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Reference };

                if (!Company.CurrentResourceIsAdmin)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not authorized to perform this action."));

                if (!Enum.TryParse<AssetTypeClass>(model.Class, out AssetTypeClass typeClass))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "No valid Class provided.Please check your request and try again."));
                model.AssetTypeClass = typeClass;

                List<AssetTypeClass> supportedClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Model, AssetTypeClass.Organization, AssetTypeClass.Policy, AssetTypeClass.Reference, AssetTypeClass.Rule };
                if (!supportedClass.Contains(model.AssetTypeClass))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Not supported class type"));


                if (string.IsNullOrEmpty(model.Name.Trim()))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "No valid Name provided.Please check your request and try again."));

                assetType = Company.Filter<AssetType>(x => x.uid == model.Uid).SingleOrDefault();
                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Invalid request", "Asset Type not found based on Uid provided."));
                else if(assetType.Object != this.GetSystemObjects(model.AssetTypeClass).ToString())
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Invalid request", "Asset Type not found based on Class provided."));
                else
                {
                    model.Object = assetType.Object;
                    model.ObjectID = assetType.ObjectID;
                }

                if (model.ParentUid != Guid.Empty)
                {
                    parentAssetType = Company.Filter<AssetType>(x => x.uid == model.ParentUid).SingleOrDefault();
                    if (parentAssetType == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Not valid ParentUid provided.Please check your request and try again."));
                    else if (parentAssetType.Object != model.Object)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Not valid ParentUid provided.Please check your request and try again."));
                    else if (!parentAssetTypeClass.Contains(model.AssetTypeClass))
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Not valid ParentUid for the Class.Please check your request and try again."));
                }

                if(model.ParentUid == model.Uid)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Not valid ParentUid provided.Please check your request and try again."));

                if (model.Hierarchy != null && model.Hierarchy.PredicateUid != Guid.Empty)
                {
                    predicate = Company.Filter<Predicate>(x => x.UID == model.Hierarchy.PredicateUid).SingleOrDefault();
                    if (predicate == null)
                        return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a proper predicate based on its asset type class."));
                }

                
                if (parentAssetType != null && predicate == null && predicateClass.Contains(model.AssetTypeClass))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a proper predicate based on its asset type class"));
                else if (parentAssetType == null && predicate != null && parentAssetTypeClass.Contains(model.AssetTypeClass))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Asset Type not found based on Uid provided"));
                else if (parentAssetType != null && predicate != null && (model.AssetTypeClass == AssetTypeClass.Glossary || model.AssetTypeClass == AssetTypeClass.Reference) && predicate.ID != 4 && predicate.ID != 9)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a proper predicate based on its asset type class"));
                else if (parentAssetType != null && predicate != null && (model.AssetTypeClass == AssetTypeClass.Model || model.AssetTypeClass == AssetTypeClass.Policy) && (predicate.ID != 8))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "You have not provided a proper predicate based on its asset type class"));



                int assetCount = Company.Filter<Asset>(x => x.AssetTypeID == assetType.ID).Count();
                AssetType currentParentType = Company.GetParentType(assetType.ID, this.GetSystemObjects(model.AssetTypeClass));
                if (assetCount !=0 && currentParentType !=null  && currentParentType.uid != model.ParentUid)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Assets already exist with assigned parents. You may not change the parent of this asset type."));
                
                if(!this.IsValidDisplayFormat(assetType.ID,model.DisplayFormat))
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid request", "Display Format contains invalid field references."));
                
                
                #endregion

                switch (model.AssetTypeClass)
                {
                    case AssetTypeClass.Glossary:
                        var a = Company.GetById<ArtifactType>(model.ObjectID);
                        if (a == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Glossary.ToString()}", $"Not valid {AssetTypeClass.Glossary.ToString()} provided.Please check your request and try again."));

                        a.Name = model.Name;
                        a.DisplayFormat = model.DisplayFormat;
                        a.Description = model.Description;
                        //a.CanOwnFusion = model.CanOwnFusion ?? false;
                        a.AutoDisplayDescription = model.AutoDisplayDescription;

                        Company.Update(a);

                       // parentType = SystemObjects.ArtifactType;
                        break;
                    case AssetTypeClass.Organization:
                        var org = Company.GetById<OrganizationType>(model.ObjectID);
                        if (org == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Organization.ToString()}", $"Not valid {AssetTypeClass.Organization.ToString()} provided.Please check your request and try again."));
                        org.Name = model.Name;
                        org.Description = model.Description;
                        org.DisplayFormat = model.DisplayFormat;
                        Company.Update(org);

                       // parentType = SystemObjects.OrganizationType;
                        break;
                    case AssetTypeClass.Policy:
                        var p = Company.GetById<PolicyType>(model.ObjectID);
                        if (p == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Policy.ToString()}", $"Not valid {AssetTypeClass.Policy.ToString()} provided.Please check your request and try again."));

                        p.Name = model.Name;
                        p.DisplayFormat = model.DisplayFormat;
                        p.Description = model.Description;
                        p.MaximumDepth = model.Hierarchy.MaximumDepth;

                        Company.Update(p);

                       // parentType = SystemObjects.PolicyType;
                        break;
                    case AssetTypeClass.Reference:
                        var rt = Company.GetById<ReferenceItemType>(model.ObjectID);
                        if (rt == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.ReferenceItemType.ToString()}", $"Not valid {AssetTypeClass.ReferenceItemType.ToString()} provided.Please check your request and try again."));

                        rt.Name = model.Name;
                        rt.DisplayFormat = model.DisplayFormat;
                        rt.Description = model.Description;
                        rt.SourceNotes = model.Notes;

                        Company.Update(rt);

                        shouldRemoveOldRelationshipType = true;
                        shouldRemoveExistingParentChildRelationshipType = true;
                       // parentType = SystemObjects.ReferenceItemType;
                        break;
                    case AssetTypeClass.Model:
                        var t = Company.GetById<TaxonomyType>(model.ObjectID);
                    
                        if (t == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Model.ToString()}", $"Not valid {AssetTypeClass.Model.ToString()} provided.Please check your request and try again."));
                        
                        t.Name = model.Name;
                        t.DisplayFormat = model.DisplayFormat;
                        t.Description = model.Description;
                        t.MaximumDepth = model.Hierarchy.MaximumDepth;

                        if (t.MaximumDepth <= 0 || t.MaximumDepth > 10)
                            return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Maximum Depth", "Invalid Maximum Depth,Model level specified must be a value between 1 and 10."));


                        Company.Update(t);

                        for (int i = 1; i <= t.MaximumDepth; i++)
                        {
                            var level = assetType.AssetTypeLevels.SingleOrDefault(l => l.Level == i);
                            if (level == null)
                            {
                                Company.Set<AssetTypeLevel>().Add(new AssetTypeLevel { Description = string.Format("Level {0}", i), Level = i, Name = string.Format("Level {0}", i), AssetTypeID = assetType.ID });
                            }
                        }
                        Company.Delete<AssetTypeLevel>(l => l.Level > t.MaximumDepth);
                        Company.SaveChanges();

                        //parentType = SystemObjects.TaxonomyType;
                        break;
                    case AssetTypeClass.Rule:
                        #region
                        var r = Company.GetById<RuleType>(model.ObjectID);
                        if (r == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, $"Wrong {AssetTypeClass.Rule.ToString()}", $"Not valid {AssetTypeClass.Rule.ToString()} provided.Please check your request and try again."));
                        r.Name = model.Name;
                        r.DisplayFormat = model.DisplayFormat;
                        r.Description = model.Description;
                        Company.Update(r);
                        #endregion
                        break;
                }

                //  upsertObjectStyle(model.AssetType.Object, model.AssetType.ObjectID, model.IconForeColor, model.IconBackColor, model.AssetType.Name);
                var parentType = this.GetSystemObjects(model.AssetTypeClass).ToString();
                if (predicateClass.Contains(model.AssetTypeClass) && ( parentAssetType !=null || predicate != null))
                {
                    var parentPredicateType = PredicateType.InterTypeHierarchy;

                    if (model.AssetTypeClass == AssetTypeClass.Model || model.AssetTypeClass == AssetTypeClass.Policy)
                    {
                        parentPredicateType = PredicateType.IntraTypeHierarchy;
                    }

                    IntersectType intersectType = null;

                    if (shouldRemoveExistingParentChildRelationshipType)
                    {
                        intersectType = Company.Filter<IntersectType>(i =>
                            i.Subject == parentType &&
                            i.Object == model.Object &&
                            i.ObjectID == model.ObjectID &&
                            i.Predicate.Type == parentPredicateType
                        ).SingleOrDefault();
                    }
                    else
                    {
                        int subjectId = parentAssetType != null ? parentAssetType.ObjectID : model.ObjectID;
                        intersectType = Company.Filter<IntersectType>(i =>
                            i.Subject == parentType &&
                            i.SubjectID == subjectId &&
                            i.Object == model.Object &&
                            i.ObjectID == model.ObjectID &&
                            i.Predicate.Type == parentPredicateType
                        ).SingleOrDefault();
                    }

                    if (predicate !=null)
                    {
                        if (intersectType != null)
                        {
                            if (intersectType.PredicateID != predicate.ID)
                            {
                                intersectType.PredicateID = predicate.ID;
                                Company.Update(intersectType);
                            }

                            var parentID = (parentAssetType !=null ? parentAssetType.ObjectID : model.ObjectID);

                            if (intersectType.SubjectID != parentID)
                            {
                                intersectType.SubjectID = parentID;
                                Company.Update(intersectType);
                            }
                        }
                        else
                        {
                            intersectType = new IntersectType
                            {
                                IsSystem = true,
                                Subject = parentType,
                                SubjectID = parentAssetType != null ? parentAssetType.ObjectID : model.ObjectID,
                                Object = model.Object,
                                ObjectID = model.ObjectID,
                                PredicateID = predicate.ID
                            };
                            Company.Add(intersectType);
                        }
                    }
                }
                else if (shouldRemoveOldRelationshipType)
                {
                    var parentPredicateType = PredicateType.InterTypeHierarchy;

                    var intersectType = Company.Filter<IntersectType>(i =>
                        i.Object == model.Object &&
                        i.ObjectID == model.ObjectID &&
                        i.Predicate.Type == parentPredicateType
                    ).FirstOrDefault();

                    if (intersectType != null)
                    {
                        Company.Delete(SystemObjects.IntersectType, intersectType.ID);
                    }
                }

                //update affected display values
                Company.CreateOrUpdateTypeDisplayValuesAsync(model.ObjectID, model.Object.ToString());
                assetType = Company.Filter<AssetType>(x => x.uid == model.Uid).SingleOrDefault();
                if (assetType == null) return await Task.FromResult(errorMessageResponse(HttpStatusCode.BadRequest, "Invalid Type", "Asset Not Found."));
                var result = new AssetTypeSuccess { Uid = assetType.uid, Message = $"{assetType.Name} successfully updated.", Success = true };

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, result)));
               
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
