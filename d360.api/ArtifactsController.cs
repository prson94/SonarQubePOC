using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using System.Web.Http.Filters;
using System.Web.Http.Description;
using d360.core.entities;
using d360.services.interfaces;
using System.Xml.Linq;
using System.Net;
using System.Net.Http;
using d360.core;
using d360.core.exceptions;
using d360.extensions;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Thinktecture.IdentityModel.Authorization.WebApi;
using d360.core.enums;

namespace d360.api
{
    #region Models

    public class ArtifactModelRequest : Dictionary<string, object> { }

    public class ArtifactModuleModelRequest : Dictionary<string, object> { }

    public class APIResponse
    {
        public int ID { get; set; }
        public string SourceID { get; set; }
        public string Name { get; set; }
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
    }

    #endregion

    [ApiExplorerSettings(IgnoreApi = false), RoutePrefix("artifacts")]
    public class ArtifactsController : BaseApiController
    {
        #region DI

        IArtifactService ArtifactService;
        IFieldService FieldService;
        IFusionService FusionService;
        ILineageService LineageService;

        public ArtifactsController(IArtifactService artifactService, IFieldService fieldService, IFusionService fusionService, ILineageService lineageService, IAuthenticationSource authenticationSource)
        {
            ArtifactService = artifactService;
            FieldService = fieldService;
            FusionService = fusionService;
            LineageService = lineageService;
            AuthenticationSource = authenticationSource;
        }

        #endregion

        /// <summary>
        /// Get a list of artifact types.
        /// </summary>
        /// <returns>A queryable list of artifacts.</returns>
        [Route(""), PermissionAuthorization] //(Permission.ArtifactTypeRead)ApiClaimsAuthorize(SystemObjects.ArtifactType, ClaimAction.READ), 
        public IQueryable<ArtifactModelRequest> GetArtifactTypes()
        {
            var types = ArtifactService.GetArtifactTypes().OrderBy(i => i.ParentID).ThenBy(i => i.Name);

            var list = new List<ArtifactModelRequest>();

            foreach (var type in types)
            {
                var listItem = new ArtifactModelRequest();

                //Static fields
                listItem.Add("ID", type.ID);
                listItem.Add("ParentID", type.ParentID);
                listItem.Add("Name", type.Name);
                listItem.Add("Description", type.Description);
                listItem.Add("CanOwnFusion", type.CanOwnFusion);
                listItem.Add("CanViewInMonitor", type.CanViewInMonitor);
                listItem.Add("ParticipateInContext", type.ParticipateInContext);
                
                // Add to list
                list.Add(listItem);
            }

            return list.AsQueryable();
        }

        /// <summary>
        /// Get a list of artifacts by type.
        /// </summary>
        /// <param name="typeID">The target type ID</param>
        /// <returns>A string</returns>
        [Route("{typeID}"), PermissionAuthorization] //(Permission.ArtifactRead)
        public IQueryable<Dictionary<string, object>> GetArtifactsByType(int typeID)
        {
            var qs = new QuerySettings(Request);
            var type = ArtifactService.GetArtifactType(typeID);
            if (type == null) throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
            return ArtifactService.GetArtifactsAsDictionary(typeID).AsQueryable();
        }

        /// <summary>
        /// Get a specific artifact with all fields loaded.
        /// </summary>
        /// <param name="typeID">The type ID</param>
        /// <param name="id">The artifact ID</param>
        /// <returns>Artifact</returns>
        [Route("{typeID}/{id}"), PermissionAuthorization] //(Permission.ArtifactRead)
        public ArtifactModelRequest GetArtifact(int typeID, int id)
        {
            var item = ArtifactService.GetArtifact(id);
            
            if (item == null)
            {
                throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }
            else
            {
                if (item.ArtifactTypeID != typeID)
                    throw new HttpResponseException(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
            }

            var fields = FieldService.GetFieldRelationsByObject(SystemObjects.Artifact, id).Where(i => i.IsListable).OrderBy(i => i.SortOrder);

            var listItem = new ArtifactModelRequest();

            //Static fields
            listItem.Add("ID", item.ID);
            listItem.Add("Name", item.Name);
            listItem.Add("Description", item.Description);
            listItem.Add("Status", item.Status);

            // Dynamic fields
            foreach (var f in fields)
            {
                listItem.Add(f.Name, f.FormattedValue);
            }

            return listItem;
        }

        ///// <summary>
        ///// Get all relationships for the given artifact.
        ///// </summary>
        ///// <param name="typeID">The type ID</param>
        ///// <param name="id">The artifact ID</param>
        ///// <returns>List of relationships</returns>
        //[Route("{typeID}/{id}/relationships"), PermissionAuthorization(Permission.ArtifactRead)]
        //public JObject GetArtifactRelationshipsWithMappings(int typeID, int id)
        //{
        //    var xml = LineageService.GetArtifactWithRelationshipsAndMappings(id);
        //    string json = JsonConvert.SerializeXNode(xml);
        //    return JObject.Parse(json);
        //}

        /// <summary>
        /// Get a specific artifact with all fields loaded.
        /// </summary>
        /// <param name="typeID">The type ID</param>
        /// <param name="id">The artifact ID</param>
        /// <returns>Artifact</returns>
        [Route("{typeID}"), HttpPost, PermissionAuthorization] //(Permission.ArtifactAdd)
        public APIResponse AddArtifact(int typeID, ArtifactModelRequest model)
        {
            var response = new APIResponse();
            Artifact item = null;
            
            try
            {
                var type = ArtifactService.GetArtifactType(typeID);
                
                #region Check that ArtifactType was found

                if (type == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                #endregion

                item.ArtifactTypeID = typeID;

                if (model.ContainsKey("Name")) item.Description = model["Name"].ToString();
                if (model.ContainsKey("Description")) item.Description = model["Description"].ToString();

                ArtifactService.AddArtifact(item);


                var fieldTypes = FieldService.GetFieldTypesByObject(SystemObjects.ArtifactType, typeID).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Artifact.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString() });
                });

                FieldService.AddOrUpdate(fields);

                response.ID = item.ID;
                response.ResponseCode = "200";
                response.ResponseMessage = "SUCCESS";
                return response;
            }
            catch (BaseException ex)
            {
                var msg = new HttpResponseMessage(ex.StatusCode);
                msg.ReasonPhrase = ex.StatusDescription;
                throw new HttpResponseException(msg);
            }
            catch (Exception ex)
            {
                var msg = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                msg.ReasonPhrase = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new HttpResponseException(msg);
            }
        }

        /// <summary>
        /// Get a specific artifact with all fields loaded.
        /// </summary>
        /// <param name="typeID">The type ID</param>
        /// <param name="id">The artifact ID</param>
        /// <returns>Artifact</returns>
        [Route("{typeID}/{id}"), HttpPut, PermissionAuthorization] //(Permission.ArtifactEdit)
        public APIResponse EditArtifact(int typeID, int id, ArtifactModelRequest model)
        {
            var response = new APIResponse { ID = id };

            try
            {
                var item = ArtifactService.GetArtifact(id);

                if (item == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }
                else
                {
                    if (item.ArtifactTypeID != typeID)
                    {
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                    }
                }

                var fieldTypes = FieldService.GetFieldTypesByObject(SystemObjects.ArtifactType, typeID).ToList();

                if (model.ContainsKey("Name")) item.Description = model["Name"].ToString();
                if (model.ContainsKey("Description")) item.Description = model["Description"].ToString();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Artifact.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString() });
                });

                ArtifactService.EditArtifact(item);
                FieldService.AddOrUpdate(fields);

                response.ResponseCode = "200";
                response.ResponseMessage = "SUCCESS";
                return response;
            }
            catch (BaseException ex)
            {
                var msg = new HttpResponseMessage(ex.StatusCode);
                msg.ReasonPhrase = ex.StatusDescription;
                throw new HttpResponseException(msg);
            }
            catch (Exception ex)
            {
                var msg = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                msg.ReasonPhrase = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                throw new HttpResponseException(msg);
            }
        }
    }
}
