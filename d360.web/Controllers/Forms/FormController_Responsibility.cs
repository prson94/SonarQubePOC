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
                .Select(i => new { ID = i.ResourceID, i.FirstName, i.LastName, i.Uid })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = $"User: {i.LastName}, {i.FirstName}",
                    Value = $"R|{i.ID}|{i.Uid}",
                    Selected = ($"R|{i.ID}" == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList();

            var groupSQL = $@"SELECT G.ID, G.Name, secasset.Uid FROM [Group] G INNER JOIN [Asset] secasset ON secasset.[Object] = 'Group' AND secasset.ObjectID = g.id";

            list.AddRange(
                Company.Query<dynamic>(groupSQL)
                .Select(i => new { i.ID, i.Name, i.Uid })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = $"Group: {i.Name}",
                    Value = $"G|{i.ID}|{i.Uid}",
                    Selected = ($"G|{i.ID}" == selectedID)
                })
                .OrderBy(i => i.Text)
                .ToList()
            );

            var organisationSQL = $@"SELECT O.ID, O.Name, secasset.Uid FROM Organization O INNER JOIN [Asset] secasset ON secasset.[Object] = 'Organization' AND secasset.ObjectID = O.id";

            list.AddRange(
                Company.Query<dynamic>(organisationSQL)
                .Select(i => new { i.ID, i.Name, i.Uid })
                .ToList()
                .Select(i => new SelectListItem
                {
                    Text = $"Organization: {i.Name}",
                    Value = $"O|{i.ID}|{i.Uid}",
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
                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.Responsibility);
                }

                if (!Company.HasAssetPermission(model.AssetID, Permission.DeleteResponsibilities))
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                Company.Delete(model);
                return jsonSuccess(string.Format(ApiMessages.SucessfullyRemoved,FormControllerApiMessage.Item), id.ToString(), "delete", HttpStatusCode.OK, new { AssetID = model.AssetID });
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
        public JsonNetResult ResponsibilityResources(long assetID, int resTypeId, string secAssettype, int secAssetTypeid, int pagenum, int pagesize, string sortDataField, string sortOrder, string gbfilter, Guid? resTypeUid)
        {
            string querySql;
            string hideUsersSql = "";
            var dbArgs = new Dapper.DynamicParameters();

            if (HideData3SixtyUsers())
            {
                hideUsersSql = " and (r.Email not like '%@data3sixty.com' and r.Email not like '%@infogix.com' and r.Email not like '%@precisely.com')";
            }

            if (resTypeId == 0 && resTypeUid != null)
            {
                resTypeId = Company.ResponsibilityTypes.FirstOrDefault(x => x.UID == resTypeUid).ID;
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
        public JsonNetResult Responsibility(long assetID, long? overrideID, Guid? assetUid, Guid? responsibilityUid, Guid? ResourceUid)
        {
            if (assetUid.HasValue)
            {
                assetID = Company.Assets.FirstOrDefault(a => a.uid == assetUid.Value).ID;
            }
            if (responsibilityUid.HasValue && ResourceUid.HasValue)
            {
                var responsibilityID = Company.ResponsibilityTypes.FirstOrDefault(r => r.UID == responsibilityUid.Value).ID;
                var Resource = Company.Assets.FirstOrDefault(a => a.uid == ResourceUid);
                var resourceType = "R";
                switch (Resource.Object)
                {
                    case "Group":
                        resourceType = "G";
                        break;
                    case "Organisation":
                    case "Organization":
                        resourceType = "O";
                        break;
                    default:
                        break;
                }
                var responsibilityTypeRelationOverrideItem = Company.ResponsibilityTypeRelationOverrideItems.FirstOrDefault(ro => ro.ResponsibilityTypeID == responsibilityID && ro.AssetID == assetID && ro.SecurityAssetID == Resource.ObjectID && ro.SecurityAsset == resourceType);
                overrideID = responsibilityTypeRelationOverrideItem?.ID;
            }

            List<SelectListItem> resources;
            List<SelectListItem> responsibilityTypes;
            ResponsibilityTypeRelationOverrideItem responsibility;
            List<ResponsibilityDetail> responsibilityDetails;
            if (overrideID.HasValue)
            {
                responsibility = Company.GetById<ResponsibilityTypeRelationOverrideItem>(overrideID.Value, i => i.ResponsibilityType);
                resources = getResponsibilityResources($"{responsibility.SecurityAsset}|{responsibility.SecurityAssetID}");
                responsibilityTypes = Company.GetAllowedResponsibilityTypesByAsset(assetID).Select(i => new SelectListItem { Text = i.Name, Value = i.UID.ToString(), Selected = (i.ID == responsibility.ResponsibilityTypeID) }).ToList();
            }
            else
            {
                resources = getResponsibilityResources();
                responsibilityTypes = Company.GetAllowedResponsibilityTypesByAsset(assetID).Select(i => new SelectListItem { Text = i.Name, Value = i.UID.ToString() }).ToList();
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


        #endregion

        #endregion

        #region ResponsibilityType

        #region Form Get/Post


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
                    {
                        selectedAllocations.RemoveAt(indx);
                    }
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
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                var existing = Company.GetById<ResponsibilityType>(model.ID, i => i.ResponsibilityTypeRelations);
                if (existing == null)
                {
                    throw new NotFoundException(ApiMessages.OwnershipType);
                }
                if (model.Name.Trim().Length > 250)
                {
                    return jsonException(FormControllerApiMessage.ResponsibilityNameMax250, HttpStatusCode.BadRequest);
                }

                existing.Name = model.Name;
                existing.Description = model.Description;

                int allPermissions = Permission.DeleteAsset.GetList().Sum(i => i.Value);

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
                            PermissionsBitMask = allPermissions,
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

                return jsonSuccess(string.Format(ApiMessages.SucessfullyUpdated,FormControllerApiMessage.Item), model.ID.ToString(), "edit", HttpStatusCode.OK);
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
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }
                //setting all permission as default
                int allPermissions = Permission.DeleteAsset.GetList().Sum(i => i.Value);
                model.ResponsibilityTypeRelations.ToList().
                    ForEach(x => { x.PermissionsBitMask = allPermissions; });

                if (model.Name.Trim().Length > 250)
                {
                    return jsonException(FormControllerApiMessage.ResponsibilityNameMax250, HttpStatusCode.BadRequest);
                }

                model.UID = Guid.NewGuid();
                Company.Add(model);

                return jsonSuccess(string.Format(ApiMessages.SucessfullyCreated,FormControllerApiMessage.Item), model.ID.ToString(), "add", HttpStatusCode.Created);
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
            var AllocationOptions = Company.Query<dynamic>($@"
select	cast(0 as bit) as IsUsed,
        A.ID, 
		A.[Class],
        A.Uid,
    case Object
            when 'ArtifactType' then
				case Class
					when 1 then 'Business Asset :: '
					else 'Technical Asset ::'
				end
			when 'TaxonomyType' then 'Model :: '
			when 'PolicyType' then 'Policy :: '
			when 'RuleType' then 'Rule :: ' 
			when 'ReferenceItemType' then 'Reference Item Type :: '
		end + P.[Path] as [Path]
from	AssetType A
		cross apply dbo.GetAssetTypeTextPathById(A.ID, ' / ') P  
where	Class in (1, 2, 6, 7, 8, 9)
order by case Object
			when 'ArtifactType' then
				case Class
					when 1 then 'Business Asset :: '
					else 'Technical Asset ::'
				end
			when 'TaxonomyType' then 'Model :: '
			when 'PolicyType' then 'Policy :: '
			when 'RuleType' then 'Rule :: '  
			when 'ReferenceItemType' then 'Reference Item Type :: '
		end + P.[Path]
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

        #endregion

        #region ResponsibilityTypeRelationRule

        #region JSON Feeds

        [HttpPost, AjaxValidateAntiForgeryToken, Route("ResponsibilityTypeRelationRule_WhenTest"), NonNullableParameters]
        public async Task<JsonNetResult> ResponsibilityTypeRelationRule_WhenTest(ResponsibilityTypeRelationRule rule)
        {
            if (!Company.CurrentResourceIsAdmin)
                return new JsonNetResult { Data = new { Message = "Permission Denied" }, Formatting = Newtonsoft.Json.Formatting.None };

            var results = (await Company.GetWhenResults(rule)).OrderBy(i => i.Name);
            return new JsonNetResult { Data = results, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpPost, AjaxValidateAntiForgeryToken, Route("ResponsibilityTypeRelationRule_ThenTest"), NonNullableParameters]
        public JsonNetResult ResponsibilityTypeRelationRule_ThenTest(ResponsibilityTypeRelationRule rule)
        {
            if (!Company.CurrentResourceIsAdmin)
                return new JsonNetResult { Data = new { Message = "Permission Denied" }, Formatting = Newtonsoft.Json.Formatting.None };

            var results = Company.GetThenResults(rule, this.HideData3SixtyUsers());
            return new JsonNetResult { Data = results, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [HttpGet, ActionName("RelationsByResponsibilityType"), Route("RelationsByResponsibilityType"), NonNullableParameters]
        public JsonNetResult GetRelationsByResponsibilityType(int id)
        {
            var list = Company.Query<dynamic>($@"
            select	{QueryConstants.HighLevelTypeCaseStatement} + coalesce(P.[Path], T.[Name]) as label,
		            T.Object + '|' + cast(T.ObjectID as varchar) as value
            from	ResponsibilityTypeRelation R
		            inner join AssetType T on T.Object = R.ObjectType and T.ObjectID = R.ObjectID and R.ResponsibilityTypeID = @id
                    cross apply dbo.GetAssetTypeTextPathById(T.ID, ' / ') P
            order by {QueryConstants.HighLevelTypeCaseStatement} + coalesce(P.[Path], T.[Name])", new { id });

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
            tempFieldDataTypes.Add($"'{DataType.Relationship.ToString()}'");
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
                hideUsersSql = " and (Email not like '%@data3sixty.com' and Email not like '%@infogix.com' and Email not like '%@precisely.com')";
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
            string objType;
            string joinColumn;

            var intersectType = Company.GetById<IntersectType>(intersectTypeID);

            if (intersectType.Object == type.ToString() && intersectType.ObjectID == id)
            {
                objType = intersectType.Subject;
                joinColumn = "Subject";
            }
            else
            {
                objType = intersectType.Object;
                joinColumn = "Object";
            }

            if (objType == SystemObjects.TaxonomyType.ToString() || objType == SystemObjects.PolicyType.ToString())
            {
                return new JsonNetResult
                {
                    Data = Company.Query<dynamic>($@"
                        select	D.Object + '|' + cast(D.ObjectID as varchar) as value,
		                    atp.textpath as label 
                        from	Asset D
                            inner join AssetType DT on DT.ID = D.AssetTypeID
                            inner join IntersectType I on I.{joinColumn} = DT.Object and I.{joinColumn}ID = DT.ObjectID and I.ID = {intersectTypeID}
                            cross apply getassettextpathbyid(D.id, '/') atp
                            order by atp.textpath"),
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
            else if ((objType == SystemObjects.ArtifactType.ToString()) || (objType == SystemObjects.RuleType.ToString()))
            {
                return new JsonNetResult
                {
                    Data = Company.Query<dynamic>($@"
                        select D.Object + '|' + cast(D.ObjectID as varchar(30)) as value,
		                    DN.DisplayValue as label
                        from Asset D
                            inner join AssetType DT on DT.ID = D.AssetTypeID
                            inner join IntersectType I on I.{joinColumn} = DT.Object and I.{joinColumn}ID = DT.ObjectID and I.ID = {intersectTypeID}
                            inner join AssetDisplayValue DN on DN.AssetID = D.ID
                            order by DN.DisplayValuePrefix"),
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }

            return new JsonNetResult
            {
                Data = Company.Query<dynamic>($@"
                        select	D.Object + '|' + cast(D.ObjectID as varchar) as value,
		                    DN.DisplayValue as label 
                        from	Asset D
                            inner join AssetType DT on DT.ID = D.AssetTypeID
                            inner join IntersectType I on I.{joinColumn} = DT.Object and I.{joinColumn}ID = DT.ObjectID and I.ID = {intersectTypeID}
                            cross apply dbo.GetAssetDisplayValueById(D.ID) DN
                            order by DN.DisplayValue"),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        #endregion

        #region Form Get/Post

        [HttpDelete, Route("DeleteResponsibilityTypeRelationRuleDateByID"), NonNullableParameters]
        public JsonResult DeleteResponsibilityTypeRelationRuleDateByID(int id)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                {
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);
                }

                var model = Company.GetById<ResponsibilityTypeRelationRule>(id);
                if (model == null)
                {
                    throw new NotFoundException(FormControllerApiMessage.ResponsibilityTypeRule);
                }

                model.LastRunOn = null;
                Company.Update(model);
                return jsonSuccess(string.Format(ApiMessages.SucessfullyRemoved,FormControllerApiMessage.ItemDate), id.ToString(), "edit", HttpStatusCode.OK);
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
                {
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);
                }

                var existing = Company.GetById<ResponsibilityTypeRelationRule>(model.ID);
                if (existing == null)
                {
                    throw new NotFoundException(ApiMessages.OwnershipType);
                }

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
                    throw new GenericException(HttpStatusCode.BadRequest, FormControllerApiMessage.ResponsibilityType, FormInfo.Responsibility_Then_Filter_Value_Required);
                }


                if (model.StructuredDefinition?.When?.Where(x => x.Value == null).Count() > 0)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, FormControllerApiMessage.ResponsibilityType, FormInfo.Responsibility_When_Filter_Value_Required);
                }

                if (!model.ApplyToType)
                {
                    if (model.StructuredDefinition?.When == null)
                    {
                        throw new GenericException(HttpStatusCode.BadRequest, FormControllerApiMessage.ResponsibilityType, FormInfo.Responsibility_When_Filter_Required_Based_ApplyToType_Value);
                    }
                    else if (model.StructuredDefinition?.When?.Count == 0)
                    {
                        throw new GenericException(HttpStatusCode.BadRequest, FormControllerApiMessage.ResponsibilityType, FormInfo.Responsibility_When_Filter_Value_Required);
                    }
                }

                if (model.StructuredDefinition?.When != null)
                {
                    var allowedFieldTypeIds = Company.FieldTypes.Where(x => x.Object == model.Object && x.ObjectID == model.ObjectID).Select(x => x.ID).ToList();

                    foreach (var action in model.StructuredDefinition.When)
                    {
                        // if a field check type AND its a field type not on this asset type dont allow it
                        if (action.CheckType == "F"  && !allowedFieldTypeIds.Contains(action.FieldTypeID))
                        {
                            throw new GenericException(HttpStatusCode.BadRequest, FormControllerApiMessage.ResponsibilityType, FormInfo.Responsibility_Then_InvalidFieldType);
                        }
                    }
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
                    await Company.ProcessResponsibilityRelationRules(existing.ID);
                }


                return jsonSuccess(FormControllerApiMessage.ItemUpdatedProcessed, model.ID.ToString(), "edit", HttpStatusCode.OK);
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
                {
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);
                }

                model.SetRawFromDefinition();
                if (model.StructuredDefinition?.Then?.Conditions?.Where(x => x.Value == null).Count() > 0)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, FormControllerApiMessage.ResponsibilityType, FormInfo.Responsibility_Then_Filter_Value_Required);
                }

                if (model.StructuredDefinition?.When?.Where(x => x.Value == null).Count() > 0)
                {
                    throw new GenericException(HttpStatusCode.BadRequest, FormControllerApiMessage.ResponsibilityType, FormInfo.Responsibility_When_Filter_Value_Required);
                }

                if (!model.ApplyToType)
                {
                    if (model.StructuredDefinition?.When == null)
                    {
                        throw new GenericException(HttpStatusCode.BadRequest, FormControllerApiMessage.ResponsibilityType, FormInfo.Responsibility_When_Filter_Required_Based_ApplyToType_Value);
                    }
                    else if (model.StructuredDefinition?.When?.Count == 0)
                    {
                        throw new GenericException(HttpStatusCode.BadRequest, FormControllerApiMessage.ResponsibilityType, FormInfo.Responsibility_When_Filter_Value_Required);
                    }
                }

                model.UpdatedOn = DateTime.UtcNow;
                Company.Add(model);

                // Process this rule.
                await Company.ProcessResponsibilityRelationRules(model.ID);

                return jsonSuccess(FormControllerApiMessage.ItemCreatedProcessed, model.ID.ToString(), "add", HttpStatusCode.Created);
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