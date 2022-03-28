using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {
        #region Resource

        string passwordRegex = Validation.Password_Regex;
        string passwordRegexMessage = Validation.Password_Requirements;

        #region Field Generation

        /// <param name="id">ResourceTypeID</param>
        [Route("Resource_AddFields"), NonNullableParameters]
        public JsonResult Resource_AddFields(int id)
        {
            var list = new List<EditableField>();

            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                        
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) });
            list.Add(new EditableField { Row = 2, Column = 2, Required = true, FieldName = "Password", Name = "Password", FieldType = DataType.Password.ToString(), Validations = checkAndAddValidation("Text", "Password", true, passwordRegex, null, null, passwordRegexMessage) });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "IsAdministrator", Name = "Administrator?", FieldType = DataType.Boolean.ToString() });
            

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ResourceType, id).ToList(), 5);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">ResourceID</param>
        [Route("Resource_EditFields"), NonNullableParameters]
        public JsonResult Resource_EditFields(int id)
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            var a = Community.GetById<Resource>(id, i => i.CompanyResources);

            var stateList = CompanyResourceState.Active.GetList().Select(i => new SelectListItem { Text = i.Name, Value = (i.Name).ToString() }).ToList();
            var cr = a.CompanyResources.Single(i => i.CompanyID == Company.CurrentCompanyID);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Value = a.FirstName, Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Value = a.LastName, Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Email", Name = "Email/Username", FieldType = DataType.Text.ToString(), Value = a.Email, Validations = checkAndAddValidation("Text", "Email", true, "", 1, 500) });
            list.Add(new EditableField { Row = 3, Column = 1, Required = true, FieldName = "IsAdministrator", Name = "Administrator?", FieldType = DataType.Boolean.ToString(), Value = cr.IsAdministrator.ToString() });
            list.Add(new EditableField { Row = 3, Column = 2, Required = true, FieldName = "State", Name = "Status", FieldType = DataType.Lookup.ToString(), Items = stateList, Value = (cr.State).ToString() });

            list = (
                loadDynamicFields(
                    SystemObjects.Resource.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.ResourceType, 1).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(),
                    4,
                    false,
                    false
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        [Route("Resource_EditMyInfoFields")]
        public JsonResult Resource_EditMyInfoFields()
        {
            var list = new List<EditableField>();
            var id = Company.CurrentResourceID;
            var a = Community.GetById<Resource>(id);

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "FirstName", Name = "First Name", FieldType = DataType.Text.ToString(), Value = a.FirstName, Validations = checkAndAddValidation("Text", "First Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "LastName", Name = "Last Name", FieldType = DataType.Text.ToString(), Value = a.LastName, Validations = checkAndAddValidation("Text", "Last Name", true, "", 1, 250) });

            list = (
                loadDynamicFields(
                    SystemObjects.Resource.ToString(),
                    id,
                    list,
                    Company.GetFieldTypesByObject(SystemObjects.ResourceType, 1).ToList(),
                    Company.GetFieldRelationsByObject(SystemObjects.Resource, id).ToList(),
                    2,
                    false,
                    false
                )
            );

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post
               

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("ResetResourcePassword")]
        public JsonResult ResetResourcePassword(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);


                if (!form.HasKeys())
                {
                    throw new NoFormDataException(FormControllerApiMessage.Resource);
                }
                var id = parseIntField(form, "ID");
                var model = Community.GetById<Resource>(id);

                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.Resource);
                }
                //valid user at this point generate a password
                ResetResourcePassword(model.ID, model.FirstName, model.Email, model.FormatDisplayName());

                return jsonSuccess(FormControllerApiMessage.ResetPassword, id.ToString(), "reset", HttpStatusCode.OK);

            }
            catch (BaseException ex)
            {
                return jsonException(ex.StatusDescription, ex.StatusCode, ex.StatusMessage);
            }
            catch (Exception ex)
            {
                SendException(ex);
                return jsonException(ex, HttpStatusCode.InternalServerError);
            }
        }

        #endregion

        #endregion

        #region Group

        #region Form Get/Post


        [HttpGet, Route("GetGroupUserList"), NonNullableParameters]
        public JsonNetResult GetGroupUserList(int id, int pagenum, int pagesize, string sortDataField, string sortOrder, string gbfilter, Guid? uid)
        {

            if (uid.HasValue && uid.Value != Guid.Empty)
            {
                id = Company.Filter<Asset>(x => x.uid == uid).SingleOrDefault().ObjectID;
            }

            string querySql;
            var dbArgs = new Dapper.DynamicParameters();

            var hideUsersSql = "";

            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (r.Email not like '%@data3sixty.com' and r.Email not like '%@infogix.com' and r.Email not like '%@precisely.com')";
            }

            querySql = @"
			select  r.LastName + ', ' + r.FirstName as Text, 'Resource|' + cast(r.ResourceID as varchar) + '|' + r.LastName + ', ' + r.FirstName + '|' + cast(r.uid as varchar(100))  as [Value],'User' as [Type] from reporting.Global_Resource r                                    
			where r.[State] = @userStatus 
			and  not exists   (select 1 from ResourceGroup where Groupid =@id   and ResourceID= r.ResourceID) "
            + hideUsersSql;
            dbArgs.Add("id", id);
            dbArgs.Add("userStatus", CompanyResourceState.Active);

            if (!string.IsNullOrEmpty(gbfilter))
            {
                querySql = string.Format(@"select * from ({0}) gb where  [Text] like '%' +   @gbfilter + '%'", querySql);
                dbArgs.Add("gbfilter", gbfilter);
            }
            var countSql = string.Format(@"select count(1) from ({0}) A", querySql);
            var sql = string.Format(@"select * from ({0}) A", querySql);
            countSql = applyFilteringSuffixBind(countSql, Request, dbArgs);
            int totalCount = Company.Query<int>(countSql, dbArgs).First();

            sql = applySortSuffix(sql, sortDataField, sortOrder, "Text", "asc");
            sql = applyPagingSuffix(sql, pagenum, pagesize);

            var query = Company.Query<dynamic>(sql, dbArgs);

            return new JsonNetResult
            {
                Data = new { total = totalCount, results = query },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        #endregion

        #region Group : Edit

        [HttpGet, ActionName("Group"), Route("Group"), NonNullableParameters]
        public JsonNetResult GetGroup(int id,Guid? uid)
        {
            var group = new Group();
            var resourceList = new List<SelectListItem>();
            if (uid.HasValue && uid.Value != Guid.Empty)
                id = Company.Filter<Asset>(x => x.uid == uid).SingleOrDefault().ObjectID;

            if (id == 0)
            {
                resourceList = GetCompanyResources()
                    .OrderBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
                    .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName })
                    .ToList()
                    .Select(i => new SelectListItem { Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() })
                    .ToList();
            }
            else
            {
                group = Company.GetById<Group>(id);
                group.Uid = Company.Assets.Where(x => x.Object == "Group" && x.ObjectID == group.ID).Select(x => x.uid).FirstOrDefault();

                var primaryOwner = GetCompanyResources().Where(x => x.ResourceID == group.PrimaryOwnerResourceID).FirstOrDefault();
                var secondaryOwner = GetCompanyResources().Where(x => x.ResourceID == group.SecondaryOwnerResourceID).FirstOrDefault();
                group.PrimaryOwnerName = primaryOwner != null ? primaryOwner.LastName + ", " + primaryOwner.FirstName : "";
                group.SecondaryOwnerName = secondaryOwner != null ? secondaryOwner.LastName + ", " + secondaryOwner.FirstName : "";
                if(primaryOwner != null)
                    group.PrimaryOwnerUid = primaryOwner.Uid;
                if(secondaryOwner != null)
                    group.SecondaryOwnerUid = secondaryOwner.Uid;

                var currentUsers = Company.Filter<ResourceGroup>(i => i.GroupID == id).Select(i => i.ResourceID).ToList();
                resourceList = GetCompanyResources()
                    .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName, MembershipStatus = currentUsers.Any(o => o == i.ResourceID) ? "Current Member" : "Not Yet a Member" })
                    .OrderBy(i => i.MembershipStatus)
                    .ThenBy(i => i.LastName)
                    .ThenBy(i => i.FirstName)
                    .ToList()
                    .Select(i => new SelectListItem { Group = new SelectListGroup { Name = i.MembershipStatus }, Text = string.Format("{0}, {1}", i.LastName, i.FirstName), Value = i.ID.ToString() })
                    .ToList();
            }

            resourceList.Insert(0, new SelectListItem { Text = "None", Value = "", Group = new SelectListGroup { Name = "" } });

            return new JsonNetResult
            {
                Data = new
                {
                    group,
                    resourceList,
                    Company.CurrentResourceIsAdmin
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        #endregion

        #endregion
    }
}