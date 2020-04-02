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

namespace d360.core.validators
{
    public class AssetTypeValidator
    {
        List<AssetTypeClass> PredicateSupportingClasses = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Reference, AssetTypeClass.Glossary };
        List<AssetTypeClass> ParentAssetTypeClass = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Reference, AssetTypeClass.Glossary };
        List<AssetTypeClass> SupportedClasses = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Organization, AssetTypeClass.Policy, AssetTypeClass.Reference, AssetTypeClass.Rule, AssetTypeClass.Glossary };
        string ColorRegex = "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";

        ICompanyContext CompanyContext;
        public AssetTypeValidator(ICompanyContext companyContext, int lineageVersion, bool isFusionEnabled)
        {
            this.CompanyContext = companyContext;

            if (isFusionEnabled)
            {
                SupportedClasses.Add(AssetTypeClass.FusionAttribute);
                SupportedClasses.Add(AssetTypeClass.FusionQuery);

                ParentAssetTypeClass.Add(AssetTypeClass.FusionAttribute);
            }

        }

        public WorkHttpStatus ValidateModel(bool isInsert, AssetTypeUpsert model, AssetType parentAssetType, Predicate predicate, AssetType assetType = null)
        {
            if (!SupportedClasses.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.UnsupportedAssetClass);

            if (string.IsNullOrEmpty(model.Name) || model.Name.Trim() == string.Empty)
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidName} {AssetTypeErrors.CheckRequest}");

            if (string.IsNullOrEmpty(model.DisplayFormat) || model.DisplayFormat.Trim() == string.Empty)
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidDisplayFormat} {AssetTypeErrors.CheckRequest}");

            #region Basic Model Validation

            List<ValidationResult> validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, new ValidationContext(model, serviceProvider: null, items: null), validationResults, true);
            if (!isValid)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, validationResults.First().ErrorMessage);
            }

            #endregion

            if (!isInsert)
            {
                var anyDupeNames = CompanyContext.Any<AssetType>(x => x.Name == model.Name && x.Class == model.Class && x.uid != model.Uid);
                if (anyDupeNames)
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ErrorNameTaken);

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
                else if (parentAssetType.Object != SystemObjectHelper.GetSystemObjects(model.Class).ToString())
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
            }

            if (parentAssetType != null && predicate == null && PredicateSupportingClasses.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ImproperPredicate);
            else if (predicate == null && (model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy))
                return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.ImproperPredicate);
            else if (isInsert && parentAssetType == null && predicate != null && ParentAssetTypeClass.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.NotFoundBasedOnUid);
            else if (parentAssetType != null && predicate != null && model.Class.In(AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Reference) && predicate.Type != PredicateType.InterTypeHierarchy)
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

            if (!this.IsValidDisplayFormat(isInsert ? 0 : assetType.ID, model.DisplayFormat, model.Class))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.BadDisplayFormat);

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

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private bool IsValidDisplayFormat(int assetTypeId, string displayFormat, AssetTypeClass assetClass)
        {
            // reference item types with {code} display format are valid
            if ((assetClass == AssetTypeClass.Reference) && !string.IsNullOrEmpty(displayFormat) && string.Compare(displayFormat, "{CODE}", true) == 0)
            {
                return true;
            }

            var fieldsToIgnore = DataType.Text.GetNonDisplayFormatFields();

            List<string> allowedFieldTokens;
            if (assetTypeId == 0 || assetClass == AssetTypeClass.FusionAttribute)
                allowedFieldTokens = new List<string> { "name" };
            else
                allowedFieldTokens = CompanyContext.Filter<FieldType>(x => x.AssetTypeID == assetTypeId && !fieldsToIgnore.Contains(x.Type)).Select(x => x.Name.ToLower()).ToList();

            if (assetClass == AssetTypeClass.Reference)
                allowedFieldTokens.Add("code");

            var regex = new Regex(@"\{.*?\}");
            var tokens = regex.Matches(displayFormat);
            foreach (var token in tokens)
            {
                var tokenString = token.ToString().ToLower();
                tokenString = tokenString.Substring(1, tokenString.Length - 2);
                if (!allowedFieldTokens.Contains(tokenString))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsValidOrderByFieldForGetAssets(Guid uid, IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if (!(queryParams.Any(p => p.Key.Trim().ToLower() == "_order")))
                return true;

            var assetType = CompanyContext.AssetTypes.FirstOrDefault(t => t.uid == uid);
            if (assetType == null)
                return false;

            var fieldName = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "_order").Value;

            string[] validFields = { "name", "sourceid", "textpath", "code" };

            if (assetType.Object == "FusionAttributeType")
            {
                var valid = validFields.Contains(fieldName.Trim().ToLower());
                if (valid) return true;
            }

            var doesOrderFieldExists = CompanyContext.FieldTypes.Any(f => f.AssetTypeID == assetType.ID && f.Name.ToLower() == fieldName.ToLower());
            List<string> defaultAssetFields = new List<string>() { "createdon", "updatedon", "assetid" };

            if (assetType.Object == SystemObjects.ReferenceItemType.ToString())
                defaultAssetFields.Add("code");

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

        public bool IsValidOwnersGetAssets(IEnumerable<KeyValuePair<string, string>> queryParams)
        {
            if(queryParams.Any(x => x.Key.Trim().ToLower() == "_ownedby")) {
                string[] owners = queryParams.FirstOrDefault(x => x.Key.Trim().ToLower() == "_ownedby").Value.Split(',');
                foreach(var owner in owners)
                {
                    if (!Guid.TryParse(owner, out Guid ownerguid))
                        return false;
                    if(!CompanyContext.Assets.Any(a => a.uid == ownerguid && (a.Object == SystemObjects.Group.ToString() || a.Object == SystemObjects.Resource.ToString())))
                        return false;
                }
            }

            return true;
        }

        public bool IsValidRelationFilter(IEnumerable<KeyValuePair<string,string>> queryParams)
        {
            if(queryParams.ToList().Any(k => k.Key.ToLower() == "_predicateuid") && queryParams.ToList().Any(k => k.Key.ToLower() == "_relationfilter"))
            {
                return false;
            }
            return true;
        }
    }
}
