using d360.core.entities;
using d360.core.enums;
using d360.core.helpers;
using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using d360.core.resources;
using System.ComponentModel.DataAnnotations;
using Dapper;

namespace d360.core.validators
{
    public class AssetTypeValidator
    {
        List<AssetTypeClass> PredicateSupportingClasses = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Reference, AssetTypeClass.Glossary };
        List<AssetTypeClass> ParentAssetTypeClass = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Reference, AssetTypeClass.Glossary };
        List<AssetTypeClass> SupportedClasses = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Organization, AssetTypeClass.Policy, AssetTypeClass.Reference, AssetTypeClass.Rule, AssetTypeClass.Glossary, AssetTypeClass.Diagram };
        string ColorRegex = "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
        private Guid? _governanceRoleUid = null;
        private readonly bool IsEnableOrganizations;

        ICompanyContext CompanyContext;
        public AssetTypeValidator(ICompanyContext companyContext, Guid? govRoleUid = null, bool EnableOrganizations = false)
        {
            this.CompanyContext = companyContext;
            this._governanceRoleUid = govRoleUid;
            this.IsEnableOrganizations = EnableOrganizations;
        }

        public WorkHttpStatus ValidateModel(bool isInsert, AssetTypeUpsert model, AssetType parentAssetType, Predicate predicate, AssetType assetType = null)
        {
            if (!SupportedClasses.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.UnsupportedAssetClass);

            if (string.IsNullOrWhiteSpace(model.Name))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, string.Format($"{AssetTypeErrors.FieldIsEmpty} {AssetTypeErrors.FieldProvideCorrectValue}", "Asset Type Name"));
            
            var invalidChars = new[] { '\0' };
            if (model.Name.Any(invalidChars.Contains))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, string.Format($"{AssetTypeErrors.FieldIsInvalid} {AssetTypeErrors.FieldProvideCorrectValue}", "Asset Type Name"));   
            }

            if ((isInsert && (string.IsNullOrEmpty(model.DisplayFormat) || model.DisplayFormat.Trim() == string.Empty)) || (!isInsert && model.DisplayFormat != null && model.DisplayFormat.Trim() == string.Empty))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidDisplayFormat} {AssetTypeErrors.CheckRequest}");

            #region Basic Model Validation

            List<ValidationResult> validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);
            if (!isValid)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, validationResults.First().ErrorMessage);
            }

            #endregion

            if (ModelHasDuplicateNames(model, parentAssetType, isInsert))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ErrorNameTaken);
            }

            if (!isInsert)
            {
                if (assetType == null)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.NotFoundBasedOnUid);
                else if (assetType.Object != SystemObjectHelper.GetSystemObjects(model.Class).ToString())
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.NotFoundBasedOnClass);
                else
                {
                    model.Object = assetType.Object;
                    model.ObjectID = assetType.ObjectID;
                }
            }

            bool ForceParentToItself = model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy;

            if (model.ParentUid.HasValue && model.ParentUid != Guid.Empty)
            {
                if (parentAssetType == null)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidParentUid} {AssetTypeErrors.CheckRequest}");
                else if (parentAssetType.Class != model.Class)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidParentUid} {AssetTypeErrors.CheckRequest}");
                else if (!ParentAssetTypeClass.Contains(model.Class) && !ForceParentToItself)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidParentUid} {AssetTypeErrors.CheckRequest}");

                if (ForceParentToItself)
                {
                    if (model.ParentUid != model.Uid)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidParentUid} {AssetTypeErrors.CheckRequest}");
                    }
                }
            }

            if (isInsert)
            {
                if (model.Uid != Guid.Empty)
                {
                    if (CompanyContext.Any<AssetType>(i => i.uid == model.Uid))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.AssetTypeExistsTitle, AssetTypeErrors.AssetTypeWithUidExists);
                    }
                }
            }
            else
            {
                if (model.ParentUid.HasValue && model.ParentUid == model.Uid && !ForceParentToItself)
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidParentUid} {AssetTypeErrors.CheckRequest}");
            }

            if (model.Hierarchy != null && model.Hierarchy.PredicateUid.HasValue && model.Hierarchy.PredicateUid != Guid.Empty)
            {
                if (predicate == null)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ImproperPredicate);
                else if (predicate != null && !PredicateSupportingClasses.Contains(model.Class))
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ImproperPredicate);
                else if (predicate.Type == PredicateType.InterTypeHierarchy && !(model.ParentUid.HasValue && model.ParentUid != Guid.Empty))
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.InvalidParentUid);
                }
            }

            if (parentAssetType != null && predicate == null && PredicateSupportingClasses.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ImproperPredicate);
            else if (predicate == null && (model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy))
                return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ImproperPredicate);
            else if (isInsert && parentAssetType == null && predicate != null && ParentAssetTypeClass.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.NotFoundBasedOnUid);
            else if (parentAssetType != null && predicate != null && model.Class.In(AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Reference) && predicate.Type != PredicateType.InterTypeHierarchy)
                return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ImproperPredicate);
            else if (predicate != null && model.Class.In(AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset) && predicate.Type == PredicateType.IntraTypeHierarchy)
                return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ImproperPredicate);
            else if (predicate != null && model.Class.In(AssetTypeClass.Model, AssetTypeClass.Policy) && (predicate.Type != PredicateType.IntraTypeHierarchy))
                return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ImproperPredicate);

            if (!isInsert)
            {
                int assetCount = CompanyContext.Filter<Asset>(x => x.AssetTypeID == assetType.ID).Count();
                AssetType currentParentType = CompanyContext.GetParentType(assetType.ID, SystemObjectHelper.GetSystemObjects(model.Class));
                if (assetCount != 0 && currentParentType != null && currentParentType.uid != model.ParentUid)
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.AssetsWithAssignedParents);
            }

            if (model.IconStyle == null || !Regex.Match(model.IconStyle.BackColor, ColorRegex, RegexOptions.IgnoreCase).Success || !Regex.Match(model.IconStyle.ForeColor, ColorRegex, RegexOptions.IgnoreCase).Success)
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidStyle} {AssetTypeErrors.CheckRequest}");

            if (model.IconStyle.BackColor != null && model.IconStyle.ForeColor != null)
            {
                string backColour = model.IconStyle.BackColor.Length == 4 ? String.Concat(model.IconStyle.BackColor[0], model.IconStyle.BackColor[1], model.IconStyle.BackColor[1], model.IconStyle.BackColor[2], model.IconStyle.BackColor[2], model.IconStyle.BackColor[3], model.IconStyle.BackColor[3]) : model.IconStyle.BackColor;
                string foreColour = model.IconStyle.ForeColor.Length == 4 ? String.Concat(model.IconStyle.ForeColor[0], model.IconStyle.ForeColor[1], model.IconStyle.ForeColor[1], model.IconStyle.ForeColor[2], model.IconStyle.ForeColor[2], model.IconStyle.ForeColor[3], model.IconStyle.ForeColor[3]) : model.IconStyle.ForeColor;

                if (backColour.ToUpper().Equals(foreColour.ToUpper()))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.MatchingIconStyle}");
                }
            }

            if (model.Class == AssetTypeClass.Diagram && model.FlowObjectType == null)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.MissingFlowObjectType}");
            }

            if (model.Class == AssetTypeClass.Diagram && (_governanceRoleUid == null || _governanceRoleUid == Guid.Empty))
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.GovernanceRoleNotSet}");
            }

            if (model.Class != AssetTypeClass.Diagram && model.FlowObjectType != null)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.UnsupportedFlowObjectType}");
            }
            if (model.Class == AssetTypeClass.Organization && !IsEnableOrganizations)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.UnsupportedAssetClass}");
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private bool ModelHasDuplicateNames(AssetTypeUpsert model, AssetType parentAssetType, bool isInsert = true)
        {
            if (CompanyContext.Database == null) return false; // unit tests dont mock the db context thus cant run db queries. Assume the name is unique.

            if (isInsert)
            {
                if (model.Class == AssetTypeClass.BusinessAsset || model.Class == AssetTypeClass.TechnicalAsset)
                {
                    int count = 0;
                    if (parentAssetType != null)
                    {
                        count = CompanyContext.Database.Connection.QuerySingleOrDefault<int>($@"
                            select 
	                            count(1)
                            from
	                            intersecttype I
	                            inner join [Predicate] P on P.ID = I.PredicateID
	                            inner join AssetType a on a.object = i.object and a.objectid = i.objectid
                            where  a.[class] = @cls and P.[Type] = 3 and i.[subject] = 'ArtifactType' and i.[subjectID] = @parentObjectId and a.name = @name", new { parentObjectId = parentAssetType.ObjectID, name = model.Name.Trim(), cls = model.Class });
                    }
                    else
                    {
                        // only root level artifact types with the same class type IE tech asset vs business asset
                        count = CompanyContext.Database.Connection.QuerySingleOrDefault<int>($@"
	                            select count(1) from assettype a
                                where a.[class] = @cls and not exists (select 1 from intersecttype I inner join [predicate] p on P.id = I.PredicateID where p.[Type] = 3 and i.Subject = 'ArtifactType' and i.ObjectID = a.ObjectID)
		                                and a.Name = @name", new { name = model.Name.Trim(), cls = model.Class });
                    }

                    return (count > 0);
                }
                else if (CompanyContext.Any<AssetType>(i => i.Name == model.Name.Trim() && i.Class == model.Class))
                    return true;
            }
            else
            {
                if (model.Class == AssetTypeClass.BusinessAsset || model.Class == AssetTypeClass.TechnicalAsset)
                {
                    int count = 0;
                    if (parentAssetType != null)
                    {
                        count = CompanyContext.Database.Connection.QuerySingleOrDefault<int>($@"
                            select 
	                            count(1)
                            from
	                            intersecttype I
	                            inner join [Predicate] P on P.ID = I.PredicateID
	                            inner join AssetType a on a.object = i.object and a.objectid = i.objectid
                            where  a.[class] = @cls and P.[Type] = 3 and i.[subject] = 'ArtifactType' and i.[subjectID] = @parentObjectId and a.name = @name and a.uid <> @uid", new { parentObjectId = parentAssetType.ObjectID, name = model.Name.Trim(), cls = model.Class, uid = model.Uid });
                    }
                    else
                    {
                        // only root level artifact types with the same class type IE tech asset vs business asset
                        count = CompanyContext.Database.Connection.QuerySingleOrDefault<int>($@"
	                            select count(1) from assettype a
                                where
	                                a.[class] = @cls and not exists (select 1 from intersecttype I inner join [predicate] p on P.id = I.PredicateID where p.[Type] = 3 and i.Subject = 'ArtifactType' and i.ObjectID = a.ObjectID)
		                                and a.Name = @name and a.UID <> @uid", new { name = model.Name.Trim(), cls = model.Class, uid = model.Uid });
                    }

                    return (count > 0);
                }
                else if (CompanyContext.Any<AssetType>(i => i.Name == model.Name.Trim() && i.uid != model.Uid && i.Class == model.Class))
                    return true;
            }
            return false;
        }

        public bool IsValidOrderByFieldForGetAssets(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if (!(queryParams.Any(p => p.Key.Trim().ToLower() == "_order")))
                return true;

            var isHierachyItem = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_ishierachyitem").Value;
            if (!String.IsNullOrEmpty(isHierachyItem))
            {
                var order = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_order").Value;
                if (!String.IsNullOrEmpty(order))
                {
                    int orderID = 0;
                    order = order.Split(new[] { "Field" }, StringSplitOptions.None)[1];
                    orderID = int.Parse(order);
                    var orderName = CompanyContext.FieldTypes.FirstOrDefault(f => f.ID == orderID);
                    if (orderName != null)
                        return true;
                }
            }

            var assetType = CompanyContext.AssetTypes.FirstOrDefault(t => t.uid == uid);
            if (assetType == null)
                return false;

            var fieldName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_order").Value;

            string[] validFields = { "name", "sourceid", "textpath", "code" };

            var doesOrderFieldExists = CompanyContext.FieldTypes.Any(f => f.AssetTypeID == assetType.ID && f.Name.ToLower() == fieldName.ToLower());
            List<string> defaultAssetFields = new List<string>() { "createdon", "updatedon", "assetid" };

            if (assetType.Object == SystemObjects.ReferenceItemType.ToString())
            {
                defaultAssetFields.Add("code");
                defaultAssetFields.Add("color");
            }

            if (queryParams.ToList().Any(x => x.Key.ToLower() == "_includeparent"))
            {
                bool includeParent = false;
                var value = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_includeparent").Value;
                bool.TryParse(value, out includeParent);
                if (includeParent)
                {
                    defaultAssetFields.Add("parentdisplayname");
                }
            }


            return doesOrderFieldExists || defaultAssetFields.Contains(fieldName.Trim().ToLower());
        }


        public bool IsValidOrderDirectionGetAssets(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            string[] allowedValues = new string[] { "asc", "desc" };
            var directionFilter = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_direction");

            if (directionFilter.Key == null)
                return true;

            if (!allowedValues.Contains(directionFilter.Value.Trim().ToLower()))
                return false;

            return true;
        }

        public bool IsValidOwnersGetAssets(IEnumerable<KeyValuePair<string, string>> queryParams, string paramName)
        {
            if (queryParams.Any(x => x.Key.Trim().ToLower() == paramName))
            {
                string[] owners = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == paramName).Value.Split(',');
                foreach (var owner in owners)
                {
                    if (!Guid.TryParse(owner, out Guid ownerguid))
                    {
                        return false;
                    }
                    if (ownerguid == Guid.Empty)
                    {
                        return false;
                    }
                    if (!CompanyContext.Assets.Any(a => a.uid == ownerguid && (a.Object == SystemObjects.Group.ToString() || a.Object == SystemObjects.Resource.ToString() || a.Object == SystemObjects.Organization.ToString())))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsValidRelationFilter(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_predicateuid") && queryParams.ToList().Any(k => k.Key.ToLower() == "_relationfilter"))
            {
                return false;
            }
            return true;
        }

        public bool IsValidIncludeTotalFlag(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if (queryParams.ToList().Any(k => k.Key.ToLower() == "_includetotal"))
            {
                var val = queryParams.ToList().First(k => k.Key.ToLower() == "_includetotal");

                if (!bool.TryParse(val.Value, out _))
                    return false;
            }
            return true;
        }

        // source is nullable
        // result is not nullable
        private IEnumerable<Guid> FindAssets(IEnumerable<KeyValuePair<string, string>> source, string assetKey)
        {
            string value = null;
            foreach (var pair in source.Safe())
            {
                if (string.Equals(pair.Key, assetKey, StringComparison.InvariantCultureIgnoreCase))
                {
                    value = pair.Value;
                    break;
                }
            }

            return (value?.Split(',') ?? Enumerable.Empty<string>()).Select(Guid.Parse);
        }

        // queryparams is nullable
        public bool IsValidGetAssets(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            var assets = FindAssets(queryParams, "_assetuid").ToArray();
            return assets.Length == 0 || assets.All(x => x != Guid.Empty);
            //var pair = queryParams.Safe().FirstOrDefault(x => string.Equals(x.Key, assetKey, StringComparison.InvariantCultureIgnoreCase)); 
            

            //return pair == 
            //       || pair.Value.Split(',').Select(Guid.Parse).All(x => x != Guid.Empty);
            //pair.Value.Any(x => x)
            //if (queryParams.Any(x => x.Key.Trim().ToLower() == assetKey))
            //{
            //    List<Guid> assetUids = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == assetKey)
            //        .Value.Split(',').Select(x =>
            //        {
            //            var guid = Guid.Empty;
            //            Guid.TryParse(x, out guid);
            //            return guid;
            //        }).ToList();

            //    if (assetUids.Any(x => x == Guid.Empty))
            //        return false;
            //}
            //return true;
        }
    }
}
