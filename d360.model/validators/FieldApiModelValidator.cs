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
                                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.DuplicateRelationship, string.Format(FieldErrors.RelationshipIDUsedMoreThanOnce,field.Type.Relationship.IntersectTypeUid));

                            }
                        }
                    }

                    if (field.Type.Relationship.IsEditable == false && field.Type.Relationship.Description.Form.Trim().Length > 0)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, string.Format(FieldErrors.FormDescriptionMustBeEmptyForRelationship, field.FriendlyName));
                    }

                }

                if (field?.Type?.Boolean != null)
                {
                    if (actionTypeIdentifierInfoModel != null && field.Type.Boolean.IsListable == true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, string.Format(FieldErrors.IsListableParameterMustBeFalseForBooleanType, field.FriendlyName));
                    }
                    if (actionTypeIdentifierInfoModel != null && field.Type.Boolean.IsPrimaryFilter == true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, string.Format(FieldErrors.IsPrimaryFilterMustBefalseForBooleanType, field.FriendlyName));
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
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.NotValidRegex);
                    }
                }

                if (field.Type.Link != null)
                {
                    if (field.Type.Link.IsPartOfKey == true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest,FieldErrors.FieldPropertyError, string.Format(FieldErrors.LinkTypeNotSupportIsPartOfKeyTrue, field.FriendlyName));
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
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, String.Format(FieldErrors.WholeNumberError, FieldErrors.ConstantIncrement));
                    }

                    if (field.Type.Number.Validation?.MaximumValue != null && (field.Type.Number.Validation?.MaximumValue % 1) != 0)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, String.Format(FieldErrors.WholeNumberError, FieldErrors.ConstantMaximumValue));
                    }

                    if (field.Type.Number.Validation?.MinimumValue != null && (field.Type.Number.Validation?.MinimumValue % 1) != 0)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, String.Format(FieldErrors.WholeNumberError, FieldErrors.ConstantMinimumValue));
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
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.JSONTypeNotSupportIsRequiredTrue);
                        }
                    }
                }

                if (field.Type.Counter != null)
                {
                    if (field.Type.Counter.IsEditable == true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.IsEditableCanotTrue);
                    }

                    if (!string.IsNullOrEmpty(field.Type.Counter.CounterPrefix))
                    {
                        var value = field.Type.Counter.CounterPrefix.Trim();
                        field.Type.Counter.CounterPrefix = value;

                        if (value.Length > 10)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.CounterPrefixMax10Char);
                        }

                        var match = Regex.Matches(value, "[a-zA-Z0-9-_]");
                        if (match.Count != value.Length)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.CounterPrefixRule);
                        }

                        if (!Regex.IsMatch(value[0].ToString(), "[a-zA-Z]"))
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.CounterPrefixStartWithAlpha);
                        }
                    }

                    if (field.Type.Counter.CounterInitialIndex.HasValue && (field.Type.Counter.CounterInitialIndex.Value <= 0 || field.Type.Counter.CounterInitialIndex.Value > 9999999))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.CounterRange);
                    }

                    var allowedTypes = new List<string> {
                            SystemObjects.ArtifactType.ToString(),
                            SystemObjects.PolicyType.ToString(),
                            SystemObjects.RuleType.ToString(),
                            SystemObjects.TaxonomyType.ToString()
                        };
                    if (assetTypeIdentifierInfoModel == null || !allowedTypes.Contains(assetTypeIdentifierInfoModel.Object))
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.CounterFieldNotSupportedThisAssetType);
                    }
                }

                if (field.Type.ComputedOwnershipLookup != null)
                {
                    if (field.Type.ComputedOwnershipLookup.DisplayInColumn == true && field.Type.ComputedOwnershipLookup.Definition.DisplayAsList != true)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldTypeError, FieldErrors.DisplayInColumnMustFlaseOnComputedWonerShipLookup);
                    }
                }

                //Diagram asset type validators
                if (assetTypeIdentifierInfoModel != null && assetTypeIdentifierInfoModel.Object == SystemObjects.TaskType.ToString())
                {

                    if (field.Type.ComputedOwnershipLookup != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.ComputedOwnershipLookupNotSupported);
                    }

                    if (field.Type.ComputedRelationshipField != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.ComputedRelationshipFieldNotSupported);
                    }

                    if (field.Type.ComputedRelationshipLookup != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.ComputedRelationshipLookupNotSupported);
                    }

                    if (field.Type.ComputedRelationshipReferenceList != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.ComputedRelationshipReferenceListNotSupported);
                    }

                    if (field.Type.Json != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.JsonNotSupported);
                    }

                    if (field.Type.JsonElement != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.JsonElementNotSupported);
                    }

                    if (field.Type.Relationship != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.RelationshipNotSupported);
                    }

                    if (field.Type.Score != null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, FieldErrors.ScoreNotSupported);
                    }

                    if (field.Type.Text != null && field.Name == "Name")
                    {

                        var message = FieldErrors.TaskTypeNameCustomError;
                        var ft = field.Type.Text;
                        if (ft.IsDisplayable == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsDisplayable, FieldErrors.Constantfalse));
                        }
                        if (ft.IsEditable == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsEditable, FieldErrors.Constantfalse));
                        }
                        if (ft.IsListable == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsListable, FieldErrors.Constantfalse));
                        }
                        if (ft.IsPartOfKey == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsPartOfKey, FieldErrors.Constantfalse));
                        }
                        if (ft.Validation.IsRequired == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsRequired, FieldErrors.Constantfalse));
                        }
                        if (ft.IsPrimaryFilter == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsPrimaryFilter, FieldErrors.Constanttrue));
                        }
                        if (ft.ShowIfEmpty == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.ShowIfEmpty, FieldErrors.Constantfalse));
                        }

                    }
                    if (field.Type.Lookup != null && field.Name == "GovernanceRole")
                    {

                        var message = FieldErrors.TaskTypeGovernanceRoleCustomError;
                        var ft = field.Type.Lookup;
                        if (ft.IsDisplayable == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsDisplayable, FieldErrors.Constantfalse));
                        }
                        if (ft.IsPartOfKey == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsPartOfKey, FieldErrors.Constanttrue));
                        }
                        if (ft.IsPrimaryFilter == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsPrimaryFilter, FieldErrors.Constanttrue));
                        }
                        if (ft.ShowIfEmpty == false)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.ShowIfEmpty, FieldErrors.Constantfalse));
                        }
                        if (ft.List.AllowMultipleValues == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.AllowMultipleValues, FieldErrors.Constanttrue));
                        }
                    }
                    if (field.Type.Decimal != null && field.Name == "StepNo")
                    {

                        var message = FieldErrors.TaskTypeStepNoCustomError;
                        var ft = field.Type.Decimal;
                        if (ft.IsDisplayable == false)
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsDisplayable, FieldErrors.Constantfalse));
                        if (ft.IsPartOfKey == true)
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsPartOfKey, FieldErrors.Constanttrue));
                        if (ft.IsPrimaryFilter == true)
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsPrimaryFilter, FieldErrors.Constanttrue));
                        if (ft.ShowIfEmpty == false)
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.ShowIfEmpty, FieldErrors.Constantfalse));

                    }

                    if (!new string[] { "Name", "GovernanceRole", "StepNo" }.Contains(field.Name))
                    {
                        var editableViewModel = GetEditableViewModel(field);

                        var message = FieldErrors.DiagramATCustomError;

                        if (editableViewModel.IsListable == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsListable, FieldErrors.Constanttrue));
                        }
                        if (editableViewModel.IsPartOfKey == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsPartOfKey, FieldErrors.Constanttrue));
                        }
                        if (editableViewModel.IsPrimaryFilter == true)
                        {
                            return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.FieldPropertyError, string.Format(message, FieldErrors.IsPrimaryFilter, FieldErrors.Constanttrue));
                        }

                    }
                }
            }
            if (fieldsHaveErrors)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest,FieldErrors.FieldsContainErrors, string.Format(FieldErrors.OneThanOneTypeDefined, string.Join(", ", fieldsHaveErrorsList)));
            }
            if (!actionIsReplaceAndKeySelected)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.NoPrimaryKeyDefined, FieldErrors.KeyFieldNotDefined);
            }
            var duplicateFieldNames = model.Fields.Select(f => f.Name.ToLower()).GroupBy(f => f).Where(f => f.Count() > 1).Select(f => f.Key).ToList();
            if (duplicateFieldNames.Count > 0)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.DuplicateFieldNames, string.Format(FieldErrors.FieldNameMustUnique, string.Join(", ", duplicateFieldNames)));
            }


            //development area
            if (model.AssetTypeUid.HasValue)
            {
                var duplicateFieldIntersectTypeUid = model.Fields.Where(f => f.Type.Relationship != null).Select(f => f.Type.Relationship.IntersectTypeUid).GroupBy(f => f).Where(f => f.Count() > 1).Select(f => f.Key).ToList();
                if (duplicateFieldIntersectTypeUid.Count > 0)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.DuplicateRelationship, string.Format(FieldErrors.RelationshipUidMustUniqueWithinAssetType, string.Join(", ", duplicateFieldIntersectTypeUid)));
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
                return (new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.ExistItemInSystem, FieldErrors.InvalidRemoveKeyFields), null);
            }

            var anyInvalidFields = fieldNamesToDelete.Any(f => !currentFieldTypes.Any(c => c.Name == f));
            if (anyInvalidFields)
            {
                return (new WorkHttpStatus(HttpStatusCode.BadRequest,FieldErrors.InvalidFields, FieldErrors.FailRemoveFieldNotExistsType), null);
            }

            return (new WorkHttpStatus(HttpStatusCode.OK, "", ""),
                        fieldNamesToDelete);
        }

        private static WorkHttpStatus BaseModelValidation(BaseFieldTypesApiModel model, TypeIdentifierInfoModel actionTypeIdentifierInfoModel, TypeIdentifierInfoModel assetTypeIdentifierInfoModel, TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel)
        {
            if (model == null)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.NoModelFound, FieldErrors.InvalidModel);
            }

            if (!model.ActionTypeUid.HasValue && !model.AssetTypeUid.HasValue && !model.RelationshipTypeUid.HasValue)
            {
                return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.NoUidFound, FieldErrors.ProvideOneValueActionAssetRelationship);
            }

            if (model.ActionTypeUid.HasValue)
            {

                if (actionTypeIdentifierInfoModel == null)
                {
                    return new WorkHttpStatus(HttpStatusCode.NotFound,AssetTypeErrors.TypeNotFound, string.Format(FieldErrors.ActionTypeUidNotFound,model.ActionTypeUid));
                }
            }

            if (model.AssetTypeUid.HasValue)
            {
                if (model.ActionTypeUid.HasValue)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest,FieldErrors.ParameterError, FieldErrors.AssetTypeUidNotRequiredIfActionTypeUidProvided);
                }
                else
                {
                    if (assetTypeIdentifierInfoModel == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.TypeNotFound, string.Format(FieldErrors.AssetTypeUidNotFound,model.ActionTypeUid));
                    }
                }
            }

            if (model.RelationshipTypeUid.HasValue)
            {
                if (model.ActionTypeUid.HasValue)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.ParameterError, FieldErrors.RelationShipTypeUidNotRequiredIfActionTypeUidProvided);
                }
                else if (model.AssetTypeUid.HasValue)
                {
                    return new WorkHttpStatus(HttpStatusCode.BadRequest, FieldErrors.ParameterError,FieldErrors.RelationShipTypeUidNotRequiredIfAssetTypeUidProvided);
                }
                else
                {

                    if (relationshipTypeIdentifierInfoModel == null)
                    {
                        return new WorkHttpStatus(HttpStatusCode.NotFound, AssetTypeErrors.TypeNotFound,string.Format(FieldErrors.RelationshipTypeUIdNotFound ,model.RelationshipTypeUid));
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
                        errMsg = String.Format(FieldErrors.WholeNumberError, FieldErrors.ConstantMaximumLength);
                        return false;
                    }
                    if (validation?.MaximumLength < 0)
                    {
                        errMsg = String.Format(FieldErrors.GreaterThanError, FieldErrors.ConstantMaximumLength, "0");
                        return false;
                    }
                    if (validation?.MaximumLength > maxDecimalFieldValue)
                    {
                        errMsg = String.Format(FieldErrors.LessThanError, FieldErrors.ConstantMaximumLength, FieldErrors.MaxDecimalFieldValue);
                        return false;
                    }

                }
                if (validation?.MinimumLength != null)
                {
                    if ((validation?.MinimumLength % 1) != 0)
                    {
                        errMsg = String.Format(FieldErrors.WholeNumberError, FieldErrors.ConstantMinimumLength);
                        return false;
                    }
                    if (validation?.MinimumLength < 0)
                    {
                        errMsg = String.Format(FieldErrors.GreaterThanError, FieldErrors.ConstantMinimumLength, "0");
                        return false;
                    }
                    if (validation?.MinimumLength > maxDecimalFieldValue)
                    {
                        errMsg = String.Format(FieldErrors.LessThanError, FieldErrors.ConstantMinimumLength, FieldErrors.MaxDecimalFieldValue);
                        return false;
                    }
                }
                if (validation?.MinimumLength > validation?.MaximumLength)
                {
                    errMsg = String.Format(FieldErrors.LessThanError, FieldErrors.ConstantMinimumLength, FieldErrors.ConstantMaximumLength);
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
                    errMsg = String.Format(FieldErrors.LessThanError,FieldErrors.ConstantMaximumValue, FieldErrors.MaxDecimalFieldValue);
                    return false;
                }
                else if (validation?.MaximumValue < -maxDecimalFieldValue)
                {
                    errMsg = String.Format(FieldErrors.GreaterThanError, FieldErrors.ConstantMaximumValue, $"-{FieldErrors.MaxDecimalFieldValue}");
                    return false;
                }
            }

            if (validation?.MinimumValue != null)
            {
                if (validation?.MinimumValue > maxDecimalFieldValue)
                {
                    errMsg = String.Format(FieldErrors.LessThanError, FieldErrors.ConstantMinimumValue, FieldErrors.MaxDecimalFieldValue);
                    return false;
                }
                else if (validation?.MinimumValue < -maxDecimalFieldValue)
                {
                    errMsg = String.Format(FieldErrors.GreaterThanError, FieldErrors.ConstantMinimumValue, $" -{FieldErrors.MaxDecimalFieldValue}");
                    return false;
                }
            }

            if (validation?.MinimumValue > validation?.MaximumValue)
            {
                errMsg = String.Format(FieldErrors.LessThanError, FieldErrors.ConstantMinimumValue, FieldErrors.ConstantMaximumValue);
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
