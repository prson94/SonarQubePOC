using d360.core.entities;
using d360.model;
using d360.web.Filters;
using Dapper;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace d360.web.Controllers.V2
{
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/crossreferences"), Authorize
    ]
    public class CrossReferencesController : BaseApiController
    {
        #region DI

        public CrossReferencesController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {

        }

        #endregion

        /// <summary>
        /// Returns all asset cross references.
        /// </summary>
        /// <returns>An array of cross references</returns>
        [
            HttpGet, MapToApiVersion("2.0"), Route(""),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.OK, "A full list of asset cross references.", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>)),
        ]
        public async Task<HttpResponseMessage> Get()
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference"));
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
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>))
        ]
        public async Task<HttpResponseMessage> GetByUid(string assetUid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse<IEnumerable<AssetCrossReference>>(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where uid = @assetUid", new { assetUid }));
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
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>))
        ]
        public async Task<HttpResponseMessage> GetByTypeID(string type, string externalId)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse<IEnumerable<AssetCrossReference>>(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [type] = @type and [ExternalID] = @externalId", new { type = new DbString { Value = type, IsFixedLength = true, Length = 50, IsAnsi = true }, externalId }));
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
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>))
       ]
        public async Task<HttpResponseMessage> GetByType(string type)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse<IEnumerable<AssetCrossReference>>(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [type] = @type", new { type = new DbString { Value = type, IsFixedLength = true, Length = 50, IsAnsi = true } }));
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
            SwaggerResponse(HttpStatusCode.Unauthorized, "Access Denied", typeof(List<AssetCrossReference>))
        ]
        public async Task<HttpResponseMessage> GetByDataSource(string dataSource)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse<IEnumerable<AssetCrossReference>>(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [datasource] = @dataSource", new { dataSource = new DbString { Value = dataSource, IsFixedLength = true, Length = 250, IsAnsi = true } }));
        }

        /// <summary>
        /// Creates a new asset cross reference.  If an asset cross reference exists already an error is returned.
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
            SwaggerResponse(HttpStatusCode.Conflict, "Asset cross reference already exists.", typeof(AssetCrossReference))
        ]
        public async Task<AssetCrossReference> Post(AssetCrossReference model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            //validate the model input
            if (string.IsNullOrEmpty(model.DataSource) || string.IsNullOrEmpty(model.ExternalID) || string.IsNullOrEmpty(model.Type))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));

            //check if the item already exists            
            if (await XrefExists(model))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Asset cross reference already exists."));
            }

            //create the new record
            var res = await Company.Database.Connection.ExecuteAsync("insert into assetcrossreference (uid,DataSource,Type,ExternalID,FieldHash) values(@u,@d,@t,@e,@f)", new { u = model.uid, d = model.DataSource, t = model.Type, f = model.FieldHash, e = model.ExternalID });

            if (res <= 0)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Asset cross reference already exists."));
            }

            return model;
        }

        /// <summary>
        /// Creates new asset cross references.  If an asset cross reference exists already an error is returned.
        /// </summary>
        /// <param name="models">List of asset cross references.</param>
        /// <returns>List of created asset cross references. If any item already exists an HTTP Confict is returned.</returns>
        [
            HttpPost,
            MapToApiVersion("2.0"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            Route("bulk"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(List<AssetCrossReference>)),
            SwaggerResponse(HttpStatusCode.Conflict, "One or more asset cross references already exist.", typeof(List<AssetCrossReference>))

        ]
        public async Task<List<AssetCrossReference>> PostBulk(List<AssetCrossReference> models)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));


            try
            {
                if (Company.Database.Connection.State != ConnectionState.Open)
                    Company.Database.Connection.Open();
                // bcp the records in
                using (var bulkCopy = new SqlBulkCopy((Company.Database.Connection) as SqlConnection))
                {
                    bulkCopy.BatchSize = models.Count;
                    bulkCopy.DestinationTableName = "AssetCrossReference";
                    bulkCopy.BulkCopyTimeout = 300;

                    var table = new DataTable();
                    var columnName = "uid";
                    table.Columns.Add(columnName, typeof(Guid));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    columnName = "DataSource";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    columnName = "Type";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    columnName = "ExternalID";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    columnName = "FieldHash";
                    table.Columns.Add(columnName, typeof(string));
                    bulkCopy.ColumnMappings.Add(columnName, columnName);

                    foreach (var item in models)
                    {
                        var row = table.NewRow();

                        row["uid"] = item.uid;
                        row["DataSource"] = item.DataSource;
                        row["Type"] = item.Type;
                        row["ExternalID"] = item.ExternalID;
                        row["FieldHash"] = item.FieldHash;

                        table.Rows.Add(row);
                    }

                    await bulkCopy.WriteToServerAsync(table);
                }

            }
            catch
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "One or more asset cross references already exist."));
            }

            //return the created items

            return models;
        }

        private async Task<bool> XrefExists(AssetCrossReference model)
        {
            return await Company.Database.Connection.QuerySingleAsync<bool>(@"if exists (select 1 from assetcrossreference where uid = @u and [type] = @t and datasource = @d and externalid = @e)
                        begin
                            select 1;
                                end
                        else 
                        begin
                            select 0;
                                end", new { u = model.uid, t = model.Type, d = model.DataSource, e = model.ExternalID });

        }

        /// <summary>
        /// Updates the specified asset cross reference.
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
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> Put(Guid uid, string dataSource, string type, string externalId, AssetCrossReference model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            //validate the model input
            if (string.IsNullOrEmpty(dataSource) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(externalId))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Asset cross reference model does not contain required fields."));

            //create the new record
            var res = await Company.Database.Connection.ExecuteAsync("update assetcrossreference set FieldHash = @fh where uid = @uid and DataSource = @ds and [Type] = @t", new { fh = model.FieldHash, uid = uid, ds = dataSource, t = type });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // updated

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing updated
        }

        /// <summary>
        /// Deletes an asset cross reference by the specified unique identifier.
        /// </summary>
        /// <param name="uid">The unique identifier of the asset cross reference.</param>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted.</returns>
        [
            HttpDelete,
            MapToApiVersion("2.0"),
            Route("{uid:Guid}"),
            SwaggerConsumes("application/json"), SwaggerProduces("application/json"),
            SwaggerResponse(HttpStatusCode.Forbidden, "Access Denied", typeof(AssetCrossReference)),
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> DeleteByUid(Guid uid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));


            //deletes the new record
            var res = await Company.Database.Connection.ExecuteAsync("delete assetcrossreference where uid = @uid", new { uid = uid });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
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
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> DeleteByDatasource(string dataSource, string type)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if (string.IsNullOrEmpty(dataSource) || string.IsNullOrEmpty(type))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Request does not contain required parameters datasource and type."));

            //deletes the new record
            var res = await Company.Database.Connection.ExecuteAsync("delete assetcrossreference where datasource = @d and [type] = @t", new { d = dataSource, t = type });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
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
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> DeleteByType(string type)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if (string.IsNullOrEmpty(type))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Request does not contain required parameter type."));

            //deletes the new record
            var res = await Company.Database.Connection.ExecuteAsync("delete assetcrossreference where [type] = @t", new { t = type });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
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
            SwaggerResponse(HttpStatusCode.NotFound, "Not found.", typeof(AssetCrossReference))
        ]
        public async Task<HttpResponseMessage> DeleteByDataSource(string dataSource)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            if (string.IsNullOrEmpty(dataSource))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Request does not contain required parameter dataSource."));

            //deletes the new record
            var res = await Company.Database.Connection.ExecuteAsync("delete assetcrossreference where [datasource] = @d", new { d = dataSource });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // deleted

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing deleted
        }
    }
}
