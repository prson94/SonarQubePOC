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
    /// This services houses all endpoints handling domain types, groups, lists, and items.
    /// </summary>
    [RoutePrefix("services/domains"), Name("Domain Service"), Authorize]
    public class DomainsController : BaseApiController
    {
        #region DI

        public DomainsController(CommunityContext community, CompanyContext company) 
            : base(community, company) 
        { 
        }

        #endregion

        /// <summary>
        /// Adds an item to a specified domain.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="listID">The ID of the domain to add the item to.</param>
        /// <param name="item">The item to add to the specified domain.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 201:Created</returns>
        [Route("{typeID:int}/lists/{listID:int}"), HttpPost]
        public HttpResponseMessage AddItemToDomain(int typeID, int listID, DomainItem item)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Domain, listID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add this domain item.");

                item.DomainID = listID;
                //if (item.ID == 0)
                //{ 
                    
                //}

                return (Company.SaveOrUpdate<DomainItem>(item) > 0) ? 
                    Request.CreateResponse<DomainItem>(HttpStatusCode.Created, item) :
                    Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to add the domain item.");
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
        /// Adds a group to a specified domain type.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="group">The group to add to the specified domain type.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 201:Created</returns>
        [Route("{typeID:int}/groups"), HttpPost]
        public HttpResponseMessage AddGroupToType(int typeID, DomainGroup group)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.DomainType, typeID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add this domain group.");

                group.DomainTypeID = typeID;
                return (Company.SaveOrUpdate<DomainGroup>(group) > 0) ?
                    Request.CreateResponse<DomainGroup>(HttpStatusCode.Created, group) :
                    Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to add the domain group.");
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
        /// Adds a domain to a specified domain type.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="list">The list to add to the specified domain type.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 201:Created</returns>
        [Route("{typeID:int}/lists"), HttpPost]
        public HttpResponseMessage AddDomainToType(int typeID, Domain list)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.DomainType, typeID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to add this domain.");

                list.DomainTypeID = typeID;
                return (Company.SaveOrUpdate<Domain>(list) > 0) ?
                    Request.CreateResponse<Domain>(HttpStatusCode.Created, list) :
                    Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to add the domain.");
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
        /// Removes an item from a specified domain, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="listID">The ID of the domain to remove the item from.</param>
        /// <param name="id">The ID of the item to remove.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{typeID:int}/lists/{listID:int}/{id:int}"), HttpDelete]
        public HttpResponseMessage DeleteItemFromDomain(int typeID, int listID, int id)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Domain, listID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this domain item.");

                var itemToRemove = Company.GetById<DomainItem>(id);
                if (itemToRemove.DomainID == listID)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The assigned list for this item does not match the list ID {0}", listID));
                }
                else
                {
                    return (Company.Delete<DomainItem>(itemToRemove)) ?
                        Request.CreateResponse(HttpStatusCode.OK) :
                        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to remove the domain item.");
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
        /// Removes a group from a specified domain type, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="groupID">The ID of the group to remove.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{typeID:int}/groups/{groupID:int}"), HttpDelete]
        public HttpResponseMessage DeleteGroupFromType(int typeID, int groupID)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.DomainGroup, groupID, Claim.Delete, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this domain group.");

                var groupToRemove = Company.GetById<DomainGroup>(groupID);
                if (groupToRemove.DomainTypeID == typeID)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The assigned type for this group does not match the ID {0}", typeID));
                }
                else
                {
                    return (Company.Delete<DomainGroup>(groupToRemove)) ?
                        Request.CreateResponse(HttpStatusCode.OK) :
                        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to remove the domain group.");
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
        /// Removes a domain from a specified domain type, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="id">The ID of the list to remove.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{typeID:int}/lists/{id:int}"), HttpDelete]
        public HttpResponseMessage DeleteListFromType(int typeID, int id)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Domain, id, Claim.Delete, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to remove this domain.");

                var listToRemove = Company.GetById<Domain>(id);
                if (listToRemove.DomainTypeID == typeID)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The assigned type for this list does not match the ID {0}", typeID));
                }
                else
                {
                    return (Company.Delete<Domain>(listToRemove)) ?
                        Request.CreateResponse(HttpStatusCode.OK) :
                        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to remove the domain.");
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
        /// Updates an item in the specified domain, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="listID">The ID of the domain to update the item from.</param>
        /// <param name="id">The ID of the item to update.</param>
        /// <param name="item">The item to update</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{typeID:int}/lists/{listID:int}/{id:int}"), HttpPut]
        public HttpResponseMessage PutItemInDomain(int typeID, int listID, int id, DomainItem item)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Domain, listID, Claim.Update, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this item.");

                var itemToUpdate = Company.GetById<DomainItem>(id);
                if (itemToUpdate.DomainID == listID)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The assigned list for this item does not match the list ID {0}", listID));
                }
                else
                {
                    itemToUpdate.Code = item.Code;
                    itemToUpdate.Description = item.Description;
                    itemToUpdate.Name = item.Name;

                    return (Company.Update<DomainItem>(itemToUpdate)) ?
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
        /// Updates a list in the specified domain type, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="listID">The ID of the domain to add the item to.</param>
        /// <param name="list">The list to update</param>
        /// <returns></returns>
        [Route("{typeID:int}/lists/{listID:int}"), HttpPut]
        public HttpResponseMessage PutListInType(int typeID, int listID, Domain list)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.Domain, listID, Claim.Create, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this list.");

                var listToUpdate = Company.GetById<Domain>(listID);
                if (listToUpdate.DomainTypeID == typeID)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The assigned type for this list does not match the ID {0}", typeID));
                }
                else 
                {
                    listToUpdate.Description = list.Description;
                    listToUpdate.EnforceParentItemSelection = list.EnforceParentItemSelection;
                    listToUpdate.Name = list.Name;

                    return (Company.Update<Domain>(listToUpdate)) ?
                        Request.CreateResponse(HttpStatusCode.OK) :
                        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to update the domain.");
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
        /// Updates a group in the specified domain type, based on its ID.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="groupID">The ID of the group to update.</param>
        /// <param name="group">The group to update.</param>
        /// <returns>Http Status Code: 400:Bad request, 401:Unauthorized, 200:OK</returns>
        [Route("{typeID:int}/groups/{groupID:int}"), HttpPut]
        public HttpResponseMessage PutGroupInType(int typeID, int groupID, DomainGroup group)
        {
            try
            {
                if (!Company.HasPermission(SystemObjects.DomainGroup, groupID, Claim.Create, ClaimObject.Root))
                    return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "You are not allowed to update this group.");

                var groupToUpdate = Company.GetById<DomainGroup>(groupID);
                if (groupToUpdate.DomainTypeID == typeID)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, string.Format("The assigned type for this group does not match the ID {0}", typeID));
                }
                else
                {
                    groupToUpdate.MasterListID = group.MasterListID;
                    groupToUpdate.Name = group.Name;

                    return (Company.Update<DomainGroup>(groupToUpdate)) ?
                        Request.CreateResponse(HttpStatusCode.OK) :
                        Request.CreateErrorResponse(HttpStatusCode.BadRequest, "An error occured while attempting to update the group.");
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
        /// Gets an OData-queryable list of domain types. 
        /// </summary>
        /// <returns>A list of domain types.</returns>
        [Route(""), HttpGet]
        public IQueryable<DomainType> GetTypes()
        {
            return Company.Table<DomainType>();
        }

        /// <summary>
        /// Gets an OData-queryable list of groups within a specified domain type.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <returns>A list of groups.</returns>
        [Route("{typeID:int}/groups"), HttpGet]
        public IQueryable<DomainGroup> GetGroupsByType(int typeID)
        {
            return Company.Filter<DomainGroup>(i => i.DomainTypeID == typeID);
        }

        /// <summary>
        /// Gets an OData-queryable list of domains within a specified domain type.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <returns>A list of domains.</returns>
        [Route("{typeID:int}/lists"), HttpGet]
        public IQueryable<Domain> GetListsByType(int typeID)
        {
            return Company.Filter<Domain>(i => i.DomainTypeID == typeID);
        }

        /// <summary>
        /// Gets an OData-queryable list of items within a specified domain.
        /// </summary>
        /// <param name="typeID">The ID of the domain type the domain belongs to.</param>
        /// <param name="listID">The ID of the domain to get the items from.</param>
        /// <returns>A list of items.</returns>
        [Route("{typeID:int}/lists/{listID:int}"), HttpGet]
        public IQueryable<DomainItem> GetItemsByDomain(int typeID, int listID)
        {
            return Company.Filter<DomainItem>(i => i.DomainID == listID);
        }

        [Route("lists/{sourceArtifactID:int}"), HttpGet]
        public IQueryable<Domain> GetDomainsBySource(int sourceArtifactID)
        {
            return Company.Filter<Domain>(i => i.SourceArtifactID == sourceArtifactID);
        }

        [Route("sources"), HttpGet]
        public IQueryable<dynamic> GetDomainSources()
        {
            return Company.Query<dynamic>(@"select objectid as id, objecttypename + ' :: ' + name as name from cache.objectdetails d
                                                    join domainsourcetype t on t.artifacttypeid = d.objecttypeid and d.objecttype = 'ArtifactType'").AsQueryable();
        }

        [Route("sources/used"), HttpGet]
        public IQueryable<dynamic> GetUsedDomainSources()
        {
            return Company.Query<dynamic>(@"select objectid as id, objecttypename + ' :: ' + d.name as name from cache.objectdetails d
                                                    join domainsourcetype t on t.artifacttypeid = d.objecttypeid and d.objecttype = 'ArtifactType'
													where objectid in (select sourceartifactid from domain)").AsQueryable();
        }

        [Route("lists/xref/{houseDomainItemID:int}"), HttpGet]
        public IQueryable<DomainXrefGridItem> GetXrefsByItem(int houseDomainItemID)
        {
            var sql = @"select x.ID, x.HouseDomainItemID, x.DomainItemID, d1.Code as HouseCode, d2.Code as Code, d.SourceArtifactID, o.Name as SourceArtifactName, d.Name as ListName from domainitemxref x
                        join domainitem d1 on d1.id = x.housedomainitemid
                        join domainitem d2 on d2.id = x.domainitemid
                        join domain d on d.id = d2.domainid
                        join cache.objectdetails o on o.object = 'Artifact' and o.objectid = d.SourceArtifactID
                        where x.HouseDomainItemID = @houseDomainItemID";

            return Company.Query<DomainXrefGridItem>(sql, new { houseDomainItemID }).AsQueryable();
        }

        [Route("lists/classifications"), HttpGet]
        public IQueryable<DomainClassification> GetClassifications()
        {
            return Company.DomainClassifications.AsQueryable();
        }
    }
}
