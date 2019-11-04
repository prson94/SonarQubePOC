using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.exceptions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Newtonsoft.Json.Linq;
using Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {

        #region Responsibility

        #region Form Get/Post

        List<SelectListItem> getResponsibilityResources(string selectedID = "")
        {
            var list = GetCompanyResources()
                .Where(i => i.ResourceID > 0 && i.State == CompanyResourceState.Active)
                .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = $"User: {i.LastName}, {i.FirstName}",
                    Value = $"R|{i.ID}",
                    Selected = ($"R|{i.ID}" == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList();

            list.AddRange(
                Company.Table<Group>()
                .Select(i => new { i.ID, i.Name })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = $"Group: {i.Name}",
                    Value = $"G|{i.ID}",
                    Selected = ($"G|{i.ID}" == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList()
            );

            list.AddRange(
                Company.Table<Organization>()
                .Select(i => new { i.ID, i.Name })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = $"Organization: {i.Name}",
                    Value = $"O|{i.ID}",
                    Selected = ($"O|{i.ID}" == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList()
            );

            return list;
        }

        [HttpDelete, Route("DeleteResponsibilityByID"), NonNullableParameters]
        public JsonResult DeleteResponsibilityByID(long id)
        {
            try
            {
                var model = Company.GetById<ResponsibilityTypeRelationOverrideItem>(id);
                if (model == null) throw new NotFoundException("responsibility");

                if (!Company.HasAssetPermission(model.AssetID, Permission.DeleteResponsibilities))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.Delete(model);
                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK, new { AssetID = model.AssetID });
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

        [HttpGet, Route("Responsibility/Resources"), NonNullableParameters]
        public JsonNetResult ResponsibilityResources(long assetID, int resTypeId, string secAssettype, int secAssetTypeid, int pagenum, int pagesize, string sortDataField, string sortOrder, string gbfilter)
        {
            string querySql;
            string hideUsersSql = "";
            var dbArgs = new Dapper.DynamicParameters();

            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (r.Email not like '%@data3sixty.com' and r.Email not like '%@infogix.com')";
            }
            if (resTypeId == 0)
            {
                querySql = @"
                            select  g.Name as Text, 'Group|' + cast(g.ID as varchar) as [Value],'Group' as [Type] from [Group] g
							where   not exists   (select 1 from ResponsibilityDetail where AssetId =@assetId and SecurityAsset='G' and SecurityAssetID= g.Id) 
							union all
							select  r.LastName + ', ' + r.FirstName as label, 'Resource|' + cast(r.ResourceID as varchar) as [Value],'User' as 'Type' from reporting.Global_Resource r
							where   r.[State] = 1 
                                    and not exists   (select 1 from ResponsibilityDetail where AssetId =@assetId and SecurityAsset='R' and ResourceID= r.ResourceID)";
                querySql += hideUsersSql;
            }
            else
            {
                if (secAssettype == "R")
                {
                    dbArgs.Add("resourceId", secAssetTypeid);
                    dbArgs.Add("groupId", -1);
                }
                else
                {
                    dbArgs.Add("resourceId", -1);
                    dbArgs.Add("groupId", secAssetTypeid);
                }
                querySql = @"
                    		select  g.Name as Text, 'Group|' + cast(g.ID as varchar) as [Value],'Group' as [Type] from [Group] g
							where   not exists   (select 1 from ResponsibilityDetail where AssetId =@assetId and SecurityAsset='G' and SecurityAssetID= g.Id and ResponsibilityTypeID=@responsibilityTypeID
                                and SecurityAssetId <> @groupId) 
							union all
							select  r.LastName + ', ' + r.FirstName as label, 'Resource|' + cast(r.ResourceID as varchar) as [Value],'User' as 'Type' from reporting.Global_Resource r
							where r.[State] = 1 and  not exists   (select 1 from ResponsibilityDetail where AssetId =@assetId and SecurityAsset='R' and ResourceID= r.ResourceID and ResponsibilityTypeID=@responsibilityTypeID
                            and ResourceID <> @resourceId)";
                querySql += hideUsersSql;
                dbArgs.Add("responsibilityTypeID", resTypeId);
            }
            dbArgs.Add("assetID", assetID);

            querySql = string.Format(@"select  Text as [Text],  [Value] + '|' + [Type] + ' :: ' + Text as [Value],[Type] from ({0}) as  Sub", querySql);

            if (!string.IsNullOrEmpty(gbfilter))
            {
                querySql = string.Format(@"select * from ({0}) gb where  [Text] like '%' +   @gbfilter + '%'  or [Type] like   @gbfilter + '%'", querySql);
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

        [HttpGet, Route("Responsibility"), NonNullableParameters]
        public JsonNetResult Responsibility(long assetID, long? overrideID)
        {
            List<SelectListItem> resources;
            List<SelectListItem> responsibilityTypes;
            ResponsibilityTypeRelationOverrideItem responsibility;
            List<ResponsibilityDetail> responsibilityDetails;
            if (overrideID.HasValue)
            {
                responsibility = Company.GetById<ResponsibilityTypeRelationOverrideItem>(overrideID.Value, i => i.ResponsibilityType);
                resources = getResponsibilityResources($"{responsibility.SecurityAsset}|{responsibility.SecurityAssetID}");
                responsibilityTypes = Company.GetAllowedResponsibilityTypesByAsset(assetID).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString(), Selected = (i.ID == responsibility.ResponsibilityTypeID) }).ToList();
            }
            else
            {
                resources = getResponsibilityResources();
                responsibilityTypes = Company.GetAllowedResponsibilityTypesByAsset(assetID).Select(i => new SelectListItem { Text = i.Name, Value = i.ID.ToString() }).ToList();
                responsibility = new ResponsibilityTypeRelationOverrideItem { AssetID = assetID };
            }
            responsibilityTypes.Insert(0, new SelectListItem() { Text = "", Value = "" });
            responsibilityDetails = Company.Filter<ResponsibilityDetail>(i => i.AssetID == assetID).ToList<ResponsibilityDetail>();
            return new JsonNetResult
            {
                Data = new
                {
                    resources,
                    responsibilityTypes,
                    responsibility,
                    responsibilityDetails
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("Responsibility")]
        public JsonResult Responsibility(ResponsibilityTypeRelationOverrideItem r)
        {
            ResponsibilityTypeRelationOverrideItem model;

            if (r.ID == 0)
            {
                try
                {
                    if (!Company.HasAssetPermission(r.AssetID, Permission.ModifyResponsibilities))
                        return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                    Company.Add(r);
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

                return jsonSuccess("Item successfully created.", r.ID.ToString(), "edit", HttpStatusCode.OK, new { AssetID = r.AssetID });
            }
            else
            {
                try
                {
                    model = Company.GetById<ResponsibilityTypeRelationOverrideItem>(r.ID);
                    if (model == null) throw new NotFoundException("responsibility");

                    if (!Company.HasAssetPermission(model.AssetID, Permission.ModifyResponsibilities))
                        return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                    model.ResponsibilityTypeID = r.ResponsibilityTypeID;
                    model.SecurityAsset = r.SecurityAsset;
                    model.SecurityAssetID = r.SecurityAssetID;
                    model.Context = r.Context;

                    Company.Update(model);

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

                return jsonSuccess("Item successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK, new { AssetID = model.AssetID });
            }
        }

        [HttpPut, Route("Responsibility")]
        public JsonResult OverrideResponsibility(ResponsibilityTypeRelationOverrideItem r)
        {
            ResponsibilityTypeRelationOverrideItem model;

            try
            {
                model = Company.GetById<ResponsibilityTypeRelationOverrideItem>(r.ID);
                if (model == null) throw new NotFoundException("responsibility");

                if (!Company.HasAssetPermission(model.AssetID, Permission.ModifyResponsibilities))
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                model.ResponsibilityTypeID = r.ResponsibilityTypeID;
                model.SecurityAsset = r.SecurityAsset;
                model.SecurityAssetID = r.SecurityAssetID;
                model.Context = r.Context;

                Company.Update(model);

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

            return jsonSuccess("Item successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK, new { AssetID = model.AssetID });
        }

        #endregion

        #endregion

        #region ResponsibilityType

        #region Form Get/Post

        [HttpDelete, Route("DeleteResponsibilityTypeByID"), NonNullableParameters]
        public JsonResult DeleteResponsibilityTypeByID(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.GetById<ResponsibilityType>(id);
                if (model == null) throw new NotFoundException("ownership type");

                Company.Delete(SystemObjects.ResponsibilityType, id);

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpGet, ActionName("ResponsibilityType"), Route("ResponsibilityType"), NonNullableParameters]
        public JsonNetResult GetResponsibilityType(int id)
        {
            ResponsibilityType model;

            var selectedAllocations = Company.Filter<ResponsibilityTypeRelation>(i => i.ResponsibilityTypeID == id)
            .ToList()
            .Select(i => new
            {
                i.ResponsibilityTypeID,
                i.ObjectID,
                i.ObjectType
            }).ToList();


            if (id < 1)
            {
                model = new ResponsibilityType();
                selectedAllocations = null;
            }
            else
            {
                model = Company.GetById<ResponsibilityType>(id);

            }

            var allocations = Company
                .GetAllocationOptions()
                .Select(i => new
                {
                    label = $"{i.ClassName} :: {i.Name}",
                    value = string.Format("{0}|{1}", i.ObjectType, i.ObjectTypeID),
                });

            //remove any selected items that no longer exist in available list
            if (selectedAllocations != null && selectedAllocations.Count > 0)
            {
                int indx = selectedAllocations.Count - 1;
                while (indx >= 0)
                {
                    var tag = $"{selectedAllocations[indx].ObjectType}|{selectedAllocations[indx].ObjectID}";
                    if (!allocations.Any(x => x.value == tag))
                        selectedAllocations.RemoveAt(indx);
                    indx--;
                }
            }

            return new JsonNetResult
            {
                Data = new
                {
                    model,
                    allocations,
                    selectedAllocations
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPut, ValidateInput(false), ActionName("ResponsibilityType"), Route("ResponsibilityType")]
        public JsonResult PutResponsibilityType(ResponsibilityType model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var existing = Company.GetById<ResponsibilityType>(model.ID, i => i.ResponsibilityTypeRelations);
                if (existing == null) throw new NotFoundException("ownership type");

                existing.Name = model.Name;
                existing.Description = model.Description;

                // First, do the ADDs.
                foreach (var nr in model.ResponsibilityTypeRelations)
                {
                    if (!existing.ResponsibilityTypeRelations.Any(i => i.ObjectType == nr.ObjectType && i.ObjectID == nr.ObjectID))
                    {
                        existing.ResponsibilityTypeRelations.Add(new ResponsibilityTypeRelation
                        {
                            ObjectType = nr.ObjectType,
                            ObjectID = nr.ObjectID,
                            ResponsibilityTypeID = existing.ID,
                            PermissionsBitMask = 0,
                            CreatedBy = Company.CurrentResourceID,
                            CreatedOn = DateTime.UtcNow,
                            UpdatedBy = Company.CurrentResourceID,
                            UpdatedOn = DateTime.UtcNow
                        });
                    }
                }

                // Last, do the DELETEs.
                var deletes = new List<ResponsibilityTypeRelation>();
                foreach (var dr in existing.ResponsibilityTypeRelations)
                {
                    if (!model.ResponsibilityTypeRelations.Any(i => i.ObjectType == dr.ObjectType && i.ObjectID == dr.ObjectID))
                    {
                        deletes.Add(dr);
                    }
                }
                foreach (var dr in deletes)
                {
                    existing.ResponsibilityTypeRelations.Remove(dr);
                }

                Company.Update(existing);

                return jsonSuccess("Item successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("ResponsibilityType"), Route("ResponsibilityType")]
        public JsonResult PostResponsibilityType(ResponsibilityType model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                //setting all permission as default
                int allPermissions = Permission.DeleteAsset.GetList().Sum(i => i.Value);
                model.ResponsibilityTypeRelations.ToList().
                    ForEach(x => { x.PermissionsBitMask = allPermissions; });
                model.UID = Guid.NewGuid();
                Company.Add(model);

                return jsonSuccess("Item successfully created.", model.ID.ToString(), "add", HttpStatusCode.Created);
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

        #region ResponsibilityTypeRelation

        [HttpGet, ActionName("ResponsibilityTypeRelation_FormData"), Route("ResponsibilityTypeRelation_FormData"), NonNullableParameters]
        public JsonNetResult GetResponsibilityTypeRelation_FormData()
        {
            List<string> ignoreObjects = new List<string>();
            string ignoreObjectTypeSQL = string.Empty;
            if (!Community.IsFusionEnabled())
            {
                ignoreObjects.Add(SystemObjects.FusionType.ToString());
                ignoreObjects.Add(SystemObjects.FusionAttributeType.ToString());
                ignoreObjects.Add(SystemObjects.FusionQueryAttributeType.ToString());
            }

            if (ignoreObjects.Count > 0)
                ignoreObjectTypeSQL = $" AND A.Object not in ({string.Join(",", ignoreObjects.Select(o => "'" + o + "'"))})";

            var AllocationOptions = Company.Query<dynamic>($@"
select	cast(0 as bit) as IsUsed,
        A.ID, 
		A.[Class],
        coalesce(FT.Name+ ' / ','') + P.[Path] as [Path]
from	AssetType A
		cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P
		left join FusionAttributeType FA on A.Object = 'FusionAttributeType' and FA.ID = A.ObjectID
		left join FusionType FT on FT.ID = FA.FusionTypeID
where	Class in (1,2,3,4,6,7,9) {ignoreObjectTypeSQL}
order by case Object
			when 'ArtifactType' then 'Artifacts :: '
			when 'TaxonomyType' then 'Models :: '
			when 'PolicyType' then 'Policies :: '
			when 'RuleType' then 'Rules :: '
			when 'FusionAttributeType' then 'Fusion Attributes :: '
			when 'FusionType' then 'Fusion Types :: '
			when 'ReferenceItemType' then 'Reference Item Type :: '
		end + coalesce(FT.Name+ ' / ','') + P.[Path]
").ToList();
            var PermissionOptions = Permission.DeleteAsset.GetList();

            return new JsonNetResult
            {
                Data = new
                {
                    PermissionOptions,
                    AllocationOptions
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpDelete, Route("ResponsibilityTypeRelation"), NonNullableParameters]
        public JsonResult DeleteResponsibilityTypeRelation(int responsibilityTypeId, string type, int typeId)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.Filter<ResponsibilityTypeRelation>(i =>
                    i.ResponsibilityTypeID == responsibilityTypeId &&
                    i.ObjectType == type &&
                    i.ObjectID == typeId).SingleOrDefault();

                if (model == null) throw new NotFoundException("responsibility type relation");

                Company.RemoveResponsibilityTypeRelation(model);

                return jsonSuccess("Item successfully removed.", "0", "delete", HttpStatusCode.OK);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("ResponsibilityTypeRelation"), Route("ResponsibilityTypeRelation")]
        public JsonResult PostResponsibilityTypeRelation(ResponsibilityTypeRelationViewModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                var assetType = Company.GetById<AssetType>(model.AssetTypeID);

                if (assetType == null)
                    return jsonException("Asset Type not found", HttpStatusCode.BadRequest);

                var rtr = new ResponsibilityTypeRelation { ObjectID = assetType.ObjectID, ObjectType = assetType.Object, ResponsibilityTypeID = model.ResponsibilityTypeID, PermissionsBitMask = 0 };

                rtr.PermissionsBitMask = model.Permissions.Where(i => i.Selected).Sum(i => i.Value);

                Company.Add(rtr);

                return jsonSuccess("Item successfully created.", model.ResponsibilityTypeID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpPut, ValidateInput(false), ActionName("ResponsibilityTypeRelation"), Route("ResponsibilityTypeRelation")]
        public JsonResult PutResponsibilityTypeRelation(ResponsibilityTypeRelationViewModel model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var existing = Company.Filter<ResponsibilityTypeRelation>(r => r.ObjectType == model.ObjectType && r.ObjectID == model.ObjectID && r.ResponsibilityTypeID == model.ResponsibilityTypeID).SingleOrDefault();
                if (existing == null) throw new NotFoundException("responsibility type relation");

                existing.PermissionsBitMask = model.Permissions.Where(i => i.Selected).Sum(i => i.Value);

                Company.Update(existing);

                return jsonSuccess("Item successfully updated.", model.ResponsibilityTypeID.ToString(), "edit", HttpStatusCode.OK);
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

        #region ResponsibilityTypeRelationRule

        #region JSON Feeds

        [HttpPost, AjaxValidateAntiForgeryToken, Route("ResponsibilityTypeRelationRule_WhenTest"), NonNullableParameters]
        public JsonNetResult ResponsibilityTypeRelationRule_WhenTest(ResponsibilityTypeRelationRule rule)
        {
            if (!Company.CurrentResourceIsAdmin)
                return new JsonNetResult { Data = new { Message = "Permission Denied" }, Formatting = Newtonsoft.Json.Formatting.None };

            var results = Company.Database.Connection.GetWhenResults(rule).OrderBy(i => i.Name);
            return new JsonNetResult { Data = results, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("ResponsibilityTypeRelationRule_ThenTest"), NonNullableParameters]
        public JsonNetResult ResponsibilityTypeRelationRule_ThenTest(ResponsibilityTypeRelationRule rule)
        {
            if (!Company.CurrentResourceIsAdmin)
                return new JsonNetResult { Data = new { Message = "Permission Denied" }, Formatting = Newtonsoft.Json.Formatting.None };

            var results = Company.Database.Connection.GetThenResults(rule, this.HideData3SixtyUsers());
            return new JsonNetResult { Data = results, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, ActionName("RelationsByResponsibilityType"), Route("RelationsByResponsibilityType"), NonNullableParameters]
        public JsonNetResult GetRelationsByResponsibilityType(int id)
        {
            var list = Company.Query<dynamic>($@"
select	{QueryConstants.HighLevelTypeCaseStatement} + T.Name as label,
		T.Object + '|' + cast(T.ObjectID as varchar) as value
from	ResponsibilityTypeRelation R
		inner join AssetType T on T.Object = R.ObjectType and T.ObjectID = R.ObjectID and R.ResponsibilityTypeID = {id}
        where R.ObjectType<>'FusionAttributeType'
order by {QueryConstants.HighLevelTypeCaseStatement} + T.Name");
            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, ActionName("ResponsibilityTypeRelationRule_FormData"), Route("ResponsibilityTypeRelationRule_FormData"), NonNullableParameters]
        public JsonNetResult GetResponsibilityTypeRelationRule_FormData(SystemObjects type, int id)
        {
            var tempFieldDataTypes = new List<string>();
            limitedFieldTypes.ForEach(o => { tempFieldDataTypes.Add($"'{o}'"); });
            var ftTypeRemoveString = string.Join(",", tempFieldDataTypes);

            var fieldTypes = Company.Query<string>($@"
select	ID as value,
		FriendlyName as label,
		FT.Type as [type],
		case FT.Type
			when 'Lookup' then cast(1 as bit)
			else cast(0 as bit) 
		end as isLookup,
		(
		select	cast(value as varchar) as [value],
				Text as label 
		from	FieldLookupValue
		where	FieldTypeID = FT.ID
		for json auto
		) as [values]
from	FieldType FT
where	[Object] = @type
		and ObjectID = @id
		and Type not in ({ftTypeRemoveString})
for json auto, WITHOUT_ARRAY_WRAPPER", new { type = type.ToString(), id }).ToList();

            if (type == SystemObjects.OrganizationType)
            {
                fieldTypes = Company.Query<string>($@"
select	FT.ID as value,
		T.[Name] + ' :: ' + FriendlyName as label,
		FT.Type as [type],
		case FT.Type
			when 'Lookup' then cast(1 as bit)
			else cast(0 as bit) 
		end as isLookup,
		(
		select	cast(value as varchar) as [value],
				Text as label 
		from	FieldLookupValue
		where	FieldTypeID = FT.ID
		for json auto
		) as [values]
from	FieldType FT
inner join OrganizationType T on T.ID = FT.ObjectID and T.[State] = 1
where	[Object] = @type
		and Type not in ({ftTypeRemoveString})
order by T.[Name] + ' :: ' + FriendlyName
for json auto, WITHOUT_ARRAY_WRAPPER", new { type = type.ToString() }).ToList();
            }

            var groupFieldTypes = new List<string>();
            if (type == SystemObjects.GroupType)
            {
                groupFieldTypes = Company.Query<string>($@"
		select	0 as value,
				'Name' as label,
				'Lookup' as type,
				cast(1 as bit) as isLookup,
				(
				select	cast(ID as varchar) as [value],
						Name as label 
				from	[Group]
				order by Name
				for json auto
				) as [values]
for json path, WITHOUT_ARRAY_WRAPPER
").ToList();
            }

            var resourceFieldTypes = new List<string>();
            var hideUsersSql = "";

            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (Email not like '%@data3sixty.com' and Email not like '%@infogix.com')";
            }

            if (type == SystemObjects.ResourceType)
            {
                resourceFieldTypes = Company.Query<string>($@"
		select	0 as value,
				'Name' as label,
				'Lookup' as type,
				cast(1 as bit) as isLookup,
				(
				select	cast(ResourceID as varchar) as [value],
						LastName + ', ' + FirstName as label 
				from	reporting.Global_Resource 
				where	[State] = {(int)CompanyResourceState.Active} " + hideUsersSql +
                @"order by LastName + ', ' + FirstName
				for json auto
				) as [values]
for json path, WITHOUT_ARRAY_WRAPPER
").ToList();

            }

            var tempAggregatedFieldValue = "";
            var fieldTypeString = "[";

            tempAggregatedFieldValue = string.Join("", fieldTypes);
            fieldTypeString += $"{tempAggregatedFieldValue}";

            tempAggregatedFieldValue = string.Join("", groupFieldTypes);
            if (fieldTypeString.Length > 1 && !string.IsNullOrEmpty(tempAggregatedFieldValue))
                fieldTypeString += ", ";
            fieldTypeString += string.IsNullOrEmpty(tempAggregatedFieldValue) ? "" : $"{tempAggregatedFieldValue}";

            tempAggregatedFieldValue = string.Join("", resourceFieldTypes);
            if (fieldTypeString.Length > 1 && !string.IsNullOrEmpty(tempAggregatedFieldValue))
                fieldTypeString += ", ";
            fieldTypeString += string.IsNullOrEmpty(tempAggregatedFieldValue) ? "" : $"{tempAggregatedFieldValue}";

            fieldTypeString += "]";

            var fieldTypeArray = JArray.Parse(fieldTypeString);

            var intersectTypes = Company.Query<dynamic>($@"
select	ID as [value],
		case
			when (Subject = @type and SubjectID = @id) then ObjectName + ' (' + coalesce(PredicateName, '') + ')'
			else SubjectName + ' (' + coalesce(PredicateInverse, 'inverse') + ')'
		end as label
from	IntersectTypeDetail 
where	(Subject = @type and SubjectID = @id) 
		or (Object = @type and ObjectID = @id)
order by	case
				when (Subject = @type and SubjectID = {id}) then ObjectName + ' (' + coalesce(PredicateName, '') + ')'
				else SubjectName + ' (' + coalesce(PredicateInverse, 'inverse') + ')'
			end", new { type = type.ToString(), id });

            return new JsonNetResult
            {
                Data = new
                {
                    FieldTypes = fieldTypeArray,
                    IntersectTypes = intersectTypes
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpGet, ActionName("ResponsibilityTypeRelationRuleRelationships_FormData"), Route("ResponsibilityTypeRelationRuleRelationships_FormData"), NonNullableParameters]
        public JsonNetResult GetResponsibilityTypeRelationRuleRelationships_FormData(SystemObjects type, int id, int intersectTypeID)
        {
            string crossApplyValue;
            string labelValue;
            string objType;
            string joinColumn;
            int objId;

            var intersectType = Company.GetById<IntersectType>(intersectTypeID);

            if (intersectType.Object == type.ToString() && intersectType.ObjectID == id)
            {
                objType = intersectType.Subject;
                objId = intersectType.SubjectID;
                joinColumn = "Subject";
            }
            else
            {
                objType = intersectType.Object;
                objId = intersectType.ObjectID;
                joinColumn = "Object";
            }

            if (objType == SystemObjects.TaxonomyType.ToString() || objType == SystemObjects.PolicyType.ToString())
            {
                crossApplyValue = "getassettextpathbyid(D.id, '/') atp";
                labelValue = "atp.textpath";
            }
            else
            {
                crossApplyValue = "dbo.GetAssetDisplayValueById(D.ID) DN";
                labelValue = "DN.DisplayValue";
            }

            var items = Company.Query<dynamic>($@"
                select	D.Object + '|' + cast(D.ObjectID as varchar) as value,
		            {labelValue} as label 
                from	Asset D
                    inner join AssetType DT on DT.ID = D.AssetTypeID
                    inner join IntersectType I on I.{joinColumn} = DT.Object and I.{joinColumn}ID = DT.ObjectID and I.ID = {intersectTypeID}
                    cross apply {crossApplyValue}
                    order by {labelValue}");

            return new JsonNetResult
            {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Form Get/Post

        [HttpDelete, Route("DeleteResponsibilityTypeRelationRuleByID"), NonNullableParameters]
        public async Task<JsonResult> DeleteResponsibilityTypeRelationRuleByID(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.GetById<ResponsibilityTypeRelationRule>(id);
                if (model == null) throw new NotFoundException("responsibility type rule");

                Company.Delete(model);
                await ((Company.Database.Connection as System.Data.SqlClient.SqlConnection).RemoveRelationRuleResultsByRule(id));

                return jsonSuccess("Item successfully removed.", id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteResponsibilityTypeRelationRuleDateByID"), NonNullableParameters]
        public JsonResult DeleteResponsibilityTypeRelationRuleDateByID(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var model = Company.GetById<ResponsibilityTypeRelationRule>(id);
                if (model == null) throw new NotFoundException("responsibility type rule");

                model.LastRunOn = null;
                Company.Update(model);
                return jsonSuccess("Item date successfully removed.", id.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpGet, ActionName("ResponsibilityTypeRelationRule"), Route("ResponsibilityTypeRelationRule"), NonNullableParameters]
        public JsonNetResult GetResponsibilityTypeRelationRule(int id)
        {

            ResponsibilityTypeRelationRule model;

            if (id < 1)
            {
                model = new ResponsibilityTypeRelationRule();
            }
            else
            {
                model = Company.GetById<ResponsibilityTypeRelationRule>(id);
                model.SetDefinitionFromRaw();
            }

            return new JsonNetResult
            {
                Data = model,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [HttpPut, ValidateInput(false), ActionName("ResponsibilityTypeRelationRule"), Route("ResponsibilityTypeRelationRule")]
        public async Task<JsonResult> PutResponsibilityTypeRelationRule(ResponsibilityTypeRelationRule model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var existing = Company.GetById<ResponsibilityTypeRelationRule>(model.ID);
                if (existing == null) throw new NotFoundException("ownership type");

                existing.Name = model.Name;
                existing.StructuredDefinition = model.StructuredDefinition;
                existing.Object = model.Object;
                existing.ObjectID = model.ObjectID;
                existing.ResponsibilityTypeID = model.ResponsibilityTypeID;
                existing.Context = model.Context;
                existing.ApplyToType = model.ApplyToType;
                existing.IsVisible = model.IsVisible;
                existing.UpdatedOn = DateTime.UtcNow;

                var previousDefinition = existing.Definition;
                existing.SetRawFromDefinition();
                if (existing.StructuredDefinition?.Then?.Conditions?.Where(x => x.Value == null).Count() > 0)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "ResponsibilityType", FormInfo.Responsibility_Then_Filter_Value_Required);
                }

                var definitionIsDifferent = (previousDefinition != existing.Definition);
                if (definitionIsDifferent)
                {
                    existing.LastRunOn = DateTime.Parse("1/1/2000");
                }

                Company.Update(existing);

                // Re-process this rule.
                if (definitionIsDifferent)
                {
                    await ((Company.Database.Connection as System.Data.SqlClient.SqlConnection).ProcessResponsibilityRelationRules(existing.ID));
                }


                return jsonSuccess("Item successfully updated and processed.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), ActionName("ResponsibilityTypeRelationRule"), Route("ResponsibilityTypeRelationRule")]
        public async Task<JsonResult> PostResponsibilityTypeRelationRule(ResponsibilityTypeRelationRule model)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                model.SetRawFromDefinition();
                if (model.StructuredDefinition?.Then?.Conditions?.Where(x => x.Value == null).Count() > 0)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, "ResponsibilityType", FormInfo.Responsibility_Then_Filter_Value_Required);
                }

                model.UpdatedOn = DateTime.UtcNow;
                Company.Add(model);

                // Process this rule.
                await ((Company.Database.Connection as System.Data.SqlClient.SqlConnection).ProcessResponsibilityRelationRules(model.ID));

                return jsonSuccess("Item successfully created and processed.", model.ID.ToString(), "add", HttpStatusCode.Created);
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
    }
}