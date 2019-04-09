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
using System.Threading.Tasks;
using Microsoft.Web.Http;
using System.Web.Http.Description;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This services houses all endpoints handling reference lists and items.
    /// </summary>
    [ApiVersion("1.0"), ApiExplorerSettings(IgnoreApi = true), RoutePrefix("services/deprecated/reference"), Name("Reference Service"), Authorize]
    public class ReferenceController : BaseApiController
    {
        #region DI

        public ReferenceController(ICommunityContext community, ICompanyContext company) 
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
            if (!Company.HasAssetTypePermission(SystemObjects.ReferenceItemType, typeID, Permission.ModifyAsset))
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add an item to this list.");

            ReferenceItem item = null;

            try
            {
                var type = Company.GetById<ReferenceItemType>(typeID);

                #region Check that ReferenceItemType was found

                if (type == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                #endregion

                item.ReferenceItemTypeID = typeID;

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, typeID).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.ReferenceItem.ToString(), Value = model[f.Name].ToString(), UpdatedBy = Company.CurrentResourceID });
                    else
                    {
                        if (f.IsRequired)
                            throw new MissingPropertiesException("Reference Item");
                    }
                });

                Company.SaveOrUpdate<ReferenceItem>(item);

                fields.ForEach(f => {
                    f.ObjectID = item.ID;
                });

                Company.AddOrUpdateFields(fields);

                return Request.CreateResponse<ReferenceItem>(HttpStatusCode.Created, item);
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
        public HttpResponseMessage AddReferenceList(ReferenceItemType list)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add this reference list.");

                return (Company.SaveOrUpdate(list) > 0) ?
                    Request.CreateResponse(HttpStatusCode.Created, list) :
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
                if (!Company.HasAssetPermission(SystemObjects.ReferenceItem, id, Permission.DeleteAsset))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this item.");

                var itemToRemove = Company.GetById<ReferenceItem>(id);
                if (itemToRemove.ReferenceItemTypeID == typeID)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The assigned list for this item does not match the list ID {0}", typeID));
                }
                else
                {
                    return (Company.Delete(itemToRemove)) ?
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
                if (!Company.HasAssetPermission(SystemObjects.ReferenceItemType, id, Permission.DeleteAsset))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this reference list.");

                var listToRemove = Company.GetById<ReferenceItemType>(id);
                if (listToRemove != null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The list could not be found that matches the ID {0}", id));
                }
                else
                {
                    return (Company.Delete(listToRemove)) ?
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
                if (!Company.HasAssetPermission(SystemObjects.ReferenceItem, id, Permission.ModifyAsset))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this item.");

                var item = Company.GetById<ReferenceItem>(id);

                if (item == null)
                {
                    throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                }
                else
                {
                    if (item.ReferenceItemTypeID != typeID)
                    {
                        throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.NotFound));
                    }
                }

                var fieldTypes = Company.GetFieldTypesByObject(SystemObjects.ReferenceItemType, typeID).ToList();

                var fields = new List<Field>();
                fieldTypes.ForEach(f =>
                {
                    if (model.ContainsKey(f.Name))
                        fields.Add(new Field { FieldTypeID = f.ID, ObjectType = SystemObjects.ReferenceItem.ToString(), ObjectID = item.ID, Value = model[f.Name].ToString(), UpdatedBy = Company.CurrentResourceID });
                });

                Company.SaveOrUpdate(item, fields);

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
        public HttpResponseMessage PutReferenceList(int id, ReferenceItemType list)
        {
            try
            {
                if (!Company.HasAssetTypePermission(SystemObjects.ReferenceItemType, id, Permission.ModifyAsset))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this list.");

                var listToUpdate = Company.GetById<ReferenceItemType>(id);
                if (listToUpdate == null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, $"The list could not be found that matches the ID {id}");
                }
                else 
                {
                    listToUpdate.Name = list.Name;
                    listToUpdate.Description = list.Description;
                    listToUpdate.DisplayFormat = list.DisplayFormat;

                    return (Company.Update(listToUpdate)) ?
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
        public async Task<HttpResponseMessage> GetItemsByList(int typeID)
        {
            var models = await Company.QueryAsync<dynamic>($"exec [dbo].[GetReferenceItemValues] {typeID}, {Company.CurrentResourceID}, 1");
            return Request.CreateResponse(HttpStatusCode.OK, models);
        }

        /// <summary>
        /// Gets an OData-queryable list of lists.
        /// </summary>
        /// <returns>A list of reference lists.</returns>
        [Route(""), HttpGet]
        public IQueryable<ReferenceItemType> GetReferenceLists()
        {
            return Company.Table<ReferenceItemType>();
        }        
    }
}
