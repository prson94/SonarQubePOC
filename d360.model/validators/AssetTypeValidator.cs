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

namespace d360.core.validators
{
    public class AssetTypeValidator
    {
        List<AssetTypeClass> PredicateSupportingClasses = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Reference };
        List<AssetTypeClass> ParentAssetTypeClass = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Reference };
        List<AssetTypeClass> SupportedClasses = new List<AssetTypeClass>() { AssetTypeClass.BusinessAsset, AssetTypeClass.TechnicalAsset, AssetTypeClass.Model, AssetTypeClass.Organization, AssetTypeClass.Policy, AssetTypeClass.Reference, AssetTypeClass.Rule };
        string ColorRegex = "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";

        ICompanyContext CompanyContext;
        public AssetTypeValidator(ICompanyContext companyContext, int lineageVersion)
        {
            this.CompanyContext = companyContext;
            if (lineageVersion != 3)
            {
                PredicateSupportingClasses = PredicateSupportingClasses.Where(x => x != AssetTypeClass.TechnicalAsset).ToList();
                ParentAssetTypeClass = ParentAssetTypeClass.Where(x => x != AssetTypeClass.TechnicalAsset).ToList();
                SupportedClasses = SupportedClasses.Where(x => x != AssetTypeClass.TechnicalAsset).ToList();
            }
        }

        public WorkHttpStatus ValidateModel(bool isInsert, AssetTypeInsert model, AssetType parentAssetType, Predicate predicate, AssetType assetType = null)
        {
            if (!SupportedClasses.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, AssetTypeErrors.UnsupportedAssetClass);

            if (string.IsNullOrEmpty(model.Name.Trim()))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidName} {AssetTypeErrors.CheckRequest}");

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

            if (model.ParentUid.HasValue && model.ParentUid != Guid.Empty)
            {
                if (parentAssetType == null)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidParentUid} {AssetTypeErrors.CheckRequest}");
                else if (parentAssetType.Object != SystemObjectHelper.GetSystemObjects(model.Class).ToString())
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidParentUid} {AssetTypeErrors.CheckRequest}");
                else if (!ParentAssetTypeClass.Contains(model.Class))
                    return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.InvalidRequestHttpErrorTitle, $"{AssetTypeErrors.InvalidParentUid} {AssetTypeErrors.CheckRequest}");
            }

            if (!isInsert)
            {
                if (model.ParentUid.HasValue && model.ParentUid == model.Uid)
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
            else if (parentAssetType == null && predicate != null && ParentAssetTypeClass.Contains(model.Class))
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
            if (assetTypeId == 0)
                allowedFieldTokens = new List<string> { "name" };
            else
                allowedFieldTokens = CompanyContext.Filter<FieldType>(x => x.AssetTypeID == assetTypeId && !fieldsToIgnore.Contains(x.Type)).Select(x => x.FriendlyName.ToLower()).ToList();

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

            var field = CompanyContext.FieldTypes.Where(f => f.AssetTypeID == assetType.ID && f.Name.ToLower() == fieldName.ToLower()).SingleOrDefault();

            return (field != null);
        }
    }
}
