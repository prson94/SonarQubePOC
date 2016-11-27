using d360.core.entities;
using d360.model;
using d360.core;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using d360.core.exceptions;
using d360.core.enums;

namespace d360.web.Controllers.Services
{
    /// <summary>
    /// This services houses all endpoints handling reference lists, and items.
    /// </summary>
    [RoutePrefix("services/domains"), Name("Reference Service"), Authorize]
    public class DomainsController : BaseApiController
    {
        #region DI

        public DomainsController(CommunityContext community, CompanyContext company) 
            : base(community, company) 
        { 
        }

        #endregion

        /// <summary>
        /// Adds an item to a specified reference.
        /// </summary>
        /// <param name="typeID">The ID of the reference type the reference belongs to.</param>
        /// <param name="item">The item to add to the specified list.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 201:Created</returns>
        [Route("{typeID:int}/items"), HttpPost]
        public HttpResponseMessage AddItemToList(int typeID, ReferenceItem item)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.ReferenceItemType, typeID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add this reference item.");

                item.ReferenceItemTypeID = typeID;

                return (Company.SaveOrUpdate<ReferenceItem>(item) > 0) ? 
                    Request.CreateResponse<ReferenceItem>(HttpStatusCode.Created, item) :
                    Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to add the reference item.");
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
        /// Removes an item from a specified reference, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the reference type the reference belongs to.</param>
        /// <param name="id">The ID of the item to remove.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{typeID:int}/items/{id:int}"), HttpDelete]
        public HttpResponseMessage DeleteItemFromList(int typeID, int id)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.ReferenceItemType, typeID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this reference item.");

                var itemToRemove = Company.GetById<ReferenceItem>(id);

                return (Company.Delete<ReferenceItem>(itemToRemove)) ?
                    Request.CreateResponse(HttpStatusCode.OK) :
                    Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to remove the reference item.");
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
        /// Updates an item in the specified domain, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the list.</param>
        /// <param name="id">The ID of the item to update.</param>
        /// <param name="item">The item to update</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{typeID:int}/items/{id:int}"), HttpPut]
        public HttpResponseMessage PutItemInList(int typeID, int id, ReferenceItem item)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.ReferenceItemType, typeID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this item.");

                var itemToUpdate = Company.GetById<ReferenceItem>(id);
                if (itemToUpdate.ReferenceItemTypeID != typeID)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The assigned list for this item does not match the list ID {0}", typeID));
                }
                else
                {
                    itemToUpdate.Code = item.Code;

                    return (Company.Update<ReferenceItem>(itemToUpdate)) ?
                        Request.CreateResponse(HttpStatusCode.OK) :
                        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to update the item.");
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
        /// Gets an OData-queryable list of reference types. 
        /// </summary>
        /// <returns>A list of reference types.</returns>
        [Route(""), HttpGet]
        public IQueryable<ReferenceItemType> GetLists()
        {
            return Company.Table<ReferenceItemType>();
        }

        [Route("{typeID:int}/responsibilities"), HttpGet]
        public IQueryable<dynamic> GetResponsibilitiesForType(int typeID)
        {
            return GetResponsibilities(SystemObjects.ReferenceItemType, typeID);
        }

        /// <summary>
        /// Gets an OData-queryable list of items within a specified reference.
        /// </summary>
        /// <param name="typeID">The ID of the reference type the reference belongs to.</param>
        /// <returns>A list of items.</returns>
        [Route("{typeID:int}/items"), HttpGet]
        public IQueryable<ReferenceItem> GetItemsByList(int typeID)
        {
            return Company.Filter<ReferenceItem>(i => i.ReferenceItemTypeID == typeID);
        }

        [Route("lists/languages"), HttpGet]
        public IQueryable<Language> GetLanguages()
        {
            return Company.Languages.AsQueryable();
        }
    }
}
