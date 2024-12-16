using d360.core.entities;
using d360.core.queue;
using d360.extensions;
using d360.web.Extensions;
using d360.web.Filters;
using d360.web.Models;
using Microsoft.Web.Http;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using d360.core.resources;

namespace d360.web.Controllers.V2
{
	[
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/crossreferences"), Authorize
    ]
    public class CrossReferencesController : BaseV2ApiController
    {
		private readonly IQueueSource Queue;
		private readonly IStorageProvider Storage;

		public CrossReferencesController(ICoreComponentSet set, IQueueSource queue, IStorageProvider storage) : base(set) 
		{
			Queue = queue;
			Storage = storage;
		}

        /// <summary>
        /// Returns all asset cross references.  Optional parameters are also supported, in the case where the optional parameters are specified only records matching that criteria are returned.
        /// </summary>
        /// <returns>An array of cross references</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A full list of asset cross references.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>)),
            SwaggerParameter("_assetUid", "The asset UID of the cross reference record.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_externalId", "The external ID of the cross reference record(s) to request.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_dataSource", "The data source of the cross reference record(s) to request.", DataType = "string", ParameterType = "query", Required = false),
            SwaggerParameter("_type", "The type of the cross reference record(s) to request.", DataType = "string", ParameterType = "query", Required = false),
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> Get()
        {
            var queryParams = Request.GetQueryNameValuePairs();
            var assetCrossReferences = await Catalog.ReadCrossReferencesAsync(queryParams);

            return Ok(assetCrossReferences);
        }

        /// <summary>
        /// Returns asset cross references for the specified asset based on its unique identifier.
        /// </summary>
        /// <param name="assetUid">The unique identifier of the asset.</param>
        /// <returns>An array of matching cross references.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("{assetUid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset cross references based on the public unique identifier (assetUid) of the asset.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>)), 
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> GetByUid(string assetUid)
        {
			var queryParams = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("_assetuid", assetUid)
			};

			var result = await Catalog.ReadCrossReferencesAsync(queryParams);

            return Ok(result);
        }

        /// <summary>
        /// Returns asset cross references for the specified type and external id.
        /// </summary>
        /// <param name="type">The type of the asset cross reference.</param>
        /// <param name="externalId">The external Id of asset cross reference.</param>
        /// <returns>An array of matching cross reference.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("{type}/{externalId}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset cross references based on the external type and identifier of the asset.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>)), 
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> GetByTypeID(string type, string externalId)
        {
			var queryParams = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("_type", type),
				new KeyValuePair<string, string>("_externalid", externalId)
			};

			var result = await Catalog.ReadCrossReferencesAsync(queryParams);

            return Ok(result);
        }

        /// <summary>
        /// Returns asset cross references for the specified type.
        /// </summary>
        /// <param name="type">The type of the asset cross reference.</param>
        /// <returns>An array of matching cross references.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("type/{type}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset cross references based on the external type.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>)), 
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> GetByType(string type)
        {
			var queryParams = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("_type", type)
			};

			var result = await Catalog.ReadCrossReferencesAsync(queryParams);

			return Ok(result);
        }

        /// <summary>
        /// Returns asset cross references for the specified data source.
        /// </summary>
        /// <param name="dataSource">The dataSource of asset cross reference.</param>
        /// <returns>An array of matching cross references.</returns>
        [
            HttpGet,
            MapToApiVersion("2.0"),
            Route("datasource/{dataSource}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A list of asset cross references based on the data source.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access Denied", typeof(List<AssetCrossReference>)), 
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> GetByDataSource(string dataSource)
        {
			var queryParams = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("_datasource", dataSource)
			};

			var result = await Catalog.ReadCrossReferencesAsync(queryParams);

			return Ok(result);
        }

        /// <summary>
        /// Creates a new asset cross reference.  If an asset cross reference exists already an error is returned.  ExternalID and DataSource fields have a limit of 250 ASCII characters each.  Type has a limit of 50 ASCII characters.
        /// </summary>
        /// <param name="model">The asset cross references model.</param>
        /// <returns>The model of the created asset cross reference. If item already exists http confict is returned.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route(""),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Asset cross reference model does not contain required fields.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.Conflict, "Asset cross reference already exists.", typeof(AssetCrossReference)), 
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> Post(AssetCrossReference model)
        {
			var response = await Catalog.CreateCrossReferenceAsync(model);
			return (response.IsSuccess) ?
				ResponseMessage(Request.CreateResponse(response.GetHttpStatusCode(), response.Data)) :
				errorMessageResponse(response.GetHttpStatusCode(), response.Message);
        }

        /// <summary>
        /// Creates new asset cross references.  If an asset cross reference exists already an error is returned.  ExternalID and DataSource fields have a limit of 250 ASCII characters each.  Type has a limit of 50 ASCII characters.
        /// </summary>
        /// <param name="models">List of asset cross references.</param>
        /// <returns>List of created asset cross references. If any item already exists an HTTP Confict is returned.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("bulk"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Conflict, "One or more asset cross references already exist.", typeof(List<AssetCrossReference>)), 
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> PostBulk(List<AssetCrossReference> models)
        {
            var execution = getApiExecution(models.Count, action: ApiExecutionAction.PostCrossReferences);
            await Catalog.CreateCrossReferencesAsync(execution, models);
			var results = await Catalog.ReadCrossReferenceResultsAsync(execution.ExecutionID);

			return await Task.FromResult<IHttpActionResult>(ResponseMessage(Request.CreateResponse(HttpStatusCode.OK, results))).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates the specified asset cross reference.  In order to update an asset cross reference record you must pass in the uid, datasource and type values for an existing cross reference item.  If you have special characters in your datasource, or type values use the PUT endpoint that only requires the uid in the URL.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset cross reference.</param>
        /// <param name="dataSource">Asset cross reference datasource</param>
        /// <param name="type">Asset cross reference type</param>
        /// <param name="externalId">Asset cross reference externalId</param>
        /// <param name="model">Asset cross reference model</param>
        /// <returns>Http Status code OK if asset cross reference was updated, Http Status code of Not Found if item could not be updated.</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("{uid:Guid}/{dataSource}/{type}/{externalId}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)), 
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> PutByXrefUid(Guid uid, string dataSource, string type, string externalId, AssetCrossReference model)
        {
			model.DataSource = dataSource;
			model.Type = type;
			model.ExternalID = externalId;
			model.uid = uid;
            var response = await Catalog.UpdateCrossReferenceAsync(model);

			if (response.IsSuccess)
			{
				return Ok();
			}
			else 
			{
				return errorMessageResponse(response.GetHttpStatusCode(), response.Message);
			}
        }

        /// <summary>
        /// Updates the specified asset cross reference.  In order to update an asset cross reference record you must pass in the uid, datasource and type values for an existing cross reference item.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset cross reference.</param>        
        /// <param name="model">Asset cross reference model</param>
        /// <returns>Http Status code OK if asset cross reference was updated, Http Status code of Not Found if item could not be updated.</returns>
        [
            HttpPut,
            MapToApiVersion("2.0"),
            Route("{uid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)),
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> PutByUid(Guid uid, AssetCrossReference model)
        {
			model.uid = uid;
			var response = await Catalog.UpdateCrossReferenceAsync(model);

			if (response.IsSuccess)
			{
				return Ok();
			}
			else
			{
				return errorMessageResponse(response.GetHttpStatusCode(), response.Message);
			}
		}

        /// <summary>
        /// Deletes all asset cross reference records by the specified unique identifier.  Asset cross reference records with the same uid and different datasource and or type will also be deleted.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset cross reference.</param>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("{uid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)),
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> DeleteByUid(Guid uid)
        {
			var queryParams = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("_assetuid", uid.ToString())
			};
			var response = await Catalog.RemoveCrossReferencesAsync(queryParams);

			if (response.IsSuccess)
			{
				return Ok();
			}
			else
			{
				return errorMessageResponse(response.GetHttpStatusCode(), response.Message);
			}
        }

        /// <summary>
        /// Deletes any asset cross references with the specified datasource and type.
        /// </summary>
        /// <param name="dataSource">Asset cross reference datasource</param>
        /// <param name="type">Asset cross reference type</param>
        /// <returns>Http Status code OK if asset cross references were deleted, Http Status code of Not Found if item could not be deleted></returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("{dataSource}/{type}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Request does not contain required parameters datasource and type.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)),
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> DeleteByDataSourceAndType(string dataSource, string type)
        {
			if (string.IsNullOrEmpty(dataSource) || string.IsNullOrEmpty(type))
            {
				return errorMessageResponse(HttpStatusCode.NotAcceptable, Error.RequestMissingDatasourceType);
            }
			
			var queryParams = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("_datasource", dataSource),
				new KeyValuePair<string, string>("_type", type)
			};
			var response = await Catalog.RemoveCrossReferencesAsync(queryParams);

			if (response.IsSuccess)
			{
				return Ok();
			}
			else
			{
				return errorMessageResponse(response.GetHttpStatusCode(), response.Message);
			}
		}

        /// <summary>
        /// Deletes any asset cross references with the specified type.
        /// </summary>
        /// <param name="type">Asset cross reference type.</param>
        /// <returns>Http Status code OK if assset cross references were deleted, Http Status code of Not Found if item could not be deleted</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("type/{type}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Request does not contain required parameter type.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)),
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> DeleteByType(string type)
        {
			if (string.IsNullOrEmpty(type))
			{
				return errorMessageResponse(HttpStatusCode.NotAcceptable, Error.RequestMissingType);
			}

			var queryParams = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("_type", type)
			};
			var response = await Catalog.RemoveCrossReferencesAsync(queryParams);

			if (response.IsSuccess)
			{
				return Ok();
			}
			else
			{
				return errorMessageResponse(response.GetHttpStatusCode(), response.Message);
			}
		}

        /// <summary>
        /// Deletes any asset cross references with the specified datasource.
        /// </summary>
        /// <param name="dataSource">Asset cross reference datasource.</param>
        /// <returns>Http Status code OK if asset cross references were deleted, Http Status code of Not Found if item could not be deleted</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("dataSource/{dataSource}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotAcceptable, "Request does not contain required parameter dataSource.", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference)), 
			RequireAdminPermissions
		]
        public async Task<IHttpActionResult> DeleteByDataSource(string dataSource)
        {
			if (string.IsNullOrEmpty(dataSource))
			{
				return errorMessageResponse(HttpStatusCode.NotAcceptable, Error.RequestMissingDatasource);
			}

			var queryParams = new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>("_datasource", dataSource)
			};
			var response = await Catalog.RemoveCrossReferencesAsync(queryParams);

			if (response.IsSuccess)
			{
				return Ok();
			}
			else
			{
				return errorMessageResponse(response.GetHttpStatusCode(), response.Message);
			}
		}

        /// <summary>
        /// Creates new asset cross references.  This endpoint is meant for batch processing.
        /// </summary>
        /// <param name="crossReferences">The payload of your request.</param>
        /// <returns>An HTTP status code and message.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("batch"),
            SwaggerResponse(HttpStatusCode.OK, "A response that provides the execution ID to use, in order to check on the status of your request.", typeof(ApiExecutionRecievedResponse)),
            SwaggerResponse(HttpStatusCode.Forbidden, "You are not allowed to add Asset Cross Reference", typeof(ErrorResponse)),
            SwaggerResponse(HttpStatusCode.InternalServerError, INTERNAL_ERROR_MESSAGE, typeof(ErrorResponse)),
			RequireAdminPermissions

		]
        public async Task<IHttpActionResult> PostBatchCrossReferenceAsync(List<AssetCrossReference> crossReferences)
        {
			if (crossReferences == null)
            {
                crossReferences = readRequestJsonContent<List<AssetCrossReference>>(Request).Result;
            }

            if (crossReferences == null)
            {
				return errorMessageResponse(HttpStatusCode.BadRequest, Error.InvalidRequest, Error.ErrorInvalidDatasetMessage);
            }

            var execution = getApiExecution(crossReferences.Count, action: ApiExecutionAction.PostCrossReferences);

			var executionInfo = new ApiExecutionInfo
			{
				CompanyID = SecurityContext.CompanyID,
				CompanyDomainPrefix = SecurityContext.CompanyPrefix,
				ExecutionID = Guid.NewGuid(),
				ResourceID = SecurityContext.ResourceID,
				SendWorkflowEvents = false
			};

			// Save to storage container.
			await Storage.CreateFile(executionInfo.StorageFolder, executionInfo.RequestFileName, JsonConvert.SerializeObject(crossReferences));

			// Save to the database.
			execution.ExecutionID = executionInfo.ExecutionID;
			Company.Add(execution);

			// Save to queue.
			await Queue.CreateMessageAsync(constants.Queue.Execution, executionInfo);

			return await sendExecutionProcessingResponse(executionInfo);
        }
    }
}
