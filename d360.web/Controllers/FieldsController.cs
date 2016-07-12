using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using d360.core;
using System.Net;
using d360.web.Models;
using d360.core.entities.Contracts;
using d360.core.entities;
using d360.model;

namespace d360.web.Controllers
{
    [RoutePrefix("fields"), Authorize]
    public class FieldsController : BaseController
    {
        #region DI

        public FieldsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {}

        #endregion

        #region Json

        [Route("{type}/{id:int}.json")]
        public JsonResult _FieldTypesByObject(SystemObjects type, int id)
        {
            var list = Company.GetFieldTypeRelationsByObject(type, id).ToList();

            return Json(
                list.Select(i => new
                {
                    i.FriendlyName,
                    i.Category,
                    i.DisplayDescription,
                    i.FormDescription,
                    i.ID,
                    i.IsListable,
                    i.IsRequired,
                    i.SortOrder,
                    ObjectType = i.Object,
                    i.ObjectID
                }), 
                JsonRequestBehavior.AllowGet
                );
        }


        //additoinal details in this one only used by angular 2 right now name is missing in old one.
        [Route("{type}/{id:int}/full")]
        public JsonResult _FieldTypesByObjectFull(SystemObjects type, int id)
        {
            var list = Company.GetFieldTypeRelationsByObject(type, id).ToList();

            return Json(
                list.Select(i => new
                {
                    i.FriendlyName,
                    i.Category,
                    i.DisplayDescription,
                    i.FormDescription,
                    i.ID,
                    i.IsListable,
                    i.IsRequired,
                    i.SortOrder,
                    ObjectType = i.Object,
                    i.ObjectID,
                    i.Name
                }),
                JsonRequestBehavior.AllowGet
                );
        }

        #endregion

        #region Command

        [Route("{type}/{id:int}/{fieldTypeID:int}/move/{direction}"), HttpPost]
        public JsonResult PerformMove(SystemObjects type, int id, int fieldTypeID, string direction)
        {
            HttpStatusCode code = HttpStatusCode.BadRequest;
            string message = "";
            string successMessage = "Field moved successfully.";
            string errorMessage = string.Format("{0} with ID {1} could not be found.", type.ToString(), id);

            var sType = type.ToString();
            var list = Company.Filter<FieldType>(i => i.Object == sType && i.ObjectID == id).OrderBy(i => i.SortOrder).ThenBy(i => i.Name).ToList();

            if (list != null)
            {
                var fieldToMove = list.SingleOrDefault(i => i.ID == fieldTypeID);

                var maxPosition = list.Count;
                
                var currentPosition = fieldToMove.SortOrder;
                var newPosition = (direction == "up") ? 
                    (currentPosition > 0 ? currentPosition - 1 : 0) : 
                    (currentPosition < maxPosition ? currentPosition + 1 : maxPosition);

                fieldToMove.SortOrder = newPosition;

                // Get list of available sort values for this list size
                var sorts = new List<int>();
                for (var i = 1; i <= maxPosition; i++)
                {
                    if (i != newPosition)
                    {
                        sorts.Add(i);
                    }
                }

                foreach (var f in list.Where(i => i.Name != fieldToMove.Name).OrderBy(i => i.SortOrder))
                {
                    f.SortOrder = sorts[0];
                    sorts.RemoveAt(0);
                }

                Company.SaveChanges();//.SaveFieldTypes(type, id, list); 

                code = HttpStatusCode.OK;
                message = successMessage;
            }
            else
            {
                code = HttpStatusCode.NotFound;
                message = errorMessage;
            }

            Response.StatusCode = (int)code;
            Response.StatusDescription = message;
            return Json(new { type = "confirm", title = "Success!", action = "update", message = message.Replace("\n", "  "), id = id, context = ContextList.FieldType, custom = new { commandname = "FieldMove" } });
        }

        #endregion
    }
}
