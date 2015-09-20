using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Http;
using d360.services.interfaces;
using d360.extensions;
using d360.core.entities;
using System.Net.Http;
using System.Net;
using d360.core.exceptions;
using d360.core;

namespace d360.api
{
    [RoutePrefix("domains")]
    public class DomainsController : BaseApiController
    {
        #region DI

        IDomainService DomainService;

        public DomainsController(IDomainService domainService, IAuthenticationSource authenticationSource)
        {
            DomainService = domainService;
            AuthenticationSource = authenticationSource;
        }

        #endregion

        [Route("{typeID:int}/lists/{listID:int}"), PermissionAuthorization]//(Permission.ArtifactTypeRead)
        public HttpResponseMessage Post(int typeID, int listID, DomainItem item)
        {
            var msg = new HttpResponseMessage();

            try
            {
                item.DomainID = listID;
                if (DomainService.AddItem(item) > 0)
                {
                    msg.StatusCode = HttpStatusCode.Created;
                }
                else
                {
                    msg.StatusCode = HttpStatusCode.BadRequest;
                }
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusDescription;
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = GetFullErrorMessage(ex);
            }

            return msg;
        }

        [Route("{typeID:int}/groups")]
        public HttpResponseMessage Post(int typeID, DomainGroup group)
        {
            var msg = new HttpResponseMessage();

            try
            {
                group.DomainTypeID = typeID;
                if (DomainService.AddGroup(group) > 0)
                {
                    msg.StatusCode = HttpStatusCode.Created;
                }
                else
                {
                    msg.StatusCode = HttpStatusCode.BadRequest;
                }
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusDescription;
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = GetFullErrorMessage(ex);
            }

            return msg;
        }

        [Route("{typeID:int}/lists")]
        public HttpResponseMessage Post(int typeID, Domain list)
        {
            var msg = new HttpResponseMessage();

            try
            {
                list.DomainTypeID = typeID;
                if (DomainService.AddList(list) > 0)
                {
                    msg.StatusCode = HttpStatusCode.Created;
                }
                else
                {
                    msg.StatusCode = HttpStatusCode.BadRequest;
                }
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusDescription;
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = GetFullErrorMessage(ex);
            }

            return msg;
        }


        [Route("{typeID:int}/lists/{listID:int}/{id:int}")]
        public HttpResponseMessage DeleteItemFromList(int typeID, int listID, int id)
        {
            var msg = new HttpResponseMessage();

            try
            {
                var itemToRemove = DomainService.GetItem(id);
                if (itemToRemove.DomainID == listID)
                {
                    msg.StatusCode = HttpStatusCode.BadRequest;
                    msg.ReasonPhrase = string.Format("The assigned list for this item does not match the list ID {0}", listID);
                }
                else
                {
                    if (DomainService.DeleteItem(itemToRemove))
                    {
                        msg.StatusCode = HttpStatusCode.OK;
                    }
                    else
                    {
                        msg.StatusCode = HttpStatusCode.BadRequest;
                    }
                }
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusDescription;
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = GetFullErrorMessage(ex);
            }

            return msg;
        }

        [Route("{typeID:int}/groups/{groupID:int}")]
        public HttpResponseMessage DeleteGroupFromType(int typeID, int groupID)
        {
            var msg = new HttpResponseMessage();

            try
            {
                var groupToRemove = DomainService.GetGroup(groupID);
                if (groupToRemove.DomainTypeID == typeID)
                {
                    msg.StatusCode = HttpStatusCode.BadRequest;
                    msg.ReasonPhrase = string.Format("The assigned type for this list does not match the type ID {0}", typeID);
                }
                else
                {
                    if (DomainService.DeleteGroup(groupToRemove))
                    {
                        msg.StatusCode = HttpStatusCode.OK;
                    }
                    else
                    {
                        msg.StatusCode = HttpStatusCode.BadRequest;
                    }
                }
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusDescription;
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = GetFullErrorMessage(ex);
            }

            return msg;
        }

        [Route("{typeID:int}/lists/{listID:int}")]
        public HttpResponseMessage DeleteListFromType(int typeID, int listID)
        {
            var msg = new HttpResponseMessage();

            try
            {
                var listToRemove = DomainService.GetList(listID);
                if (listToRemove.DomainTypeID == typeID)
                {
                    msg.StatusCode = HttpStatusCode.BadRequest;
                    msg.ReasonPhrase = string.Format("The assigned type for this list does not match the type ID {0}", typeID);
                }
                else
                {
                    if (DomainService.DeleteList(listToRemove))
                    {
                        msg.StatusCode = HttpStatusCode.OK;
                    }
                    else
                    {
                        msg.StatusCode = HttpStatusCode.BadRequest;
                    }
                }
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusDescription;
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = GetFullErrorMessage(ex);
            }

            return msg;
        }


        [Route("{typeID:int}/lists/{listID:int}/{id:int}")]
        public HttpResponseMessage PutItemInList(int typeID, int listID, int id, DomainItem item)
        {
            var msg = new HttpResponseMessage();

            try
            {
                var itemToUpdate = DomainService.GetItem(id);
                if (itemToUpdate.DomainID == listID)
                {
                    msg.StatusCode = HttpStatusCode.BadRequest;
                    msg.ReasonPhrase = string.Format("The assigned list for this item does not match the list ID {0}", listID);
                }
                else
                {
                    itemToUpdate.Code = item.Code;
                    itemToUpdate.Description = item.Description;
                    itemToUpdate.Name = item.Name;

                    if (DomainService.EditItem(itemToUpdate) > 0)
                    {
                        msg.StatusCode = HttpStatusCode.OK;
                    }
                    else
                    {
                        msg.StatusCode = HttpStatusCode.BadRequest;
                    }
                }
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusDescription;
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = GetFullErrorMessage(ex);
            }

            return msg;
        }

        [Route("{typeID:int}/lists/{listID:int}")]
        public HttpResponseMessage PutListInType(int typeID, int listID, Domain list)
        {
            var msg = new HttpResponseMessage();

            try
            {
                var listToUpdate = DomainService.GetList(listID);
                if (listToUpdate.DomainTypeID == typeID)
                {
                    msg.StatusCode = HttpStatusCode.BadRequest;
                    msg.ReasonPhrase = string.Format("The assigned type for this list does not match the type ID {0}", typeID);
                }
                else 
                {
                    listToUpdate.Description = list.Description;
                    listToUpdate.EnforceParentItemSelection = list.EnforceParentItemSelection;
                    listToUpdate.Name = list.Name;

                    if (DomainService.EditList(listToUpdate) > 0)
                    {
                        msg.StatusCode = HttpStatusCode.OK;
                    }
                    else
                    {
                        msg.StatusCode = HttpStatusCode.BadRequest;
                    }
                }
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusDescription;
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = GetFullErrorMessage(ex);
            }

            return msg;
        }

        [Route("{typeID:int}/groups/{groupID:int}")]
        public HttpResponseMessage PutGroupInType(int typeID, int groupID, DomainGroup group)
        {
            var msg = new HttpResponseMessage();

            try
            {
                var groupToUpdate = DomainService.GetGroup(groupID);
                if (groupToUpdate.DomainTypeID == typeID)
                {
                    msg.StatusCode = HttpStatusCode.BadRequest;
                    msg.ReasonPhrase = string.Format("The assigned type for this group does not match the type ID {0}", typeID);
                }
                else
                {
                    groupToUpdate.MasterListID = group.MasterListID;
                    groupToUpdate.Name = group.Name;

                    if (DomainService.EditGroup(groupToUpdate) > 0)
                    {
                        msg.StatusCode = HttpStatusCode.OK;
                    }
                    else
                    {
                        msg.StatusCode = HttpStatusCode.BadRequest;
                    }
                }
            }
            catch (BaseException ex)
            {
                msg.StatusCode = ex.StatusCode;
                msg.ReasonPhrase = ex.StatusDescription;
            }
            catch (Exception ex)
            {
                msg.StatusCode = HttpStatusCode.InternalServerError;
                msg.ReasonPhrase = GetFullErrorMessage(ex);
            }

            return msg;
        }


        [Route("{typeID:int}/groups")]
        public IQueryable<DomainGroup> GetGroupsByType(int typeID)
        {
            return DomainService.GetGroupsByType(typeID);
        }

        [Route("{typeID:int}/lists")]
        public IQueryable<Domain> GetListsByType(int typeID)
        {
            return DomainService.GetListsByType(typeID);
        }

        [Route("{typeID:int}/lists/{listID:int}")]
        public IQueryable<DomainItem> GetItemsByLists(int listID)
        {
            return DomainService.GetItemsByDomain(listID);
        }
    }
}
