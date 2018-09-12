using d360.core.entities;
using d360.extensions;
using d360.model;
using Dapper;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
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

        IQueueSource QueueSource;

        public CrossReferencesController(CommunityContext community, CompanyContext company, IQueueSource queueSource)
            : base(community, company)
        {
            QueueSource = queueSource;
        }

        #endregion

        /// <summary>
        /// Returns all asset cross references
        /// </summary>
        /// <returns>An array of cross reference records</returns>
        [HttpGet, MapToApiVersion("2.0"), Route(""), SwaggerResponse(HttpStatusCode.OK, "A full list of asset cross reference values.", typeof(List<AssetCrossReference>))]
        public async Task<HttpResponseMessage> Get()
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference"));            
        }

        /// <summary>
        /// Returns asset cross reference values for the specified uid
        /// </summary>
        /// <returns>An array of matching cross reference records</returns>
        [HttpGet, MapToApiVersion("2.0"), Route("{uid}"), SwaggerResponse(HttpStatusCode.OK, "A list of asset cross reference values based on the public ID (uid) of the asset.", typeof(List<AssetCrossReference>))]
        public async Task<HttpResponseMessage> GetByUid(string uid)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse<IEnumerable<AssetCrossReference>>(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where uid = @uid", new { uid }));
        }

        /// <summary>
        /// Returns asset cross reference values for the specified type and external id
        /// </summary>
        /// <returns>An array of matching cross reference records</returns>
        [HttpGet, MapToApiVersion("2.0"), Route("{type}/{externalId}"), SwaggerResponse(HttpStatusCode.OK, "A list of asset cross reference values based on the external type and identifier of the asset.", typeof(List<AssetCrossReference>))]
        public async Task<HttpResponseMessage> GetByTypeID(string type, string externalId)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse<IEnumerable<AssetCrossReference>>(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [type] = @type and [ExternalID] = @externalId", new { type, externalId }));
        }

        /// <summary>
        /// Returns asset cross reference values for the specified type
        /// </summary>
        /// <returns>An array of matching cross reference records</returns>
        [HttpGet, MapToApiVersion("2.0"), Route("type/{type}"), SwaggerResponse(HttpStatusCode.OK, "A list of asset cross reference values based on the external type.", typeof(List<AssetCrossReference>))]
        public async Task<HttpResponseMessage> GetByType(string type)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse<IEnumerable<AssetCrossReference>>(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [type] = @type", new { type }));
        }


        /// <summary>
        /// Returns asset cross reference values for the specified data source
        /// </summary>
        /// <returns>An array of matching cross reference records</returns>
        [HttpGet, MapToApiVersion("2.0"), Route("datasource/{dataSource}"), SwaggerResponse(HttpStatusCode.OK, "A list of asset cross reference values based on the data source.", typeof(List<AssetCrossReference>))]
        public async Task<HttpResponseMessage> GetByDataSource(string dataSource)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            return Request.CreateResponse<IEnumerable<AssetCrossReference>>(await Company.QueryAsync<AssetCrossReference>("select uid, DataSource,Type,ExternalID,FieldHash from AssetCrossReference where [datasource] = @dataSource", new { dataSource }));
        }

        /// <summary>
        /// Creates a new AssetCrossReference record.  If an asset cross reference exists already an error is returned
        /// </summary>
        /// <returns>AssetCrossReference model of the created item if item already exists http confict is returned.</returns>
        [HttpPost, MapToApiVersion("2.0"), Route("")]
        public async Task<AssetCrossReference> Post(AssetCrossReference model)
        {
            if(!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));
            //validate the model input
            if (string.IsNullOrEmpty(model.DataSource) || string.IsNullOrEmpty(model.ExternalID) || string.IsNullOrEmpty(model.Type))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));

            //check if the item already exists            
            if(await XrefExists(model))
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Item already exists"));
            }

            //create the new record
            var res = await Company.Database.Connection.ExecuteAsync("insert into assetcrossreference (uid,DataSource,Type,ExternalID,FieldHash) values(@u,@d,@t,@e,@f)", new { u = model.uid, d = model.DataSource, t = model.Type, f = model.FieldHash, e = model.ExternalID });

            if(res <= 0)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Item already exists"));
            }

            return model;
        }

        /// <summary>
        /// Creates new AssetCrossReference records.  If an asset cross reference exists already an error is returned
        /// </summary>
        /// <returns>AssetCrossReference model of the created item if item already exists http confict is returned.</returns>
        [HttpPost, MapToApiVersion("2.0"), Route("bulk")]
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
            catch(Exception e)
            {
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "One or items already exist"));
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
        /// Updates the specified AssetCrossReference record.
        /// </summary>
        /// <returns>Http Status code OK if item was updated, Http Status code of Not Found if item could not be updated</returns>
        [HttpPut, MapToApiVersion("2.0"), Route("{uid}/{dataSource}/{type}/{externalId}")]
        public async Task<HttpResponseMessage> Put(Guid uid, string dataSource, string type, string externalId, AssetCrossReference model)
        {
            if (!Company.CurrentResourceIsAdmin)
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Access Denied"));

            //validate the model input
            if (string.IsNullOrEmpty(dataSource) || string.IsNullOrEmpty(type) || string.IsNullOrEmpty(externalId))
                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Model does not contain required fields."));
            
            //create the new record
            var res = await Company.Database.Connection.ExecuteAsync("update assetcrossreference set FieldHash = @fh where uid = @uid and DataSource = @ds and [Type] = @t", new { fh = model.FieldHash, uid = uid, ds = dataSource, t = type });

            if (res > 0) return Request.CreateResponse(HttpStatusCode.OK); // updated

            return Request.CreateResponse(HttpStatusCode.NotFound); // nothing updated
        }
                
        /// <summary>
        /// Deletes a AssetCrossReference by the specified UID value
        /// </summary>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted</returns>
        [HttpDelete, MapToApiVersion("2.0"), Route("{uid}")]
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
        /// Deletes a AssetCrossReference with the specified datasource and type
        /// </summary>
        /// <returns>Http Status code OK if item was deleted, Http Status code of Not Found if item could not be deleted</returns>
        [HttpDelete, MapToApiVersion("2.0"), Route("{dataSource}/{type}")]
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
        /// Deletes a AssetCrossReference records with the specified type
        /// </summary>
        /// <returns>Http Status code OK if item(s) was deleted, Http Status code of Not Found if item could not be deleted</returns>
        [HttpDelete, MapToApiVersion("2.0"), Route("type/{type}")]
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
        /// Deletes a AssetCrossReference records with the specified datasource
        /// </summary>
        /// <returns>Http Status code OK if item(s) was deleted, Http Status code of Not Found if item could not be deleted</returns>
        [HttpDelete, MapToApiVersion("2.0"), Route("dataSource/{dataSource}")]
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
