using d360.core;
using d360.core.entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using d360.core.resources;
using System.Text.RegularExpressions;

namespace d360.model.validators
{
    public static class FieldApiModelValidator
    {

        public static WorkHttpStatus ValidateModel(FieldTypesApiEditModel model, TypeIdentifierInfoModel actionTypeIdentifierInfoModel, TypeIdentifierInfoModel assetTypeIdentifierInfoModel, TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel, List<FieldType> existingFieldTypes = null, List<Tuple<string, Guid>> ExistingIntersectID = null, bool isJsonAttributeFieldTypeEnabled = true)
        {
            var baseValidation = BaseModelValidation(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel);


            if (baseValidation.StatusCode != HttpStatusCode.OK)
            {
                return baseValidation;
            }

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
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, string.Format(FieldErrors.InvalidField, validationResults.First().MemberNames.First()), validationResults.First().ErrorMessage);
                }

                #endregion

                #region Name Validation

                if (!IsFieldNameAllowed(field.Name.Trim(), relationshipTypeIdentifierInfoModel != null, assetTypeIdentifierInfoModel))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.InvalidFieldName, string.Format(FieldErrors.NameCannotBe, field.Name.Trim().ToUpper()));
                }

                #endregion

                #region FriendlyName Validation                

                if (!IsFieldNameAllowed(field.FriendlyName.Trim(), assetTypeIdentifierInfoModel: assetTypeIdentifierInfoModel))
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.InvalidFieldFriendlyName, string.Format(FieldErrors.FriendNameCannotBe, field.FriendlyName.Trim().ToUpper()));
                }

                #endregion

                if (field.Type == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.TypeObjectMissing);
                }


                if (!field.Type.IsOnlyOneTypeModelDefined())
                {
                    fieldsHaveErrors = true;
                    fieldsHaveErrorsList.Add(field.Name);
                }
                if (model.Action == FieldTypesApiEditAction.Replace)
                {
                    if (field.Type.IsPartOfKey())
                    {
                        actionIsReplaceAndKeySelected = true;
                    }
                }

                if (!isJsonAttributeFieldTypeEnabled && field.Type.JsonElement != null)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.JsonElementFieldTypeNotenabled);
                }

                if (assetTypeIdentifierInfoModel != null && assetTypeIdentifierInfoModel.Object == SystemObjects.ReferenceItemType.ToString())
                {
                    if (field.Type.IsPartOfKey())
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest,FieldErrors.FieldPropertyError,FieldErrors.ReferenceItemIsPartOfKeyNotAllowedTrue);
                    }

                    if (field.Type.Json != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError,FieldErrors.ReferenceListNotSupportJson);
                    }
                    else if (field.Type.Tag != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.ReferenceListNotSupportTag);
                    }
                    else if (field.Type.JsonElement != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError,FieldErrors.ReferenceItemNotSupportJsonElement);
                    }
                }

                if (relationshipTypeIdentifierInfoModel != null)
                {
                    if (field.Type.IsPartOfKey())
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError ,FieldErrors.RelationNotAllowedIsPartyOFKeyTrue);
                    }
                }

                if (actionTypeIdentifierInfoModel != null)
                {
                    if (field.Type.IsPartOfKey())
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError,FieldErrors.ActionNotAllowedIsPartyOFKeyTrue);
                    }
                }

                if (field.Type.Path != null)
                {
                    if (actionTypeIdentifierInfoModel != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError,FieldErrors.ActionNotSupportPath);
                    }
                    if (relationshipTypeIdentifierInfoModel != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError,FieldErrors.RelationshipNotSupportPath);
                    }
                    if (assetTypeIdentifierInfoModel != null)
                    {
                        var restrictedTypes = new List<string>() {
                            SystemObjects.OrganizationType.ToString(),
                            SystemObjects.ResourceType.ToString()
                        };
                        if (restrictedTypes.Contains(assetTypeIdentifierInfoModel.Object))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError,FieldErrors.ThisAssetTypeNotSupportPath);
                        }
                    }
                }

                #region IsDisplayable   
                if (field.Type.ComputedRelationshipLookup != null)
                {
                    if (field.Type.ComputedRelationshipLookup.IsDisplayable == false)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, string.Format(FieldErrors.IsDisplayAbleTrueRelationshipLookup, field.FriendlyName));
                    }
                }

                if (field.Type.ComputedRelationshipReferenceList != null)
                {
                    if (field.Type.ComputedRelationshipReferenceList.IsDisplayable == false)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, string.Format(FieldErrors.IsDisplayAbleTrueReferenceItemListForRelationship, field.FriendlyName));
                    }
                }
                #endregion

                #region isPartOfKey
                if (field.Type.IsPartOfKey() == true && assetTypeIdentifierInfoModel != null)
                {
                    if (assetTypeIdentifierInfoModel.Object == SystemObjects.ResourceType.ToString() || (assetTypeIdentifierInfoModel.Object == SystemObjects.OrganizationType.ToString() && field.Name.ToLower() != "name"))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.AssetTypeNotHaveKeyField);
                    }
                }

                #endregion

                if (field.Type.JsonElement != null)
                {
                    if (existingFieldTypes != null)
                    {
                        var jsonAttribute = field.Type.JsonElement.JsonAttribute;
                        if (jsonAttribute == null)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.MissingJsonAttribute);
                        }
                        if (!existingFieldTypes.Any(x => x.Name == jsonAttribute.FieldName && x.Type == "JSON"))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, string.Format(FieldErrors.JsonFiledNotPartAssetType, jsonAttribute.FieldName));
                        }
                        var allowedTypes = new List<string>() { "bit", "date", "datetime", "float", "nvarchar", "int", "bigint" };
                        if (!allowedTypes.Contains(jsonAttribute.DataType))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, string.Format(FieldErrors.InvalidJsonFieldType, string.Join(", ", allowedTypes)));
                        }


                    }
                }

                if (field.Type.Score != null)
                {
                    if (actionTypeIdentifierInfoModel != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError,FieldErrors.ActionNotAllowedScoreFieldType);
                    }
                    if (relationshipTypeIdentifierInfoModel != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.RelationshipNotAllowedScoreFieldType);
                    }
                    if (assetTypeIdentifierInfoModel != null)
                    {
                        var restrictedTypes = new List<string>() {
                            SystemObjects.OrganizationType.ToString(),
                            SystemObjects.ReferenceItemType.ToString(),
                            SystemObjects.ResourceType.ToString()
                        };
                        if (restrictedTypes.Contains(assetTypeIdentifierInfoModel.Object))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.AssetTypeNotHaveScoreField);
                        }
                    }
                }

                if (field.Type.Tag != null)
                {
                    if (actionTypeIdentifierInfoModel != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.AssetTypeError, FieldErrors.ActionNotAllowedTagFieldType);
                    }
                    if (relationshipTypeIdentifierInfoModel != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.AssetTypeError, FieldErrors.RelationshipsNotAllowedTagFieldType);
                    }
                    if (assetTypeIdentifierInfoModel != null)
                    {
                        var allowedTypes = new List<string>() { SystemObjects.ArtifactType.ToString(), SystemObjects.PolicyType.ToString(), SystemObjects.TaxonomyType.ToString(), SystemObjects.RuleType.ToString(), SystemObjects.TaskType.ToString() };
                        if (!allowedTypes.Contains(assetTypeIdentifierInfoModel.Object))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.AssetTypeError, FieldErrors.SpecificHaveTagFieldType);
                        }
                    }

                    if (existingFieldTypes != null)
                    {
                        if (existingFieldTypes.Any(x => x.Type == SystemObjects.Tag.ToString() && x.Name != field.Name))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.AssetTypeError, FieldErrors.OnlyOneTagFieldAllowed);
                        }
                    }

                }

                if (model.AssetTypeUid.HasValue && field.Type.Relationship != null)
                {
                    if (ExistingIntersectID != null)
                    {
                        if (ExistingIntersectID.Count() > 0)
                        {
                            var duplicateFieldIntersectTypeUid1 = ExistingIntersectID.Where(f => f.Item1 != field.Name && f.Item2 == field.Type.Relationship.IntersectTypeUid).Select(f => f.Item1).ToList();
                            if (duplicateFieldIntersectTypeUid1.Count > 0)
                            {
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.DuplicateRelationship, $"The following relationship ID are used more than once: {field.Type.Relationship.IntersectTypeUid}. Relationship must be unique on same assettype");

                            }
                        }
                    }

                    if (field.Type.Relationship.IsEditable == false && field.Type.Relationship.Description.Form.Trim().Length > 0)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"Field {field.FriendlyName}. Form description must be empty for Relationship field when IsEditable set to false.");
                    }

                }

                if (field?.Type?.Boolean != null)
                {
                    if (actionTypeIdentifierInfoModel != null && field.Type.Boolean.IsListable == true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"Field {field.FriendlyName}. IsListable parameter value must be false for boolean type field defined for action type!");
                    }
                    if (actionTypeIdentifierInfoModel != null && field.Type.Boolean.IsPrimaryFilter == true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"Field {field.FriendlyName}. IsPrimaryFilter(Show As Top Level Filter) parameter value must be false for boolean type field defined for action type!");
                    }
                }

                if (!string.IsNullOrEmpty(field?.Type?.Text?.Validation?.Pattern))
                {
                    try
                    {
                        new Regex(field.Type.Text.Validation.Pattern);
                    }
                    catch (Exception)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"Validation pattern is not valid Regex expression!");
                    }
                }

                if (field.Type.Link != null)
                {
                    if (field.Type.Link.IsPartOfKey == true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest,FieldErrors.FieldPropertyError, $"Link Types cannot have field property IsPartOfKey on field {field.FriendlyName} set to true.");
                    }
                }

                #region Type Min/Max

                if (field?.Type?.Text != null)
                {
                    if (!FieldLengthValid(field.Type.Text.Validation, out string validationErrorMsg))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"{validationErrorMsg}");
                    }
                }

                if (field?.Type?.Html != null)
                {
                    if (!FieldLengthValid(field.Type.Html.Validation, out string validationErrorMsg))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"{validationErrorMsg}");
                    }
                }

                if (field?.Type?.Number != null)
                {
                    if (field.Type.Number.Increment != null && (field.Type.Number.Increment % 1 != 0))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, String.Format(FieldErrors.WholeNumberError, "Increment"));
                    }

                    if (field.Type.Number.Validation?.MaximumValue != null && (field.Type.Number.Validation?.MaximumValue % 1) != 0)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, String.Format(FieldErrors.WholeNumberError, "MaximumValue"));
                    }

                    if (field.Type.Number.Validation?.MinimumValue != null && (field.Type.Number.Validation?.MinimumValue % 1) != 0)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, String.Format(FieldErrors.WholeNumberError, "MinimumValue"));
                    }

                    if (!FieldLengthValue(field.Type.Number.Validation, out string validationErrorMsg, field.Type.Number.DefaultValue))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"{validationErrorMsg}");
                    }
                }

                if (field?.Type?.Decimal != null)
                {
                    if (!FieldLengthValue(field.Type.Decimal.Validation, out string validationErrorMsg, field.Type.Decimal.DefaultValue))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"{validationErrorMsg}");
                    }
                }

                #endregion


                if (assetTypeIdentifierInfoModel != null && field?.Type?.Json != null)
                {
                    if (field.Type.Json.Validation != null)
                    {
                        if (field.Type.Json.Validation.IsRequired)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"IsRequired property can not be true for JSON field types defined on this asset type!");
                        }
                    }
                }

                if (field.Type.Counter != null)
                {
                    if (field.Type.Counter.IsEditable == true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"IsEditable cannot be true for this field type.");
                    }

                    if (!string.IsNullOrEmpty(field.Type.Counter.CounterPrefix))
                    {
                        var value = field.Type.Counter.CounterPrefix.Trim();
                        field.Type.Counter.CounterPrefix = value;

                        if (value.Length > 10)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"Counter Prefix cannot be longer than 10 characters.");
                        }

                        var match = Regex.Matches(value, "[a-zA-Z0-9-_]");
                        if (match.Count != value.Length)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"Counter Prefix must be consisted of alphanumericals and/or symbols _ or -.");
                        }

                        if (!Regex.IsMatch(value[0].ToString(), "[a-zA-Z]"))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"Counter Prefix must start with a character.");
                        }
                    }

                    if (field.Type.Counter.CounterInitialIndex.HasValue && (field.Type.Counter.CounterInitialIndex.Value <= 0 || field.Type.Counter.CounterInitialIndex.Value > 9999999))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"Counter Initial Value must be between 1 and 9999999.");
                    }

                    var allowedTypes = new List<string> {
                            SystemObjects.ArtifactType.ToString(),
                            SystemObjects.PolicyType.ToString(),
                            SystemObjects.RuleType.ToString(),
                            SystemObjects.TaxonomyType.ToString()
                        };
                    if (assetTypeIdentifierInfoModel == null || !allowedTypes.Contains(assetTypeIdentifierInfoModel.Object))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"This asset type may not have a Counter field type!");
                    }
                }

                if (field.Type.ComputedOwnershipLookup != null)
                {
                    if (field.Type.ComputedOwnershipLookup.DisplayInColumn == true && field.Type.ComputedOwnershipLookup.Definition.DisplayAsList != true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, $"DisplayInColumn on ComputedOwnershipLookup can be set to True only if DisplayAsList is set to True.");
                    }
                }

                //Diagram asset type validators
                if (assetTypeIdentifierInfoModel != null && assetTypeIdentifierInfoModel.Object == SystemObjects.TaskType.ToString())
                {

                    if (field.Type.ComputedOwnershipLookup != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"ComputedOwnershipLookup fields are not allowed for current Asset Type!");
                    }

                    if (field.Type.ComputedRelationshipField != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"ComputedRelationshipField fields are not allowed for current Asset Type!");
                    }

                    if (field.Type.ComputedRelationshipLookup != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"ComputedRelationshipLookup fields are not allowed for current Asset Type!");
                    }

                    if (field.Type.ComputedRelationshipReferenceList != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"ComputedRelationshipReferenceList fields are not allowed for current Asset Type!");
                    }

                    if (field.Type.Json != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"Json fields are not allowed for current Asset Type!");
                    }

                    if (field.Type.JsonElement != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"JsonElement fields are not allowed for current Asset Type!");
                    }

                    if (field.Type.Relationship != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"Relationship fields are not allowed for current Asset Type!");
                    }

                    if (field.Type.Score != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"Score fields are not allowed for current Asset Type!");
                    }

                    if (field.Type.Text != null && field.Name == "Name")
                    {

                        var message = "Task Types cannot have field property '{0}' on field Name set to {1}.";
                        var ft = field.Type.Text;
                        if (ft.IsDisplayable == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsDisplayable", "false"));
                        }
                        if (ft.IsEditable == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsEditable", "false"));
                        }
                        if (ft.IsListable == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsListable", "false"));
                        }
                        if (ft.IsPartOfKey == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsPartOfKey", "false"));
                        }
                        if (ft.Validation.IsRequired == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsRequired", "false"));
                        }
                        if (ft.IsPrimaryFilter == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsPrimaryFilter", "true"));
                        }
                        if (ft.ShowIfEmpty == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "ShowIfEmpty", "false"));
                        }

                    }
                    if (field.Type.Lookup != null && field.Name == "GovernanceRole")
                    {

                        var message = "Task Types cannot have field property '{0}' on field GovernanceRole set to {1}.";
                        var ft = field.Type.Lookup;
                        if (ft.IsDisplayable == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsDisplayable", "false"));
                        }
                        if (ft.IsPartOfKey == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsPartOfKey", "true"));
                        }
                        if (ft.IsPrimaryFilter == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsPrimaryFilter", "true"));
                        }
                        if (ft.ShowIfEmpty == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "ShowIfEmpty", "false"));
                        }
                        if (ft.List.AllowMultipleValues == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "AllowMultipleValues", "true"));
                        }
                    }
                    if (field.Type.Decimal != null && field.Name == "StepNo")
                    {

                        var message = "Task Types cannot have field property '{0}' on field StepNo set to {1}.";
                        var ft = field.Type.Decimal;
                        if (ft.IsDisplayable == false)
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsDisplayable", "false"));
                        if (ft.IsPartOfKey == true)
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsPartOfKey", "true"));
                        if (ft.IsPrimaryFilter == true)
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "IsPrimaryFilter", "true"));
                        if (ft.ShowIfEmpty == false)
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, "ShowIfEmpty", "false"));

                    }

                    if (!new string[] { "Name", "GovernanceRole", "StepNo" }.Contains(field.Name))
                    {
                        var editableViewModel = GetEditableViewModel(field);
                        if (editableViewModel.IsListable == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"Non static fields on Diagram Asset Type cannot have 'IsListable' set to true!");
                        }
                        if (editableViewModel.IsPartOfKey == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"Non static fields on Diagram Asset Type cannot have 'IsPartOfKey' set to true!");
                        }
                        if (editableViewModel.IsPrimaryFilter == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, $"Non static fields on Diagram Asset Type cannot have 'IsPrimaryFilter' set to true!");
                        }

                    }
                }
            }
            if (fieldsHaveErrors)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest,FieldErrors.FieldsContainErrors, $"The following fields have more than one type defined: {string.Join(", ", fieldsHaveErrorsList)}.");
            }
            if (!actionIsReplaceAndKeySelected)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.NoPrimaryKeyDefined, $"You have elected to replace all current fields, yet you have not defined a key. You must define at least one field as a key, or choose Merge as an Action.");
            }
            var duplicateFieldNames = model.Fields.Select(f => f.Name.ToLower()).GroupBy(f => f).Where(f => f.Count() > 1).Select(f => f.Key).ToList();
            if (duplicateFieldNames.Count > 0)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.DuplicateFieldNames, $"The following field names are used more than once: {string.Join(", ", duplicateFieldNames)}. Field names must be unique.");
            }


            //development area
            if (model.AssetTypeUid.HasValue)
            {
                var duplicateFieldIntersectTypeUid = model.Fields.Where(f => f.Type.Relationship != null).Select(f => f.Type.Relationship.IntersectTypeUid).GroupBy(f => f).Where(f => f.Count() > 1).Select(f => f.Key).ToList();
                if (duplicateFieldIntersectTypeUid.Count > 0)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.DuplicateRelationship, $"The following relationship ID are used more than once: {string.Join(", ", duplicateFieldIntersectTypeUid)}. Relationship must be unique on same assettype.");
                }
            }

            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private static FieldTypeEditableApiViewModel GetEditableViewModel(FieldTypeApiEditModel field)
        {
            var editableViewModel = new FieldTypeEditableApiViewModel();
            if (field.Type.Text != null)
            {
                editableViewModel = field.Type.Text as FieldTypeEditableApiViewModel;
            }
            if (field.Type.Boolean != null)
            {
                editableViewModel = field.Type.Boolean as FieldTypeEditableApiViewModel;
            }
            if (field.Type.Date != null)
            {
                editableViewModel = field.Type.Date as FieldTypeEditableApiViewModel;
            }
            if (field.Type.DateTime != null)
            {
                editableViewModel = field.Type.DateTime as FieldTypeEditableApiViewModel;
            }
            if (field.Type.Decimal != null)
            {
                editableViewModel = field.Type.Decimal as FieldTypeEditableApiViewModel;
            }
            if (field.Type.Html != null)
            {
                editableViewModel = field.Type.Html as FieldTypeEditableApiViewModel;
            }
            if (field.Type.Link != null)
            {
                editableViewModel = field.Type.Link as FieldTypeEditableApiViewModel;
            }
            if (field.Type.Lookup != null)
            {
                editableViewModel = field.Type.Lookup as FieldTypeEditableApiViewModel;
            }
            if (field.Type.Number != null)
            {
                editableViewModel = field.Type.Number as FieldTypeEditableApiViewModel;
            }
            return editableViewModel;
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
                return (new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.ExistItemInSystem, $"You may not remove key fields as there are existing items in your environment. You may not perform a Delete action until those items are removed, or you alter the key fields defined on this type."), null);
            }

            var anyInvalidFields = fieldNamesToDelete.Any(f => !currentFieldTypes.Any(c => c.Name == f));
            if (anyInvalidFields)
            {
                return (new WorkHttpStatus(HttpStatusCode.BadRequest,FieldErrors.InvalidFields, $"You are attempting to remove one or more fields that do not exist on this type."), null);
            }

            return (new WorkHttpStatus(HttpStatusCode.OK, "", ""),
                        fieldNamesToDelete);
        }

        private static WorkHttpStatus BaseModelValidation(BaseFieldTypesApiModel model, TypeIdentifierInfoModel actionTypeIdentifierInfoModel, TypeIdentifierInfoModel assetTypeIdentifierInfoModel, TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel)
        {
            if (model == null)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.NoModelFound, "You did not provide a valid model. Please check your request and try again.");
            }

            if (!model.ActionTypeUid.HasValue && !model.AssetTypeUid.HasValue && !model.RelationshipTypeUid.HasValue)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.NoUidFound, "You must provide only one of the three Uid properties: ActionTypeUid, AssetTypeUid, or RelationshipTypeUid.");
            }

            if (model.ActionTypeUid.HasValue)
            {

                if (actionTypeIdentifierInfoModel == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound,AssetTypeErrors.TypeNotFound, $"Action Type not found based on Uid provided [{model.ActionTypeUid}].");
                }
            }

            if (model.AssetTypeUid.HasValue)
            {
                if (model.ActionTypeUid.HasValue)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest,FieldErrors.ParameterError, "You may not provide an AssetTypeUid since you have already provided an ActionTypeUid.");
                }
                else
                {
                    if (assetTypeIdentifierInfoModel == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.TypeNotFound, $"Asset Type not found based on Uid provided [{model.ActionTypeUid}].");
                    }
                }
            }

            if (model.RelationshipTypeUid.HasValue)
            {
                if (model.ActionTypeUid.HasValue)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.ParameterError, "You may not provide an RelationshipTypeUid since you have already provided an ActionTypeUid.");
                }
                else if (model.AssetTypeUid.HasValue)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.ParameterError, "You may not provide an RelationshipTypeUid since you have already provided an AssetTypeUid.");
                }
                else
                {

                    if (relationshipTypeIdentifierInfoModel == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.TypeNotFound, $"Relationship Type not found based on Uid provided [{model.RelationshipTypeUid}].");
                    }
                }
            }
            return new WorkHttpStatus(HttpStatusCode.OK, "", "");
        }

        private static bool IsFieldNameAllowed(string fieldApiName, bool isRelationshipType = false, TypeIdentifierInfoModel assetTypeIdentifierInfoModel = null)
        {
            if (string.IsNullOrEmpty(fieldApiName))
            {
                return false;
            }
            List<string> disallowedFieldNames = new List<string> { "id", "uid", "assetid", "assetuid", "assettypeid", "assettypeuid", "createdon", "updatedon", "parentdisplayname", "parentassetuid", "keypath" };
            if (isRelationshipType)
            {
                disallowedFieldNames.Add("source");
            }

            if (assetTypeIdentifierInfoModel != null)
            {
                if (assetTypeIdentifierInfoModel.Object == SystemObjects.ResourceType.ToString())
                {
                    disallowedFieldNames.AddRange(new List<string> { "firstname", "lastname", "email", "status", "state", "resourceid", "resourceuri", "datelastloggedin", "lastloggedinon", "isadministrator" });
                }
            }

            return !disallowedFieldNames.Contains(fieldApiName.ToLower());
        }

        private static bool FieldLengthValid(FieldTypeDescriptionApiViewModel_ValidationLength validation, out string errMsg)
        {
            decimal maxDecimalFieldValue = decimal.Parse(FieldErrors.MaxDecimalFieldValue);
            errMsg = "";
            if (validation != null)
            {
                if (validation?.MaximumLength != null)
                {
                    if ((validation?.MaximumLength % 1) != 0)
                    {
                        errMsg = String.Format(FieldErrors.WholeNumberError, "MaximumLength");
                        return false;
                    }
                    if (validation?.MaximumLength < 0)
                    {
                        errMsg = String.Format(FieldErrors.GreaterThanError, "MaximumLength", "0");
                        return false;
                    }
                    if (validation?.MaximumLength > maxDecimalFieldValue)
                    {
                        errMsg = String.Format(FieldErrors.LessThanError, "MaximumLength", FieldErrors.MaxDecimalFieldValue);
                        return false;
                    }

                }
                if (validation?.MinimumLength != null)
                {
                    if ((validation?.MinimumLength % 1) != 0)
                    {
                        errMsg = String.Format(FieldErrors.WholeNumberError, "MinimumLength");
                        return false;
                    }
                    if (validation?.MinimumLength < 0)
                    {
                        errMsg = String.Format(FieldErrors.GreaterThanError, "MinimumLength", "0");
                        return false;
                    }
                    if (validation?.MinimumLength > maxDecimalFieldValue)
                    {
                        errMsg = String.Format(FieldErrors.LessThanError, "MinimumLength", FieldErrors.MaxDecimalFieldValue);
                        return false;
                    }
                }
                if (validation?.MinimumLength > validation?.MaximumLength)
                {
                    errMsg = String.Format(FieldErrors.LessThanError, "MinimumLength", "MaximumLength");
                    return false;
                }
            }
            return true;
        }
        private static bool FieldLengthValue(FieldTypeDescriptionApiViewModel_ValidationMinMaxValue validation, out string errMsg, decimal? defaultValue)
        {
            decimal maxDecimalFieldValue = decimal.Parse(FieldErrors.MaxDecimalFieldValue);
            errMsg = "";

            if (validation?.MaximumValue != null)
            {
                if (validation?.MaximumValue > maxDecimalFieldValue)
                {
                    errMsg = String.Format(FieldErrors.LessThanError, "MaximumValue", FieldErrors.MaxDecimalFieldValue);
                    return false;
                }
                else if (validation?.MaximumValue < -maxDecimalFieldValue)
                {
                    errMsg = String.Format(FieldErrors.GreaterThanError, "MaximumValue", $"-{FieldErrors.MaxDecimalFieldValue}");
                    return false;
                }
            }

            if (validation?.MinimumValue != null)
            {
                if (validation?.MinimumValue > maxDecimalFieldValue)
                {
                    errMsg = String.Format(FieldErrors.LessThanError, "MinimumValue", FieldErrors.MaxDecimalFieldValue);
                    return false;
                }
                else if (validation?.MinimumValue < -maxDecimalFieldValue)
                {
                    errMsg = String.Format(FieldErrors.GreaterThanError, "MinimumValue", $"-{FieldErrors.MaxDecimalFieldValue}");
                    return false;
                }
            }

            if (validation?.MinimumValue > validation?.MaximumValue)
            {
                errMsg = String.Format(FieldErrors.LessThanError, "MinimumValue", "MaximumValue");
                return false;
            }
            if (defaultValue.HasValue)
            {
                if (defaultValue > validation?.MaximumValue || defaultValue < validation?.MinimumValue)
                {
                    errMsg = string.Format(FieldErrors.DefaultValueError, validation?.MaximumValue, validation?.MinimumValue);
                    return false;
                }
            }
            return true;
        }
    }
}
