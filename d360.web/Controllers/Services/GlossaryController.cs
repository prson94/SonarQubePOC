using d360.core.entities;
using d360.extensions;
using d360.model;
using d360.core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Query;
using System.Web.Http.OData;
using System.Dynamic;
using System.Runtime.Serialization;
using d360.core.exceptions;
using d360.core.enums;
using d360.web.Models.Attributes;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [RoutePrefix("services/glossary"), Authorize]
    public class GlossaryController : BaseApiController
    {
        #region DI

        public GlossaryController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        #region Artifacts

        /// <summary>
        /// Gets all artifact types.  You can optionally use OData query parameters: $filter, $order, $skip, $take, etc.
        /// </summary>
        /// <returns>A list of artifact types.</returns>
        [Route("artifacts")]
        public IQueryable<ArtifactType> GetArtifactTypes()
        {
            return Company.Table<ArtifactType>();
        }

        /// <summary>
        /// Gets all artifacts based on a given type ID.
        /// </summary>
        /// <returns>A list of artifacts.</returns>
        [Route("artifacts/{id:int}")]
        public IQueryable<dynamic> GetArtifactsByType(int id)
        {
            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Artifact", out joins, out columns);

            var querySql = string.Format(@"select	A.ID,
		A.Name,
		A.Description,
        A.ParentID,
		P.Name as Parent,
        dbo.GenerateObjectUrl('Artifact', P.ArtifactTypeID, P.ID) as ParentUrl,
		A.Status,
        A.DateLastCertified,
        {0}
		dbo.GenerateObjectUrl('Artifact', A.ArtifactTypeID, A.ID) as Url
from	Artifact A 
left join Artifact P on P.ID = A.ParentID {1}
where A.ArtifactTypeID = @id", columns, joins);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            return Company.Query<dynamic>(sql, new { id = id }).AsQueryable();
        }

        /// <summary>
        /// Add an artifact.
        /// </summary>
        /// <param name="id">The type ID</param>
        /// <param name="model">The artifact fields</param>
        /// <returns>Artifact</returns>
        [Route("artifacts/{id:int}"), HttpPost]
        public HttpResponseMessage AddArtifact(int id, Dictionary<string, string> model)
        {
            if (!Company.HasPermission(SystemObjects.ArtifactType, id, Claim.Create, ClaimObject.Root))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add an artifact.");

            Artifact item = null;

            try
            {
                var type = Company.GetById<ArtifactType>(id);

                #region Check that ArtifactType was found

                if (type == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                #endregion

                item.ArtifactTypeID = id;

                if (model.ContainsKey("Name")) 
                    item.Description = model["Name"].ToString();
                else
                    throw new MissingPropertiesException("Artifact");

                if (model.ContainsKey("Description")) 
                    item.Description = model["Description"].ToString();
                else
                    throw new MissingPropertiesException("Artifact");

                item.Status = "Draft";

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("Model");
                    }
                }
                if (parentID > 0) item.ParentID = parentID;

                var fieldTypes = Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, id).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Artifact.ToString(), Value = model[f.Name].ToString() });
                    else
                    {
                        if (f.IsRequired)
                            throw new MissingPropertiesException("Artifact");
                    }
                });

                Company.SaveOrUpdate<Artifact>(item);

                fields.ForEach(f => {
                    f.ObjectID = item.ID;
                });

                Company.AddOrUpdateFields(fields);

                return Request.CreateResponse<Artifact>(HttpStatusCode.Created, item);
            }
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }

        /// <summary>
        /// Update a specific artifact.
        /// </summary>
        /// <param name="typeID">The type ID</param>
        /// <param name="id">The artifact ID</param>
        /// <param name="model">The artifact fields</param>
        /// <returns>Artifact</returns>
        [Route("artifacts/{typeID:int}/{id:int}"), HttpPut]
        public HttpResponseMessage EditArtifact(int typeID, int id, Dictionary<string, string> model)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Artifact, id, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this artifact.");

                var item = Company.GetById<Artifact>(id);

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

                var fieldTypes = Company.GetFieldTypeRelationsByObject(SystemObjects.ArtifactType, typeID).ToList();

                if (model.ContainsKey("Name")) item.Name = model["Name"].ToString();
                if (model.ContainsKey("Description")) item.Description = model["Description"].ToString();

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("Model");
                    }
                }
                if (parentID > 0) item.ParentID = parentID;

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Artifact.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString() });
                });

                Company.SaveOrUpdate<Artifact>(item);
                Company.AddOrUpdateFields(fields);

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }

        #endregion

        #region Models

        /// <summary>
        /// Gets all model types.  You can optionally use OData query parameters: $filter, $order, $skip, $take, etc.
        /// </summary>
        /// <returns>A list of model types.</returns>
        [Route("models")]
        public IQueryable<TaxonomyType> GetModelTypes()
        {
            return Company.Table<TaxonomyType>();
        }

        /// <summary>
        /// Gets all models based on a given type ID.
        /// </summary>
        /// <returns>A list of models.</returns>
        [Route("models/{id:int}")]
        public IQueryable<dynamic> GetModelsByType(int id)
        {
            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Taxonomy", out joins, out columns);

            var querySql = string.Format(@"select	A.ID,
		A.Name,
		A.Description,
        A.ParentID,
		P.Name as Parent,
        dbo.GenerateObjectUrl('Taxonomy', P.TaxonomyTypeID, P.ID) as ParentUrl,
        {0}
		dbo.GenerateObjectUrl('Taxonomy', A.TaxonomyTypeID, A.ID) as Url
from	Taxonomy A {1}
left join Taxonomy P on P.ID = A.ParentID
where A.TaxonomyTypeID = @id ", columns, joins);

            var sql = string.Format(@"select * from ({0}) A", querySql);

            return Company.Query<dynamic>(sql, new { id = id }).AsQueryable();
        }

        /// <summary>
        /// Add a model.
        /// </summary>
        /// <param name="id">The type ID</param>
        /// <param name="model">The model fields</param>
        /// <returns>Model</returns>
        [Route("models/{id:int}"), HttpPost]
        public HttpResponseMessage AddModel(int id, Dictionary<string, string> model)
        {
            if (!Company.HasPermission(SystemObjects.TaxonomyType, id, Claim.Create, ClaimObject.Root))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add a model of this type.");

            Taxonomy item = null;

            try
            {
                var type = Company.GetById<TaxonomyType>(id);

                #region Check that TaxonomyType was found

                if (type == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                #endregion

                item.TaxonomyTypeID = id;

                if (model.ContainsKey("Name"))
                    item.Description = model["Name"].ToString();
                else
                    throw new MissingPropertiesException("Model");

                if (model.ContainsKey("Description"))
                    item.Description = model["Description"].ToString();
                else
                    throw new MissingPropertiesException("Model");

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("Model");
                    }
                }
                if (parentID > 0) item.ParentID = parentID;

                var fieldTypes = Company.GetFieldTypeRelationsByObject(SystemObjects.TaxonomyType, id).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Taxonomy.ToString(), Value = model[f.Name].ToString() });
                    else
                    {
                        if (f.IsRequired)
                            throw new MissingPropertiesException("Taxonomy");
                    }
                });

                Company.SaveOrUpdate<Taxonomy>(item);

                fields.ForEach(f =>
                {
                    f.ObjectID = item.ID;
                });

                Company.AddOrUpdateFields(fields);

                return Request.CreateResponse<Taxonomy>(HttpStatusCode.Created, item);
            }
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }

        /// <summary>
        /// Update a specific model.
        /// </summary>
        /// <param name="typeID">The type ID</param>
        /// <param name="id">The model ID</param>
        /// <param name="model">The model fields</param>
        /// <returns>Model</returns>
        [Route("models/{typeID:int}/{id:int}"), HttpPut]
        public HttpResponseMessage EditModel(int typeID, int id, Dictionary<string, string> model)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Taxonomy, id, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this model.");

                var item = Company.GetById<Taxonomy>(id);

                if (item == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }
                else
                {
                    if (item.TaxonomyTypeID != typeID)
                    {
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                    }
                }

                var fieldTypes = Company.GetFieldTypeRelationsByObject(SystemObjects.TaxonomyType, typeID).ToList();

                if (model.ContainsKey("Name")) item.Name = model["Name"].ToString();
                if (model.ContainsKey("Description")) item.Description = model["Description"].ToString();

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("Model");
                    }
                }
                if (parentID > 0) item.ParentID = parentID;

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Taxonomy.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString() });
                });

                Company.SaveOrUpdate<Taxonomy>(item);
                Company.AddOrUpdateFields(fields);

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (BaseException ex)
            {
                return Request.CreateErrorResponse(ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "An unknown error occured.  Please try again later.", ex);
            }
        }

        #endregion
    }
}
