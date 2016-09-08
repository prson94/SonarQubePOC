using d360.core.entities;
using d360.model;
using d360.core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using d360.core.exceptions;
using d360.core.enums;
using System.Text;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This services houses all endpoints handling reference lists and items.
    /// </summary>
    [RoutePrefix("services/reference"), Name("Reference Service"), Authorize]
    public class ReferenceController : BaseApiController
    {
        #region DI

        public ReferenceController(CommunityContext community, CompanyContext company) 
            : base(community, company) 
        { 
        }

        #endregion

        /// <summary>
        /// Adds an item to a specified reference list.
        /// </summary>
        /// <param name="typeID">The ID of the reference list.</param>
        /// <param name="model">The item to add to the specified list, in the form of name/value pairs.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 201:Created</returns>
        [Route("{typeID:int}/items"), HttpPost]
        public HttpResponseMessage AddItemToList(int typeID, Dictionary<string, string> model)
        {
            if (!Company.HasPermission(SystemObjects.LookupType, typeID, Claim.Create, ClaimObject.Root))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add an item to this list.");

            Lookup item = null;

            try
            {
                var type = Company.GetById<LookupType>(typeID);

                #region Check that LookupType was found

                if (type == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                #endregion

                item.LookupTypeID = typeID;

                var fieldTypes = Company.GetFieldTypeRelationsByObject(SystemObjects.LookupType, typeID).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Lookup.ToString(), Value = model[f.Name].ToString() });
                    else
                    {
                        if (f.IsRequired)
                            throw new MissingPropertiesException("Lookup");
                    }
                });

                Company.SaveOrUpdate<Lookup>(item);

                fields.ForEach(f => {
                    f.ObjectID = item.ID;
                });

                Company.AddOrUpdateFields(fields);

                return Request.CreateResponse<Lookup>(HttpStatusCode.Created, item);
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
        /// Adds a reference list.
        /// </summary>
        /// <param name="list">The list to add, as name/value properties.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 201:Created</returns>
        [Route(""), HttpPost]
        public HttpResponseMessage AddReferenceList(LookupType list)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.LookupType, 0, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add this reference list.");

                return (Company.SaveOrUpdate<LookupType>(list) > 0) ?
                    Request.CreateResponse<LookupType>(HttpStatusCode.Created, list) :
                    Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to add the reference list.");
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
        /// Removes an item from a specified reference list, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the reference list.</param>
        /// <param name="id">The ID of the item to remove.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{typeID:int}/items/{id:int}"), HttpDelete]
        public HttpResponseMessage DeleteItemFromList(int typeID, int id)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.LookupType, typeID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this item.");

                var itemToRemove = Company.GetById<Lookup>(id);
                if (itemToRemove.LookupTypeID == typeID)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The assigned list for this item does not match the list ID {0}", typeID));
                }
                else
                {
                    return (Company.Delete<Lookup>(itemToRemove)) ?
                        Request.CreateResponse(HttpStatusCode.OK) :
                        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to remove the item.");
                }
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
        /// Removes a reference list, based on its ID.
        /// </summary>
        /// <param name="id">The ID of the list to remove.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{id:int}"), HttpDelete]
        public HttpResponseMessage DeleteReferenceList(int id)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Delete, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this reference list.");

                var listToRemove = Company.GetById<LookupType>(id);
                if (listToRemove == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The list could not be found that matches the ID {0}", id));
                }
                else
                {
                    return (Company.Delete<LookupType>(listToRemove)) ?
                        Request.CreateResponse(HttpStatusCode.OK) :
                        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to remove the reference list.");
                }
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
        /// Updates an item in the specified list, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the reference list.</param>
        /// <param name="id">The ID of the item to update.</param>
        /// <param name="model">The item to update, in name/value pair format.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{typeID:int}/items/{id:int}"), HttpPut]
        public HttpResponseMessage PutItemInList(int typeID, int id, Dictionary<string, string> model)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.LookupType, typeID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this item.");

                var item = Company.GetById<Lookup>(id);

                if (item == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }
                else
                {
                    if (item.LookupTypeID != typeID)
                    {
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                    }
                }

                var fieldTypes = Company.GetFieldTypeRelationsByObject(SystemObjects.LookupType, typeID).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.Lookup.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString() });
                });

                Company.SaveOrUpdate<Lookup>(item);
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

        /// <summary>
        /// Updates a list, based on its ID.
        /// </summary>
        /// <param name="id">The ID of the reference list.</param>
        /// <param name="list">The list to update</param>
        /// <returns></returns>
        [Route("{id:int}"), HttpPut]
        public HttpResponseMessage PutReferenceList(int id, LookupType list)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.LookupType, id, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this list.");

                var listToUpdate = Company.GetById<LookupType>(id);
                if (listToUpdate == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"The list could not be found that matches the ID {id}");
                }
                else 
                {
                    listToUpdate.Name = list.Name;

                    return (Company.Update<LookupType>(listToUpdate)) ?
                        Request.CreateResponse(HttpStatusCode.OK) :
                        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to update the reference list.");
                }
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
        /// Gets an OData-queryable list of items within a specified reference list.
        /// </summary>
        /// <param name="typeID">The ID of the reference list.</param>
        /// <returns>A list of items.</returns>
        [Route("{typeID:int}/items"), HttpGet]
        public HttpResponseMessage GetItemsByList(int typeID)
        {
            var joins = "";
            var columns = "";

            var fields = Company.Filter<FieldTypeWithRelation>(i => i.Object == "LookupType" && i.ObjectID == typeID).ToList();
            var fieldTypeIDs = fields.Select(i => i.ID).ToList();

            foreach (var f in fields)
            {
                var name = f.Name.Replace("'", "''").Replace("--", "");
                columns += $"T{f.ID}.FormattedValue as [{name}], ";
                if (f.Type == "Lookup")
                {
                    columns += $"T{f.ID}.LookupUrl as [{name}Uri], ";
                }
                joins += $" left join FieldWithRelation T{f.ID} on T{f.ID}.ObjectType = 'Lookup' and T{f.ID}.ObjectID = A.ID and T{f.ID}.FieldTypeID = {f.ID}";
            }

            fields = null;

            var querySql = $@"
select	A.ID,
        {columns}
		dbo.GenerateObjectUrl('Lookup', A.LookupTypeID, A.ID) as Url
from	Lookup A 
        {joins}
where   A.LookupTypeID = @id 
for json path";

            var jsonResults = Company.Query<string>(querySql, new { id = typeID }).ToList();

            var json = string.Join("", jsonResults);
            var arr = (string.IsNullOrEmpty(json)) ? new JArray() : JArray.Parse(json);

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(
                arr.ToString(Newtonsoft.Json.Formatting.None),
                Encoding.UTF8,
                "application/json");
            return response;
        }

        [Route("{typeID:int}/responsibilities"), HttpGet]
        public IQueryable<dynamic> GetResponsibilitiesForDomainType(int typeID)
        {
            return GetResponsibilities(SystemObjects.DomainType, typeID);
        }

        /// <summary>
        /// Gets an OData-queryable list of lists.
        /// </summary>
        /// <returns>A list of reference lists.</returns>
        [Route(""), HttpGet]
        public IQueryable<LookupType> GetReferenceLists()
        {
            return Company.Table<LookupType>();
        }
    }
}
