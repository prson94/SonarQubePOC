using d360.core.entities;
using d360.model;
using d360.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using d360.core.exceptions;
using d360.core.enums;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Web.Http;
using Swashbuckle.Swagger.Annotations;
using d360.web.Models;
using System.Web.Http.Description;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This service houses all endpoints handling glossary-related data such as artifacts and models.
    /// </summary>
    [ApiVersion("1.0"), ApiExplorerSettings(IgnoreApi = true), RoutePrefix("services/deprecated/glossary"), Authorize]
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
        /// <param name="id">The ID of the artifact type.</param>
        /// <returns>A list of artifacts.</returns>
        [Route("artifacts/{id:int}"), SwaggerResponse(HttpStatusCode.OK, "A list of artifacts based on the type.", typeof(List<ArtifactResultModel>))]
        public HttpResponseMessage GetArtifactsByType(int id)
        {
            var joins = "";
            var columns = "";
            
            var fields = Company.Filter<FieldType>(i => i.Object == "ArtifactType" && i.ObjectID == id).ToList();
            var fieldTypeIDs = fields.Select(i => i.ID).ToList();
            var filteredLookupDefinitions = Company.Filter<FieldTypeFilteredLookupDefinition>(i => fieldTypeIDs.Contains(i.FieldTypeID), i => i.FieldTypeFilteredLookupDisplayFields).ToList();
            var parentIntersectType = Company.Filter<IntersectTypeDetail>(i => i.Object == "ArtifactType" && i.ObjectID == id && i.PredicateType == PredicateType.InterTypeHierarchy).FirstOrDefault();


            foreach (var f in fields)
            {
                var name = f.Name.Replace("'", "''").Replace("--", "");

                switch (f.Type)
                {
                    case "FilteredLookup":
                        var fld = filteredLookupDefinitions.SingleOrDefault(i => i.FieldTypeID == f.ID);
                        if (fld != null)
                        {
                            if (fld.FieldTypeFilteredLookupDisplayFields != null)
                            {
                                var where = string.Empty;
                                var orderBy = string.Empty;

                                #region Build sub-select

                                if (fld.FieldTypeFilteredLookupDisplayFields.Count > 0)
                                {
                                    var columnSql = new List<string>();
                                    var joinSql = new List<string>();

                                    foreach (var df in fld.FieldTypeFilteredLookupDisplayFields.OrderBy(i => i.SortOrder).ThenBy(i => i.FieldTypeName))
                                    {
                                        var selectPrefix = $"{name}_{df.FieldTypeID}_FLF_{df.ID}";
                                        
                                        columnSql.Add($"{selectPrefix}.FormattedValue as [{df.FieldTypeName}]");
                                        columnSql.Add($"{selectPrefix}.LookupUrl as [{df.FieldTypeName}Uri]");
                                        
                                        joinSql.Add($"left join Field {selectPrefix} on {selectPrefix}.FieldTypeID = {df.FieldTypeID} and {selectPrefix}.ObjectType = 'Lookup' and {selectPrefix}.ObjectID = L.ID");

                                        //Build where
                                        if (df.Filter)
                                        {
                                            where += (string.IsNullOrEmpty(where) ? "" : "AND ");
                                            where += $" {selectPrefix}.Value = A.ID";
                                        }

                                        #region Build order by

                                        if (df.SortOrder.HasValue)
                                        {
                                            orderBy += (string.IsNullOrEmpty(orderBy) ? "" : ", ");
                                            if (df.FieldTypeID > 0)
                                            {
                                                var fieldTypeInfo = Company.Filter<FieldType>(i => i.ID == df.FieldTypeID).SingleOrDefault();
                                                if (fieldTypeInfo != null)
                                                {
                                                    switch (fieldTypeInfo.Type)
                                                    {
                                                        case "Date":
                                                        case "DateTime":
                                                            orderBy += $" cast({selectPrefix}.FormattedValue as datetime) asc";
                                                            break;
                                                        case "Decimal":
                                                        case "Number":
                                                            orderBy += $" cast({selectPrefix}.FormattedValue as decimal) asc";
                                                            break;
                                                        default:
                                                            orderBy += $" {selectPrefix}.FormattedValue asc";
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    orderBy += $" {selectPrefix}.FormattedValue asc";
                                                }
                                            }
                                            else
                                            {
                                                orderBy += $" D_{fld.ID}.[{df.FieldTypeName}] asc";
                                            }
                                        }

                                        #endregion
                                    }

                                    columns += "(select  "; 
                                    columns += string.Join(", ", columnSql);

                                    columns += $@" from [Lookup] L ";

                                    columns += string.Join(" ", joinSql);

                                    if (!string.IsNullOrEmpty(where))
                                    {
                                        where = $" where {where}";
                                        columns += where;
                                    }

                                    if (!string.IsNullOrEmpty(orderBy))
                                    {
                                        orderBy = $" order by {orderBy}";
                                        columns += orderBy;
                                    }

                                    columns += $" for json path) as [{name}], ";
                                }

                                #endregion Build sub-select
                            }
                        }
                        break;
                    case "FusionLookup":
                    case "FieldFromRelationship":
                    case "OwnershipLookup":
                    case "RefListRelationship":
                    case "Relationship":
                    case "ComplexRelationLookup":
                        break;
                    default:
                        columns += $"T{f.ID}.FormattedValue as [{name}], ";
                        joins += $" left join FieldDetail T{f.ID} on T{f.ID}.Object = 'Artifact' and T{f.ID}.ObjectID = A.ID and T{f.ID}.FieldTypeID = {f.ID}";
                        break;
                }
            }

            fields = null;

            var parentColumns = "";
            var parentJoins = "";

            if (parentIntersectType != null)
            {
                parentColumns = ", P.[uid] as ParentUid, P.ObjectID as ParentID ";
                parentJoins = $@"
 left join [Intersect] PI on PI.IntersectTypeID = {parentIntersectType.ID} and PI.Object = O.Object and PI.ObjectID = O.ObjectID 
 left join Asset P on P.Object = PI.Subject and P.ObjectID = PI.SubjectID";
            }

            var querySql = $@"
select	A.ID,
        {columns}
		dbo.GenerateAssetUrl(O.ID) as Url,
        O.UID as uid
        {parentColumns}
from	Artifact A 
        {joins}
        inner join Asset O on O.Object = 'Artifact' and O.ObjectID = A.ID
        {parentJoins} 
where   A.ArtifactTypeID = @id 
        and O.ID not in ({Company.GetNoReadSqlStatement()})
for json path";

            var jsonResults = Company.Query<string>(querySql, new { id }).ToList();

            var json = string.Join("", jsonResults);
            var arr = JArray.Parse(json);

            var attributes = Company.Query<AttributeDetail>($@"
select  A.* 
from    AttributeDetail A 
        inner join Artifact AR on A.ObjectType = 'Artifact' and AR.ID = A.ObjectID and AR.ArtifactTypeID = {id}").ToList();

            var attributeFields = Company.Query<FieldWithRelation>($@"
select  F.* 
from    FieldWithRelation F
        inner join Attribute A on F.ObjectType = 'Attribute' and F.ObjectID = A.ID
        inner join Artifact AR on A.ObjectType = 'Artifact' and AR.ID = A.ObjectID and AR.ArtifactTypeID = {id}").ToList();

            foreach (JObject o in arr)
            {
                var artifactID = o.GetValue("ID").Value<int>();
                var children = GetAttributesProperty(attributes.Where(i => i.ObjectID == artifactID).ToList(), attributeFields, null);
                if (children != null)
                    o.Add("Attributes", children);
            }

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(
                arr.ToString(Newtonsoft.Json.Formatting.None), 
                Encoding.UTF8, 
                "application/json");
            return response;
        }

        /// <summary>
        /// Gets an artifact based on a given ID.
        /// </summary>
        /// <param name="typeID">The ID of the artifact type.</param>
        /// <param name="id">The ID of the specific artifact you want to retrieve.</param>
        /// <returns>An instance of an artifact.</returns>
        [Route("artifacts/{typeID:int}/{id:int}"), SwaggerResponse(HttpStatusCode.OK, "An artifact based on the type.", typeof(ArtifactResultModel))]
        public HttpResponseMessage GetArtifact(int typeID, int id)
        {
            var joins = "";
            var columns = "";

            var artifact = Company.Filter<Artifact>(i => i.ArtifactTypeID == typeID && i.ID == id).SingleOrDefault();

            if (artifact == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Artifact with Type of {typeID} and ID of {id} could not be located.");
            }

            var fields = Company.Filter<FieldType>(i => i.Object == "ArtifactType" && i.ObjectID == typeID).ToList();
            var fieldTypeIDs = fields.Select(i => i.ID).ToList();
            var filteredLookupDefinitions = Company.Filter<FieldTypeFilteredLookupDefinition>(i => fieldTypeIDs.Contains(i.FieldTypeID), i => i.FieldTypeFilteredLookupDisplayFields).ToList();

            foreach (var f in fields)
            {
                var name = f.Name.Replace("'", "''").Replace("--", "");

                switch (f.Type)
                {
                    case "FilteredLookup":
                        var fld = filteredLookupDefinitions.SingleOrDefault(i => i.FieldTypeID == f.ID);
                        if (fld != null)
                        {
                            if (fld.FieldTypeFilteredLookupDisplayFields != null)
                            {
                                var where = string.Empty;
                                var orderBy = string.Empty;

                                #region Build sub-select

                                if (fld.FieldTypeFilteredLookupDisplayFields.Count > 0)
                                {
                                    var columnSql = new List<string>();
                                    var joinSql = new List<string>();

                                    foreach (var df in fld.FieldTypeFilteredLookupDisplayFields.OrderBy(i => i.SortOrder).ThenBy(i => i.FieldTypeName))
                                    {
                                        var selectPrefix = $"{name}_{df.FieldTypeID}_FLF_{df.ID}";
                                        
                                        columnSql.Add($"{selectPrefix}.FormattedValue as [{df.FieldTypeName}]");
                                        columnSql.Add($"{selectPrefix}.LookupUrl as [{df.FieldTypeName}Uri]");

                                        joinSql.Add($"left join Field {selectPrefix} on {selectPrefix}.FieldTypeID = {df.FieldTypeID} and {selectPrefix}.ObjectType = 'Lookup' and {selectPrefix}.ObjectID = L.ID");

                                        //Build where
                                        if (df.Filter)
                                        {
                                            where += (string.IsNullOrEmpty(where) ? "" : "AND ");
                                            where += $" {selectPrefix}.Value = A.ID";
                                        }

                                        #region Build order by

                                        if (df.SortOrder.HasValue)
                                        {
                                            orderBy += (string.IsNullOrEmpty(orderBy) ? "" : ", ");
                                            if (df.FieldTypeID > 0)
                                            {
                                                var fieldTypeInfo = Company.Filter<FieldType>(i => i.ID == df.FieldTypeID).SingleOrDefault();
                                                if (fieldTypeInfo != null)
                                                {
                                                    switch (fieldTypeInfo.Type)
                                                    {
                                                        case "Date":
                                                        case "DateTime":
                                                            orderBy += $" cast({selectPrefix}.FormattedValue as datetime) asc";
                                                            break;
                                                        case "Decimal":
                                                        case "Number":
                                                            orderBy += $" cast({selectPrefix}.FormattedValue as decimal) asc";
                                                            break;
                                                        default:
                                                            orderBy += $" {selectPrefix}.FormattedValue asc";
                                                            break;
                                                    }
                                                }
                                                else
                                                {
                                                    orderBy += $" {selectPrefix}.FormattedValue asc";
                                                }
                                            }
                                            else
                                            {
                                                orderBy += $" D_{fld.ID}.[{df.FieldTypeName}] asc";
                                            }
                                        }

                                        #endregion
                                    }

                                    columns += "(select  ";
                                    columns += string.Join(", ", columnSql);

                                    columns += $@" from [Lookup] L ";

                                    columns += string.Join(" ", joinSql);

                                    if (!string.IsNullOrEmpty(where))
                                    {
                                        where = $" where {where}";
                                        columns += where;
                                    }

                                    if (!string.IsNullOrEmpty(orderBy))
                                    {
                                        orderBy = $" order by {orderBy}";
                                        columns += orderBy;
                                    }

                                    columns += $" for json path) as [{name}], ";
                                }

                                #endregion Build sub-select
                            }
                        }
                        break;
                    case "FusionLookup":                        
                        break;
                    default:
                        columns += $"T{f.ID}.FormattedValue as [{name}], ";
                        joins += $" left join FieldDetail T{f.ID} on T{f.ID}.Object = 'Artifact' and T{f.ID}.ObjectID = A.ID and T{f.ID}.FieldTypeID = {f.ID}";
                        break;
                }
            }

            fields = null;

            var querySql = $@"
select	A.ID,
        {columns}
		dbo.GenerateAssetUrl(O.ID) as Url,
        O.Uid as Uid
        , P.[uid] as ParentUid, P.ObjectID as ParentID
from	Artifact A 
        inner join Asset O on O.Object = 'Artifact' and O.ObjectID = A.ID 
        {joins} 
 left join [PredicateIntersect] PI on PI.PredicateType = {(int)PredicateType.InterTypeHierarchy} and PI.Object = O.Object and PI.ObjectID = O.ObjectID 
 left join Asset P on P.Object = PI.Subject and P.ObjectID = PI.SubjectID
where   A.ID = @id 
        and O.ID not in ({Company.GetNoReadSqlStatement()})
for json path";

            var jsonResults = Company.Query<string>(querySql, new { id = id }).ToList();

            var json = string.Join("", jsonResults);
            var arr = JArray.Parse(json);

            var attributes = Company.Query<AttributeDetail>($@"
select  A.* 
from    AttributeDetail A 
        inner join Artifact AR on A.ObjectType = 'Artifact' and AR.ID = A.ObjectID and AR.ArtifactTypeID = {typeID}").ToList();

            var attributeFields = Company.Query<FieldWithRelation>($@"
select  F.* 
from    FieldWithRelation F
        inner join Attribute A on F.ObjectType = 'Attribute' and F.ObjectID = A.ID
        inner join Artifact AR on A.ObjectType = 'Artifact' and AR.ID = A.ObjectID and AR.ArtifactTypeID = {typeID}").ToList();

            foreach (JObject o in arr)
            {
                var artifactID = o.GetValue("ID").Value<int>();
                var children = GetAttributesProperty(attributes.Where(i => i.ObjectID == artifactID).ToList(), attributeFields, null);
                if (children != null)
                    o.Add("Attributes", children);
            }

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(
                arr.ToString(Newtonsoft.Json.Formatting.None),
                Encoding.UTF8,
                "application/json");
            return response;
        }

        JArray GetAttributesProperty(List<AttributeDetail> attributes, List<FieldWithRelation> attributeFields, int? parentAttributeID)
        {
            JArray attributeArray = null;
            foreach (var att in attributes.Where(i => i.ParentID == parentAttributeID))
            {
                if (attributeArray == null)
                    attributeArray = new JArray();

                var attributeObject = new JObject();
                attributeObject.Add(new JProperty("ID", att.ID));
                attributeObject.Add(new JProperty("Type", att.Name));
                foreach (var field in attributeFields.Where(i => i.ObjectID == att.ID).OrderBy(i => i.Name))
                {
                    attributeObject.Add(new JProperty(field.Name, field.FormattedValue));
                }
                var children = GetAttributesProperty(attributes, attributeFields, att.ID);
                if (children != null)
                    attributeObject.Add("Attributes", children);

                attributeArray.Add(attributeObject);
            }
            return attributeArray;
        }

        /// <summary>
        /// Gets all artifacts based on a given type ID and a set of search criteria that roughly matches the field layout of the type.
        /// </summary>
        /// <returns>A list of artifacts.</returns>
        [Route("artifacts/{id:int}/search"), HttpPost]
        public IQueryable<dynamic> GetArtifactsByTypeAndSearchModel(int id, Dictionary<string, string> model)
        {
            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Artifact", out joins, out columns);

            var querySql = $@"select	A.ID,
		A.Name,
		A.Description,
        A.ParentID,
		P.Name as Parent,
        dbo.GenerateAssetUrl(P_Asset.ID) as ParentUrl,
		A.Status,
        A.DateLastCertified,
        {columns}
		dbo.GenerateAssetUrl(O.ID) as Url
from	Artifact A 
        inner join Asset O on O.Object = 'Artifact' and O.ObjectID = A.ID 
        left join Artifact P on P.ID = A.ParentID 
        left join Asset P_Asset on P_Asset.Object = 'Artifact' and P_Asset.ObjectID = P.ID
        {joins}
where   O.ID not in ({Company.GetNoReadSqlStatement()}) 
        and A.ArtifactTypeID = @id ";

            var sql = string.Format(@"select * from ({0}) A", querySql);

            if (model != null)
            {
                foreach (string key in model.Keys)
                {
                    if (key == "Name") sql += $"A.Name like '{model[key].Replace("'", "''")}%'";
                    if (key == "Description") sql += $"A.Description like '%{model[key].Replace("'", "''")}%'";
                    if (key == "Parent") sql += $"A.Parent like '{model[key].Replace("'", "''")}%'";
                    if (key == "Status") sql += $"A.Status like '{model[key].Replace("'", "''")}%'";                    
                }
            }

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
            if (!Company.HasAssetTypePermission(SystemObjects.ArtifactType, id, Permission.ModifyAsset))
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

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("Model");
                    }
                }

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.ArtifactType, id).Where(i => !CalculatedFieldTypes.Contains(i.Type)).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Artifact.ToString(), Value = model[f.Name].ToString(), UpdatedBy = Company.CurrentResourceID });
                    else
                    {
                        if (f.IsRequired)
                            throw new MissingPropertiesException("Artifact");
                    }
                });

                Company.SaveOrUpdate<Artifact>(item, fields);

                if (parentID > 0)
                {
                    var parent = Company.GetById<Artifact>(parentID);
                    if (parent == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"The parent taxonomy with id {parentID} could not be found.");
                    var intersectType = Company.GetHierarchyIntersectType(SystemObjects.ArtifactType, parent.ArtifactTypeID, id);
                    if (intersectType != null)
                    {
                        var intersect = new Intersect()
                        {
                            Subject = "Artifact",
                            Object = "Artifact",
                            SubjectID = parentID,
                            ObjectID = item.ID
                        };

                        Company.SaveOrUpdate(intersect);
                    }
                }

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
                if (!Company.HasAssetTypePermission(SystemObjects.Artifact, id, Permission.ModifyAsset))
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

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.ArtifactType, typeID).Where(i => !CalculatedFieldTypes.Contains(i.Type)).ToList();

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("ParentID");
                    }
                }

                if (parentID > 0)
                {
                    var parent = Company.GetById<Artifact>(parentID);
                    var existing = Company.GetParentObject(item.ID, SystemObjects.Artifact);
                    var intersectType = Company.GetHierarchyIntersectType(SystemObjects.ArtifactType, parent.ArtifactTypeID, item.ArtifactTypeID);
                    if (intersectType == null)
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

                    if (existing == null)
                    {
                        var intersect = new Intersect()
                        {
                            Subject = "Artifact",
                            Object = "Artifact",
                            SubjectID = parentID,
                            ObjectID = item.ID,
                            IntersectTypeID = intersectType.ID,
                        };

                        Company.Add(intersect);
                    }
                    else if (existing.ID != parentID)
                    {
                        var intersect = Company.Filter<Intersect>(i => i.Subject == "Artifact" && i.Object == "Artifact" && i.SubjectID == existing.ID && i.ObjectID == item.ID).FirstOrDefault();
                        if (intersect != null)
                        {
                            intersect.SubjectID = parentID;
                            Company.Update(intersect);
                        }
                    }
                }

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Artifact.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString(), UpdatedBy = Company.CurrentResourceID });
                });

                Company.SaveOrUpdate<Artifact>(item, fields);

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
        [Route("models/{id:int}"), SwaggerResponse(HttpStatusCode.OK, "A list of models based on the type.", typeof(List<TaxonomyResultModel>))]
        public IQueryable<dynamic> GetModelsByType(int id)
        {
            var joins = "";
            var columns = "";
            getDynamicFieldJoinStatements(id, "Taxonomy", out joins, out columns);

            var querySql = $@"select	A.ID,
        A.ParentID,
		P.DisplayValue as Parent,
        dbo.GenerateAssetUrl(P_Asset.ID) as ParentUrl,
        {columns}
		dbo.GenerateAssetUrl(O.ID) as Url
from	Taxonomy A 
        {joins} 
        inner join Asset O on O.Object = 'Taxonomy' and O.ObjectID = A.ID 
        left join Taxonomy P on P.ID = A.ParentID 
        left join Asset P_Asset on P_Asset.Object = 'Taxonomy' and P_Asset.ObjectID = P.ID
where   O.ID not in ({Company.GetNoReadSqlStatement()}) 
        and A.TaxonomyTypeID = @id ";

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
            if (!Company.HasAssetTypePermission(SystemObjects.TaxonomyType, id, Permission.ModifyAsset))
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

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("Model");
                    }
                }

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, id).Where(i => !CalculatedFieldTypes.Contains(i.Type)).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Taxonomy.ToString(), Value = model[f.Name].ToString(), UpdatedBy=Company.CurrentResourceID });
                    else
                    {
                        if (f.IsRequired)
                            throw new MissingPropertiesException("Taxonomy");
                    }
                });

                Company.SaveOrUpdate<Taxonomy>(item, fields);

                if (parentID > 0)
                {
                    var parent = Company.GetById<Taxonomy>(parentID);
                    if (parent == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, $"The parent taxonomy with id {parentID} could not be found.");
                    var intersectType = Company.GetHierarchyIntersectType(SystemObjects.TaxonomyType, parent.TaxonomyTypeID, id);
                    if (intersectType != null)
                    {
                        var intersect = new Intersect()
                        {
                            Subject = "Taxonomy",
                            Object = "Taxonomy",
                            SubjectID = parentID,
                            ObjectID = item.ID
                        };

                        Company.SaveOrUpdate(intersect);
                    }
                }

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
                if (!Company.HasAssetPermission(SystemObjects.Taxonomy, id, Permission.ModifyAsset))
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

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.TaxonomyType, typeID).Where(i => !CalculatedFieldTypes.Contains(i.Type)).ToList();

                int parentID = 0;
                if (model.ContainsKey("ParentID"))
                {
                    if (!int.TryParse(model["ParentID"], out parentID))
                    {
                        throw new MissingPropertiesException("Model");
                    }
                }

                if (parentID > 0)
                {
                    var parent = Company.GetById<Taxonomy>(parentID);
                    var existing = Company.GetParentObject(item.ID, SystemObjects.Taxonomy);
                    var intersectType = Company.GetHierarchyIntersectType(SystemObjects.TaxonomyType, parent.TaxonomyTypeID, item.TaxonomyTypeID);
                    if (intersectType == null)
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));

                    if (existing == null)
                    {
                        var intersect = new Intersect()
                        {
                            Subject = "Taxonomy",
                            Object = "Taxonomy",
                            SubjectID = parentID,
                            ObjectID = item.ID,
                            IntersectTypeID = intersectType.ID,
                        };

                        Company.Add(intersect);
                    }
                    else if (existing.ObjectID != parentID)
                    {
                        var intersect = Company.Filter<Intersect>(i => i.Subject == "Taxonomy" && i.Object == "Taxonomy" && i.SubjectID == existing.ObjectID && i.ObjectID == item.ID).FirstOrDefault();
                        if (intersect != null)
                        {
                            intersect.SubjectID = parentID;
                            Company.Update(intersect);
                        }
                    }
                }

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Taxonomy.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString(), UpdatedBy = Company.CurrentResourceID });
                });

                Company.SaveOrUpdate<Taxonomy>(item, fields);

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
