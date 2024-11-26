using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;

using Resources;

namespace d360.web.Controllers
{
	public partial class FormController : BaseController
	{
		#region Resource

		private readonly string passwordRegex = Validation.Password_Regex;
		private readonly string passwordRegexMessage = Validation.Password_Requirements;

		#region Field Generation

		/// <param name="id">ResourceTypeID</param>
		[Route("Resource_AddFields"), NonNullableParameters, RequireAdminPermissions]
		public async Task<JsonResult> Resource_AddFields(int id)
		{
			var list = new List<EditableField>
			{
				new EditableField
				{
					Row = 1,
					Column = 1,
					Required = true,
					FieldName = "FirstName",
					Name = "First Name",
					FieldType = DataType.Text.ToString(),
					Validations = checkAndAddValidation(fieldType: "Text",
										friendlyName: "First Name",
										required: true,
										pattern: "",
										minLength: 1,
										maxLength: 250)
				},
				new EditableField
				{
					Row = 1,
					Column = 2,
					Required = true,
					FieldName = "LastName",
					Name = "Last Name",
					FieldType = DataType.Text.ToString(),
					Validations = checkAndAddValidation(fieldType: "Text",
										friendlyName: "Last Name",
										required: true,
										pattern: "",
										minLength: 1,
										maxLength: 250)
				},
				new EditableField
				{
					Row = 2,
					Column = 1,
					Required = true,
					FieldName = "Email",
					Name = "Email/Username",
					FieldType = DataType.Text.ToString(),
					Validations = checkAndAddValidation(fieldType: "Text",
										friendlyName: "Email",
										required: true,
										pattern: "",
										minLength: 1,
										maxLength: 500)
				},
				new EditableField
				{
					Row = 2,
					Column = 2,
					Required = true,
					FieldName = "Password",
					Name = "Password",
					FieldType = "Password",
					Validations = checkAndAddValidation(fieldType: "Text",
										friendlyName: "Password",
										required: true,
										pattern: passwordRegex,
										minLength: null,
										maxLength: null,
										validationMessage: passwordRegexMessage)
				},
				new EditableField
				{
					Row = 3,
					Column = 1,
					Required = true,
					FieldName = "IsAdministrator",
					Name = "Administrator?",
					FieldType = DataType.Boolean.ToString()
				}
			};

			list = await loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.ResourceType, id).ToList(), 5);

			return Json(list, JsonRequestBehavior.AllowGet);
		}

		/// <param name="id">ResourceID</param>
		[RequireAdminPermissions]
		public async Task<JsonResult> Resource_EditFields(int id)
		{
			var list = new List<EditableField>();
			var resourceResponse = await Community.ReadUserByIdAsync(id);
			Resource user = null;
			if (!resourceResponse.IsSuccess)
			{
				return null;
			}
			user = resourceResponse.Data;

			var tenantUser = await Community.ReadTenantUserAsync(SecurityContext.CompanyID, id);
			if (tenantUser == null)
			{
				return null;
			}

			var stateList = CompanyResourceState.Active.GetList().Select(i => new SelectListItem { Text = i.Name, Value = (i.Name).ToString() }).ToList();
			var asset = Company.Filter<Asset>(i => i.Object == "Resource" && i.ObjectID == id).SingleOrDefault();
			var assettype = Company.AssetTypes.Where(i => i.Class == AssetTypeClass.User).Select(i => new { i.ID }).FirstOrDefault();

			list.Add(new EditableField 
			{
				FieldName = "ID",
				FieldType = DataType.Hidden.ToString(), 
				Value = id.ToString() 
			});
			
			list.Add(new EditableField 
			{ 
				Row = 1,
				Column = 1, 
				Required = true, 
				FieldName = "FirstName", 
				Name = "First Name", 
				FieldType = DataType.Text.ToString(),
				Value = user.FirstName, 
				Validations = checkAndAddValidation(fieldType: "Text",
										friendlyName: "First Name",
										required: true,
										pattern: "",
										minLength: 1,
										maxLength: 250) 
			});
			
			list.Add(new EditableField 
			{ 
				Row = 1,
				Column = 2,
				Required = true,
				FieldName = "LastName",
				Name = "Last Name", 
				FieldType = DataType.Text.ToString(),
				Value = user.LastName, 
				Validations = checkAndAddValidation(fieldType: "Text",
										friendlyName: "Last Name",
										required: true,
										pattern: "",
										minLength: 1,
										maxLength: 250) 
			});
			
			list.Add(new EditableField 
			{ 
				Row = 2, 
				Column = 1,
				Required = true,
				FieldName = "Email",
				Name = "Email/Username",
				FieldType = DataType.Text.ToString(), 
				Value = user.Email,
				Validations = checkAndAddValidation(fieldType: "Text",
										friendlyName: "Email",
										required: true,
										pattern: "",
										minLength: 1,
										maxLength: 500)
			});
			
			list.Add(new EditableField 
			{
				Row = 3, 
				Column = 1, 
				Required = true, 
				FieldName = "IsAdministrator",
				Name = "Administrator?", 
				FieldType = DataType.Boolean.ToString(), 
				Value = tenantUser.IsAdministrator.ToString() 
			});
			
			list.Add(new EditableField 
			{ 
				Row = 3, 
				Column = 2,
				Required = true,
				FieldName = "State", 
				Name = "Status", 
				FieldType = DataType.Lookup.ToString(),
				Items = stateList, Value = tenantUser.State.ToString() 
			});

			var fieldTypes = Company.Filter<FieldType>(i => i.AssetTypeID == assettype.ID).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();

			var fields = new List<FieldWithRelation>();

			if (asset != null)
			{
				fields = Company.Filter<FieldWithRelation>(i => i.AssetID == asset.ID).ToList();
			}

			list = await loadDynamicFields(
					SystemObjects.Resource.ToString(),
					id,
					list,
					fieldTypes,
					fields,
					4,
					false,
					false
				);

			return Json(list, JsonRequestBehavior.AllowGet);
		}

		[Route("Resource_EditMyInfoFields")]
		public async Task<JsonResult> Resource_EditMyInfoFields()
		{
			var list = new List<EditableField>();
			var id = SecurityContext.ResourceID;
			var a = await Community.ReadUserByIdAsync(id);
			var asset = Company.Filter<Asset>(i => i.Object == "Resource" && i.ObjectID == id).SingleOrDefault();

			list.Add(new EditableField
			{ 
				Row = 1, 
				Column = 1, 
				Required = true, 
				FieldName = "FirstName",
				Name = "First Name",
				FieldType = DataType.Text.ToString(), 
				Value = a.Data.FirstName,
				Validations = checkAndAddValidation(fieldType: "Text",
										friendlyName: "First Name",
										required: true,
										pattern: "",
										minLength: 1,
										maxLength: 250) 
			});
			
			list.Add(new EditableField 
			{
				Row = 1,
				Column = 2, 
				Required = true,
				FieldName = "LastName",
				Name = "Last Name", 
				FieldType = DataType.Text.ToString(),
				Value = a.Data.LastName,
				Validations = checkAndAddValidation(fieldType: "Text",
										friendlyName: "Last Name",
										required: true,
										pattern: "",
										minLength: 1,
										maxLength: 250) 
			});

			var fieldTypes = Company.Filter<FieldType>(i => i.AssetTypeID == asset.AssetTypeID).OrderBy(i => i.ColumnOrder).ThenBy(i => i.FriendlyName).ToList();
			var fields = Company.Filter<FieldWithRelation>(i => i.AssetID == asset.ID).ToList();

			list = await loadDynamicFields(
					SystemObjects.Resource.ToString(),
					id,
					list,
					fieldTypes,
					fields,
					2,
					false,
					false
				);

			return Json(list, JsonRequestBehavior.AllowGet);
		}

		#endregion

		#region Form Get/Post


		[HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("ResetResourcePassword"), RequireAdminPermissions]
		public async Task<JsonResult> ResetResourcePassword(FormCollection form)
		{
			if (!form.HasKeys())
			{
				return jsonException(FormControllerApiMessage.Resource, HttpStatusCode.BadRequest);
			}

			var id = parseIntField(form, "ID");
			var model = await Community.ReadUserByIdAsync(id);

			if (!model.IsSuccess)
			{
				return jsonException(FormControllerApiMessage.Resource, HttpStatusCode.NotFound);
			}

			//valid user at this point generate a password
			await ResetResourcePassword(model.Data.ID, model.Data.FirstName, model.Data.Email, model.Data.FormatDisplayName());

			return jsonSuccess(FormControllerApiMessage.ResetPassword, id.ToString(), "reset", HttpStatusCode.OK);
		}

		#endregion

		#endregion

		#region Group

		#region Form Get/Post


		[HttpGet, Route("GetGroupUserList"), NonNullableParameters]
		public async Task<JsonNetResult> GetGroupUserList(int id, int pagenum, int pagesize, string sortDataField, string sortOrder, string gbfilter, Guid? uid)
		{

			if (uid.HasValue && uid.Value != Guid.Empty)
			{
				id = Company.Filter<Asset>(x => x.uid == uid).SingleOrDefault().ObjectID;
			}

			string querySql;
			var dbArgs = new Dapper.DynamicParameters();
			var hideUsersSql = "";

			if (await GetHideData3SixtyUsers())
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
		public async Task<JsonNetResult> GetGroup(int id, Guid? uid)
		{
			var group = new Group();
			var resourceList = new List<SelectListItem>();

			if (uid.HasValue && uid.Value != Guid.Empty)
			{
				id = Company.Filter<Asset>(x => x.uid == uid).SingleOrDefault().ObjectID;
			}

			var companyResources = await GetCompanyResources();

			if (id == 0)
			{
				resourceList = companyResources
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

				var primaryOwner = companyResources.Where(x => x.ResourceID == group.PrimaryOwnerResourceID).FirstOrDefault();
				var secondaryOwner = companyResources.Where(x => x.ResourceID == group.SecondaryOwnerResourceID).FirstOrDefault();
				group.PrimaryOwnerName = primaryOwner != null ? primaryOwner.LastName + ", " + primaryOwner.FirstName : "";
				group.SecondaryOwnerName = secondaryOwner != null ? secondaryOwner.LastName + ", " + secondaryOwner.FirstName : "";
				
				if (primaryOwner != null)
				{
					group.PrimaryOwnerUid = primaryOwner.Uid;
				}

				if (secondaryOwner != null)
				{
					group.SecondaryOwnerUid = secondaryOwner.Uid;
				}

				var currentUsers = Company.Filter<ResourceGroup>(i => i.GroupID == id).Select(i => i.ResourceID).ToList();
				resourceList = companyResources
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
					CurrentResourceIsAdmin = SecurityContext.IsAdministrator
				},
				Formatting = Newtonsoft.Json.Formatting.None
			};

		}

		#endregion

		#endregion
	}
}
