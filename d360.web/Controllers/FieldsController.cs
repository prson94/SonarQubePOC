using d360.core;
using d360.core.entities;
using d360.model;
using d360.web.Models.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

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
        public JsonNetResult _FieldTypesByObject(SystemObjects type, int id)
        {
            var list = Company
                .GetFieldTypesByObject(type, id)
                .Select(i => new {
                    i.FriendlyName,
                    i.Category,
                    i.DisplayDescription,
                    i.FormDescription,
                    i.ID,
                    i.IsListable,
                    i.IsRequired,
                    i.ColumnOrder,
                    i.SortOrder,
                    ObjectType = i.Object,
                    i.ObjectID
                });

            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
        }

        /// <summary>
        /// Provides additional details in this one only used by Angular right now name is missing in old one.
        /// </summary>
        /// <param name="type">The object type name</param>
        /// <param name="id">The object type ID</param>
        /// <returns></returns>
        [Route("{type}/{id:int}/full")]
        public JsonNetResult _FieldTypesByObjectFull(SystemObjects type, int id)
        {
            var types = DataType.Text.GetDataTypeInfoList();
            var list = (from ft in Company.GetFieldTypesByObject(type, id).ToList()
                       join dt in types on ft.Type equals dt.Name
                       select new {
                           ft.FriendlyName,
                           ft.Category,
                           ft.DisplayDescription,
                           ft.FormDescription,
                           ft.ID,
                           ft.IsListable,
                           ft.IsPartOfKey,
                           ft.IsRequired,
                           ft.ColumnOrder,
                           ft.SortOrder,
                           ObjectType = ft.Object,
                           ft.ObjectID,
                           ft.Name,
                           Type = dt.Description,
                           ft.ColumnWidth
                       }).ToList().OrderBy(i => i.ColumnOrder);

            return new JsonNetResult { Data = list, Formatting = Newtonsoft.Json.Formatting.None };
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
            var list = Company.Filter<FieldType>(i => i.Object == sType && i.ObjectID == id).OrderBy(i => i.ColumnOrder).ThenBy(i => i.Name).ToList();

            if (list != null)
            {
                var fieldToMove = list.SingleOrDefault(i => i.ID == fieldTypeID);

                var maxPosition = list.Count;
                
                var currentPosition = fieldToMove.ColumnOrder;
                var newPosition = (direction == "up") ? 
                    (currentPosition > 0 ? currentPosition - 1 : 0) : 
                    (currentPosition < maxPosition ? currentPosition + 1 : maxPosition);

                fieldToMove.ColumnOrder = newPosition;

                // Get list of available sort values for this list size
                var sorts = new List<int>();
                for (var i = 1; i <= maxPosition; i++)
                {
                    if (i != newPosition)
                    {
                        sorts.Add(i);
                    }
                }

                foreach (var f in list.Where(i => i.Name != fieldToMove.Name).OrderBy(i => i.ColumnOrder))
                {
                    f.ColumnOrder = sorts[0];
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
            return Json(new { type = "confirm", title = "Success!", action = "update", message = message.Replace("\n", "  "), id = id, custom = new { commandname = "FieldMove" } });
        }

        #endregion
    }
}
