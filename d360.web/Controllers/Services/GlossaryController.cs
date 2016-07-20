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
        public HttpResponseMessage GetArtifactsByType(int id)
        {
            var joins = "";
            var columns = "";

            var fields = Company.Filter<FieldTypeWithRelation>(i => i.Object == "ArtifactType" && i.ObjectID == id).ToList();
            var fieldTypeIDs = fields.Select(i => i.ID).ToList();
            var filteredLookupDefinitions = Company.Filter<FieldTypeFilteredLookupDefinition>(i => fieldTypeIDs.Contains(i.FieldTypeID), i => i.FieldTypeFilteredLookupDisplayFields).ToList();
            var relationDefinitions = Company.Filter<FieldTypeRelationLookupDefinition>(i => fieldTypeIDs.Contains(i.FieldTypeID), i => i.FieldTypeRelationLookupDisplayFields).ToList();

            //var relationships = new List<Intersect>();
            //var relationshipFields = new List<FieldWithRelation>();

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
                                        var selectTypePrefix = $"{name}_{df.FieldTypeID}_FLFT_{df.ID}";

                                        columnSql.Add($"{selectPrefix}.FormattedValue as [{df.FieldTypeName}]");
                                        columnSql.Add($"{selectPrefix}.LookupUrl as [{df.FieldTypeName}Uri]");
                                        

                                        //joinSql.Add($"inner join FieldType {selectTypePrefix} on {selectTypePrefix}.ID = {df.FieldTypeID}");
                                        joinSql.Add($"left join FieldWithRelation {selectPrefix} on {selectPrefix}.FieldTypeID = {df.FieldTypeID} and {selectPrefix}.ObjectType = 'Lookup' and {selectPrefix}.ObjectID = L.ID");

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
                        //columns += string.Format("{0}_T.FormattedValue as [{0}], ", name);
                        //joins += string.Format(" left join FieldWithRelation {0}_T on {0}_T.ObjectType = 'Artifact' and {0}_T.ObjectID = A.ID and {0}_T.FieldTypeID = {1} and {0}_T.IsListable = 1", name, f.ID);
                        break;
                    case "RelationLookup":
                        var rd = relationDefinitions.SingleOrDefault(i => i.FieldTypeID == f.ID);
                        if (rd != null)
                        {
                            if (rd.FieldTypeRelationLookupDisplayFields != null)
                            {
                                var where = string.Empty;
                                var orderBy = string.Empty;

                                #region Build sub-select

                                if (rd.FieldTypeRelationLookupDisplayFields.Count > 0)
                                {

                                    var columnSql = new List<string>();
                                    var joinSql = new List<string>();

                                    foreach (var df in rd.FieldTypeRelationLookupDisplayFields)
                                    {
                                        if (df.FieldTypeID == 0)
                                        {
                                            columnSql.Add($"D_{rd.ID}.{df.FieldTypeName}");
                                        }
                                        else
                                        {
                                            columnSql.Add($"{name}_{df.FieldTypeID}_RF_{df.ID}.FormattedValue as [{df.FieldTypeName}]");

                                            joinSql.Add($"inner join FieldType {name}_{df.FieldTypeID}_RFT_{df.ID} on {name}_{df.FieldTypeID}_RFT_{df.ID}.ID = {df.FieldTypeID}");
                                            joinSql.Add($"left join Field {name}_{df.FieldTypeID}_RF_{df.ID} on {name}_{df.FieldTypeID}_RF_{df.ID}.FieldTypeID = {df.FieldTypeID} and {name}_{df.FieldTypeID}_RF_{df.ID}.ObjectType = D_{rd.ID}.Object and {name}_{df.FieldTypeID}_RF_{df.ID}.ObjectID = D_{rd.ID}.ObjectID");
                                        }
                                    }

                                    #region Build where

                                    foreach (var df in rd.FieldTypeRelationLookupDisplayFields.Where(i => !string.IsNullOrEmpty(i.FilterValue)))
                                    {
                                        where += (string.IsNullOrEmpty(where) ? "" : "AND ");
                                        if (df.FieldTypeID > 0)
                                        {
                                            where += $" {name}_{df.FieldTypeID}_RF_{df.ID}.FormattedValue like '{df.FilterValue.StripFormatting(null).CleanForSql()}%'";
                                        }
                                        else
                                        {
                                            where += $" D_{rd.ID}.[{df.FieldTypeName}] like '{df.FilterValue.StripFormatting(null).CleanForSql()}%'";
                                        }
                                    }

                                    #endregion Build where

                                    #region Build order by


                                    foreach (var df in rd.FieldTypeRelationLookupDisplayFields.Where(i => i.SortOrder.HasValue).OrderBy(i => i.SortOrder).ThenBy(i => i.FieldTypeName))
                                    {
                                        orderBy += (string.IsNullOrEmpty(orderBy) ? "" : ", ");
                                        if (df.FieldTypeID > 0)
                                        {
                                            var prefix = $"{name}_{df.FieldTypeID}_RF_{df.ID}";

                                            var fieldTypeInfo = Company.Filter<FieldType>(i => i.ID == df.FieldTypeID).SingleOrDefault();
                                            if (fieldTypeInfo != null)
                                            {
                                                switch (fieldTypeInfo.Type)
                                                {
                                                    case "Date":
                                                    case "DateTime":
                                                        orderBy += $" cast({prefix}.FormattedValue as datetime) asc";
                                                        break;
                                                    case "Decimal":
                                                    case "Number":
                                                        orderBy += $" cast({prefix}.FormattedValue as decimal) asc";
                                                        break;
                                                    default:
                                                        orderBy += $" {prefix}.FormattedValue asc";
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                orderBy += $" {prefix}.FormattedValue asc";
                                            }
                                        }
                                        else
                                        {
                                            orderBy += $" D_{rd.ID}.[{df.FieldTypeName}] asc";
                                        }
                                    }

                                    #endregion Build order by

                                    columns += "(select  "; //" distinct "
                                    columns += string.Join(", ", columnSql);
                                    if (rd.ReferenceType == 1)
                                    {
//                                        relationships.AddRange(Company.Query<Intersect>($@"
//select  I.* 
//from    [Intersect] I 
//        inner join "));

                                        // self reference
                                        columns += $@" from [Intersect] I_{rd.ID} 
 inner join [cache].[ObjectDetails] D_{rd.ID} on D_{rd.ID}.[Object] = case when I_{rd.ID}.Subject = 'Artifact' and I_{rd.ID}.SubjectID = A.ID then I_{rd.ID}.Object else I_{rd.ID}.Subject end 
 and D_{rd.ID}.ObjectID = case when I_{rd.ID}.Subject = 'Artifact' and I_{rd.ID}.SubjectID = A.ID then I_{rd.ID}.ObjectID else I_{rd.ID}.SubjectID end ";

                                        columns += string.Join(" ", joinSql);
                                        columns += $" where I_{rd.ID}.IntersectTypeID = {rd.IntersectTypeID} and ( (I_{rd.ID}.Subject = 'Artifact' and I_{rd.ID}.SubjectID = A.ID) OR (I_{rd.ID}.Object = 'Artifact' and I_{rd.ID}.ObjectID = A.ID) )";
                                    }
                                    else
                                    {
                                        // child reference
                                        columns += $@" from [Intersect] I_{rd.ID} inner join [Intersect] I_C_{rd.ID} on I_{rd.ID}.IntersectTypeID = {rd.IntersectTypeID} and 
										( 
											(I_{rd.ID}.Subject = 'Artifact' and I_{rd.ID}.SubjectID = A.ID) OR 
											(I_{rd.ID}.Object = 'Artifact' and I_{rd.ID}.ObjectID = A.ID) 
										) 
                                        and I_C_{rd.ID}.IntersectTypeID = {rd.ChildIntersectTypeID} and 
										( 
										    (I_C_{rd.ID}.Subject = 'Intersect' and I_C_{rd.ID}.SubjectID = I_{rd.ID}.ID) OR (I_C_{rd.ID}.Object = 'Intersect' and I_C_{rd.ID}.ObjectID = I_{rd.ID}.ID)
                                        ) 
 inner join [cache].[ObjectDetails] D_{rd.ID} on D_{rd.ID}.[Object] = case when I_C_{rd.ID}.Subject = 'Artifact' and I_C_{rd.ID}.SubjectID = A.ID then I_C_{rd.ID}.Object else I_C_{rd.ID}.Subject end 
 and D_{rd.ID}.ObjectID = case when I_C_{rd.ID}.Subject = 'Artifact' and I_C_{rd.ID}.SubjectID = A.ID then I_C_{rd.ID}.ObjectID else I_C_{rd.ID}.SubjectID end ";

                                        columns += string.Join(" ", joinSql);
                                    }

                                    if (!string.IsNullOrEmpty(where))
                                    {
                                        where = ((rd.ReferenceType == 1) ? " and " : " where ") + where;
                                    }
                                    columns += where;

                                    if (!string.IsNullOrEmpty(orderBy))
                                    {
                                        orderBy = " order by " + orderBy;
                                    }
                                    columns += orderBy;

                                    columns += $" for json path) as [{name}], ";
                                }

                                #endregion Build sub-select
                            }
                        }
                        break;
                    default:
                        columns += $"T{f.ID}.FormattedValue as [{name}], ";
                        joins += $" left join FieldWithRelation T{f.ID} on T{f.ID}.ObjectType = 'Artifact' and T{f.ID}.ObjectID = A.ID and T{f.ID}.FieldTypeID = {f.ID}";
                        break;
                }
            }

            fields = null;

            var querySql = $@"
select	A.ID,
		A.Name,
		A.Description,
		A.Status,
        A.DateLastCertified,
        {columns}
		dbo.GenerateObjectUrl('Artifact', A.ArtifactTypeID, A.ID) as Url
from	Artifact A 
        {joins}
where   A.ArtifactTypeID = @id 
for json path";

            var jsonResults = Company.Query<string>(querySql, new { id = id }).ToList();

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

            if (model != null)
            {
                foreach (string key in model.Keys)
                {
                    if (key == "Name") sql += $"A.Name like '{model[key].Replace("'", "''")}%'";
                    if (key == "Description") sql += $"A.Description like '%{model[key].Replace("'", "''")}%'";
                    if (key == "Parent") sql += $"A.Parent like '{model[key].Replace("'", "''")}%'";
                    if (key == "Status") sql += $"A.Status like '{model[key].Replace("'", "''")}%'";
                    //if ()
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
                    item.Name = model["Name"].ToString();
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
