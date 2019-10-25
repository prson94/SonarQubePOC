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

        /// <summary>
        /// Used to get the child types of a specific parent type.
        /// </summary>
        /// <param name="type">The Type></param>
        /// <param name="id">The Type ID></param>
        /// <returns>A list of child realtionship types</returns>
        [Route("FieldType_ComplexLookup_ChildItems"), NonNullableParameters]
        public JsonNetResult FieldType_ComplexLookup_ChildItems(SystemObjects type, int id)
        {
            dynamic list = null;

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    list = Company.GetChildTypes(id, SystemObjects.ArtifactType)
                        .ToList()
                        .Select(i => new { value = $"0|ArtifactType|{i.ObjectID}|0", title = i.Name })
                        .ToList();
                    break;
                case SystemObjects.FusionAttributeType:
                    list = Company.GetChildTypes(id, SystemObjects.FusionAttributeType)
                        .ToList()
                        .Select(i => new { value = $"0|FusionAttributeType|{i.ObjectID}|0", title = i.Name })
                        .ToList();
                    break;
            }

            return new JsonNetResult
            {
                Data = list ?? new List<dynamic>(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Used to get the parent types of a specific child type.
        /// </summary>
        /// <param name="type">The Type></param>
        /// <param name="id">The Type ID></param>
        /// <returns>A list of child realtionship types</returns>
        [Route("FieldType_ComplexLookup_ParentItems"), NonNullableParameters]
        public JsonNetResult FieldType_ComplexLookup_ParentItems(SystemObjects type, int id)
        {
            dynamic list = null;
            BaseIntObject parent;

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    list = new List<AssetType>();
                    parent = Company.GetParentType(id, SystemObjects.ArtifactType);
                    if (parent != null)
                        list.Add((AssetType)parent);

                    list = ((List<AssetType>)list).Select(i => new { value = $"0|ArtifactType|{i.ObjectID}", title = i.Name })
                    .Where(i => i.title != null)
                    .ToList();
                    break;
                case SystemObjects.FusionAttributeType:
                    list = new List<AssetType>();
                    parent = Company.GetParentType(id, SystemObjects.FusionAttributeType);
                    if (parent != null)
                        list.Add((AssetType)parent);

                    list = ((List<AssetType>)list).Select(i => new { value = $"0|FusionAttributeType|{i.ObjectID}", title = i.Name })
                        .Where(i => i.title != null)
                        .ToList();
                    break;
            }

            return new JsonNetResult
            {
                Data = list ?? new List<dynamic>(),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Used for complex lookup
        /// </summary>
        /// <param name="type">The Type></param>
        /// <param name="id">The Type ID></param>
        /// <returns>A list of relationship types</returns>
        [Route("FieldType_ComplexLookup_IntersectTypes"), NonNullableParameters]
        public JsonNetResult FieldType_ComplexLookup_IntersectTypes(SystemObjects type, int id)
        {
            var intersectTypes = Company.Query<dynamic>($@"select value, title from utility.GetIntersectTypesByType('{type.ToString()}', {id}) order by title");

            return new JsonNetResult
            {
                Data = intersectTypes,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Gets a list of display fields that match a lookup.
        /// </summary>
        /// <param name="type">The type of object we are adding field type to.</param>
        /// <param name="id">The type Id of object we are adding field type to.</param>
        /// <param name="listType">The type of list to pull fields for.</param>
        /// <param name="listID">The type Id of the list to pull fields for.</param>
        /// <returns>A list of relevant fusion attribute types.</returns>
        [Route("FieldType_FilteredLookup_DisplayFields"), NonNullableParameters]
        public JsonNetResult FieldType_FilteredLookup_DisplayFields(string type, int id, string listType, int listID)
        {
            var list = Company.GetFieldTypesByObject(SystemObjects.LookupType, listID)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.ComplexRelationLookup.ToString())
                .OrderBy(i => i.Name)
                .Select(i => new { i.ID, i.Name, i.FriendlyName, i.LookupObjectType, i.LookupObjectID })
                .ToList()
                .Select(i => new
                {
                    title = i.FriendlyName,
                    value = $"{i.ID}|{i.Name}",
                    AllowFilter = ($"{i.LookupObjectType}|{i.LookupObjectID}" == $"{type.Replace("Type", "")}|{id}")
                });

            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Gets a list of fusion attribute types that meet the criteria based on the reference type and source fusion attribute type ID.
        /// </summary>
        /// <param name="id">The Source FusionAttributeType ID</param>
        /// <returns>A list of relevant fusion attribute types.</returns>
        [Route("FieldType_FusionLookup_DisplayFields"), NonNullableParameters]
        public JsonNetResult FieldType_FusionLookup_DisplayFields(int id)
        {
            var list = Company.GetFieldTypesByObject(SystemObjects.FusionAttributeType, id)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.ComplexRelationLookup.ToString())
                .Select(i => new { i.ID, i.Name })
                .ToDictionary(i => i.Name, i => i.ID);
            list.Add("Name", 0);
            list.Add("TextPath", 0);

            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = $"{i.Value}|{i.Key}" }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        /// <summary>
        /// Gets a list of fusion attribute types that meet the criteria based on the reference type and source fusion attribute type ID.
        /// </summary>
        /// <param name="s">The Source FusionAttributeType ID</param>
        /// <param name="r">The Reference Type we are checking</param>
        /// <returns>A list of relevant fusion attribute types.</returns>
        [Route("FieldType_FusionLookup_TargetAttributeTypes"), NonNullableParameters]
        public JsonNetResult FieldType_FusionLookup_TargetAttributeTypes(int s, int r)
        {
            IQueryable<FusionAttributeType> qry = null;
            switch (r)
            {
                case 2: //Parent Reference
                    var self = Company.GetById<FusionAttributeType>(s);
                    if (self != null)
                    {
                        qry = Company.Filter<FusionAttributeType>(i => i.ID == self.ParentID);
                    }
                    break;
                case 3: //Child Reference
                    qry = Company.Filter<FusionAttributeType>(i => i.ParentID == s);
                    break;
                case 4: //Relationship Reference
                    var relations = Company.Query<int>(@"select case when (SubjectID = @id) then ObjectID else SubjectID end as ID from [IntersectType] where (Subject = 'FusionAttributeType' and Object = 'FusionAttributeType') AND (SubjectID = @id or ObjectID = @id)", new { id = s }).ToList();
                    qry = Company.Filter<FusionAttributeType>(i => relations.Contains(i.ID));
                    break;
            }

            if (qry != null)
            {
                return new JsonNetResult
                {
                    Data = qry.OrderBy(x => x.TextPath).Select(i => new { title = i.TextPath, value = i.ID }),
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
            else
            {
                return new JsonNetResult
                {
                    Data = JArray.Parse("[]"),
                    Formatting = Newtonsoft.Json.Formatting.None
                };
            }
        }

        /// <summary>
        /// Used for both relation lookup and complex lookup
        /// </summary>
        /// <param name="id">IntersectTypeID></param>
        /// <returns>A list of child relationship types</returns>
        [Route("FieldType_RelationLookup_ChildIntersectTypes"), NonNullableParameters]
        public JsonNetResult FieldType_RelationLookup_ChildIntersectTypes(int id)
        {
            var intersectTypes = Company.Query<dynamic>($@"select value, title from utility.GetIntersectTypesByType('IntersectType', {id}) order by title");

            return new JsonNetResult
            {
                Data = intersectTypes,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_RelationLookup_DisplayFields"), NonNullableParameters]
        public JsonNetResult FieldType_RelationLookup_DisplayFields(int intersectTypeID, SystemObjects type, int id)
        {
            var list = Company.GetFieldTypesByObject(type, id)
                .Where(i => i.Type != DataType.Attribute.ToString()
                        && i.Type != DataType.Relationship.ToString()
                        && i.Type != DataType.OwnershipLookup.ToString()
                        && i.Type != DataType.RefListRelationship.ToString()
                        && i.Type != DataType.FusionLookup.ToString()
                        && i.Type != DataType.FilteredLookup.ToString()
                        && i.Type != DataType.ComplexRelationLookup.ToString()
                        && i.Type != DataType.JSON.ToString()
                        && i.Type != DataType.Tag.ToString())
                .Select(i => new { i.ID, i.Name })
                .ToDictionary(i => i.Name, i => i.ID);

            if (type == SystemObjects.ReferenceItemType)
            {
                if (id == 0)
                {
                    list.Add("Name", 0);
                    if (!list.ContainsKey("Description"))
                        list.Add("Description", 0);
                }
                else
                {
                    list.Add("Code", 0);
                }
            }
            else if (type == SystemObjects.ResourceType)
            {
                list.Add("FirstName", 0);
                list.Add("LastName", 0);
                list.Add("Email", 0);
                list.Add("LastLoggedInOn", 0);
                list.Add("DisplayValue", 0);
            }
            else if (type == SystemObjects.FusionAttributeType)
            {
                list.Add("Name", 0);
            }
            else if (type == SystemObjects.FusionQueryAttributeType)
            {
                list.Add("Name", 0);
                list.Add("DisplayValue", 0);
            }
            else
            {
                list.Add("DisplayValue", 0);
            }

            list.Add("TextPath", 0);

            var relList = Company.GetFieldTypesByObject(SystemObjects.IntersectType, intersectTypeID)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString())
                .Select(i => new { i.ID, i.Name }).ToList();
            relList.ForEach(r =>
            {
                list.Add($"Relation.{r.Name}", r.ID);
            });

            var sType = type.ToString();
            var relatedTypeList = Company.Filter<IntersectTypeDetail>(i =>
                (i.Subject == sType && i.SubjectID == id) ||
                (i.Object == sType && i.ObjectID == id)
                ).ToList().Select(i => new
                {
                    ID = i.ID,
                    Name = (i.Subject == sType && i.SubjectID == id) ? $"{i.ObjectName} ({i.PredicateName})" : $"{i.SubjectName} ({i.PredicateName})"
                }).Distinct().ToList();
            relatedTypeList.ForEach(r =>
            {
                if (list.ContainsKey($"Related Item.{r.Name}"))
                {
                    list.Add($"Related Item.{r.Name} ({r.ID})", r.ID);
                }
                else
                {
                    list.Add($"Related Item.{r.Name}", r.ID);
                }
            });


            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = $"{i.Value}|{i.Key}" }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

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

        [Route("FieldType_FieldFromRelationship_Fields"), NonNullableParameters]
        public JsonNetResult FieldType_FieldFromRelationship_Fields(SystemObjects type, int id, int intersectTypeID)
        {
            var intersectType = Company.GetById<IntersectType>(intersectTypeID);

            if (intersectType == null)
                return new JsonNetResult { Data = new Dictionary<string, int>() };

            var isSubject = (intersectType.Subject == type.ToString() && intersectType.SubjectID == id);

            var targetObjectType = isSubject ? intersectType.Object : intersectType.Subject;
            var targetObjectTypeID = isSubject ? intersectType.ObjectID : intersectType.SubjectID;

            var list = Company.Filter<FieldType>(f => f.Object == targetObjectType && f.ObjectID == targetObjectTypeID)
                .Where(i => i.Type != DataType.Attribute.ToString() &&
                        i.Type != DataType.FusionLookup.ToString() &&
                        i.Type != DataType.ComplexRelationLookup.ToString() &&
                        i.Type != DataType.Relationship.ToString() &&
                        i.Type != DataType.JSON.ToString()
                        && i.Type != DataType.Tag.ToString())
                .Select(i => new { i.ID, i.Name })
                .Distinct()
                .ToDictionary(i => i.Name, i => i.ID);

            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = $"{i.Value}" }),
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_Lookup_Tokens"), NonNullableParameters]
        public JsonNetResult FieldType_Lookup_Tokens(SystemObjects type, int id)
        {
            var list = Company.GetFieldTypesByObject(type, id)
                .Where(i => i.Type != DataType.Attribute.ToString() && i.Type != DataType.FusionLookup.ToString() && i.Type != DataType.ComplexRelationLookup.ToString())
                .Select(i => new { i.ID, i.Name })
                .ToDictionary(i => i.Name, i => i.Name);

            switch (type)
            {
                case SystemObjects.ArtifactType:
                    list.Add("ID", "ID");
                    break;
                case SystemObjects.ReferenceItem:
                case SystemObjects.ReferenceItemType:
                    list.Add("Code", "Code");
                    break;
                case SystemObjects.PolicyType:
                    list.Add("TextPath", "TextPath");
                    break;
                case SystemObjects.Resource:
                case SystemObjects.ResourceType:
                    list.Add("First Name", "FirstName");
                    list.Add("Last Name", "LastName");
                    list.Add("Email", "Email");
                    break;
                case SystemObjects.TaxonomyType:
                    if (id == 0)
                    {
                        list.Add("Name", "Name");
                    }
                    else
                    {
                        list.Add("TextPath", "TextPath");
                    }
                    break;
            }

            return new JsonNetResult
            {
                Data = list.Select(i => new { title = i.Key, value = "{" + i.Value + "}" }),
                Formatting = Newtonsoft.Json.Formatting.None
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

        [Route("FieldType_Lookup_DefaultValueOptions"), NonNullableParameters]
        public JsonNetResult FieldType_Lookup_DefaultValueOptions(SystemObjects type, int id)
        {
            var list = new List<ListIntItem>();
            list.Add(new ListIntItem { title = "- No default -", value = null });

            switch (type)
            {
                case SystemObjects.ReferenceItem:
                case SystemObjects.ReferenceItemType:
                case SystemObjects.ArtifactType:
                case SystemObjects.PolicyType:
                case SystemObjects.TaxonomyType:
                case SystemObjects.RuleType:
                    var typeString = type.ToString().Replace("Type", "");

                    var sql = $"select ast.ObjectID as value,d.DisplayValue as title  from asset ast inner join assettype astt on (ast.assettypeid = astt.id and ast.[object] = '{typeString}') cross apply [dbo].GetAssetDisplayValueById(ast.id) d where astt.ObjectID = @id order by d.DisplayValue";

                    list.AddRange(
                        Company.Query<ListIntItem>(sql, new { id })
                    );
                    break;
                case SystemObjects.Resource:
                case SystemObjects.ResourceType:
                    if (HideData3SixtyUsers())
                    {
                        list.AddRange(
                            Company.Table<GlobalReportingResource>().ToList()
                            .Where(i => !i.Email.EndsWith("@data3sixty.com") && !i.Email.EndsWith("@infogix.com"))
                            .OrderBy(i => i.FullName)
                            .Select(i => new ListIntItem { title = i.FullName, value = i.ResourceID }));
                    }
                    else
                    {
                        list.AddRange(
                            Company.Table<GlobalReportingResource>().ToList()
                            .OrderBy(i => i.FullName)
                            .Select(i => new ListIntItem { title = i.FullName, value = i.ResourceID }));
                    }
                    break;
            }

            return new JsonNetResult
            {
                Data = list,
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_Lookups"), NonNullableParameters]
        public JsonNetResult FieldType_Lookups(SystemObjects type, int id, bool isNg = false)
        {
            #region Load static lists

            var lists = Company.Query<dynamic>("exec utility.GetFieldTypeLookupList @type, @id", new { type = new Dapper.DbString { IsAnsi = true, Value = type.ToString() }, id }).ToList();
            var intersectTypes = lists.Where(i => i.type == "I").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
            var attributes = lists.Where(i => i.type == "A").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
            var fusionAttributeTypes = lists.Where(i => i.type == "F").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
            var lookups = lists.Where(i => i.type == "L").Select(i => new { i.value, i.title }).OrderBy(i => i.title);
            var filteredLookups = lists.Where(i => i.type == "FL").Select(i => new { i.value, i.title }).OrderBy(i => i.title);

            var complexLookupRelations = ComplexLookupRelationType.ChildItem.GetComplexLookupRelationTypeInfoList().ToList();

            var sType = type.ToString();

            IQueryable<IntersectTypeDetail> queryAllRelationships = Company.Filter<IntersectTypeDetail>(i =>
                (i.Subject == sType && i.SubjectID == id) ||
                (i.Object == sType && i.ObjectID == id)
            );

            //Hide self reference relationships for models and policies 
            if (type == SystemObjects.TaxonomyType || type == SystemObjects.PolicyType)
            {
                queryAllRelationships = queryAllRelationships.Where(x => x.PredicateType != PredicateType.IntraTypeHierarchy);
            }

            var allRelationships = queryAllRelationships.ToList();

            var cardinalRelationships = allRelationships.Where(i =>
                (i.Subject == sType && i.SubjectID == id && i.SubjectCardinality == Cardinality.One) ||
                (i.Object == sType && i.ObjectID == id && i.ObjectCardinality == Cardinality.One)
            ).ToList();

            var fieldFromRelRelationships = allRelationships.Where(i =>
                (i.Subject == sType && i.SubjectID == id && i.ObjectCardinality == Cardinality.One) ||
                (i.Object == sType && i.ObjectID == id && i.SubjectCardinality == Cardinality.One)
            ).ToList();

            var Field_Relationships = allRelationships
                .Where(x => x.PredicateType != PredicateType.InterTypeHierarchy)
                .Select(i => new
                {
                    title = ((i.Subject == sType && i.SubjectID == id) ?
                        $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                        $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                    value = $"{i.ID}"
                });

            var Field_CardinalRelationships = cardinalRelationships
                .Select(i => new
                {
                    title = ((i.Subject == sType && i.SubjectID == id) ?
                        $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                        $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                    value = $"{i.ID}"
                });

            var Field_CardinalReferenceRelationships = cardinalRelationships
                .Where(i =>
                    (i.Subject == sType && i.SubjectID == id) ?
                        (i.Object == SystemObjects.ReferenceItemType.ToString() && i.ObjectID == 0) :
                        (i.Subject == SystemObjects.ReferenceItemType.ToString() && i.SubjectID == 0)
                )
                .Select(i => new
                {
                    title = ((i.Subject == sType && i.SubjectID == id) ?
                        $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                        $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                    value = $"{i.ID}"
                });

            var Field_FieldFromRelRelationships = fieldFromRelRelationships.Select(i => new
            {
                title = ((i.Subject == sType && i.SubjectID == id) ?
                        $"{i.SubjectName} {i.PredicateName} {i.ObjectName}" :
                        $"{i.ObjectName} {i.PredicateInverse} {i.SubjectName}"),
                value = $"{i.ID}"
            });

            var patterns = new Dictionary<string, string>() {
                { "Choose sample...", "" },
                { "Email", @"^$|\b([A-Za-z0-9'_\.-]+)@([\dA-Za-z\.-]+)\.([A-Za-z\.]{2,6})\b" },
                { "IP Address", @"^$|^([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})\.([0-9]{1,3})$" },
                { "North American Phone", @"^$|\b\d{3}[-.]?\d{3}[-.]?\d{4}\b" },
                { "Internal Url", @"^$|\b(http(s)?:\/\/){1}([\da-z\.-]+)([\/\w \.-]*)*\/?\b" },
                { "Public Url", @"^$|\b(http(s)?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?\b" },
                { "US Zip Code", @"^(\d{5}(?:\-\d{4})?)$" }
            };
            var dataTypeOptions = DataType.Boolean.GetDataTypeInfoList(type)
                    .Where(i => !i.ReadOnly)
                    .Select(i => new
                    {
                        title = i.Description,
                        value = i.Name
                    })
                    .OrderBy(i => i.title).ToList();

            if (!Community.IsFusionEnabled())
            {
                dataTypeOptions = dataTypeOptions.Where(x => x.value != "FusionLookup").ToList();
            }

            var jsonFieldType = new Dictionary<string, string>()
            {
                { "Boolean", "bit" },
                { "Date", "date" },
                { "Date With Time", "datetime" },
                { "Decimal", "float" },
                { "Text", "nvarchar" },
                { "Whole Number", "int" },
                { "Whole Number (Large)", "bigint" },
            };
            var Field_JsonDataTypes = jsonFieldType.Select(i => new { title = i.Key, value = i.Value });
            var Field_JsonFields = Company.Filter<FieldType>(ft => ft.Object == sType && ft.ObjectID == id && ft.Type == "JSON")
                .OrderBy(ft => ft.FriendlyName)
                .Select(ft => new { ft.FriendlyName, ft.Name, ft.ID })
                .ToList()
                .Select(ft => new { title = $"{ft.FriendlyName} ({ft.Name})", value = ft.ID.ToString() })
                .ToList();

            #endregion

            return new JsonNetResult
            {
                Data = new
                {
                    Attributes = attributes,
                    Field_Relationships,
                    Field_JsonFields,
                    Field_JsonDataTypes,
                    Field_CardinalRelationships,
                    Field_FieldFromRelRelationships,
                    Field_CardinalReferenceRelationships,
                    DataTypes = dataTypeOptions,
                    FilteredLookups = filteredLookups,
                    Patterns = patterns.Select(i => new { title = i.Key, value = i.Value }),
                    IntersectTypes = intersectTypes,
                    FusionAttributeTypes = fusionAttributeTypes,
                    Lookups = lookups,
                    ComplexLookupRelations = complexLookupRelations
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_FormData"), NonNullableParameters]
        public JsonNetResult FieldType_FormData(int id)
        {
            FieldType ft = null;
            List<dynamic> filteredLookupItems = null;
            List<dynamic> fusionItems = null;
            List<dynamic> relationItems = null;
            dynamic ownershipLookupSettings = null;
            dynamic JsonElementSettings = null;

            if (id > 0)
            {
                ft = Company.GetById<FieldType>(id, i => i.FieldTypeFusionLookupDefinitions);

                if (ft.FieldTypeFilteredLookupDefinitions != null)
                {
                    if (ft.FieldTypeFilteredLookupDefinitions.Count > 0)
                    {
                        filteredLookupItems = new List<dynamic>();
                        foreach (var i in ft.FieldTypeFilteredLookupDefinitions)
                        {
                            filteredLookupItems.Add(new
                            {
                                i.ID,
                                i.Object,
                                i.ObjectID,
                                DisplayFields = (i.FieldTypeFilteredLookupDisplayFields != null) ? i.FieldTypeFilteredLookupDisplayFields.Select(df => new { value = $"{df.FieldTypeID}|{df.FieldTypeName}", Filter = df.Filter, Show = df.Show, SortOrder = df.SortOrder }).ToList() : null,
                                i.HideHeader,
                                i.HideFooter
                            });
                        }
                    }
                }

                if (ft.FieldTypeFusionLookupDefinitions != null)
                {
                    if (ft.FieldTypeFusionLookupDefinitions.Count > 0)
                    {
                        fusionItems = new List<dynamic>();
                        foreach (var i in ft.FieldTypeFusionLookupDefinitions)
                        {
                            fusionItems.Add(new
                            {
                                ID = i.ID,
                                SourceFusionAttributeType = i.SourceFusionAttributeTypeID,
                                ReferenceType = i.ReferenceType,
                                TargetFusionAttributeType = i.TargetFusionAttributeTypeID,
                                DisplayFields = (i.FieldTypeFusionLookupDisplayFields != null) ? i.FieldTypeFusionLookupDisplayFields.Select(df => new { value = $"{df.FieldTypeID}|{df.FieldTypeName}", FilterValue = df.FilterValue, Show = df.Show, SortOrder = df.SortOrder }).ToList() : null,
                                HideHeader = i.HideHeader,
                                HideFooter = i.HideFooter
                            });
                        }
                    }
                }

                if (ft.Type == DataType.JsonElement.ToString())
                {
                    if (!string.IsNullOrEmpty(ft.Definition))
                    {
                        JsonElementSettings = (dynamic)Newtonsoft.Json.JsonConvert.DeserializeObject(ft.Definition);
                    }
                }

                var lookup = Company.FieldTypeLookups.Where(i => i.FieldTypeID == id).FirstOrDefault();
                if (lookup != null)
                {
                    var definition = (dynamic)Newtonsoft.Json.JsonConvert.DeserializeObject(lookup.Definition);

                    if (ft.Type == DataType.ComplexRelationLookup.ToString())
                    {
                        relationItems = new List<dynamic>();
                        foreach (var r in definition.Relations)
                        {
                            relationItems.Add(new
                            {
                                r.ID,
                                IntersectType = r.IntersectTypeID,
                                ReferenceType = r.RelationType,
                                ChildIntersectType = 0,
                                DisplayFields = new List<dynamic>(),
                                lookup.HideHeader,
                                lookup.HideFooter,
                                lookup.HideFilter,
                                Direction = r.Direction ?? 0,
                                r.Object,
                                r.ObjectID
                            });
                        }
                        if (definition.Fields != null)
                        {
                            foreach (var f in definition.Fields)
                            {
                                var r = relationItems.Where(i => i.Object == f.Object && i.ObjectID == f.ObjectID).FirstOrDefault();

                                if (r != null)
                                {
                                    r.DisplayFields.Add(f);
                                }
                            }
                        }
                    }
                    else if (ft.Type == DataType.OwnershipLookup.ToString())
                    {
                        ownershipLookupSettings = new
                        {
                            definition.DisplayAssignmentSource,
                            definition.ExpandGroupMembership,
                            lookup.HideFilter,
                            lookup.HideFooter,
                            lookup.HideHeader
                        };
                    }
                }
            }

            return new JsonNetResult
            {
                Data = new
                {
                    FieldType = ft,
                    FilteredLookupItems = filteredLookupItems,
                    FusionItems = fusionItems,
                    JsonElementSettings,
                    OwnershipLookupSettings = ownershipLookupSettings,
                    RelationItems = relationItems
                },
                Formatting = Newtonsoft.Json.Formatting.None
            };
        }

        [Route("FieldType_TypeAheadLookup"), NonNullableParameters]
        public JsonNetResult FieldType_TypeAheadLookup(int fieldTypeId, string value = "", string query = "")
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
                V.Text";

            var selectedSql = $@"select {columns} 
                from FieldLookupValue V 
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
        public JsonNetResult FieldType_TypeaheadJsonPropertyOptionsForJsonField(int fieldTypeId, string phrase)
        {
            var selectList = new List<SelectListItem>();
            var ft = Company.GetById<FieldType>(fieldTypeId);
            phrase = phrase.Replace("[", @"\[");
            var sql = $@"
select		P.[Path]
from		FieldJsonProperty P
			inner join Field F on F.ID = P.FieldID and F.FieldTypeID = @fieldTypeId and P.[Path] like @phrase+'%' escape '\'
group by	P.[Path]
order by	P.[Path]
offset 0 rows fetch next 25 rows only
                ";

            var items = Company.Query<string>(sql, new { fieldTypeId, phrase }).ToList();

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

            if (nameUpper == "PARENTID" || nameUpper == "DATABASE") throw new Exception("Use of a field type with the name " + name + " is prohibited.");
        }

        [HttpPost, AjaxValidateAntiForgeryToken, ValidateInput(false), Route("AddFieldType")]
        public JsonResult AddFieldType(FieldTypeEditorModel model)
        {
            try
            {
                if (!Company.HasAssetTypePermission(model.FieldType.Object, model.FieldType.ObjectID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                int maxColumnOrder = 0;
                try { maxColumnOrder = Company.GetFieldTypesByObject((SystemObjects)Enum.Parse(typeof(SystemObjects), model.FieldType.Object), model.FieldType.ObjectID).Max(i => i.ColumnOrder); }
                catch { }

                //dont let fields with reserved names in
                CheckIsFieldTypeNameReserved(model.FieldType.Name);

                model.FieldType.ColumnOrder = maxColumnOrder + 1;
                model.FieldType.UpdatedBy = Company.CurrentResourceID;

                //set the default formatted value to the same as the default value, for lists the trigger will update this to the display value for the list
                // however for strings, bools etc it will stay since the lookupobjecttype is null since the trigger only looks at where it is not null.
                if (!string.IsNullOrEmpty(model.FieldType.DefaultValue))
                    model.FieldType.DefaultFormattedValue = model.FieldType.DefaultValue;

                var nameRegex = new System.Text.RegularExpressions.Regex("^[a-zA-Z][a-zA-Z0-9_-]+$");
                if (!nameRegex.IsMatch(model.FieldType.Name))
                {
                    throw new ConflictException("Error Occurred!", $"{FieldInfo.ApiName_Name} can only have uppercase letters, lowercase letters, numbers, dash, or underscore. It must also begin with a letter.");
                }

                if (!string.IsNullOrEmpty(model.FieldType.Name) && (model.FieldType.Name.ToUpper().Equals("ID") || model.FieldType.Name.ToUpper().Equals("UID")))
                {
                    throw new ConflictException("Error Occurred!", "You can not add field with API Name [ID] or [UID].");
                }

                if (model.FieldType.MinimumLength.HasValue && model.FieldType.MaximumLength.HasValue)
                {
                    if (model.FieldType.MinimumLength.Value > model.FieldType.MaximumLength.Value)
                    {
                        throw new ConflictException("Error Occurred!", "You may not have a minimum length that is greater than the maximum length.");
                    }
                }
                if (new[] { "Text" }.Contains(model.FieldType.Type))
                {
                    if (!string.IsNullOrEmpty(model.FieldType.Pattern)) model.FieldType.MinimumLength = 0;
                }
                else if (!new[] { "Number", "Decimal" }.Contains(model.FieldType.Type))
                {
                    if (!model.FieldType.IsRequired) model.FieldType.MinimumLength = 0;
                }

                var val = model.Validation();
                if (!val.Valid)
                {
                    throw new ConflictException("Error Occurred!", val.Message);
                }

                if (model.FieldType.Type == DataType.RefListRelationship.ToString() && (model.FieldType.LookupObjectType != "IntersectType" || model.FieldType.LookupObjectID == null))
                {
                    throw new ConflictException("Error Occurred!", FieldInfo.FieldReferenceItemListFromRelationship_NeededRelationship);
                }
                if (model.FieldType.Type != DataType.Lookup.ToString())
                    model.FieldType.ParentFieldTypeID = 0;

                switch (model.FieldType.Type)
                {
                    case "Date":

                        var date = ConverDate(model.FieldType.DefaultValue);
                        if (date != null)
                        {
                            model.FieldType.DefaultValue = date;
                        }
                        Company.Add<FieldType>(model.FieldType);
                        break;
                    case "JsonElement":
                        #region
                        try
                        {
                            var jsonElementSettings = new
                            {
                                FieldTypeID = model.JsonElementSettings.FieldTypeID,
                                Path = model.JsonElementSettings.Path,
                                DataType = model.JsonElementSettings.DataType
                            };
                            model.FieldType.Definition = Newtonsoft.Json.JsonConvert.SerializeObject(jsonElementSettings);

                            model.FieldType.IsPartOfKey = false;
                            model.FieldType.IsEditable = false;
                            model.FieldType.IsRequired = false;
                            model.FieldType.IsPrimaryFilter = false;

                            Company.Add(model.FieldType);
                        }
                        catch
                        {
                            throw;
                        }

                        break;
                    #endregion
                    case "Html":
                        model.FieldType.MinimumLength = (!model.FieldType.IsRequired) ? (int?)null : 1;
                        model.FieldType.MaximumLength = null;
                        Company.Add<FieldType>(model.FieldType);
                        break;
                    case "Lookup":
                        #region
                        if (string.IsNullOrEmpty(model.FieldType.LookupDisplayFormat))
                        {
                            throw new ConflictException("Error Occurred!", $"{FieldInfo.ListDisplayFormat_Name} is required if the field type is List.");
                        }
                        Company.Add<FieldType>(model.FieldType);
                        break;
                    #endregion
                    case "FilteredLookup":
                        #region
                        if (model.FilteredLookupItem != null)
                        {
                            val = model.FilteredLookupItem.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            var def = new FieldTypeFilteredLookupDefinition
                            {
                                Object = model.FilteredLookupItem.Object,
                                ObjectID = model.FilteredLookupItem.ObjectID,
                                HideHeader = model.FilteredLookupItem.HideHeader,
                                HideFooter = model.FilteredLookupItem.HideFooter
                            };

                            if (model.FilteredLookupItem.DisplayFields != null)
                            {
                                if (model.FilteredLookupItem.DisplayFields.Count > 0)
                                {
                                    def.FieldTypeFilteredLookupDisplayFields = new List<FieldTypeFilteredLookupDisplayField>();

                                    foreach (var df in model.FilteredLookupItem.DisplayFields)
                                    {
                                        var ndf = new FieldTypeFilteredLookupDisplayField
                                        {
                                            FieldTypeFilteredLookupDefinitionID = def.ID,
                                            FieldTypeName = df.FieldTypeName,
                                            FieldTypeID = df.FieldTypeID,
                                            Filter = df.Filter,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || ndf.Filter || ndf.SortOrder.HasValue)
                                            def.FieldTypeFilteredLookupDisplayFields.Add(ndf);
                                    }
                                }
                            }

                            model.FieldType.IsPartOfKey = false;
                            model.FieldType.IsDisplayable = true;
                            model.FieldType.IsEditable = false;
                            model.FieldType.IsListable = false;
                            model.FieldType.IsRequired = false;
                            model.FieldType.FieldTypeFilteredLookupDefinitions = new List<FieldTypeFilteredLookupDefinition>() { def };

                            Company.Add<FieldType>(model.FieldType);
                        }
                        break;
                    #endregion
                    case "FusionLookup":
                        #region
                        foreach (var fi in model.FusionItems)
                        {
                            val = fi.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            var def = new FieldTypeFusionLookupDefinition
                            {
                                ReferenceType = fi.ReferenceType,
                                SourceFusionAttributeTypeID = fi.SourceFusionAttributeType,
                                TargetFusionAttributeTypeID = fi.TargetFusionAttributeType,
                                HideHeader = fi.HideHeader,
                                HideFooter = fi.HideFooter
                            };

                            if (fi.DisplayFields != null)
                            {
                                if (fi.DisplayFields.Count > 0)
                                {
                                    def.FieldTypeFusionLookupDisplayFields = new List<FieldTypeFusionLookupDisplayField>();

                                    foreach (var df in fi.DisplayFields)
                                    {
                                        var ndf = new FieldTypeFusionLookupDisplayField
                                        {
                                            FieldTypeFusionLookupDefinitionID = def.ID,
                                            FieldTypeName = df.FieldTypeName,
                                            FieldTypeID = df.FieldTypeID,
                                            FilterValue = string.IsNullOrEmpty(df.FilterValue) ? null : df.FilterValue,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || !string.IsNullOrEmpty(ndf.FilterValue) || ndf.SortOrder.HasValue)
                                            def.FieldTypeFusionLookupDisplayFields.Add(ndf);
                                    }
                                }
                            }
                            model.FieldType.FieldTypeFusionLookupDefinitions = new List<FieldTypeFusionLookupDefinition>() { def };
                        }

                        model.FieldType.IsDisplayable = true;

                        Company.Add<FieldType>(model.FieldType);
                        break;
                    #endregion
                    case "ComplexRelationLookup":
                        #region
                        var relations = new List<FieldLookupRelationItem>();
                        var fields = new List<FieldLookupFieldItem>();
                        foreach (var r in model.RelationItems)
                        {
                            relations.Add(new FieldLookupRelationItem
                            {
                                IntersectTypeID = r.IntersectType,
                                Object = r.Object,
                                ObjectID = r.ObjectID,
                                RelationType = r.ReferenceType,
                                Direction = r.Direction

                            });
                            if (r.DisplayFields == null)
                                r.DisplayFields = new List<FieldTypeItemDisplayFieldEditorModel>();
                            foreach (var f in r.DisplayFields.Where(i => i.Show || !string.IsNullOrEmpty(i.FilterValue) || (i.SortOrder.HasValue && i.SortOrder != 0)))
                            {
                                fields.Add(new FieldLookupFieldItem
                                {
                                    DisplayOrder = f.DisplayOrder,
                                    Object = r.Object,
                                    ObjectID = r.ObjectID,
                                    FieldTypeID = f.FieldTypeID,
                                    FieldTypeName = f.FieldTypeName,
                                    SortOrder = f.SortOrder ?? 0,
                                    OverrideDisplayName = f.OverrideDisplayName,
                                    Filter = f.FilterValue,
                                    Show = f.Show,
                                    Width = f.Width
                                });
                            }
                        }

                        var lookup = new
                        {
                            Relations = relations,
                            Fields = fields
                        };
                        var lookupRow = new FieldTypeLookup
                        {
                            FieldTypeID = model.FieldType.ID,
                            HideFooter = model.RelationItems[0].HideFooter,
                            HideHeader = model.RelationItems[0].HideHeader,
                            HideFilter = model.RelationItems[0].HideFilter,
                            LookupType = model.RelationItems[0].RelationType,
                            Definition = Newtonsoft.Json.JsonConvert.SerializeObject(lookup)
                        };
                        try
                        {
                            var existing = Company.FieldTypeLookups.Where(i => i.FieldTypeID == model.FieldType.ID).FirstOrDefault();

                            if (existing != null)
                            {
                                Company.FieldTypeLookups.Remove(existing);
                            }

                            model.FieldType.IsPartOfKey = false;
                            model.FieldType.IsDisplayable = true;
                            model.FieldType.IsEditable = false;
                            model.FieldType.IsListable = false;
                            model.FieldType.IsRequired = false;

                            Company.Add<FieldType>(model.FieldType);
                            lookupRow.FieldTypeID = model.FieldType.ID;
                            Company.FieldTypeLookups.Add(lookupRow);
                            Company.SaveChanges();
                        }
                        catch
                        {
                            throw;
                        }

                        break;
                    #endregion
                    case "OwnershipLookup":
                        #region
                        var ownershipSettings = new
                        {
                            DisplayAssignmentSource = model.OwnershipLookupSettings.DisplayAssignmentSource,
                            ExpandGroupMembership = model.OwnershipLookupSettings.ExpandGroupMembership
                        };
                        var ownershipLookupRow = new FieldTypeLookup
                        {
                            FieldTypeID = model.FieldType.ID,
                            HideFooter = model.OwnershipLookupSettings.HideFooter,
                            HideHeader = model.OwnershipLookupSettings.HideHeader,
                            HideFilter = model.OwnershipLookupSettings.HideFilter,
                            LookupType = 1,
                            Definition = Newtonsoft.Json.JsonConvert.SerializeObject(ownershipSettings)
                        };
                        try
                        {
                            var existing = Company.FieldTypeLookups.Where(i => i.FieldTypeID == model.FieldType.ID).FirstOrDefault();

                            if (existing != null)
                            {
                                Company.FieldTypeLookups.Remove(existing);
                            }

                            model.FieldType.IsPartOfKey = false;
                            model.FieldType.IsDisplayable = true;
                            model.FieldType.IsEditable = false;
                            model.FieldType.IsListable = false;
                            model.FieldType.IsRequired = false;

                            Company.Add<FieldType>(model.FieldType);
                            ownershipLookupRow.FieldTypeID = model.FieldType.ID;
                            Company.FieldTypeLookups.Add(ownershipLookupRow);
                            Company.SaveChanges();
                        }
                        catch
                        {
                            throw;
                        }

                        break;
                    #endregion
                    default:
                        Company.Add<FieldType>(model.FieldType);
                        break;
                }

                return jsonSuccess(FormInfo.Add_FieldType_Confirmation, model.FieldType.ID.ToString(), "add", HttpStatusCode.Created);
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

        [HttpDelete, Route("DeleteFieldType")]
        public JsonResult DeleteFieldType(FormCollection form)
        {
            try
            {
                if (!form.HasKeys()) throw new NoFormDataException(FormInfo.NoFormData_FieldType);

                var id = parseIntField(form, "ID");

                var model = Company.GetById<FieldType>(id);
                if (model == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);

                if (!Company.HasAssetTypePermission(model.Object, model.ObjectID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                if (model.Type == SystemObjects.Tag.ToString())
                {
                    var assetTypeID = Company.AssetTypes.FirstOrDefault(x => x.Object == model.Object && x.ObjectID == model.ObjectID)?.ID;
                    var assets = Company.Assets.Where(x => x.AssetTypeID == assetTypeID).Select(x => x.ID);
                    var assetTagsForDeletion = Company.AssetTags.Where(x => assets.Contains(x.AssetID)).ToList();
                    Company.AssetTags.RemoveRange(assetTagsForDeletion);
                }

                Company.Delete(model);

                return jsonSuccess(FormInfo.Delete_FieldType_Confirmation, id.ToString(), "delete", HttpStatusCode.OK);
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

        [HttpDelete, Route("DeleteFieldTypeByID"), NonNullableParameters]
        public JsonResult DeleteFieldTypeByID(int id)
        {
            var form = new FormCollection();
            form.Add("ID", id.ToString());
            return DeleteFieldType(form);
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

        [HttpPut, ValidateInput(false), Route("EditFieldType")]
        public JsonResult EditFieldType(FieldTypeEditorModel model)
        {
            try
            {
                //dont let fields with reserved names in
                CheckIsFieldTypeNameReserved(model.FieldType.Name);

                var ft = Company.GetById<FieldType>(model.FieldType.ID);
                var used = Company.Any<Field>(i => i.FieldTypeID == ft.ID);

                if (ft == null) throw new NotFoundException(FormInfo.NoFormData_FieldType);

                if (!Company.HasAssetTypePermission(ft.Object, ft.ObjectID, Permission.ModifyAsset))
                    return jsonException(FormInfo.Permisions_Error_Delete, HttpStatusCode.Forbidden);

                var val = model.Validation();
                if (!val.Valid)
                {
                    throw new ConflictException("Error Occurred!", val.Message);
                }

                var nameRegex = new System.Text.RegularExpressions.Regex("^[a-zA-Z][a-zA-Z0-9_-]+$");
                if (!nameRegex.IsMatch(model.FieldType.Name))
                {
                    throw new ConflictException("Error Occurred!", $"{FieldInfo.ApiName_Name} can only have uppercase letters, lowercase letters, numbers, dash, or underscore. It must also begin with a letter.");
                }
                if (new string[2] { "id", "uid" }.Contains(model.FieldType.Name.Trim().ToLower()))
                {
                    throw new ConflictException("Error Occurred!", $"{FieldInfo.ApiName_Name} cannot be ID or UID!");
                }


                if (ft.Type == "Lookup" && ft.AllowMultipleValues && !model.FieldType.AllowMultipleValues &&
                            Company.Fields.Where(x => x.FieldTypeID == ft.ID).Where(x => x.Value.Contains(",")).ToList().Count() > 0)
                {
                    throw new ConflictException("Error Occurred!", FormInfo.FieldType_List_Error_Multiple_Items_Used);
                }

                if (model.FieldType.Type == DataType.RefListRelationship.ToString() && (model.FieldType.LookupObjectType != "IntersectType" || model.FieldType.LookupObjectID == null))
                {
                    throw new ConflictException("Error Occurred!", FieldInfo.FieldReferenceItemListFromRelationship_NeededRelationship);
                }
                //shallow copy of fieldType
                var ftCopy = (FieldType)Company.Entry(ft)
                                              .CurrentValues.ToObject();
                // Static fields

                ft.Name = model.FieldType.Name;
                ft.SortOrder = model.FieldType.SortOrder;
                ft.Category = model.FieldType.Category;
                ft.FriendlyName = model.FieldType.FriendlyName;
                ft.DefaultValue = (string.IsNullOrEmpty(model.FieldType.DefaultValue)) ? null : model.FieldType.DefaultValue.Trim();
                //set the default formatted value to the same as the default value, for lists the trigger will update this to the display value for the list
                // however for strings, bools etc it will stay as there is no lookupfield column.
                ft.DefaultFormattedValue = ft.DefaultValue;
                ft.DisplayDescription = model.FieldType.DisplayDescription;
                ft.FormDescription = model.FieldType.FormDescription;
                ft.ValidationDescription = model.FieldType.ValidationDescription;
                ft.ColumnWidth = model.FieldType.ColumnWidth;
                ft.AllowMultipleValues = model.FieldType.AllowMultipleValues;
                ft.ShowIfEmpty = model.FieldType.ShowIfEmpty;
                ft.Increment = model.FieldType.Increment;
                ft.Precision = model.FieldType.Precision;
                if (model.FieldType.Type == DataType.Lookup.ToString())
                    ft.ParentFieldTypeID = model.FieldType.ParentFieldTypeID;
                else
                    ft.ParentFieldTypeID = 0;

                if (
                    (model.FieldType.Type == DataType.ComplexRelationLookup.ToString()) ||
                    (model.FieldType.Type == DataType.FilteredLookup.ToString()) ||
                    (model.FieldType.Type == DataType.FusionLookup.ToString()) ||
                    (model.FieldType.Type == DataType.OwnershipLookup.ToString())
                    )
                {
                    ft.IsDisplayable = true;
                    ft.IsEditable = false;
                    ft.IsListable = false;
                    ft.IsPartOfKey = false;
                }
                else
                {
                    ft.IsDisplayable = model.FieldType.IsDisplayable;
                    ft.IsEditable = model.FieldType.IsEditable;
                    ft.IsListable = model.FieldType.IsListable;
                    ft.IsPartOfKey = model.FieldType.IsPartOfKey;
                    ft.IsPrimaryFilter = model.FieldType.IsPrimaryFilter;
                }

                if (model.FieldType.Type == DataType.Lookup.ToString())
                {
                    ft.AllowAllLabel = model.FieldType.AllowAllLabel;
                    ft.AllowAllValue = model.FieldType.AllowAllValue;
                }
                else
                {
                    ft.AllowAllLabel = null;
                    ft.AllowAllValue = false;
                }

                ft.IsRequired = model.FieldType.IsRequired;

                ft.MaximumLength = model.FieldType.MaximumLength;
                ft.Pattern = model.FieldType.Pattern;

                if (new[] { "Number", "Decimal" }.Contains(ft.Type))
                {
                    ft.MinimumLength = model.FieldType.MinimumLength;
                }
                else if (new[] { "Text" }.Contains(ft.Type))
                {
                    if (string.IsNullOrEmpty(model.FieldType.Pattern))
                    {
                        ft.MinimumLength = model.FieldType.MinimumLength;
                    }
                    else
                    {
                        ft.MinimumLength = 0;
                    }
                }
                else
                {
                    if (!ft.IsRequired) ft.MinimumLength = 0;
                }

                bool isNew;

                var defs = Company.Filter<FieldTypeFusionLookupDefinition>(i => i.FieldTypeID == ft.ID, i => i.FieldTypeFusionLookupDisplayFields).ToList();
                var efli = Company.Filter<FieldTypeFilteredLookupDefinition>(i => i.FieldTypeID == ft.ID, i => i.FieldTypeFilteredLookupDisplayFields).FirstOrDefault();
                var fl = Company.Filter<FieldTypeLookup>(i => i.FieldTypeID == ft.ID).FirstOrDefault();

                if (ft.Type == "Date")
                {
                    var date = ConverDate(ft.DefaultValue);
                    if (date != null)
                        ft.DefaultValue = date;
                }

                if (used)
                {
                    var allowTypeChange = false;
                    switch (ft.Type)
                    {
                        case "Text":
                            allowTypeChange = (model.FieldType.Type == DataType.Text.ToString()) || (model.FieldType.Type == DataType.Html.ToString()) || (model.FieldType.Type == DataType.Password.ToString());
                            break;
                        case "Number":
                            allowTypeChange = (model.FieldType.Type == DataType.Number.ToString()) || (model.FieldType.Type == DataType.Decimal.ToString());
                            break;
                        case "Password":
                            allowTypeChange = (model.FieldType.Type == DataType.Password.ToString()) || (model.FieldType.Type == DataType.Html.ToString()) || (model.FieldType.Type == DataType.Text.ToString());
                            break;
                    }
                    if (allowTypeChange)
                    {
                        ft.Type = model.FieldType.Type;
                    }
                    else
                    {
                        if (ft.Type != model.FieldType.Type)
                        {
                            throw new ConflictException("Error Occurred!", $"You may not change the input type for {ft.FriendlyName} as it is already used.");
                        }
                    }
                }
                else
                {
                    ft.Type = model.FieldType.Type;

                    //reset type specific properties
                    ft.LookupObjectType = null;
                    ft.LookupObjectID = null;
                    ft.LookupDisplayFormat = null;

                    if (defs != null && ft.Type != DataType.FusionLookup.ToString())
                    {
                        foreach (var i in defs)
                        {
                            var d = Company.FieldTypeFusionLookupDisplayFields.Where(j => j.FieldTypeFusionLookupDefinitionID == i.ID).ToList();
                            if (d != null && d.Count > 0)
                                Company.FieldTypeFusionLookupDisplayFields.RemoveRange(d);
                        }
                        Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);

                    }

                    if (efli != null && ft.Type != DataType.FilteredLookup.ToString())
                    {

                        var d = Company.FieldTypeFilteredLookupDisplayFields.Where(j => j.FieldTypeFilteredLookupDefinitionID == efli.ID).ToList();
                        if (d != null && d.Count > 0)
                            Company.FieldTypeFilteredLookupDisplayFields.RemoveRange(d);
                        Company.FieldTypeFilteredLookupDefinitions.Remove(efli);
                    }

                    if (fl != null && ft.Type != DataType.ComplexRelationLookup.ToString())
                    {
                        Company.FieldTypeLookups.Remove(fl);
                    }

                }

                switch (ft.Type)
                {
                    case "JsonElement":
                        #region
                        try
                        {
                            var jsonElementSettings = new
                            {
                                FieldTypeID = model.JsonElementSettings.FieldTypeID,
                                Path = model.JsonElementSettings.Path,
                                DataType = model.JsonElementSettings.DataType
                            };
                            ft.Definition = Newtonsoft.Json.JsonConvert.SerializeObject(jsonElementSettings);

                            ft.IsPartOfKey = false;
                            ft.IsEditable = false;
                            ft.IsRequired = false;
                            ft.IsPrimaryFilter = false;
                        }
                        catch
                        {
                            throw;
                        }

                        break;
                    #endregion
                    case "Html":
                        ft.MinimumLength = (!ft.IsRequired) ? (int?)null : 1;
                        ft.MaximumLength = null;
                        break;
                    case "FilteredLookup":
                        #region
                        isNew = false;
                        if (model.FilteredLookupItem != null)
                        {
                            val = model.FilteredLookupItem.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            var listToRemove = new List<FieldTypeFilteredLookupDisplayField>();

                            if (efli == null)
                            {
                                isNew = true;
                                efli = new FieldTypeFilteredLookupDefinition
                                {
                                    FieldTypeID = model.FieldType.ID,
                                    Object = model.FilteredLookupItem.Object,
                                    ObjectID = model.FilteredLookupItem.ObjectID,
                                    HideHeader = model.FilteredLookupItem.HideHeader,
                                    HideFooter = model.FilteredLookupItem.HideFooter,
                                    FieldTypeFilteredLookupDisplayFields = new List<FieldTypeFilteredLookupDisplayField>()
                                };
                            }
                            else
                            {
                                efli.Object = model.FilteredLookupItem.Object;
                                efli.ObjectID = model.FilteredLookupItem.ObjectID;
                                efli.HideHeader = model.FilteredLookupItem.HideHeader;
                                efli.HideFooter = model.FilteredLookupItem.HideFooter;
                            }

                            if (model.FilteredLookupItem.DisplayFields != null)
                            {
                                // Add those that do not yet exist.
                                foreach (var df in model.FilteredLookupItem.DisplayFields)
                                {
                                    if (!efli.FieldTypeFilteredLookupDisplayFields.Any(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName))
                                    {
                                        var ndf = new FieldTypeFilteredLookupDisplayField
                                        {
                                            FieldTypeFilteredLookupDefinitionID = efli.ID,
                                            FieldTypeID = df.FieldTypeID,
                                            FieldTypeName = df.FieldTypeName,
                                            Filter = df.Filter,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || ndf.Filter || ndf.SortOrder.HasValue)
                                            efli.FieldTypeFilteredLookupDisplayFields.Add(ndf);
                                    }
                                    else
                                    {
                                        var edf = efli.FieldTypeFilteredLookupDisplayFields.Single(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName);

                                        edf.Filter = df.Filter;
                                        edf.SortOrder = df.SortOrder;
                                        edf.Show = df.Show;

                                        if (!edf.Show && !edf.Filter && !edf.SortOrder.HasValue)
                                            efli.FieldTypeFilteredLookupDisplayFields.Remove(edf);
                                    }
                                }

                                // Remove those that no longer exist.
                                foreach (var edf in efli.FieldTypeFilteredLookupDisplayFields)
                                {
                                    if (!model.FilteredLookupItem.DisplayFields.Any(i => i.FieldTypeID == edf.FieldTypeID && i.FieldTypeName == edf.FieldTypeName))
                                    {
                                        listToRemove.Add(edf);
                                    }
                                }
                            }
                            else
                            {
                                if (efli.FieldTypeFilteredLookupDisplayFields != null)
                                {
                                    listToRemove.AddRange(efli.FieldTypeFilteredLookupDisplayFields);
                                }
                            }

                            if (listToRemove.Count > 0)
                            {
                                Company.FieldTypeFilteredLookupDisplayFields.RemoveRange(listToRemove);
                            }

                            listToRemove = null;

                            if (isNew)
                                Company.Add<FieldTypeFilteredLookupDefinition>(efli);
                            else
                                Company.Update<FieldTypeFilteredLookupDefinition>(efli);
                        }
                        else
                        {
                            if (efli != null)
                            {
                                ft.FieldTypeFilteredLookupDefinitions.Remove(efli);
                            }
                        }

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        break;
                    #endregion
                    case "FusionLookup":
                        #region
                        foreach (var fi in model.FusionItems)
                        {
                            val = fi.Validation();
                            if (!val.Valid)
                            {
                                throw new ConflictException("Error Occurred!", val.Message);
                            }

                            isNew = false;
                            FieldTypeFusionLookupDefinition efi = null;

                            if (fi.ID > 0)
                            {
                                efi = defs.SingleOrDefault(i => i.ID == fi.ID);
                                if (efi == null)
                                {
                                    isNew = true;
                                }
                            }
                            else
                            {
                                isNew = true;
                            }


                            if (isNew)
                            {
                                efi = new FieldTypeFusionLookupDefinition
                                {
                                    FieldTypeID = ft.ID,
                                    ReferenceType = fi.ReferenceType,
                                    SourceFusionAttributeTypeID = fi.SourceFusionAttributeType,
                                    TargetFusionAttributeTypeID = fi.TargetFusionAttributeType,
                                    FieldTypeFusionLookupDisplayFields = new List<FieldTypeFusionLookupDisplayField>(),
                                    HideHeader = fi.HideHeader,
                                    HideFooter = fi.HideFooter
                                };
                            }
                            else
                            {
                                efi.ReferenceType = fi.ReferenceType;
                                efi.SourceFusionAttributeTypeID = fi.SourceFusionAttributeType;
                                efi.TargetFusionAttributeTypeID = fi.TargetFusionAttributeType;
                                efi.HideHeader = fi.HideHeader;
                                efi.HideFooter = fi.HideFooter;
                            }


                            var listToRemove = new List<FieldTypeFusionLookupDisplayField>();

                            if (fi.DisplayFields != null)
                            {
                                // Add those that do not yet exist.
                                foreach (var df in fi.DisplayFields)
                                {
                                    if (!efi.FieldTypeFusionLookupDisplayFields.Any(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName))
                                    {
                                        var ndf = new FieldTypeFusionLookupDisplayField
                                        {
                                            FieldTypeFusionLookupDefinitionID = efi.ID,
                                            FieldTypeID = df.FieldTypeID,
                                            FieldTypeName = df.FieldTypeName,
                                            FilterValue = string.IsNullOrEmpty(df.FilterValue) ? null : df.FilterValue,
                                            SortOrder = df.SortOrder,
                                            Show = df.Show
                                        };

                                        if (ndf.Show || !string.IsNullOrEmpty(ndf.FilterValue) || ndf.SortOrder.HasValue)
                                            efi.FieldTypeFusionLookupDisplayFields.Add(ndf);
                                    }
                                    else
                                    {
                                        var edf = efi.FieldTypeFusionLookupDisplayFields.Single(i => i.FieldTypeID == df.FieldTypeID && i.FieldTypeName == df.FieldTypeName);

                                        edf.FilterValue = string.IsNullOrEmpty(df.FilterValue) ? null : df.FilterValue;
                                        edf.SortOrder = df.SortOrder;
                                        edf.Show = df.Show;

                                        if (!edf.Show && string.IsNullOrEmpty(edf.FilterValue) && !edf.SortOrder.HasValue)
                                            efi.FieldTypeFusionLookupDisplayFields.Remove(edf);
                                    }
                                }

                                // Remove those that no longer exist.
                                foreach (var edf in efi.FieldTypeFusionLookupDisplayFields)
                                {
                                    if (!fi.DisplayFields.Any(i => i.FieldTypeID == edf.FieldTypeID && i.FieldTypeName == edf.FieldTypeName))
                                    {
                                        listToRemove.Add(edf);
                                    }
                                }
                            }
                            else
                            {
                                if (efi.FieldTypeFusionLookupDisplayFields != null)
                                {
                                    listToRemove.AddRange(efi.FieldTypeFusionLookupDisplayFields);
                                }
                            }

                            if (listToRemove.Count > 0)
                            {
                                Company.FieldTypeFusionLookupDisplayFields.RemoveRange(listToRemove);
                            }

                            listToRemove = null;

                            if (isNew)
                                Company.Add<FieldTypeFusionLookupDefinition>(efi);
                            else
                                Company.Update<FieldTypeFusionLookupDefinition>(efi);
                        }

                        //Clean up previous stuff
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        break;
                    #endregion
                    case "Lookup":
                        #region
                        ft.LookupObjectType = model.FieldType.LookupObjectType;
                        ft.LookupObjectID = model.FieldType.LookupObjectID;
                        ft.LookupDisplayFormat = model.FieldType.LookupDisplayFormat;
                        ft.LookupEditFormat = model.FieldType.LookupEditFormat;
                        if (string.IsNullOrEmpty(model.FieldType.LookupDisplayFormat))
                        {
                            throw new ConflictException("Error Occurred!", $"{FieldInfo.ListDisplayFormat_Name} is required if the field type is List.");
                        }

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);

                        ft.FilterPredicateID = model.FieldType.FilterPredicateID;
                        if (model.FieldType.FilterPredicateID != null) //Filtered lists should not have default values
                            ft.DefaultValue = null;
                        ft.FilterPredicateDirection = model.FieldType.FilterPredicateDirection;
                        ft.FilterFieldTypeID = model.FieldType.FilterFieldTypeID;

                        break;
                    #endregion
                    case "ComplexRelationLookup":
                        #region
                        var relations = new List<FieldLookupRelationItem>();
                        var fields = new List<FieldLookupFieldItem>();
                        foreach (var r in model.RelationItems)
                        {
                            relations.Add(new FieldLookupRelationItem
                            {
                                IntersectTypeID = r.IntersectType,
                                Object = r.Object,
                                ObjectID = r.ObjectID,
                                RelationType = r.ReferenceType,
                                Direction = r.Direction

                            });
                            if (r.DisplayFields == null)
                                r.DisplayFields = new List<FieldTypeItemDisplayFieldEditorModel>();
                            foreach (var f in r.DisplayFields.Where(i => i.Show || !string.IsNullOrEmpty(i.FilterValue) || (i.SortOrder.HasValue && i.SortOrder != 0)))
                            {
                                fields.Add(new FieldLookupFieldItem
                                {
                                    DisplayOrder = f.DisplayOrder,
                                    Object = r.Object,
                                    ObjectID = r.ObjectID,
                                    FieldTypeID = f.FieldTypeID,
                                    FieldTypeName = f.FieldTypeName,
                                    SortOrder = f.SortOrder ?? 0,
                                    OverrideDisplayName = f.OverrideDisplayName,
                                    Filter = f.FilterValue,
                                    Show = f.Show,
                                    Width = f.Width
                                });
                            }
                        }

                        var lookup = new
                        {
                            Relations = relations,
                            Fields = fields
                        };
                        var lookupRow = new FieldTypeLookup
                        {
                            FieldTypeID = model.FieldType.ID,
                            HideFooter = model.RelationItems[0].HideFooter,
                            HideHeader = model.RelationItems[0].HideHeader,
                            HideFilter = model.RelationItems[0].HideFilter,
                            LookupType = model.RelationItems[0].RelationType,
                            Definition = Newtonsoft.Json.JsonConvert.SerializeObject(lookup)
                        };
                        try
                        {
                            var existing = Company.FieldTypeLookups.Where(i => i.FieldTypeID == model.FieldType.ID).FirstOrDefault();

                            if (existing != null)
                            {
                                Company.FieldTypeLookups.Remove(existing);
                            }

                            Company.FieldTypeLookups.Add(lookupRow);
                            Company.SaveChanges();
                        }
                        catch
                        {
                            throw;
                        }

                        break;
                    #endregion
                    case "OwnershipLookup":
                        #region
                        var ownershipSettings = new
                        {
                            DisplayAssignmentSource = model.OwnershipLookupSettings.DisplayAssignmentSource,
                            ExpandGroupMembership = model.OwnershipLookupSettings.ExpandGroupMembership
                        };
                        var ownershipLookupRow = new FieldTypeLookup
                        {
                            FieldTypeID = model.FieldType.ID,
                            HideFooter = model.OwnershipLookupSettings.HideFooter,
                            HideHeader = model.OwnershipLookupSettings.HideHeader,
                            HideFilter = model.OwnershipLookupSettings.HideFilter,
                            LookupType = 1,
                            Definition = Newtonsoft.Json.JsonConvert.SerializeObject(ownershipSettings)
                        };
                        try
                        {
                            var existing = Company.FieldTypeLookups.Where(i => i.FieldTypeID == model.FieldType.ID).FirstOrDefault();

                            if (existing != null)
                            {
                                Company.FieldTypeLookups.Remove(existing);
                            }

                            model.FieldType.IsDisplayable = true;
                            model.FieldType.IsEditable = false;
                            model.FieldType.IsListable = false;
                            model.FieldType.IsPartOfKey = false;
                            model.FieldType.IsRequired = false;

                            ownershipLookupRow.FieldTypeID = model.FieldType.ID;
                            Company.FieldTypeLookups.Add(ownershipLookupRow);
                            Company.SaveChanges();
                        }
                        catch
                        {
                            throw;
                        }

                        break;
                    #endregion
                    case "Relationship":
                        #region
                        ft.LookupObjectType = model.FieldType.LookupObjectType;
                        ft.LookupObjectID = model.FieldType.LookupObjectID;
                        ft.LookupDisplayFormat = null;
                        ft.LookupEditFormat = null;

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        break;
                    #endregion
                    case "FieldFromRelationship":
                        #region
                        ft.LookupObjectType = model.FieldType.LookupObjectType;
                        ft.LookupObjectID = model.FieldType.LookupObjectID;
                        ft.LookupObjectFieldTypeID = model.FieldType.LookupObjectFieldTypeID;
                        ft.LookupDisplayFormat = null;
                        ft.LookupEditFormat = null;

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        break;
                    #endregion
                    case "RefListRelationship":
                        #region
                        ft.LookupObjectType = model.FieldType.LookupObjectType;
                        ft.LookupObjectID = model.FieldType.LookupObjectID;
                        ft.LookupDisplayFormat = null;
                        ft.LookupEditFormat = null;

                        //Clean up previous stuff
                        if (defs.Count != 0)
                            Company.FieldTypeFusionLookupDefinitions.RemoveRange(defs);
                        if (efli != null)
                            Company.Set<FieldTypeFilteredLookupDefinition>().Remove(efli);
                        break;
                        #endregion
                }

                ft.UpdatedBy = Company.CurrentResourceID;

                bool columnModified = false;
                foreach (System.Reflection.PropertyInfo property in ft.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                {
                    if (property.Name == "Fields" || property.Name == "FieldTypeLookup" || property.Name == "FieldTypeFilteredLookupDefinitions"
                          || property.Name == "UpdatedBy" || property.Name == "FieldTypeFusionLookupDefinitions")
                        continue;

                    object value1 = property.GetValue(ft, null);
                    object value2 = property.GetValue(ftCopy, null);
                    if (!object.Equals(value1, value2))
                    {
                        Company.Entry(ft).Property(property.Name).IsModified = true;
                        columnModified = true;
                    }
                    else
                        Company.Entry(ft).Property(property.Name).IsModified = false;

                }

                if (columnModified)
                    Company.Entry(ft).Property(x => x.UpdatedBy).IsModified = true;

                Company.SaveChanges();



                return jsonSuccess(FormInfo.Edit_FieldType_Confirmation, ft.ID.ToString(), "edit", HttpStatusCode.OK);
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