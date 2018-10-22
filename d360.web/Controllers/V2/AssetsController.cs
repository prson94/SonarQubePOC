using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.queue;
using d360.extensions;
using d360.model;
using d360.web.Models;
using Microsoft.Web.Http;
using Newtonsoft.Json;
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
using System.Data.Entity;
using Dapper;

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
    public class AssetsController : BaseApiController
    {
        #region DI

        IQueueSource QueueSource;
        IStorageProvider Storage;

        public AssetsController(CommunityContext community, CompanyContext company, IStorageProvider storage, IQueueSource queueSource)
            : base(community, company)
        {
            QueueSource = queueSource;
            Storage = storage;
        }

        #endregion

        #region utils

        private async Task<T> readRequestJsonContent<T>(HttpRequestMessage request)
        {
            string json = "";

            if (request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MultipartMemoryStreamProvider();
                await request.Content.ReadAsMultipartAsync(streamProvider);

                json = await streamProvider.Contents.Single().ReadAsStringAsync();
            }
            else
            {
                json = await request.Content.ReadAsStringAsync();
            }

            return JsonConvert.DeserializeObject<T>(json);
        }

        private string getFieldDataType(FieldType field)
        {
            switch (field.Type)
            {
                case "Number":
                    return "int";
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
                        fieldColumns.Add($"cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
                    else
                        fieldColumns.Add($"{tableAlias}.{valueColumn} as [{columnName}]");
                }
                else
                {
                    if (!string.IsNullOrEmpty(f.DefaultValue))
                    {
                        if (!string.IsNullOrEmpty(fieldDataType))
                            fieldColumns.Add($"coalesce(cast({tableAlias}.{valueColumn} as {fieldDataType}), @defaultValue{tableAlias}) as [{columnName}]");
                        else
                            fieldColumns.Add($"coalesce({tableAlias}.{valueColumn}, @defaultValue{tableAlias}) as [{columnName}]");

                        dbArgs.Add($"@defaultValue{tableAlias}", defaultVal);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(fieldDataType))
                            fieldColumns.Add($"cast({tableAlias}.{valueColumn} as {fieldDataType}) as [{columnName}]");
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
                    fieldJoins.Add($"{joinPrefix} join Field {tableAlias} on {tableAlias}.FieldTypeID = {f.ID} and {tableAlias}.AssetID = A.ID");
                }
            });
        }

        private void getQueryParamsSql(List<FieldType> fieldTypes, DynamicParameters dbArgs, List<string> whereStatements, List<string> pagingSql, IEnumerable<KeyValuePair<string, string>> queryParams)
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
                                var field = fieldTypes.FirstOrDefault(f => f.Name.ToLower() == q.Value.ToLower());
                                var valueColumn = "FormattedValue";
                                var fieldDataType = getFieldDataType(field);
                                if (field.Type == "Link") valueColumn = "Value";

                                if (field == null)
                                {
                                    orderBySql = "order by A.ID";
                                    return;
                                }

                                if (!string.IsNullOrEmpty(fieldDataType))
                                    orderBySql = $"order by cast(F{field.ID}.{valueColumn} as {fieldDataType})";
                                else
                                    orderBySql = $"order by F{field.ID}.{valueColumn}";
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
                            var field = fieldTypes.Find(f => f.Name.ToLower() == key);

                            if (field != null)
                            {
                                var tableAlias = $"F{field.ID}";
                                whereStatements.Add($"{tableAlias}.FormattedValue = @field{field.ID}");
                                dbArgs.Add($"@field{field.ID}", q.Value);
                            }
                        }
                    });

                pagingSql.Add(orderBySql);

                if (pageSize > 0 && pageNum > 0)
                {
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
            SwaggerResponse(HttpStatusCode.OK, "A list of asset type classes.", typeof(List<AssetTypeClassInfo>))
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
                Trace.TraceError("{0}{1}", prefix, errorMessage);

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
            SwaggerResponse(HttpStatusCode.OK, "A list of asset types.", typeof(List<AssetTypeApiViewModel>))
        ]
        public async Task<HttpResponseMessage> GetAssetTypesAsync()
        {
            var prefix = "Assets.GetAssetTypesAsync => ";
            var errorMessage = "";

            try
            {
                var assetTypes = await Company.QueryAsync<AssetTypeApiViewModel>(@"
SELECT		A.[Name]
			,A.[Description]
			,A.[Class] as ClassID
			,A.[Notes]
			,A.[uid],
			P.[Path]
FROM		AssetType A
			cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
where		A.[State] = 1
order by	P.[Path]
");

                return Request.CreateResponse(HttpStatusCode.OK, assetTypes);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return ReturnApiError(HttpStatusCode.InternalServerError, errorMessage);
            }
        }

        /// <summary>
        /// Get information for the given asset UID
        /// </summary>
        /// <param name="uid">The UID of the asset</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{uid}"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetAssetAsync(Guid uid)
        {
            var prefix = "Assets.GetAssetAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var results = await GetAssets(uid, queryParams, false);
                var asset = results.FirstOrDefault();

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, (object)asset)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }

        }

        /// <summary>
        /// Get a list of assets which have the specified asset as a subject.
        /// </summary>
        /// <param name="uid">The UID of the asset</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpGet,
            Route("{uid}/related"),
            SwaggerResponse(HttpStatusCode.OK, "", typeof(List<dynamic>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> GetRelatedAssetsAsync(Guid uid)
        {
            var prefix = "Assets.GetRelatedAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var queryParams = Request.GetQueryNameValuePairs();
                var asset = await GetRelatedAssets(uid, queryParams);

                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, (object)asset)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }


        }

        private async Task<IEnumerable<dynamic>> GetAssets(Guid uid, IEnumerable<KeyValuePair<string,string>> queryParams, bool byType)
        {
            var assetTypeID = 0;

            if (byType)
                assetTypeID = Company.AssetTypes.FirstOrDefault(t => t.uid == uid)?.ID ?? 0;
            else
                assetTypeID = Company.Assets.FirstOrDefault(a => a.uid == uid)?.AssetTypeID ?? 0;

            if (assetTypeID == 0)
                throw new Exception("not found");

            var fieldTypes = Company.FieldTypes.Where(f => f.AssetTypeID == assetTypeID).ToList();

            var sql = @"
                select
                    A.ID as AssetID,
                    A.[UID] as [AssetUID],
                    A.AssetTypeID,
                    T.[UID] as AssetTypeUID,
                    A.UpdatedOn,
                    A.CreatedOn
                    {0}
                from Asset A
                {1}
                {2}
                {3}
            ";

            List<string> fieldColumns = new List<string>();
            List<string> fieldJoins = new List<string>();
            var dbArgs = new DynamicParameters();

            dbArgs.Add("@uid", uid.ToString());

            if (byType)
                fieldJoins.Add("inner join AssetType T on T.ID = A.AssetTypeID and T.UID = @uid");
            else
                fieldJoins.Add("inner join AssetType T on T.ID = A.AssetTypeID");


            getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);
           

            List<string> whereStatements = new List<string>();
            List<string> pagingSql = new List<string>();

            if (!byType)
                whereStatements.Add("A.UID = @uid");

            getQueryParamsSql(fieldTypes, dbArgs, whereStatements, pagingSql, queryParams);
           

            var whereSql = "";
            if (whereStatements.Any())
                whereSql = $"where {string.Join(" and ", whereStatements)}";

            var fieldsSql = "";
            if (fieldColumns.Any())
                fieldsSql = $",\n {string.Join(",\n", fieldColumns)}";

            sql = string.Format(sql, fieldsSql, string.Join("\n", fieldJoins), whereSql, string.Join("\n",pagingSql));

            var result = await Company.QueryAsync<dynamic>(sql, dbArgs);

            return result;
        }

        protected async Task<IEnumerable<dynamic>> GetRelatedAssets(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {

            
            var intersectTypeSql = @"
                select 
                    IT.*
                from IntersectType IT
                inner join [Predicate] P on P.ID = IT.PredicateID
                inner join Asset A on A.[UID] = @uid
                inner join AssetType T on T.ID = A.AssetTypeID
                inner join AssetType O on O.Object = IT.Object and O.ObjectID = IT.objectID
                where IT.[Subject] = T.[Object] and IT.SubjectID = T.ObjectID {0}";


            var predicateFilter = "";
            var dbArgs = new DynamicParameters();
            dbArgs.Add("@uid", uid);

            if (queryParams != null && queryParams.Any(q => q.Key.ToLower() == "_predicateuid"))
            {
                predicateFilter = "and P.[UID] = @puid";
                dbArgs.Add("@puid", queryParams.First(q => q.Key.ToLower() == "_predicateuid").Value);
            }

            var intersectTypes = await Company.QueryAsync<IntersectType>(string.Format(intersectTypeSql, predicateFilter), dbArgs);
            var results = new List<dynamic>();

            
            intersectTypes
                .ToList()
                .ForEach(i =>
            {
                var sql = @"
                select 
                    A.ID as AssetID,
                    A.[UID] as [AssetUID],
                    A.AssetTypeID,
                    T.[UID] as AssetTypeUID,
                    A.UpdatedOn,
                    A.CreatedOn
                    {0}
                from Asset A
                inner join AssetType T on T.ID = A.AssetTypeID
                inner join Asset B on B.[UID] = @uid
                inner join [Intersect] I on I.IntersectTypeID = @intersectTypeId and I.[Subject] = B.[Object] and I.SubjectID = B.ObjectID and I.[Object] = A.[Object] and I.ObjectID = A.ObjectID
                {1}
                {2}
                {3}
            ";

                List<string> fieldColumns = new List<string>();
                List<string> fieldJoins = new List<string>();

                dbArgs = new DynamicParameters();
                dbArgs.Add("@uid", uid);
                dbArgs.Add("@intersectTypeId", i.ID, System.Data.DbType.Int32);

                var fieldTypes = Company.FieldTypes.Where(f => f.Object == i.Object && f.ObjectID == i.ObjectID).ToList();

                getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns);


                List<string> whereStatements = new List<string>();
                List<string> pagingSql = new List<string>();


                getQueryParamsSql(fieldTypes, dbArgs, whereStatements, pagingSql, queryParams);

                var whereSql = "";
                if (whereStatements.Any())
                    whereSql = $"where {string.Join(" and ", whereStatements)}";

                var fieldsSql = "";
                if (fieldColumns.Any())
                    fieldsSql = $",\n {string.Join(",\n", fieldColumns)}";

                sql = string.Format(sql, fieldsSql, string.Join("\n", fieldJoins), whereSql, string.Join("\n",pagingSql));

                var result = Company.Query<dynamic>(sql, dbArgs);

                results.AddRange(result.ToList());
            });

            return results;
        }


        /// <summary>
        /// Adds a given set of assets based on the specific asset type Uid. Use this endpoint if you want to process under 200 items and need immediate results.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost, 
            Route("{uid}"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostAssetsAsync(Guid uid, AssetInserts assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to add assets of this type."));

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == uid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with UID {uid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetInserts>(Request).Result;

                var results = (Company.Database.Connection as SqlConnection).InsertAssets(
                    QueueSource, 
                    Company.CurrentCompanyDomain, 
                    Company.CurrentCompanyID, 
                    Company.CurrentResourceID, 
                    assetType, 
                    assets
                );
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates a given set of assets based on the specific asset type Uid. Use this endpoint if you want to process under 200 items and need immediate results.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("{uid}"),
            SwaggerResponse(HttpStatusCode.OK, "A list of bulk asset results, including any error messages.", typeof(List<DatabaseBulkAssetResult>)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutAssetsAsync(Guid uid, AssetUpdates assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to update assets of this type."));

            var prefix = "Assets.PutAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == uid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with UID {uid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetUpdates>(Request).Result;

                var results = (Company.Database.Connection as SqlConnection).UpdateAssets(
                    QueueSource, 
                    Company.CurrentCompanyDomain, 
                    Company.CurrentCompanyID, 
                    Company.CurrentResourceID, 
                    assetType, 
                    assets
                );
                return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results)));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + (ex.InnerException != null ? ex.InnerException.Message : "");
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        #region Batch

        /// <summary>
        /// Adds a given set of assets based on the specific asset type Uid. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            Route("batch/{uid}"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PostBulkAssetsAsync(Guid uid, AssetInserts assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to add assets of this type."));

            var prefix = "Assets.PostBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == uid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with UID {uid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetInserts>(Request).Result;

                var executionInfo = new ApiExecutionInfo
                {
                    CompanyID = Company.CurrentCompanyID,
                    CompanyDomainPrefix = Company.CurrentCompanyDomain,
                    ExecutionID = Guid.NewGuid(),
                    Action = ApiExecutionAction.PostAssets
                };

                // Save to storage container.
                Storage.CreateFolder(executionInfo.StorageFolder);
                Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

                // Save to queue.
                await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

                // Save to the database.
                Company.Add(new ApiExecution
                {
                    ExecutionID = executionInfo.ExecutionID,
                    Error = 0,
                    Processed = 0,
                    Total = assets.Count,
                    StartedOn = DateTime.UtcNow,
                    ResourceID = Company.CurrentResourceID,
                    Fields = JsonConvert.SerializeObject(new ApiExecutionFields_PostAssets { AssetTypeUid = uid })
                });

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
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// Updates a given set of assets based on the specific asset type Uid. This endpoint is meant for a greater number of items as it stores the asset list for asynchronous or batch processing.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset type.</param>
        /// <param name="assets">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPut,
            Route("batch/{uid}"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, "An unknown error occured while processing this request.", typeof(ErrorResponse))
        ]
        public async Task<IHttpActionResult> PutBulkAssetsAsync(Guid uid, AssetUpdates assets)
        {
            if (!Company.CurrentResourceIsAdmin)
                return await Task.FromResult(errorMessageResponse(HttpStatusCode.Unauthorized, "Not authorized", "You are not allowed to update assets of this type."));

            var prefix = "Assets.PutBulkAssetsAsync => ";
            var errorMessage = "";

            try
            {
                var assetType = Company.Filter<AssetType>(i => i.uid == uid).SingleOrDefault();

                if (assetType == null)
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", $"Asset Type with UID {uid} could not be found."));

                if (assets == null)
                    assets = readRequestJsonContent<AssetUpdates>(Request).Result;

                var executionInfo = new ApiExecutionInfo
                {
                    CompanyID = Company.CurrentCompanyID,
                    CompanyDomainPrefix = Company.CurrentCompanyDomain,
                    ExecutionID = Guid.NewGuid(),
                    Action = ApiExecutionAction.PutAssets
                };

                // Save to storage container.
                Storage.CreateFolder(executionInfo.StorageFolder);
                Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(assets));

                // Save to queue.
                await QueueSource.CreateMessageAsync(Config.GetValue<string>("ApiExecutionQueue"), executionInfo);

                // Save to the database.
                Company.Add(new ApiExecution
                {
                    ExecutionID = executionInfo.ExecutionID,
                    Error = 0,
                    Processed = 0,
                    Total = assets.Count,
                    StartedOn = DateTime.UtcNow,
                    ResourceID = Company.CurrentResourceID,
                    Fields = JsonConvert.SerializeObject(new ApiExecutionFields_PostAssets { AssetTypeUid = uid })
                });

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
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        /// <summary>
        /// GETs the status of an execution record, including the results for the execution.
        /// </summary>
        /// <param name="uid">The execution ID to retrieve status for.</param>
        /// <returns></returns>
        [
            HttpGet,
            Route("executions/{uid}/status"),
            SwaggerResponse(HttpStatusCode.OK, "An execution status including a list of assets.", typeof(ApiExecutionStatusModel))
        ]
        public async Task<IHttpActionResult> GetExecutionStatus(Guid uid)
        {
            var prefix = "Assets.GetExecutionStatus => ";
            var errorMessage = "";

            try
            {
                var dbExecutionItem = Company.Filter<ApiExecution>(i => i.ExecutionID == uid).SingleOrDefault();

                if (dbExecutionItem == null)
                {
                    return await Task.FromResult(errorMessageResponse(HttpStatusCode.NotFound, "Not found", "Execution ID not found."));
                }

                var info = new ApiExecutionInfo { CompanyID = Company.CurrentCompanyID, ExecutionID = uid };

                List<DatabaseBulkAssetResult> results = null;
                try
                {
                    var resultsJson = Storage.GetFileContentsAsString(info.StorageFolder, info.ResponseFileName);
                    results = JsonConvert.DeserializeObject<List<DatabaseBulkAssetResult>>(resultsJson);
                }
                catch
                {
                }

                var statusModel = new ApiExecutionStatusModel {
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
                Trace.TraceError("{0}{1}", prefix, errorMessage);

                return await Task.FromResult(errorMessageResponse(HttpStatusCode.InternalServerError, "Unknown error", errorMessage));
            }
        }

        #endregion
    }
}
