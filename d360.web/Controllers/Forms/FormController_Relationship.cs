using d360.core;
using d360.core.entities;
using d360.core.entities.Views;
using d360.core.enums;
using d360.core.exceptions;
using d360.core.queue;
using d360.extensions;
using d360.extensions.powerbi;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using Newtonsoft.Json.Linq;
using Resources;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Data.Entity;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Configuration;
using d360.core.helpers;
using System.Text;
using d360.core.resources;


namespace d360.web.Controllers
{
    public partial class FormController : BaseController
    {

        #region Predicate

        #region Field Generation

        [Route("Predicate_AddFields")]
        public JsonResult Predicate_AddFields()
        {
            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var lineageVersion = Community.GetCompanySettingByKey<int>("LineageVersion");

            var functionalTypes = PredicateType.DataLineage.GetAsList()
                .Where(f => f.AllowEditFromRelationshipEditor && f.AllowIntersectTypeAssignment && f.LineageVersionsSupported.Contains(lineageVersion))
                .Select(i => new SelectListItem { Value = ((int)i.ID).ToString(), Text = i.Name })
                .ToList();

            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Inverse", Name = "Inverse", FieldType = DataType.Text.ToString(), Validations = checkAndAddValidation("Text", "Inverse", true, "", 1, 250) });
            list.Add(new EditableField { Row = 2, Column = 1, Required = true, FieldName = "Type", Name = "Functional Type", FieldType = DataType.Lookup.ToString(), Items = functionalTypes });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        /// <param name="id">PredicateID</param>
        [Route("Predicate_EditFields"), NonNullableParameters]
        public JsonResult Predicate_EditFields(int id)
        {
            var list = new List<EditableField>();
            var a = Company.GetById<Predicate>(id);
            var any = Company.Any<IntersectType>(i => i.PredicateID == id);

            var lineageVersion = Community.GetCompanySettingByKey<int>("LineageVersion");

            var functionalTypes = PredicateType.DataLineage.GetAsList()
                .Where(f => f.AllowEditFromRelationshipEditor && f.AllowIntersectTypeAssignment && f.LineageVersionsSupported.Contains(lineageVersion))
                .Select(i => new SelectListItem { Value = ((int)i.ID).ToString(), Text = i.Name })
                .ToList();

            if (!Company.CurrentResourceIsAdmin)
                return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = a.ID.ToString() });
            list.Add(new EditableField { Row = 1, Column = 1, Required = true, FieldName = "Name", Name = "Name", FieldType = DataType.Text.ToString(), Value = a.Name, Validations = checkAndAddValidation("Text", "Name", true, "", 1, 250) });
            list.Add(new EditableField { Row = 1, Column = 2, Required = true, FieldName = "Inverse", Name = "Inverse", FieldType = DataType.Text.ToString(), Value = a.Inverse, Validations = checkAndAddValidation("Text", "Inverse", true, "", 1, 250) });
            list.Add(new EditableField { ReadOnly = any, Row = 2, Column = 1, Required = true, FieldName = "Type", Name = "Functional Type", FieldType = DataType.Lookup.ToString(), Value = ((int)a.Type).ToString(), Items = functionalTypes });

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #endregion

        #region Relationship

        #region Field Generation

        /// <param name="it">IntersectTypeID</param>
        /// <param name="type">Object</param>
        /// <param name="id">ObjectID</param>
        [Route("Relationship_AddFields"), NonNullableParameters]
        public JsonResult Relationship_AddFields(int it, SystemObjects type, int id)
        {
            if (!Company.HasAssetPermission(type, id, Permission.ModifyRelationships))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();

            var relationshipType = Company.GetById<IntersectType>(it, i => i.Predicate);
            var obj = Company.GetObjectDetail(type.ToString(), id);

            if (obj == null || relationshipType == null)
            {
                return jsonException("Invalid relationship type or source item.", HttpStatusCode.NotFound);
            }


            var targetCardinality = Cardinality.Many;
            if (relationshipType.Subject == obj.Type && relationshipType.SubjectID == obj.TypeID)
            {
                targetCardinality = relationshipType.ObjectCardinality;
            }
            else
            {
                targetCardinality = relationshipType.SubjectCardinality;
            }

            list.Add(new EditableField { FieldName = "IntersectTypeID", FieldType = DataType.Hidden.ToString(), Value = it.ToString() });
            list.Add(new EditableField { FieldName = "Source", FieldType = DataType.Hidden.ToString(), Value = type.ToString() });
            list.Add(new EditableField { FieldName = "SourceID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });

            list.Add(new EditableField
            {
                Row = 1,
                Column = 1,
                Required = true,
                FieldName = "Items",
                Name = "What Items Are You Relating?",
                MultiSelect = (targetCardinality == Cardinality.Many),
                FieldType = DataType.DataTableSelect.ToString(),
                TypeaheadUri = $"/form/Relationship_DataTable?intersectTypeId={it}&type={type}&objectId={id}"
            });

            list = loadDynamicFields(list, Company.GetFieldTypesByObject(SystemObjects.IntersectType, it).ToList(), 2);

            return Json(list, JsonRequestBehavior.AllowGet);
        }


        /// <param name="id">RelationshipID</param>
        [Route("Relationship_EditFields"), NonNullableParameters]
        public JsonResult Relationship_EditFields(int id)
        {
            var relationship = Company.GetById<Intersect>(id, i => i.IntersectType);
            if (relationship == null) return jsonException("Relationship not found.", HttpStatusCode.NotFound);

            if (!Company.HasAssetPermission(relationship.Subject, relationship.SubjectID, Permission.ModifyRelationships) &&
                !Company.HasAssetPermission(relationship.Object, relationship.ObjectID, Permission.ModifyRelationships))
                return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

            var list = new List<EditableField>();
            list.Add(new EditableField { FieldName = "ID", FieldType = DataType.Hidden.ToString(), Value = id.ToString() });
            list = loadDynamicFields(SystemObjects.Intersect.ToString(), id, list, Company.GetFieldTypesByObject(SystemObjects.IntersectType, relationship.IntersectTypeID).ToList(), Company.GetFieldRelationsByObject(SystemObjects.Intersect, relationship.ID).ToList(), 1);

            return Json(list, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddRelationship")]
        public JsonResult AddRelationship(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("relationship");

                var source = parseTextField(form, "Source");
                var sourceID = parseIntField(form, "SourceID");
                int typeID = parseIntField(form, "IntersectTypeID");
                var relationshipType = Company.GetById<IntersectType>(typeID, p => p.Predicate);
                var sourceObject = Company.GetObjectDetail(source, sourceID);

                if (!Company.HasAssetPermission(source, sourceID, Permission.ModifyRelationships))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (relationshipType == null) throw new NotFoundException("relationship");

                var predicateTypeInfo = relationshipType.Predicate.Type.AsInfoModel();

                if (!predicateTypeInfo.AllowEditFromRelationshipEditor)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);


                var targetCardinality = Cardinality.Many;
                if (relationshipType.Subject == sourceObject.Type && relationshipType.SubjectID == sourceObject.TypeID)
                {
                    targetCardinality = relationshipType.ObjectCardinality;
                }
                else
                {
                    targetCardinality = relationshipType.SubjectCardinality;
                }


                var rawItems = parseTextField(form, "Items");
                if (string.IsNullOrEmpty(rawItems))
                    return jsonException("No selected items", HttpStatusCode.BadRequest);

                var items = rawItems.Split(',').ToList();

                if ((targetCardinality == Cardinality.One && items.Count > 1))
                    return jsonException("Invalid relationship cardinality for multiple items.", HttpStatusCode.BadRequest);

                List<Asset> assetToAddIntersect = new List<Asset>();

                items.ForEach(item =>
                {
                    Guid uid = Guid.Parse(item);
                    var asset = Company.Assets.FirstOrDefault(x => x.uid == uid);
                    assetToAddIntersect.Add(asset);
                });

                if (assetToAddIntersect.Any(x => x.Object == source && x.ObjectID == sourceID))
                {
                    return jsonException("The item cannot be related to itself.", HttpStatusCode.BadRequest);
                }

                foreach (var asset in assetToAddIntersect)
                {
                    if (asset != null)
                    {

                        var intersect = Company.AddIntersect(typeID,
                            source, sourceID,
                            asset.Object, asset.ObjectID
                        );

                        var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Intersect, intersect.ID, Company.GetFieldTypesByObject(SystemObjects.IntersectType, typeID).ToList(), form, Server);
                        Company.AddOrUpdateFields(fields);
                    }
                }
                var name = Company.GetIntersectTypeName(relationshipType);

                return jsonSuccess(name + " successfully created.", "0", "add", HttpStatusCode.Created, new { ObjectType = SystemObjects.Intersect.ToString(), ObjectID = 0 });
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

        [HttpPut, ValidateInput(false), Route("EditRelationship")]
        public JsonResult EditRelationship(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException("relationship");

                int id = parseIntField(form, "ID");
                var intersect = Company.GetById<Intersect>(id);

                if (intersect == null) throw new NotFoundException("relationship");

                var intersectType = Company.GetById<IntersectType>(intersect.IntersectTypeID, p => p.Predicate);
                var predicateTypeInfo = intersectType.Predicate.Type.AsInfoModel();

                if (!predicateTypeInfo.AllowEditFromRelationshipEditor)
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                if (!Company.HasAssetPermission(intersect.Subject, intersect.SubjectID, Permission.ModifyRelationships) &&
                    !Company.HasAssetPermission(intersect.Object, intersect.ObjectID, Permission.ModifyRelationships))
                    return jsonException(FormInfo.Permisions_Error_Add, HttpStatusCode.Forbidden);

                Company.Update(intersect);
                var fields = new FieldLoader().GetFormDynamicFieldValues(SystemObjects.Intersect, intersect.ID, Company.GetFieldTypesByObject(SystemObjects.IntersectType, intersect.IntersectTypeID).ToList(), form, Server, false);
                Company.AddOrUpdateFields(fields);

                return jsonSuccess("Relationship successfully updated.", intersect.ID.ToString(), "add", HttpStatusCode.Created, new { ObjectType = SystemObjects.Intersect.ToString(), ObjectID = intersect.ID });
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

        #region DataTable Select Source

        [HttpGet, Route("Relationship_DataTable"), NonNullableParameters]
        public JsonResult Relationship_DataTable(int intersectTypeId, SystemObjects type, int objectId)
        {
            var relationshipType = Company.GetById<IntersectType>(intersectTypeId, i => i.Predicate);

            int objectTypeID = -1;
            string parentType = string.Empty;

            #region Resolve Type

            if (type == SystemObjects.FusionAttribute)
            {
                objectTypeID = Company.FusionAttributes.Where(x => x.ID == objectId).Single().FusionAttributeTypeID;
                parentType = "FusionAttributeType";
            }
            else
            {
                var obj = Company.GetObjectDetail(type.ToString(), objectId);
                objectTypeID = obj.TypeID;
                parentType = obj.Type;
            }

            if (objectTypeID <= 0 || string.IsNullOrEmpty(parentType) || relationshipType == null)
            {
                return jsonException("Invalid relationship type or source item.", HttpStatusCode.NotFound);
            }

            if (type == SystemObjects.ReferenceItemType)
            {
                objectTypeID = 0;
            }

            var targetType = "";
            var targetTypeID = 0;
            var IntersectDirectionSql = "";
            var IntersectCardinalitySql = "";
            var IntersectTypeDirectionSql = "";
            if (relationshipType.Subject == parentType && relationshipType.SubjectID == objectTypeID)
            {
                targetType = relationshipType.Object;
                targetTypeID = relationshipType.ObjectID;
                IntersectDirectionSql = "and I.Subject = @source and I.SubjectID = @id and I.Object = A.[Object] and I.ObjectID = A.ObjectID ";
                IntersectCardinalitySql = "and not exists (select ID from [Intersect] where IntersectTypeID = @it and IT.SubjectCardinality = 1 and Object = A.[Object] and ObjectID = A.ObjectID) ";
                IntersectTypeDirectionSql = " and IT.Object = T.Object and IT.ObjectID = T.ObjectID ";
            }
            else
            {
                targetType = relationshipType.Subject;
                targetTypeID = relationshipType.SubjectID;
                IntersectDirectionSql = "and I.Subject = A.[Object] and I.SubjectID = A.ObjectID and I.Object = @source and I.ObjectID = @id ";
                IntersectCardinalitySql = "and not exists (select ID from [Intersect] where IntersectTypeID = @it and IT.ObjectCardinality = 1 and Subject = A.[Object] and SubjectID = A.ObjectID) ";
                IntersectTypeDirectionSql = " and IT.Subject = T.Object and IT.SubjectID = T.ObjectID ";
            }

            if (relationshipType.Subject == relationshipType.Object && relationshipType.SubjectID == relationshipType.ObjectID)
            {
                IntersectDirectionSql = @"and (  ( (I.Subject = @source and I.SubjectID = @id) AND(I.Object = A.[Object] and I.ObjectID = A.ObjectID) ) OR
                                          ( (I.Subject = A.[Object] and I.SubjectID = A.ObjectID) AND(I.Object = @source and I.ObjectID = @id) )   )";
            }

            var targetAssetType = Company.Filter<AssetType>(i => i.Object == targetType && i.ObjectID == targetTypeID).SingleOrDefault();
            if (targetAssetType == null) throw new NotFoundException("target asset type");

            #endregion

            #region sql

            var sql = "";

            var PermissionJoins = "";
            if (!Company.CurrentResourceIsAdmin)
            {
                PermissionJoins = @" and exists (select 1 from UserAssetPermissions(@userId,@targetAssetTypeId) P where P.PermissionsBitMask & 1024 = 1024 and P.AssetTypeID = A.AssetTypeID and (P.AssetID = A.ID or P.AssetID = 0)) ";
            }

            var subSql = $@"(
select		A.ID,
            A.[Object],
            A.ObjectID,
            A.Uid
from		Asset A
            inner join AssetType T on A.AssetTypeID = T.ID
            inner join IntersectType IT on IT.ID = @it {IntersectTypeDirectionSql}
			left join [Intersect] I on	I.IntersectTypeID = IT.ID {IntersectDirectionSql}
where		I.ID is null 
            and A.[State] = 1 
            and T.ObjectID = @targetTypeID 
            and T.[Object] = @targetType 
            and A.ObjectID != @id {IntersectCardinalitySql} {PermissionJoins}
) C";

            switch (targetType)
            {
                case "FusionAttributeType":
                    #region
                    if ((relationshipType.Predicate != null) && (relationshipType.Predicate.Type == PredicateType.FusionMapping))
                    {
                        sql = $@"
select	'FusionAttribute' as [Object], 
        FA.ID as ObjectID, 
        A.uid,
        F.Name + '.' + FA.TextPath as Name
from	FusionAttribute FA with(nolock)
		inner join Fusion F with(nolock) on F.ID = FA.FusionID and FA.FusionAttributeTypeID = @targetTypeID and FA.Deleted = 0
        inner join Asset A on A.Object = 'FusionAttribute' and A.ObjectId = FA.ID 
where	FA.ID not in (
					select	1 
					from	[IntersectDetail]
					where	( (Subject = @source and SubjectID = @id) AND (ObjectType = @targetType and ObjectTypeID = @targetTypeID) )
					)
        {PermissionJoins} 
        and FA.ID != @id 
order by F.Name, FA.TextPath";
                    }
                    else
                    {
                        sql = $@"
declare @OwnerSourceType varchar(50)
declare @owners table (ID int)


IF @source = 'Intersect'
BEGIN
	set @OwnerSourceType = 'Artifact'

	insert into @owners
		select	A.ID
		from	[IntersectDetail] N
				inner join Asset A with(nolock) on N.[Subject] = 'Artifact' and A.objectID = N.SubjectID and N.ID = @id
				inner join AssetType [AT] with(nolock) on [AT].ID = A.AssetTypeID and [AT].CanOwnFusion = 1
	insert into @owners
		select	A.ID
		from	[IntersectDetail] N
				inner join Asset A with(nolock) on N.[Object] = 'Artifact' and A.ObjectID = N.ObjectID and N.ID = @id
				inner join AssetType [AT] with(nolock) on [AT].ID = A.AssetTypeID and [AT].CanOwnFusion = 1
END
ELSE
BEGIN
	set @OwnerSourceType = @source
	insert into @owners 
	Select ID from Asset where [object]=@OwnerSourceType and [objectId]=@id
END

declare @h table (ID int);

if @OwnerSourceType = 'Artifact'
	begin
		with h as	(
					select	A.ID,
							PA.ID as ParentID
					from	Asset A with(nolock)
							inner join @owners O on O.ID = A.ID
							cross apply [dbo].[GetParentByAssetID] (A.ID) as PA
					union all
					select	AA.ID,
							PA.ID as ParentID
					from	Asset AA with(nolock)
							cross apply [dbo].[GetParentByAssetID] (AA.ID) as PA
							inner join h as C on C.ParentID =AA.ID
					)
		insert into @h
			select ID from h;
	end

		insert into @h values (@id)
        insert into @h select id from @Owners


select distinct 'FusionAttribute' as [Object], 
        FA.ID as ObjectID, 
        A.uid,
        F.Name + '.' + FA.TextPath as Name
from	FusionAttribute FA with(nolock)
		inner join Fusion F with(nolock) on F.ID = FA.FusionID and FA.FusionAttributeTypeID = @targetTypeID and FA.Deleted = 0
        inner join FusionOwner FO on FO.FusionID = FA.FusionID
        inner join @h H on H.ID = FO.ASSETID
        inner join Asset A on A.Object = 'FusionAttribute' and A.ObjectID = FA.ID 
where	FA.ID not in (
					select	1 
					from	[IntersectDetail]
					where	IntersectTypeID = @it and ( (Subject = @source and SubjectID = @id) AND (ObjectType = @targetType and ObjectTypeID = @targetTypeID) )
					)
        and FA.ID != @id 
        {PermissionJoins} 
order by 3";
                    }
                    break;
                #endregion
                case "FusionQueryAttributeType":
                    #region                    
                    sql = $@"
select	'FusionQueryAttribute' as [Object], 
        FA.ID as ObjectID, 
        A.uid,
        F.Name + '.' + FA.DisplayValue as Name
from	FusionQueryAttribute FA with(nolock)
        inner join FusionQueryAttributeType FAT on (FA.FusionQueryAttributeTypeID = FAT.ID)
		inner join Fusion F with(nolock) on F.ID = FAT.FusionID and FA.FusionQueryAttributeTypeID = @targetTypeID and FA.Deleted = 0
        inner join Asset A on A.object = 'FusionQueryAttribute' and A.ObjectID =  FA.ID
where	FA.ID not in (
					select	1 
					from	[IntersectDetail]
					where	( (Subject = @source and SubjectID = @id) AND (ObjectType = @targetType and ObjectTypeID = @targetTypeID) )
					)
        and FA.ID != @id 
order by F.Name, FA.DisplayValue";
                    break;
                #endregion
                case "Group":
                case "GroupType":
                    #region
                    sql = $@"
select	'Group' as [Object], 
        D.ID as ObjectID, 
		A.uid,
        D.Name
from	[Group] D with(nolock)
inner join Asset A on A.Object = 'Group' and A.ObjectID= D.ID
where	D.ID not in (
					select	case 
                                when SubjectType = 'Group' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'Group') ) OR
							 ( (SubjectType = 'Group') AND (Object = @source and ObjectID = @id) )
							)
					)
        and D.ID != @id 
order by D.Name";
                    break;
                #endregion
                case "Resource":
                case "ResourceType":
                    #region
                    sql = $@"
                SELECT
  'Resource' AS [Object],
D.ResourceID AS ObjectID,
  D.LastName + ', ' + D.FirstName AS Name,
  A.uid
FROM reporting.Global_Resource D WITH (NOLOCK)
INNER JOIN Asset A
  ON A.Object = 'Resource'
  AND A.ObjectID = D.ResourceID
WHERE D.ResourceID NOT IN (SELECT
  CASE
    WHEN SubjectType = 'ResourceType' THEN SubjectID
    ELSE ObjectID
  END
FROM [IntersectDetail]
WHERE IntersectTypeID = @it
AND ((Subject = @source
AND SubjectID = @id)
AND (ObjectType = 'Resource'))
union all
SELECT
  CASE
    WHEN SubjectType = 'ResourceType' THEN SubjectID
    ELSE ObjectID
  END
FROM [IntersectDetail]
WHERE IntersectTypeID = @it
and ((SubjectType = 'Resource')
AND (Object = @source
AND ObjectID = @id))
)
AND D.ResourceID != @id
ORDER BY D.LastName, D.FirstName";
                    break;
                #endregion
                case "ReferenceItemType":
                    #region
                    if (targetTypeID == 0)
                    {
                        sql = $@"
select	'ReferenceItemType' as [Object], 
        r.ObjectID as ObjectID, 
        r.uid,
        r.Name as Name
from	AssetType r with(nolock)
where   r.[objectId] not in (
					select	case 
                                when SubjectType = 'ReferenceItemType' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'ReferenceItemType') ) OR
							 ( (SubjectType = 'ReferenceItemType') AND (Object = @source and ObjectID = @id) )
							)
					)
        and r.[ObjectId] != @id
        and r.[Object]='ReferenceItemType'
order by r.Name";
                    }
                    else
                    {
                        sql = $@"select C.uid, C.Object, D.DisplayValue as Name from {subSql} inner join AssetDisplayValue D on D.AssetID =  C.ID order by D.DisplayValue";
                    }
                    break;
                #endregion
                case "RuleImplementationType":
                    #region
                    if (targetTypeID == 0)
                    {
                        sql = $@"
select	'RuleImplementation' as [Object], 
        r.ID as ObjectID, 
        coalesce(r.Name, 'Implementation ' + cast(r.ID as varchar)) as Name
from	RuleImplementation r with(nolock)
where   r.ID not in (
					select	case 
                                when SubjectType = 'RuleImplementation' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'RuleImplementation') ) OR
							 ( (SubjectType = 'RuleImplementation') AND (Object = @source and ObjectID = @id) )
							)
					)
        and r.ID != @id
order by r.Name";
                    }
                    else
                    {
                        sql = $@"
select	'RuleImplementation' as [Object], 
        r.ID as ObjectID, 
        coalesce(r.Name, 'Implementation ' + cast(r.ID as varchar)) as Name
from	RuleImplementation r with(nolock)
where   r.ID not in (
					select	case 
                                when SubjectType = 'RuleImplementation' then SubjectID
                                else ObjectID
                            end
					from	[IntersectDetail]
					where	IntersectTypeID = @it and (
							 ( (Subject = @source and SubjectID = @id) AND (ObjectType = 'RuleImplementation' and ObjectTypeID = r.RuleID) ) OR
							 ( (SubjectType = 'RuleImplementation' and SubjectTypeID = r.RuleID) AND (Object = @source and ObjectID = @id) )
							)
					)
        and r.ID != @id
        and r.RuleID = @targetTypeID 
order by r.Name";
                    }
                    break;
                #endregion
                case "ArtifactType":
                case "LookupType":
                case "RuleType":
                    sql = $@"select C.uid, C.Object, D.DisplayValue as Name from {subSql} inner join AssetDisplayValue D on D.AssetID =  C.ID order by D.DisplayValue";
                    break;
                case "PolicyType":
                case "TaxonomyType":
                    sql = $@"select	c.uid, TP.TextPath as Name, c.Object from {subSql} cross apply dbo.GetAssetTextPathById(C.ID, '/') TP order by TP.TextPath";
                    break;
            }

            #endregion

            var items = Company.Query<dynamic>(sql, new { targetAssetTypeId = targetAssetType.ID, targetType, targetTypeID, source = type.ToString(), id = objectId, it = intersectTypeId, userId = Company.CurrentResourceID }).Select(i => new { Text = i.Name, Value = $"{i.uid}", ObjectType = i.Object }).ToList();

            return Json(items, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #endregion

        #region Intersect/Other Relationships

        [HttpDelete, Route("DeleteIntersect"), NonNullableParameters]
        public JsonResult DeleteIntersect(int id)
        {
            try
            {
                var intersect = Company.GetById<Intersect>(id);

                if (intersect == null)
                    throw new NotFoundException("relationship");

                if (
                    !Company.HasAssetPermission(intersect.Subject, intersect.SubjectID, Permission.DeleteRelationships) ||
                    !Company.HasAssetPermission(intersect.Object, intersect.ObjectID, Permission.DeleteRelationships)
                    )
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                Company.DeleteRelationship(id);
                Response.StatusCode = (int)HttpStatusCode.OK;
                Response.StatusDescription = "Successfully unrelated item.";
                return Json(new { message = Response.StatusDescription });
            }
            catch (BaseException ex)
            {
                Response.StatusCode = (int)ex.StatusCode;
                Response.StatusDescription = ex.StatusDescription;
                return Json(new { message = Response.StatusDescription });
            }
            catch (Exception ex)
            {
                SendException(ex);
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                Response.StatusDescription = ex.Message;
                return Json(new { message = Response.StatusDescription });
            }
        }

        #endregion

        #region IntersectType

        #region Json Feeds To Support Editing

        [Route("IntersectType_FormData"), NonNullableParameters]
        public JsonNetResult IntersectType_FormData(int id)
        {
            try
            {
                var type = Company.GetById<IntersectType>(id);
                if (type == null) return new JsonNetResult { Data = null };

                var currentIntersects = Company.Filter<Intersect>(i => i.IntersectTypeID == id).Any();

                var model = new Dictionary<string, object> {
                    { "ID", id },
                    { "LimitedChangesOnly", currentIntersects },
                    { "Subject", $"{type.Subject}|{type.SubjectID}" },
                    { "SubjectCardinality", $"{(int)type.SubjectCardinality}" },
                    { "Object", $"{type.Object}|{type.ObjectID}" },
                    { "ObjectCardinality", $"{(int)type.ObjectCardinality}" },
                    { "Predicate", type.PredicateID }
                };

                return new JsonNetResult { Data = model, Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        [Route("IntersectType_PredicateOptions"), NonNullableParameters]
        public JsonNetResult IntersectType_PredicateOptions(SystemObjects subject, int subjectID, SystemObjects? @object = null, int? objectID = null, int? predicateID = null)
        {
            try
            {
                var lineageVersion = Community.GetCompanySettingByKey<int>("LineageVersion");
                var models = Company.GetPredicateOptions(lineageVersion, subject, subjectID, @object, objectID, predicateID)
                    .Select(i => new { title = $"{i.Name} / {i.Inverse} ({i.Type.AsInfoModel().Name})", value = i.ID })
                    .OrderBy(i => i.title);

                return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        [Route("IntersectType_CardinalityOptions")]
        public JsonNetResult IntersectType_CardinalityOptions()
        {
            var models = Cardinality.One.GetList()
                .Select(i => new { title = i.Name, value = i.ID });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("IntersectType_SubjectOptions")]
        public JsonNetResult IntersectType_SubjectOptions()
        {
            var models = Company.GetIntersectTypeOptions()
                .Select(i => new { title = i.Name, value = i.Type + "|" + i.ID });

            return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
        }

        [Route("IntersectType_ObjectOptions"), NonNullableParameters]
        public JsonNetResult IntersectType_ObjectOptions(SystemObjects type, int id, SystemObjects? side2Type = null, int? side2ID = null, int? predicateID = null)
        {
            try
            {
                List<AssetTypeClass> classLimits = null;

                if (predicateID.HasValue)
                {
                    var predicate = Company.GetById<Predicate>(predicateID.Value);
                    if (predicate != null)
                    {
                        switch (predicate.Type)
                        {
                            case PredicateType.BusinessToTechnical:
                                classLimits = new List<AssetTypeClass>() { AssetTypeClass.TechnicalAsset };
                                break;
                            case PredicateType.FusionMapping:
                                classLimits = new List<AssetTypeClass>() { AssetTypeClass.FusionAttribute };
                                break;
                        }
                    }
                }

                var models = Company.GetIntersectTypeOptions(type, id, side2Type, side2ID, predicateID, classLimits)
                    .Where(i => i.Type != "IntersectType")
                    .Select(i => new { title = i.Name, value = i.Type + "|" + i.ID });

                return new JsonNetResult { Data = models, Formatting = Newtonsoft.Json.Formatting.None };
            }
            catch (Exception ex)
            {
                return jsonNetException(ex);
            }
        }

        #endregion

        #region Form Get/Post

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddIntersectType")]
        public JsonResult AddIntersectType(FormCollection form)
        {
            try
            {
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (form == null) throw new NoFormDataException("relationship type");

                var subject = form["Subject"];
                var subjectInfo = subject.Split('|');
                var @object = form["Object"];
                var objectInfo = @object.Split('|');
                var predicate = form["Predicate"];

                if (string.IsNullOrEmpty(predicate))
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "Please select a predicate for this relationship.");
                }

                var model = new IntersectType
                {
                    Subject = subjectInfo[0],
                    SubjectCardinality = parseEnumField<Cardinality>(form, "SubjectCardinality"),
                    SubjectID = int.Parse(subjectInfo[1]),
                    Object = objectInfo[0],
                    ObjectCardinality = parseEnumField<Cardinality>(form, "ObjectCardinality"),
                    ObjectID = int.Parse(objectInfo[1]),
                    IsSystem = false,
                    PredicateID = int.Parse(predicate)
                };

                var lineageVersion = Community.GetCompanySettingByKey<int>("LineageVersion");

                Company.UpsertIntersectType(model, lineageVersion);
                var id = model.ID;

                return jsonSuccess("Relationship type successfully created.", id.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteIntersectType")]
        public JsonResult DeleteIntersectType(FormCollection form)
        {
            try
            {
                var id = parseIntField(form, "ID");
                if (!form.HasKeys()) throw new NoFormDataException("relationship type");

                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (Company.Filter<Intersect>(i => i.IntersectTypeID == id).Count() > 0)
                    return jsonException(FormInfo.InUse_Error_Delete, HttpStatusCode.Conflict);
                if (Company.Filter<FieldType>(i => i.LookupObjectID == id && i.Type == "Relationship" && i.LookupObjectType == "IntersectType").Count() > 0)
                    return jsonException(FormInfo.InUse_RelationShipType_Error_Delete, HttpStatusCode.Conflict);

                var model = Company.GetById<IntersectType>(id);
                if (model == null) throw new NotFoundException("relationship type");

                Company.Delete(SystemObjects.IntersectType, id);

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

        [HttpPut, ValidateInput(false), Route("EditIntersectType")]
        public JsonResult EditIntersectType(FormCollection form)
        {
            try
            {
                if (form == null) throw new NoFormDataException("relationship type");

                var id = int.Parse(form["ID"]);

                // Permissions validation.
                if (!Company.CurrentResourceIsAdmin)
                    return jsonException(FormInfo.Permisions_Error_Edit, HttpStatusCode.Forbidden);

                var model = Company.GetById<IntersectType>(id);
                if (model == null) throw new NotFoundException("relationship type");


                var subject = form["Subject"];
                var subjectInfo = subject.Split('|');

                var @object = form["Object"];
                var objectInfo = @object.Split('|');

                var predicate = form["Predicate"];

                if (string.IsNullOrEmpty(predicate))
                {
                    throw new GenericException(HttpStatusCode.Conflict, "Predicate", "Please select a predicate for this relationship.");
                }

                model.Subject = subjectInfo[0];
                model.SubjectCardinality = parseEnumField<Cardinality>(form, "SubjectCardinality");
                model.SubjectID = int.Parse(subjectInfo[1]);
                model.Object = objectInfo[0];
                model.ObjectCardinality = parseEnumField<Cardinality>(form, "ObjectCardinality");
                model.ObjectID = int.Parse(objectInfo[1]);
                model.PredicateID = int.Parse(predicate);

                var lineageVersion = Community.GetCompanySettingByKey<int>("LineageVersion");

                Company.UpsertIntersectType(model, lineageVersion);

                return jsonSuccess("Relationship type  successfully updated.", model.ID.ToString(), "edit", HttpStatusCode.OK);
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