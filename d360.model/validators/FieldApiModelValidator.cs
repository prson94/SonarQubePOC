using d360.core;
using d360.core.entities;
using d360.core.validators;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace d360.model.validators
{
    public static class FieldApiModelValidator
    {

        public static WorkHttpStatus ValidateModel(FieldTypesApiEditModel model, TypeIdentifierInfoModel actionTypeIdentifierInfoModel, TypeIdentifierInfoModel assetTypeIdentifierInfoModel, TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel, bool areFusionFieldsAllowed = true, List<FieldType> existingFieldTypes = null)
        {
            var baseValidation = BaseModelValidation(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel);


            if (baseValidation.StatusCode != HttpStatusCode.OK)
                return baseValidation;

            bool actionIsReplaceAndKeySelected = (model.Action == FieldTypesApiEditAction.Merge); //If set to merge we can set to true and skip this step.
            bool fieldsHaveErrors = false;
            var fieldsHaveErrorsList = new List<string>();
            List<ValidationResult> validationResults = new List<ValidationResult>();
            bool isValid = true;

            foreach (var field in model.Fields)
            {
                #region Basic field Model validation

                isValid = Validator.TryValidateObject(field, new ValidationContext(field, serviceProvider: null, items: null), validationResults, true);
                if (!isValid)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, $"Invalid Field {validationResults.First().MemberNames.First()}", validationResults.First().ErrorMessage);
                }

                #endregion

                #region Name Validation

                if (!IsFieldNameAllowed(field.Name.Trim()))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid Field Name", $"Name cannot be [{field.Name.Trim().ToUpper()}].");
                }

                #endregion

                #region FriendlyName Validation                

                if (!IsFieldNameAllowed(field.FriendlyName.Trim()))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid Field FriendlyName", $"FriendlyName cannot be [{field.FriendlyName.Trim().ToUpper()}].");
                }

                #endregion

                if (!field.Type.IsOnlyOneTypeModelDefined())
                {
                    fieldsHaveErrors = true;
                    fieldsHaveErrorsList.Add(field.Name);
                }
                if (model.Action == FieldTypesApiEditAction.Replace)
                {
                    if (field.Type.IsPartyOfKey())
                    {
                        actionIsReplaceAndKeySelected = true;
                    }
                }
                if (assetTypeIdentifierInfoModel != null && assetTypeIdentifierInfoModel.Object == SystemObjects.ReferenceItemType.ToString())
                {
                    if (field.Type.IsPartyOfKey())
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field property error", $"Reference item types cannot have field property 'IsPartOfKey' set to true.");
                    }
                }
                if (field.Type.Tag != null)
                {
                    if (actionTypeIdentifierInfoModel != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Asset type error", $"Actions cannot have Tag field type!");
                    }
                    if (relationshipTypeIdentifierInfoModel != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Asset type error", $"Relationships cannot have Tag field type!");
                    }
                    if (assetTypeIdentifierInfoModel != null)
                    {
                        var allowedTypes = new List<string>() { SystemObjects.ArtifactType.ToString(), SystemObjects.PolicyType.ToString(), SystemObjects.TaxonomyType.ToString(), SystemObjects.RuleType.ToString() };
                        if (!allowedTypes.Contains(assetTypeIdentifierInfoModel.Object))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Asset type error", $"Only Artifacts, Policies, Models and Rules are allowed to have Tag field type!");
                        }
                    }

                    if (existingFieldTypes != null)
                    {
                        if (existingFieldTypes.Any(x => x.Type == SystemObjects.Tag.ToString() && x.Name != field.Name))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Asset type error", $"Asset type can have only one Tag field type!");
                        }
                    }

                }
                if (field.Type.JsonElement != null)
                {
                    if (existingFieldTypes != null)
                    {
                        var jsonAttribute = field.Type.JsonElement.JsonAttribute;
                        if (jsonAttribute == null)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"Missing Json attribute definition!");
                        }
                        if (!existingFieldTypes.Any(x => x.Name == jsonAttribute.FieldName && x.Type == "JSON"))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"JSON field {jsonAttribute.FieldName} does not exist or is not part of this asset type!");
                        }
                        var allowedTypes = new List<string>() { "bit", "date", "datetime", "float", "nvarchar", "int", "bigint" };
                        if (!allowedTypes.Contains(jsonAttribute.DataType))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"Invalid Json attribute field type. Allowed values are {string.Join(", ", allowedTypes)}!");
                        }


                    }
                }

                #region Type Min/Max

                if (field?.Type?.Text != null)
                {
                    if (!FieldLengthValid(field.Type.Text.Validation, out string validationErrorMsg))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"{validationErrorMsg}");
                    }
                }

                if (field?.Type?.Html != null)
                {
                    if (!FieldLengthValid(field.Type.Html.Validation, out string validationErrorMsg))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"{validationErrorMsg}");
                    }
                }

                if (field?.Type?.Number != null)
                {
                    if (field.Type.Number.Increment != null && (field.Type.Number.Increment % 1 != 0))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"Increment must be a whole number");
                    }

                    if (field.Type.Number.Validation?.MaximumValue != null && (field.Type.Number.Validation?.MaximumValue % 1) != 0)
                    {                        
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"MaximumValue must be a whole number"); ;
                    }
                    if (field.Type.Number.Validation?.MinimumValue != null && (field.Type.Number.Validation?.MinimumValue % 1) != 0)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field type error", $"MinimumValue must be a whole number"); ;
                    }                    
                }

                #endregion

                if (!areFusionFieldsAllowed && field.Type.ComputedFusionLookup != null)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Field property error", $"Fusion field types are not allowed!");
                }

            }
            if (fieldsHaveErrors)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Fields contain errors", $"The following fields have more than one type defined: {string.Join(", ", fieldsHaveErrorsList)}.");
            }
            if (!actionIsReplaceAndKeySelected)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "No primary key defined", $"You have elected to replace all current fields, yet you have not defined a key. You must define at least one field as a key, or choose Merge as an Action.");
            }
            var duplicateFieldNames = model.Fields.Select(f => f.Name).GroupBy(f => f).Where(f => f.Count() > 1).Select(f => f.Key).ToList();
            if (duplicateFieldNames.Count > 0)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "Duplicate field names", $"The following field names are used more than once: {string.Join(", ", fieldsHaveErrorsList)}. Field names must be unique.");
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        public static WorkHttpStatus ValidateModel(FieldTypesApiDeleteModel model, TypeIdentifierInfoModel actionTypeIdentifierInfoModel, TypeIdentifierInfoModel assetTypeIdentifierInfoModel, TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel)
        {
            return BaseModelValidation(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel);
        }

        public static (WorkHttpStatus, List<string>) FieldValidator(FieldTypesApiDeleteModel model, bool anyExistingItems, List<FieldType> currentFieldTypes)
        {
            var fieldNamesToDelete = model.Fields.Select(i => i.Name).ToList();
            var keyFieldsWillBeDeleted = currentFieldTypes.Any(d => d.IsPartOfKey == true && fieldNamesToDelete.Contains(d.Name));

            if (anyExistingItems && keyFieldsWillBeDeleted)
            {
                return (new WorkHttpStatus(HttpStatusCode.BadRequest, "Existing items in system", $"You may not remove key fields as there are existing items in your environment. You may not perform a Delete action until those items are removed, or you alter the key fields defined on this type."), null);
            }

            var anyInvalidFields = fieldNamesToDelete.Any(f => !currentFieldTypes.Any(c => c.Name == f));
            if (anyInvalidFields)
            {
                return (new WorkHttpStatus(HttpStatusCode.BadRequest, "Invalid fields", $"You are attempting to remove one or more fields that do not exist on this type."), null);
            }

            return (new WorkHttpStatus(HttpStatusCode.OK, "", ""),
                        fieldNamesToDelete);
        }

        private static WorkHttpStatus BaseModelValidation(BaseFieldTypesApiModel model, TypeIdentifierInfoModel actionTypeIdentifierInfoModel, TypeIdentifierInfoModel assetTypeIdentifierInfoModel, TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel)
        {
            if (model == null)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "No model found", "You did not provide a valid model. Please check your request and try again.");
            }

            if (!model.ActionTypeUid.HasValue && !model.AssetTypeUid.HasValue && !model.RelationshipTypeUid.HasValue)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, "No Uid found", "You must provide only one of the three Uid properties: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.");
            }

            if (model.ActionTypeUid.HasValue)
            {

                if (actionTypeIdentifierInfoModel == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound, "Type not found", $"Action Type not found based on Uid provided [{model.ActionTypeUid}].");
                }
            }

            if (model.AssetTypeUid.HasValue)
            {
                if (model.ActionTypeUid.HasValue)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an AssetTypeUid since you have already provided an ActionTypeUid.");
                }
                else
                {
                    if (assetTypeIdentifierInfoModel == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Type not found", $"Asset Type not found based on Uid provided [{model.ActionTypeUid}].");
                    }
                }
            }

            if (model.RelationshipTypeUid.HasValue)
            {
                if (model.ActionTypeUid.HasValue)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an ActionTypeUid.");
                }
                else if (model.AssetTypeUid.HasValue)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, "Parameter error", "You may not provide an RelationshipTypeUid since you have already provided an AssetTypeUid.");
                }
                else
                {

                    if (relationshipTypeIdentifierInfoModel == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, "Type not found", $"Relationship Type not found based on Uid provided [{model.RelationshipTypeUid}].");
                    }
                }
            }
            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private static bool IsFieldNameAllowed(string fieldApiName)
        {
            if (string.IsNullOrEmpty(fieldApiName)) return false;
            List<string> disallowedFieldNames = new List<string> { "id", "uid", "assetid", "assetuid", "assettypeid", "assettypeuid", "createdon", "updatedon" };
            return !disallowedFieldNames.Contains(fieldApiName.ToLower());
        }

        private static bool FieldLengthValid(FieldTypeDescriptionApiViewModel_ValidationLength validation, out string errMsg)
        {
            errMsg = "";
            if (validation != null)
            {
                if (validation?.MaximumLength != null && (validation?.MaximumLength % 1) != 0)
                {
                    errMsg = "MaximumLength must be a whole number";
                    return false;
                }
                if (validation?.MinimumLength != null && (validation?.MinimumLength % 1) != 0)
                {
                    errMsg = "MinimumLength must be a whole number";
                    return false;
                }
            }
            return true;
        }
    }
}
