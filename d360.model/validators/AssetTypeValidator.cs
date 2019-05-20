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

namespace d360.core.validators
{
    public class AssetTypeValidator
    {
        ICompanyContext CompanyContext;
        public AssetTypeValidator(ICompanyContext companyContext)
        {
            this.CompanyContext = companyContext;
        }

        public WorkHttpStatus ValidateModelForPost(AssetTypeInsert model, AssetType parentAssetType, Predicate predicate)
        {

            List<AssetTypeClass> predicateClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Reference };
            List<AssetTypeClass> parentAssetTypeClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Reference };

            List<AssetTypeClass> supportedClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Model, AssetTypeClass.Organization, AssetTypeClass.Policy, AssetTypeClass.Reference, AssetTypeClass.Rule };
            if (!supportedClass.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "Not supported class type");


            if (string.IsNullOrEmpty(model.Name.Trim()))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "No valid Name provided.Please check your request and try again.");

            if (model.ParentUid.HasValue && model.ParentUid != Guid.Empty)
            {
                if (parentAssetType == null)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Not valid ParentUid provided.Please check your request and try again.");
                else if (parentAssetType.Object != SystemObjectHelper.GetSystemObjects(model.Class).ToString())
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Not valid ParentUid provided.Please check your request and try again.");
                else if (!parentAssetTypeClass.Contains(model.Class))
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Not valid ParentUid for the Class.Please check your request and try again.");
            }


            if (model.Hierarchy != null && model.Hierarchy.PredicateUid.HasValue && model.Hierarchy.PredicateUid != Guid.Empty)
            {
                if (predicate == null)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");
                else if (predicate != null && !predicateClass.Contains(model.Class))
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");

            }



            if (parentAssetType != null && predicate == null && predicateClass.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");
            else if (predicate == null && (model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy))
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");
            else if (parentAssetType == null && predicate != null && parentAssetTypeClass.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Asset Type not found based on Uid provided");
            else if (parentAssetType != null && predicate != null && (model.Class == AssetTypeClass.Glossary || model.Class == AssetTypeClass.Reference) && predicate.Type != PredicateType.InterTypeHierarchy)
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");
            else if (predicate != null && (model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy) && (predicate.Type != PredicateType.IntraTypeHierarchy))
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");



            if (!this.IsValidDisplayFormat(0, model.DisplayFormat, model.Class))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "Display Format contains invalid field references.");

            var regex = "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
            if (model.IconStyle == null || !Regex.Match(model.IconStyle.BackColor, regex, RegexOptions.IgnoreCase).Success || !Regex.Match(model.IconStyle.ForeColor, regex, RegexOptions.IgnoreCase).Success)
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "Not valid Icon Style provided.Please check your request and try again.");

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        public WorkHttpStatus ValidateModelForPut(AssetTypeInsert model, AssetType parentAssetType, Predicate predicate, AssetType assetType)
        {
            List<AssetTypeClass> predicateClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Model, AssetTypeClass.Policy, AssetTypeClass.Reference };
            List<AssetTypeClass> parentAssetTypeClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Reference };

            List<AssetTypeClass> supportedClass = new List<AssetTypeClass>() { AssetTypeClass.Glossary, AssetTypeClass.Model, AssetTypeClass.Organization, AssetTypeClass.Policy, AssetTypeClass.Reference, AssetTypeClass.Rule };
            if (!supportedClass.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "Not supported class type");


            if (string.IsNullOrEmpty(model.Name.Trim()))
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "No valid Name provided.Please check your request and try again.");

            if (assetType == null)
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Asset Type not found based on Uid provided.");
            else if (assetType.Object != SystemObjectHelper.GetSystemObjects(model.Class).ToString())
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Asset Type not found based on Class provided.");
            else
            {
                model.Object = assetType.Object;
                model.ObjectID = assetType.ObjectID;
            }

            if (model.ParentUid != Guid.Empty)
            {
                if (parentAssetType == null)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Not valid ParentUid provided.Please check your request and try again.");
                else if (parentAssetType.Object != model.Object)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Not valid ParentUid provided.Please check your request and try again.");
                else if (!parentAssetTypeClass.Contains(model.Class))
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Not valid ParentUid for the Class.Please check your request and try again.");
            }

            if (model.ParentUid == model.Uid)
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "Not valid ParentUid provided.Please check your request and try again.");

            if (model.Hierarchy != null && model.Hierarchy.PredicateUid != Guid.Empty)
            {
                if (predicate == null)
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class.");
                else if (predicate != null && !predicateClass.Contains(model.Class))
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");

            }

            if (parentAssetType != null && predicate == null && predicateClass.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");
            else if (predicate == null && (model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy))
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");
            else if (parentAssetType == null && predicate != null && parentAssetTypeClass.Contains(model.Class))
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "Asset Type not found based on Uid provided");
            else if (parentAssetType != null && predicate != null && (model.Class == AssetTypeClass.Glossary || model.Class == AssetTypeClass.Reference) && predicate.Type != PredicateType.InterTypeHierarchy)
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");
            else if (predicate != null && (model.Class == AssetTypeClass.Model || model.Class == AssetTypeClass.Policy) && (predicate.Type != PredicateType.IntraTypeHierarchy))
                return new WorkHttpStatus(HttpStatusCode.NotFound, "Invalid request", "You have not provided a proper predicate based on its asset type class");

            int assetCount = CompanyContext.Filter<Asset>(x => x.AssetTypeID == assetType.ID).Count();
            AssetType currentParentType = CompanyContext.GetParentType(assetType.ID, SystemObjectHelper.GetSystemObjects(model.Class));
            if (assetCount != 0 && currentParentType != null && currentParentType.uid != model.ParentUid)
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "Assets already exist with assigned parents. You may not change the parent of this asset type.");

            if (!this.IsValidDisplayFormat(assetType.ID, model.DisplayFormat, model.Class) )
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "Display Format contains invalid field references.");

            var regex = "^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
            if (model.IconStyle == null || !Regex.Match(model.IconStyle.BackColor, regex, RegexOptions.IgnoreCase).Success || !Regex.Match(model.IconStyle.ForeColor, regex, RegexOptions.IgnoreCase).Success)
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid request", "Not valid Icon Style provided.Please check your request and try again.");

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");

        }

        private bool IsValidDisplayFormat(int assetTypeId, string displayFormat, AssetTypeClass assetClass)
        {
            // reference item types with {code} display format are valid
            if((assetClass == AssetTypeClass.Reference) && !string.IsNullOrEmpty(displayFormat) && string.Compare(displayFormat,"{CODE}", true) == 0 )
            {
                return true;
            }

            List<string> fieldNames;
            if (assetTypeId == 0)
                fieldNames = new List<string> { "name" };
            else
                fieldNames = CompanyContext.Filter<FieldType>(x => x.AssetTypeID == assetTypeId).Select(x => x.Name.ToLower()).ToList();

            displayFormat = displayFormat.Replace("}{", "} {");
            var displayFieldNames = displayFormat.Split().Where(x => x.StartsWith("{") && x.EndsWith("}"))
                    .Select(x => x.ToLower().Replace("{", string.Empty).Replace("}", string.Empty))
                    .ToList();
            return !displayFieldNames.Except(fieldNames).Any();
        }
      
    }
}
