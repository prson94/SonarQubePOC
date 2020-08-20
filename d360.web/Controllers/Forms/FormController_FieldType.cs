using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.exceptions;
using d360.model;
using d360.web.Filters;
using d360.web.Models;
using d360.web.Models.Attributes;
using Dapper;
using Newtonsoft.Json.Linq;
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
        #region FieldType

        #region Supporting Json Feeds

        [Route("FieldType_ListFilter"), NonNullableParameters]
        public JsonNetResult FieldType_ListFilter(SystemObjects objectType, int objectId, SystemObjects type, int id)
        {
            var predicateTypes = string.Join(",", PredicateType.DataLineage.GetAsList()
                .Where(f => f.AllowEditFromRelationshipEditor && f.AllowIntersectTypeAssignment)
                .Select(i => ((int)i.ID).ToString())
                .ToArray());

            string sql = $@"SELECT 
                        Concat(A.PredicateID, '|',A.Direction) as PredicateValue, 
                        A.PredicateName, 
                        A.ObjectName, 
                        A.[Object], 
                        A.[ObjectID], 
                        B.FieldTypeID, 
                        B.[FriendlyName],
						B.Type,
                        B.Class,
                        B.Name
                    FROM ( 
                        SELECT 
                            it.[ID] as IntersectTypeID, 
                            0 AS Direction, 
                            p.[ID] as PredicateID, 
                            p.[Name] as PredicateName, 
                            ot.[Name] as ObjectName, 
                            it.[Object] as [Object], 
                            it.[ObjectID] as [ObjectID] 
                        FROM [dbo].[IntersectType] it 
                            join [dbo].[Predicate] p on it.[PredicateID] = p.[ID] 
                            join [dbo].[AssetType] ot on ot.[Object] = it.[Object] and ot.[ObjectId] = it.[ObjectID] 
                            join [dbo].[AssetType] st on st.[Object] = it.[Subject] and st.[ObjectId] = it.[SubjectID] 
                        where it.[Subject] = @objectType 
                        and it.[SubjectID] = @objectId
                        and p.Type IN ({predicateTypes})
                        and it.[Object] in ('ArtifactType', 'TaxonomyType')
                        UNION ALL 
                        SELECT 
                            it.[ID], 
                            1 AS Direction, 
                            p.[ID] as PredicateID, 
                            p.[Inverse] as PredicateName,
                            st.[Name] as ObjectName, 
                            it.[Subject] as [Object], 
                            it.[SubjectID] as [ObjectID] 
                        FROM [dbo].[IntersectType] it 
                            join [dbo].[Predicate] p on it.[PredicateID] = p.[ID] 
                            join [dbo].[AssetType] ot on ot.[Object] = it.[Object] and ot.[ObjectId] = it.[ObjectID] 
                            join [dbo].[AssetType] st on st.[Object] = it.[Subject] and st.[ObjectId] = it.[SubjectID] 
                         where it.[Object] = @objectType 
                         and it.[ObjectID] = @objectId 
                         and p.Type IN ({predicateTypes})
                         and it.[Subject] in ('ArtifactType', 'TaxonomyType')
                        ) A LEFT OUTER JOIN
                    (SELECT 
                        ft.[ID] as FieldTypeID,
                        ft.[FriendlyName], 
                        ft.[Object], 
                        ft.[ObjectID], 
                        at.Object as LookupObject, 
                        ft.LookupObjectID,
                        ft.Type,
                        at.Class,
                        at.Name
                    FROM [dbo].[FieldType] ft
                    INNER JOIN [dbo].[AssetType] at ON ft.LookupObjectType +'Type' = at.Object AND ft.LookupObjectID = at.ObjectID
                    WHERE ft.[ObjectID] = @id AND ft.[Object] = @type  
                    ) B ON A.[Object] = B.LookupObject AND A.ObjectID = B.LookupObjectID";
            var parms = new
            {
                objectType = objectType.ToString(),
                objectId = objectId,
                type = type.ToString(),
                id = id
            };

            return new JsonNetResult
            {
                Data = Company.Query<dynamic>(sql, parms).Select(i => new
                {
                    PredicateValue = i.PredicateValue,
                    PredicateName = i.PredicateName,
                    FieldTypeID = i.FieldTypeID,
                    FriendlyName = i.FriendlyName,
                    Info = string.IsNullOrEmpty(i.Name) ? "" : "List(" + (AssetTypeClass)i.Class + " : " + i.Name + ")" //@TODO use i.Type instead of hardcoded field type
                })
            };
        }


        [Route("FieldType_Lookup_FilteredByPredicate"), NonNullableParameters]
        public JsonNetResult FieldType_Lookup_FilteredByPredicate(int fieldTypeId, string objectType, int ObjectID, string value = "", string query = "")
        {
            IntersectType it;
            DynamicParameters queryParameters = new DynamicParameters();

            var selectList = new List<SelectListInfoItem>();
            string exceptionMessage = "";
            Boolean useTypeahead = false;

            int typeaheadThreshold = 1 + int.Parse(Community.GetCompanySettings()["MaxDropdownItems"]);

            try
            {
                var filterObject = Company.Filter<Asset>(i => i.ObjectID == ObjectID && i.Object == objectType).SingleOrDefault();
                if (filterObject == null)
                {
                    exceptionMessage = "Action subject is not an asset. Filter disabled.";
                }

                var ft = Company.GetById<FieldType>(fieldTypeId);

                queryParameters.Add("fieldTypeId", ft.ID);
                queryParameters.Add("lookupObjectType", ft.LookupObjectType);
                queryParameters.Add("lookupObjectId", ft.LookupObjectID);

                string selectedValue = string.IsNullOrWhiteSpace(value) ? (string.IsNullOrWhiteSpace(ft.DefaultValue) ? "" : ft.DefaultValue) : value;
                queryParameters.Add("selectedValue", selectedValue);

                if (!string.IsNullOrWhiteSpace(query))
                {
                    //If the query is not empty, this is a typeahead query, so our threshold on results is 20
                    typeaheadThreshold = 20;
                }
                else
                {
                    //Don't include "choose.." and allow all in typeahead results
                    if (!ft.IsRequired && !ft.AllowMultipleValues)
                        selectList.Add(new SelectListInfoItem { Text = "Choose...", Value = "" });

                    if (ft.AllowAllValue)
                        selectList.Add(new SelectListInfoItem { Text = ft.AllowAllLabel, Value = "0" });
                }

                if (exceptionMessage == "")
                {
                    if (ft.FilterPredicateDirection == true)
                    {
                        it = Company.Filter<IntersectType>(i =>
                            i.Object == ft.LookupObjectType + "Type" &&
                            i.ObjectID == ft.LookupObjectID &&
                            i.Subject == filterObject.AssetType.Object &&
                            i.SubjectID == filterObject.AssetType.ObjectID &&
                            i.PredicateID == ft.FilterPredicateID
                        ).SingleOrDefault();
                    }
                    else
                    {
                        it = Company.Filter<IntersectType>(i =>
                            i.Subject == ft.LookupObjectType + "Type" &&
                            i.SubjectID == ft.LookupObjectID &&
                            i.Object == filterObject.AssetType.Object &&
                            i.ObjectID == filterObject.AssetType.ObjectID &&
                            i.PredicateID == ft.FilterPredicateID
                        ).SingleOrDefault();
                    }

                    if (it == null)
                    {
                        var lookupObjectType = Company.Filter<AssetType>(i => i.ObjectID == ft.LookupObjectID && i.Object == ft.LookupObjectType + "Type").SingleOrDefault();
                        string listObjectType = lookupObjectType.Class + ":" + lookupObjectType.Name; ;
                        Predicate pred = Company.GetById<Predicate>(ft.FilterPredicateID.GetValueOrDefault());
                        string predicate = (ft.FilterPredicateDirection == true) ? pred.Inverse : pred.Name;
                        var filterObjectDetail = Company.Filter<AssetDetail>(i => i.ObjectID == ObjectID && i.Object == objectType).SingleOrDefault();
                        string actionSubject = filterObjectDetail.DisplayValue;
                        string actionSubjectType = filterObject.AssetType.Class + ":" + filterObject.AssetType.Name;
                        exceptionMessage = $@"Filtering for this list has been disabled as we cannot filter a list of types {listObjectType} by the action subject {actionSubject}.";
                        exceptionMessage += $@" The relationship {listObjectType} - {predicate} - {actionSubjectType} does not exist.";
                    }
                    else
                    {
                        queryParameters.Add("IntersectTypeID", it.ID);
                    }
                }

                var selectedSql = $@"select TOP ({typeaheadThreshold}) V.Value, V.Text, '' as Info
                    from FieldLookupValue V 
                    where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId and V.Value = @selectedValue 
                    union
                    ";
                var columns = $@"
                    V.Value,
                    V.Text";
                var joinSql = "";
                var whereSql = "where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId ";

                if (exceptionMessage == "")
                {
                    if (ft.FilterPredicateDirection == true)
                    {
                        columns += @", concat(I.PredicateInverse,' ', I.SubjectShortName) as Info";
                    }
                    else
                    {
                        columns += @", concat(I.PredicateName,' ', I.ObjectShortName) as Info";
                    }

                    joinSql = $@" inner join [IntersectDetail] I on I.{ (ft.FilterPredicateDirection == true ? "ObjectID" : "SubjectID") } = V.Value ";
                    whereSql += $@" and I.IntersectTypeID = @IntersectTypeID and I.{(ft.FilterPredicateDirection == true ? "SubjectID" : "ObjectID")} = @ObjecctID ";

                    queryParameters.Add("ObjecctID", ObjectID);
                }
                else
                {
                    columns += @", '' as Info";

                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    whereSql += " and V.Text like '%' + @query + '%' ";
                    queryParameters.Add("query", query);
                }

                var itemsSql = $@"
                    {(string.IsNullOrWhiteSpace(selectedValue) ? "" : selectedSql)}
                    select {columns}
                    from FieldLookupValue V
                    {joinSql}
                    {whereSql}
                    ";

                var items = Company.Query<SelectListInfoItem>(itemsSql, queryParameters).ToList();

                if (items.Count() >= typeaheadThreshold)
                    useTypeahead = true;

                selectList.AddRange(items.Select(i => new SelectListInfoItem
                {
                    Text = i.Text,
                    Value = i.Value.ToString(),
                    Selected = string.IsNullOrEmpty(selectedValue) ? false : i.Value.ToString() == selectedValue,
                    Info = i.Info
                }));

            }
            catch
            {
                if (exceptionMessage == "")
                {
                    exceptionMessage = "Filter disabled. An error occured when attempting to apply it.";
                }
            }
            selectList = selectList.OrderBy(i => i.Selected ? 0 : 1).ToList();

            return new JsonNetResult
            {
                Data = new { items = selectList, exceptionMessage = exceptionMessage, useTypeahead = useTypeahead },
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [Route("Reference_Hierarchy"), NonNullableParameters]
        public JsonNetResult Reference_Hierarchy(int id, SystemObjects objectType, int objectId)
        {
            //return possible hierarchy parents for this object type
            var parent = Company.GetParentType(id, SystemObjects.ReferenceItemType);
            var list = new List<PrimeSelectItem>();

            if (parent != null)
            {
                //get possible parent reference list types defined for this object / object id they cant already be parents
                list = Company.FieldTypes.Where(x => x.Object == objectType.ToString() && x.ObjectID == objectId && x.LookupObjectType == "ReferenceItem" && x.LookupObjectID == parent.ObjectID).Select(i => new PrimeSelectItem { label = i.FriendlyName, value = i.ID.ToString() }).ToList();
                if (list.Count > 0) list.Insert(0, new PrimeSelectItem { label = "", value = "" });
            }

            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_Relationship_IsListable"), NonNullableParameters]
        public JsonNetResult FieldType_Relationship_IsListable(SystemObjects type, int id, int intersectTypeId)
        {
            bool isListable = false;
            var sType = type.ToString();

            var intersectType = Company.Filter<IntersectTypeDetail>(i => i.ID == intersectTypeId).FirstOrDefault();

            if (intersectType != null)
            {
                if (intersectType.Subject == sType && intersectType.SubjectID == id && intersectType.ObjectCardinality == Cardinality.One) isListable = true;
                else if (intersectType.Object == sType && intersectType.ObjectID == id && intersectType.SubjectCardinality == Cardinality.One) isListable = true;
            }

            return new JsonNetResult
            {
                Data = isListable,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

 
        [Route("FieldType_TypeAheadLookup"), NonNullableParameters]
        public JsonNetResult FieldType_TypeAheadLookup(int fieldTypeId, string value = "", string query = "", bool useColor = false)
        {
            var selectList = new List<SelectListItem>();
            var ft = Company.GetById<FieldType>(fieldTypeId);
            string selectedValue = string.IsNullOrWhiteSpace(value) ? (string.IsNullOrWhiteSpace(ft.DefaultValue) ? "" : ft.DefaultValue) : value;

            if (ft.AllowAllValue)
                selectList.Add(new SelectListItem { Text = ft.AllowAllLabel, Value = "0" });

            int maxItems = 20;
            var columns = $@"
                V.FieldTypeID,
                V.LookupObjectType,
                V.LookupObjectID,
                V.Value,
                {(useColor ? "colorJson.FV AS Text" : "V.Text")}";

            var colorjoin = $@"
                                        outer apply(SELECT FV = (SELECT V.Text as name, COALESCE(JSON_VALUE(ACJ.ColorJSON,'$.Value'), 'transparent') as color 
                                                    from Asset A 
                                                    outer apply dbo.GetAssetColorJsonByColor(A.Color) ACJ
													where A.Object = v.LookupObjectType and A.ObjectID = V.Value FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) 
                                        )colorJSON ";

            var selectedSql = $@"select {columns} 
                from FieldLookupValue V 
                {(useColor ? colorjoin : "")}
                where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId and V.Value = @selectedValue 
                union
                ";

            var resourceJoin = $@"
                inner join reporting.Global_resource R on R.ResourceID = V.Value and R.Email not like '%@data3sixty.com' and R.Email not like '%@infogix.com'
                ";

            var itemsSql = $@"
                {(string.IsNullOrWhiteSpace(selectedValue) ? "" : selectedSql)}
                select top {maxItems} {columns}
                from FieldLookupValue V
                {(HideData3SixtyUsers() && ft.LookupObjectType == "Resource" ? resourceJoin : "")}
                {(useColor ? colorjoin : "")}
                where V.FieldTypeID = @fieldTypeId and V.LookupObjectType = @lookupObjectType and V.lookupObjectID = @lookupObjectId {(string.IsNullOrWhiteSpace(query) ? "" : " and V.Text like '%' + @query + '%' ")}
                ";

            var items = Company.Query<FieldLookupValue>(itemsSql, new { fieldTypeId = ft.ID, lookupObjectType = ft.LookupObjectType, lookupObjectId = ft.LookupObjectID, selectedValue, query }).ToList();

            selectList.AddRange(items.Select(i => new SelectListItem { Text = i.Text, Value = i.Value.ToString(), Selected = string.IsNullOrEmpty(selectedValue) ? false : i.Value.ToString() == selectedValue }));

            selectList = selectList.OrderBy(i => i.Selected ? 0 : 1).ToList();

            return new JsonNetResult
            {
                Data = selectList,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        [Route("FieldType_TypeaheadJsonPropertyOptionsForJsonField"), NonNullableParameters]
        public JsonNetResult FieldType_TypeaheadJsonPropertyOptionsForJsonField(string fieldName, string phrase, Guid? assetTypeUid, Guid? actionTypeUid, Guid? relationshipTypeUid)
        {
            var selectList = new List<SelectListItem>();
            FieldType ft = null;
            if (assetTypeUid != null)
            {
                int atID = Company.Filter<AssetType>(x => x.uid == assetTypeUid).SingleOrDefault().ID;
                ft = Company.Filter<FieldType>(x => x.AssetTypeID == atID && x.Name == fieldName).SingleOrDefault();
            }
            else if (actionTypeUid != null)
            {
                int atID = Company.Filter<IssueType>(x => x.uid == actionTypeUid).SingleOrDefault().ID;
                ft = Company.Filter<FieldType>(x => x.AssetTypeID == atID && x.Name == fieldName).SingleOrDefault();
            }
            else if (relationshipTypeUid != null)
            {
                var itID = Company.Filter<IntersectType>(i => i.uid == relationshipTypeUid).SingleOrDefault().ID;
                ft = Company.Filter<FieldType>(x => x.AssetTypeID == itID && x.Name == fieldName).SingleOrDefault();
            }
            else
            {
                throw new Exception("No assetTypeUid or actionTypeUid or relationshipTypeUid provided");
            }
            phrase = phrase.Replace("[", @"\[");
            var sql = $@"
select		P.[Path]
from		FieldJsonProperty P
			inner join Field F on F.ID = P.FieldID and F.FieldTypeID = @fieldTypeId and P.[Path] like @phrase+'%' escape '\'
group by	P.[Path]
order by	P.[Path]
offset 0 rows fetch next 25 rows only
                ";

            var items = Company.Query<string>(sql, new { fieldTypeId = ft.ID, phrase }).ToList();

            return new JsonNetResult
            {
                Data = items,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }

        #endregion

        #region Form Get/Post

        private void CheckIsFieldTypeNameReserved(string name)
        {
            var nameUpper = name.ToUpper();

            if (nameUpper == "PARENTID" || nameUpper == "DATABASE" || nameUpper == "COLOR" || nameUpper == "ICON") throw new Exception("Use of a field type with the name " + name + " is prohibited.");
        }

       
        [HttpGet, ActionName("FieldType"), Route("FieldType"), NonNullableParameters]
        public JsonNetResult GetFieldType(int id)
        {
            var a = Company.GetById<FieldType>(id);
            if (a == null) return null;
            var used = Company.Any<Field>(i => i.FieldTypeID == id);

            if (new[] { "Text" }.Contains(a.Type))
            {
                if (!string.IsNullOrEmpty(a.Pattern)) a.MinimumLength = 0;
            }
            else if (!new[] { "Number", "Decimal" }.Contains(a.Type))
            {
                if (!a.IsRequired) a.MinimumLength = 0;
            }

            var model = new FieldTypeEditorModel
            {
                FieldIsUsed = used,
                FieldType = a
            };
            return new JsonNetResult
            {
                Data = model,
                Formatting = Newtonsoft.Json.Formatting.None
            };

        }
        #endregion

        #endregion

    }
}