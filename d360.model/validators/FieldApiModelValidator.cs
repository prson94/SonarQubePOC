using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

using d360.core;
using d360.core.entities;
using d360.core.resources;
using d360.core.validators;

namespace d360.model.validators
{
	public static class FieldApiModelValidator
	{
		public static WorkHttpStatus ValidateModel(FieldTypesApiEditModel model, TypeIdentifierInfoModel actionTypeIdentifierInfoModel, TypeIdentifierInfoModel assetTypeIdentifierInfoModel, TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel, List<FieldType> existingFieldTypes = null, List<Tuple<string, Guid>> ExistingIntersectID = null, bool isJsonAttributeFieldTypeEnabled = true, List<IntersectType> existingIntersects = null)
		{
			WorkHttpStatus baseValidation = BaseModelValidation(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel);

			if (baseValidation.StatusCode != HttpStatusCode.OK)
			{
				return baseValidation;
			}

			bool actionIsReplaceAndKeySelected = (model.Action == FieldTypesApiEditAction.Merge); //If set to merge we can set to true and skip this step.
			bool fieldsHaveErrors = false;
			List<string> fieldsHaveErrorsList = new List<string>();
			List<ValidationResult> validationResults = new List<ValidationResult>();
			bool isValid = true;

			foreach (FieldTypeApiEditModel field in model.Fields)
			{
				#region Basic field Model validation

				isValid = Validator.TryValidateObject(field, new ValidationContext(field, serviceProvider: null, items: null), validationResults, true);

				if (!isValid)
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, string.Format(Error.InvalidField, validationResults.First().MemberNames.First()), validationResults.First().ErrorMessage);
				}

				#endregion

				#region Name Validation

				if (!IsFieldNameAllowed(field.Name.Trim(), relationshipTypeIdentifierInfoModel != null, assetTypeIdentifierInfoModel))
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.InvalidFieldName, string.Format(Error.NameCannotBe, field.Name.Trim().ToUpper()));
				}

				#endregion

				#region FriendlyName Validation                

				if (!IsFieldNameAllowed(field.FriendlyName.Trim(), assetTypeIdentifierInfoModel: assetTypeIdentifierInfoModel))
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.InvalidFieldFriendlyName, string.Format(Error.FriendNameCannotBe, field.FriendlyName.Trim().ToUpper()));
				}

				#endregion

				if (field.Type == null)
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.TypeObjectMissing);
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
					return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.JsonElementFieldTypeNotenabled);
				}

				if (assetTypeIdentifierInfoModel != null && assetTypeIdentifierInfoModel.Object == SystemObjects.ReferenceItemType.ToString())
				{
					if (field.Type.IsPartOfKey())
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.ReferenceItemIsPartOfKeyNotAllowedTrue);
					}

					if (field.Type.Json != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.ReferenceListNotSupportJson);
					}
					else if (field.Type.Tag != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.ReferenceListNotSupportTag);
					}
					else if (field.Type.JsonElement != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.ReferenceItemNotSupportJsonElement);
					}
				}

				if (assetTypeIdentifierInfoModel != null && assetTypeIdentifierInfoModel.Object == SystemObjects.GroupType.ToString())
				{
					if (field.Type.IsPartOfKey())
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.ReferenceItemIsPartOfKeyNotAllowedTrue);
					}

					List<string> allowedGroupFieldTypes = new List<string> { "Counter", "DateTime", "Date", "Decimal", "Text", "Boolean", "Lookup", "Number" };

					if (!allowedGroupFieldTypes.Contains(field.Type.GetFieldType()))
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.GroupInvalidFieldType, field.Type.GetFieldType()));
					}
				}

				if (relationshipTypeIdentifierInfoModel != null)
				{
					if (field.Type.IsPartOfKey())
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.RelationNotAllowedIsPartyOFKeyTrue);
					}
				}

				if (actionTypeIdentifierInfoModel != null)
				{
					if (field.Type.IsPartOfKey())
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.ActionNotAllowedIsPartyOFKeyTrue);
					}
				}

				if (field.Type.Json != null)
				{
					if (assetTypeIdentifierInfoModel != null)
					{
						List<string> restrictedTypes = new List<string> {SystemObjects.ResourceType.ToString()};

						if (restrictedTypes.Contains(assetTypeIdentifierInfoModel.Object))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.JsonNotSupported);
						}
					}
				}

				if (field.Type.Path != null)
				{
					if (actionTypeIdentifierInfoModel != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.ActionNotSupportPath);
					}

					if (relationshipTypeIdentifierInfoModel != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.RelationshipNotSupportPath);
					}

					if (assetTypeIdentifierInfoModel != null)
					{
						List<string> restrictedTypes = new List<string> {
							SystemObjects.ResourceType.ToString()
						};

						if (restrictedTypes.Contains(assetTypeIdentifierInfoModel.Object))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.ThisAssetTypeNotSupportPath);
						}
					}
				}

				#region IsDisplayable

				if (field.Type.ComputedRelationshipLookup != null)
				{
					if (field.Type.ComputedRelationshipLookup.IsDisplayable == false)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.IsDisplayAbleTrueRelationshipLookup, field.FriendlyName));
					}
					if (assetTypeIdentifierInfoModel != null)
					{
						List<string> restrictedTypes = new List<string> {
							SystemObjects.ResourceType.ToString()
						};

						if (restrictedTypes.Contains(assetTypeIdentifierInfoModel.Object))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.NotUseComputedRelationshipLookuptypeField, "User", field.Name));
						}
					}
				}

				if (field.Type.ComputedRelationshipReferenceList != null)
				{
					if (field.Type.ComputedRelationshipReferenceList.IsDisplayable == false)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.IsDisplayAbleTrueReferenceItemListForRelationship, field.FriendlyName));
					}

					if (assetTypeIdentifierInfoModel != null)
					{
						List<string> restrictedTypes = new List<string> {
							SystemObjects.ResourceType.ToString()
						};

						if (restrictedTypes.Contains(assetTypeIdentifierInfoModel.Object))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.ComputedRelationshipReferenceListNotSupported);
						}
					}
				}

				#endregion

				#region isPartOfKey

				if (field.Type.IsPartOfKey() == true && assetTypeIdentifierInfoModel != null)
				{
					if (assetTypeIdentifierInfoModel.Object == SystemObjects.ResourceType.ToString())
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.AssetTypeNotHaveKeyField);
					}
				}

				#endregion

				if (field.Type.JsonElement != null)
				{
					if (existingFieldTypes != null)
					{
						JsonAttributeApiViewModel jsonAttribute = field.Type.JsonElement.JsonAttribute;

						if (jsonAttribute == null)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.MissingJsonAttribute);
						}

						if (!existingFieldTypes.Any(x => x.Name == jsonAttribute.FieldName && x.Type == "JSON"))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.JsonFiledNotPartAssetType, jsonAttribute.FieldName));
						}

						List<string> allowedTypes = new List<string> { "bit", "date", "datetime", "float", "nvarchar", "int", "bigint" };

						if (!allowedTypes.Contains(jsonAttribute.DataType))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.InvalidJsonFieldType, string.Join(", ", allowedTypes)));
						}

						if (assetTypeIdentifierInfoModel != null)
						{
							List<string> restrictedTypes = new List<string> {
							SystemObjects.ResourceType.ToString()
						};

							if (restrictedTypes.Contains(assetTypeIdentifierInfoModel.Object))
							{
								return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.JsonElementNotSupported);
							}
						}
					}
				}

				if (field.Type.Score != null)
				{
					if (actionTypeIdentifierInfoModel != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.ActionNotAllowedScoreFieldType);
					}

					if (relationshipTypeIdentifierInfoModel != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.RelationshipNotAllowedScoreFieldType);
					}

					if (assetTypeIdentifierInfoModel != null)
					{
						List<string> restrictedTypes = new List<string> {
							SystemObjects.ReferenceItemType.ToString(),
							SystemObjects.ResourceType.ToString()
						};

						if (restrictedTypes.Contains(assetTypeIdentifierInfoModel.Object))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.AssetTypeNotHaveScoreField);
						}
					}
				}

				if (field.Type.Tag != null)
				{
					if (actionTypeIdentifierInfoModel != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.AssetTypeError, Error.ActionNotAllowedTagFieldType);
					}

					if (relationshipTypeIdentifierInfoModel != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.AssetTypeError, Error.RelationshipsNotAllowedTagFieldType);
					}

					if (assetTypeIdentifierInfoModel != null)
					{
						List<string> allowedTypes = new List<string> { SystemObjects.ArtifactType.ToString(), SystemObjects.PolicyType.ToString(), SystemObjects.TaxonomyType.ToString(), SystemObjects.RuleType.ToString(), SystemObjects.TaskType.ToString() };
						if (!allowedTypes.Contains(assetTypeIdentifierInfoModel.Object))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.AssetTypeError, Error.SpecificHaveTagFieldType);
						}
					}
				}

				if (model.AssetTypeUid.HasValue && field.Type.Relationship != null)
				{
					if (ExistingIntersectID != null)
					{
						if (ExistingIntersectID.Count() > 0)
						{
							List<string> duplicateFieldIntersectTypeUid1 = ExistingIntersectID.Where(f => f.Item1 != field.Name && f.Item2 == field.Type.Relationship.IntersectTypeUid).Select(f => f.Item1).ToList();

							if (duplicateFieldIntersectTypeUid1.Count > 0)
							{
								var invalidIntersects = existingIntersects.Where(it =>
																		it.uid == field.Type.Relationship.IntersectTypeUid
																		&&
																		(
																			it.SubjectAssetTypeID != it.ObjectAssetTypeID
																			||
																			(
																				it.SubjectAssetTypeID == it.ObjectAssetTypeID
																				&&
																				existingFieldTypes.Any(x => x.LookupObjectID == it.ID
																				&&
																				x.IsSubject == field.Type.Relationship.IsSubject
																				&&
																				x.Name != field.Name
																				)
																			)
																		)
																).ToList();
								if (invalidIntersects.Count > 0)
								{
									return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.DuplicateRelationship, string.Format(Error.RelationshipIDUsedMoreThanOnce, field.Type.Relationship.IntersectTypeUid));
								}
							}
						}
					}

					if (field.Type.Relationship.IsEditable == false && field.Type.Relationship?.Description?.Form?.Trim().Length > 0)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.FormDescriptionMustBeEmptyForRelationship, field.FriendlyName));
					}
				}

				if (field?.Type?.Boolean != null)
				{
					if (actionTypeIdentifierInfoModel != null && field.Type.Boolean.IsListable == true)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.IsListableParameterMustBeFalseForBooleanType, field.FriendlyName));
					}

					if (actionTypeIdentifierInfoModel != null && field.Type.Boolean.IsPrimaryFilter == true)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.IsPrimaryFilterMustBefalseForBooleanType, field.FriendlyName));
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
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.NotValidRegex);
					}
				}

				if (field.Type.Link != null)
				{
					if (field.Type.Link.IsPartOfKey == true)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(Error.LinkTypeNotSupportIsPartOfKeyTrue, field.FriendlyName));
					}
				}

				#region Type Min/Max

				if (field?.Type?.Text != null)
				{
					if (!FieldLengthValid(field.Type.Text.Validation, out string validationErrorMsg))
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, $"{validationErrorMsg}");
					}
				}

				if (field?.Type?.Html != null)
				{
					if (!FieldLengthValid(field.Type.Html.Validation, out string validationErrorMsg))
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, $"{validationErrorMsg}");
					}
				}

				if (field?.Type?.Number != null)
				{
					if (field.Type.Number.Increment != null && (field.Type.Number.Increment % 1 != 0))
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.WholeNumberError, Error.ConstantIncrement));
					}

					if (field.Type.Number.Validation?.MaximumValue != null && (field.Type.Number.Validation?.MaximumValue % 1) != 0)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.WholeNumberError, Error.ConstantMaximumValue));
					}

					if (field.Type.Number.Validation?.MinimumValue != null && (field.Type.Number.Validation?.MinimumValue % 1) != 0)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, string.Format(Error.WholeNumberError, Error.ConstantMinimumValue));
					}

					if (!FieldLengthValue(field.Type.Number.Validation, out string validationErrorMsg, field.Type.Number.DefaultValue))
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, $"{validationErrorMsg}");
					}
				}

				if (field?.Type?.Decimal != null)
				{
					if (!FieldLengthValue(field.Type.Decimal.Validation, out string validationErrorMsg, field.Type.Decimal.DefaultValue))
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, $"{validationErrorMsg}");
					}
				}

				#endregion


				if (assetTypeIdentifierInfoModel != null && field?.Type?.Json != null)
				{
					if (field.Type.Json.Validation != null)
					{
						if (field.Type.Json.Validation.IsRequired)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.JSONTypeNotSupportIsRequiredTrue);
						}
					}
				}

				if (assetTypeIdentifierInfoModel != null && field?.Type?.Relationship != null)
				{
					//validate IsSubject property when adding a relationship field
					//if property is set to invalid value, silently inverse bool value instead of throwing exception to not break existing functionality
					var currentAssetTypeId = assetTypeIdentifierInfoModel.ID;
					if (field.Type.Relationship.IsSubject)
					{
						if (!existingIntersects.Any(x => x.SubjectAssetTypeID == currentAssetTypeId))
						{
							field.Type.Relationship.IsSubject = !field.Type.Relationship.IsSubject;
						}
					}
					else
					{
						if (!existingIntersects.Any(x => x.ObjectAssetTypeID == currentAssetTypeId))
						{
							field.Type.Relationship.IsSubject = !field.Type.Relationship.IsSubject;
						}
					}
				}

				if (field.Type.Counter != null)
				{
					if (field.Type.Counter.IsEditable == true)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.IsEditableCanotTrue);
					}

					if (!string.IsNullOrEmpty(field.Type.Counter.CounterPrefix))
					{
						string value = field.Type.Counter.CounterPrefix.Trim();
						field.Type.Counter.CounterPrefix = value;

						if (value.Length > 10)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.CounterPrefixMax10Char);
						}

						MatchCollection match = Regex.Matches(value, "[a-zA-Z0-9-_]");

						if (match.Count != value.Length)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.CounterPrefixRule);
						}

						if (!Regex.IsMatch(value[0].ToString(), "[a-zA-Z]"))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.CounterPrefixStartWithAlpha);
						}
					}

					if (field.Type.Counter.CounterInitialIndex.HasValue && (field.Type.Counter.CounterInitialIndex.Value <= 0 || field.Type.Counter.CounterInitialIndex.Value > 9999999))
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.CounterRange);
					}

					List<string> allowedTypes = new List<string> {
							SystemObjects.ArtifactType.ToString(),
							SystemObjects.PolicyType.ToString(),
							SystemObjects.RuleType.ToString(),
							SystemObjects.TaxonomyType.ToString(),
							SystemObjects.GroupType.ToString()
						};

					if (assetTypeIdentifierInfoModel == null || !allowedTypes.Contains(assetTypeIdentifierInfoModel.Object))
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.CounterFieldNotSupportedThisAssetType);
					}
				}

				if (field.Type.ComputedOwnershipLookup != null)
				{
					if (field.Type.ComputedOwnershipLookup.DisplayInColumn == true && field.Type.ComputedOwnershipLookup.Definition.DisplayAsList != true)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.DisplayInColumnMustFlaseOnComputedWonerShipLookup);
					}
					if (assetTypeIdentifierInfoModel != null)
					{
						List<string> restrictedTypes = new List<string> {
							SystemObjects.ResourceType.ToString()
						};

						if (restrictedTypes.Contains(assetTypeIdentifierInfoModel.Object))
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldTypeError, Error.ComputedOwnershipLookupNotSupported);
						}
					}
				}

				//Diagram asset type validators
				if (assetTypeIdentifierInfoModel != null && assetTypeIdentifierInfoModel.Object == SystemObjects.TaskType.ToString())
				{
					if (field.Type.ComputedOwnershipLookup != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.ComputedOwnershipLookupNotSupported);
					}

					if (field.Type.ComputedRelationshipField != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.ComputedRelationshipFieldNotSupported);
					}

					if (field.Type.ComputedRelationshipLookup != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.ComputedRelationshipLookupNotSupported);
					}

					if (field.Type.ComputedRelationshipReferenceList != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.ComputedRelationshipReferenceListNotSupported);
					}

					if (field.Type.Json != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.JsonNotSupported);
					}

					if (field.Type.JsonElement != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.JsonElementNotSupported);
					}

					if (field.Type.Relationship != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.RelationshipNotSupported);
					}

					if (field.Type.Score != null)
					{
						return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, Error.ScoreNotSupported);
					}

					if (field.Type.Text != null && field.Name == "Name")
					{

						string message = Error.TaskTypeNameCustomError;
						FieldTypeDataTypeTextApiViewModel ft = field.Type.Text;

						if (ft.IsDisplayable == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsDisplayable, Error.Constantfalse));
						}

						if (ft.IsEditable == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsEditable, Error.Constantfalse));
						}

						if (ft.IsListable == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsListable, Error.Constantfalse));
						}

						if (ft.IsPartOfKey == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsPartOfKey, Error.Constantfalse));
						}

						if (ft.Validation.IsRequired == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsRequired, Error.Constantfalse));
						}

						if (ft.IsPrimaryFilter == true)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsPrimaryFilter, Error.Constanttrue));
						}

						if (ft.ShowIfEmpty == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.ShowIfEmpty, Error.Constantfalse));
						}
					}

					if (field.Type.Lookup != null && field.Name == "GovernanceRole")
					{

						string message = Error.TaskTypeGovernanceRoleCustomError;
						FieldTypeDataTypeLookupApiViewModel ft = field.Type.Lookup;

						if (ft.IsDisplayable == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsDisplayable, Error.Constantfalse));
						}

						if (ft.IsPartOfKey == true)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsPartOfKey, Error.Constanttrue));
						}

						if (ft.IsPrimaryFilter == true)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsPrimaryFilter, Error.Constanttrue));
						}

						if (ft.ShowIfEmpty == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.ShowIfEmpty, Error.Constantfalse));
						}

						if (ft.List.AllowMultipleValues == true)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.AllowMultipleValues, Error.Constanttrue));
						}
					}

					if (field.Type.Decimal != null && field.Name == "StepNo")
					{

						string message = Error.TaskTypeStepNoCustomError;
						FieldTypeDataTypeDecimalApiViewModel ft = field.Type.Decimal;

						if (ft.IsDisplayable == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsDisplayable, Error.Constantfalse));
						}

						if (ft.IsPartOfKey == true)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsPartOfKey, Error.Constanttrue));
						}

						if (ft.IsPrimaryFilter == true)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsPrimaryFilter, Error.Constanttrue));
						}

						if (ft.ShowIfEmpty == false)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.ShowIfEmpty, Error.Constantfalse));
						}
					}

					if (!new string[] { "Name", "GovernanceRole", "StepNo" }.Contains(field.Name))
					{
						FieldTypeEditableApiViewModel editableViewModel = GetEditableViewModel(field);

						string message = Error.DiagramATCustomError;

						if (editableViewModel.IsListable == true)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsListable, Error.Constanttrue));
						}

						if (editableViewModel.IsPartOfKey == true)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsPartOfKey, Error.Constanttrue));
						}

						if (editableViewModel.IsPrimaryFilter == true)
						{
							return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldPropertyError, string.Format(message, Error.IsPrimaryFilter, Error.Constanttrue));
						}
					}
				}
			}

			if (fieldsHaveErrors)
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.FieldsContainErrors, string.Format(Error.OneThanOneTypeDefined, string.Join(", ", fieldsHaveErrorsList)));
			}

			if (!actionIsReplaceAndKeySelected)
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.NoPrimaryKeyDefined, Error.KeyFieldNotDefined);
			}

			List<string> duplicateFieldNames = model.Fields.Select(f => f.Name.ToLower()).GroupBy(f => f).Where(f => f.Count() > 1).Select(f => f.Key).ToList();

			if (duplicateFieldNames.Count > 0)
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.DuplicateFieldNames, string.Format(Error.FieldNameMustUnique, string.Join(", ", duplicateFieldNames)));
			}

			//development area
			if (model.AssetTypeUid.HasValue)
			{
				List<Guid> duplicateFieldIntersectTypeUid = model.Fields.Where(f => f.Type.Relationship != null).Select(f => f.Type.Relationship.IntersectTypeUid).GroupBy(f => f).Where(f => f.Count() > 1).Select(f => f.Key).ToList();

				if (duplicateFieldIntersectTypeUid.Count > 0)
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.DuplicateRelationship, string.Format(Error.RelationshipUidMustUniqueWithinAssetType, string.Join(", ", duplicateFieldIntersectTypeUid)));
				}
			}

			return new WorkHttpStatus(HttpStatusCode.OK, "", "");
		}

		private static FieldTypeEditableApiViewModel GetEditableViewModel(FieldTypeApiEditModel field)
		{
			FieldTypeEditableApiViewModel editableViewModel = new FieldTypeEditableApiViewModel();

			if (field.Type.Text != null)
			{
				editableViewModel = field.Type.Text;
			}

			if (field.Type.Boolean != null)
			{
				editableViewModel = field.Type.Boolean;
			}

			if (field.Type.Date != null)
			{
				editableViewModel = field.Type.Date;
			}

			if (field.Type.DateTime != null)
			{
				editableViewModel = field.Type.DateTime;
			}

			if (field.Type.Decimal != null)
			{
				editableViewModel = field.Type.Decimal;
			}

			if (field.Type.Html != null)
			{
				editableViewModel = field.Type.Html;
			}

			if (field.Type.Link != null)
			{
				editableViewModel = field.Type.Link;
			}

			if (field.Type.Lookup != null)
			{
				editableViewModel = field.Type.Lookup;
			}

			if (field.Type.Number != null)
			{
				editableViewModel = field.Type.Number;
			}

			return editableViewModel;
		}

		public static WorkHttpStatus ValidateModel(FieldTypesApiDeleteModel model, TypeIdentifierInfoModel actionTypeIdentifierInfoModel, TypeIdentifierInfoModel assetTypeIdentifierInfoModel, TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel)
		{
			return BaseModelValidation(model, actionTypeIdentifierInfoModel, assetTypeIdentifierInfoModel, relationshipTypeIdentifierInfoModel);
		}

		public static (WorkHttpStatus, List<string>) FieldValidator(FieldTypesApiDeleteModel model, bool anyExistingItems, List<FieldType> currentFieldTypes)
		{
			List<string> fieldNamesToDelete = model.Fields.Select(i => i.Name).ToList();
			bool keyFieldsWillBeDeleted = currentFieldTypes.Any(d => d.IsPartOfKey == true && fieldNamesToDelete.Contains(d.Name));

			if (anyExistingItems && keyFieldsWillBeDeleted)
			{
				return (new WorkHttpStatus(HttpStatusCode.BadRequest, Error.ExistItemInSystem, Error.InvalidRemoveKeyFields), null);
			}

			bool anyInvalidFields = fieldNamesToDelete.Any(f => !currentFieldTypes.Any(c => c.Name == f));
			if (anyInvalidFields)
			{
				return (new WorkHttpStatus(HttpStatusCode.BadRequest, Error.InvalidFields, Error.FailRemoveFieldNotExistsType), null);
			}

			return (new WorkHttpStatus(HttpStatusCode.OK, "", ""),
						fieldNamesToDelete);
		}

		private static WorkHttpStatus BaseModelValidation(BaseFieldTypesApiModel model, TypeIdentifierInfoModel actionTypeIdentifierInfoModel, TypeIdentifierInfoModel assetTypeIdentifierInfoModel, TypeIdentifierInfoModel relationshipTypeIdentifierInfoModel)
		{
			if (model == null)
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.NoModelFound, Error.InvalidModel);
			}

			if (!model.ActionTypeUid.HasValue && !model.AssetTypeUid.HasValue && !model.RelationshipTypeUid.HasValue)
			{
				return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.NoUidFound, Error.ProvideOneValueActionAssetRelationship);
			}

			if (model.ActionTypeUid.HasValue)
			{

				if (actionTypeIdentifierInfoModel == null)
				{
					return new WorkHttpStatus(HttpStatusCode.NotFound, Error.TypeNotFound, string.Format(Error.ActionTypeUidNotFound, model.ActionTypeUid));
				}
			}

			if (model.AssetTypeUid.HasValue)
			{
				if (model.ActionTypeUid.HasValue)
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.ParameterError, Error.AssetTypeUidNotRequiredIfActionTypeUidProvided);
				}
				else
				{
					if (assetTypeIdentifierInfoModel == null)
					{
						return new WorkHttpStatus(HttpStatusCode.NotFound, Error.TypeNotFound, string.Format(Error.AssetTypeUidNotFound, model.ActionTypeUid));
					}
				}
			}

			if (model.RelationshipTypeUid.HasValue)
			{
				if (model.ActionTypeUid.HasValue)
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.ParameterError, Error.RelationShipTypeUidNotRequiredIfActionTypeUidProvided);
				}
				else if (model.AssetTypeUid.HasValue)
				{
					return new WorkHttpStatus(HttpStatusCode.BadRequest, Error.ParameterError, Error.RelationShipTypeUidNotRequiredIfAssetTypeUidProvided);
				}
				else
				{

					if (relationshipTypeIdentifierInfoModel == null)
					{
						return new WorkHttpStatus(HttpStatusCode.NotFound, Error.TypeNotFound, string.Format(Error.RelationshipTypeUIdNotFound, model.RelationshipTypeUid));
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

			List<string> disallowedFieldNames = new List<string> { "id", "uid", "assetid", "assetuid", "assettypeid",
				"assettypeuid", "createdon", "updatedon", "parentdisplayname", "parentassetuid", "keypath", "displayvalue", "path", "xrefid" };

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
			decimal maxDecimalFieldValue = decimal.Parse(Error.MaxDecimalFieldValue);
			errMsg = "";
			if (validation != null)
			{
				if (validation?.MaximumLength != null)
				{
					if ((validation?.MaximumLength % 1) != 0)
					{
						errMsg = string.Format(Error.WholeNumberError, Error.ConstantMaximumLength);

						return false;
					}

					if (validation?.MaximumLength < 0)
					{
						errMsg = string.Format(Error.GreaterThanError, Error.ConstantMaximumLength, "0");

						return false;
					}

					if (validation?.MaximumLength > maxDecimalFieldValue)
					{
						errMsg = string.Format(Error.LessThanError, Error.ConstantMaximumLength, Error.MaxDecimalFieldValue);

						return false;
					}
				}

				if (validation?.MinimumLength != null)
				{
					if ((validation?.MinimumLength % 1) != 0)
					{
						errMsg = string.Format(Error.WholeNumberError, Error.ConstantMinimumLength);

						return false;
					}

					if (validation?.MinimumLength < 0)
					{
						errMsg = string.Format(Error.GreaterThanError, Error.ConstantMinimumLength, "0");

						return false;
					}

					if (validation?.MinimumLength > maxDecimalFieldValue)
					{
						errMsg = string.Format(Error.LessThanError, Error.ConstantMinimumLength, Error.MaxDecimalFieldValue);

						return false;
					}
				}

				if (validation?.MinimumLength > validation?.MaximumLength)
				{
					errMsg = string.Format(Error.LessThanError, Error.ConstantMinimumLength, Error.ConstantMaximumLength);

					return false;
				}
			}

			return true;
		}

		private static bool FieldLengthValue(FieldTypeDescriptionApiViewModel_ValidationMinMaxValue validation, out string errMsg, decimal? defaultValue)
		{
			decimal maxDecimalFieldValue = decimal.Parse(Error.MaxDecimalFieldValue);
			errMsg = "";

			if (validation?.MaximumValue != null)
			{
				if (validation?.MaximumValue > maxDecimalFieldValue)
				{
					errMsg = string.Format(Error.LessThanError, Error.ConstantMaximumValue, Error.MaxDecimalFieldValue);

					return false;
				}
				else if (validation?.MaximumValue < -maxDecimalFieldValue)
				{
					errMsg = string.Format(Error.GreaterThanError, Error.ConstantMaximumValue, $"-{Error.MaxDecimalFieldValue}");

					return false;
				}
			}

			if (validation?.MinimumValue != null)
			{
				if (validation?.MinimumValue > maxDecimalFieldValue)
				{
					errMsg = string.Format(Error.LessThanError, Error.ConstantMinimumValue, Error.MaxDecimalFieldValue);

					return false;
				}
				else if (validation?.MinimumValue < -maxDecimalFieldValue)
				{
					errMsg = string.Format(Error.GreaterThanError, Error.ConstantMinimumValue, $" -{Error.MaxDecimalFieldValue}");

					return false;
				}
			}

			if (validation?.MinimumValue > validation?.MaximumValue)
			{
				errMsg = string.Format(Error.LessThanError, Error.ConstantMinimumValue, Error.ConstantMaximumValue);

				return false;
			}

			if (defaultValue.HasValue)
			{
				if (defaultValue > validation?.MaximumValue || defaultValue < validation?.MinimumValue)
				{
					errMsg = string.Format(Error.DefaultValueError, validation?.MaximumValue, validation?.MinimumValue);

					return false;
				}
			}

			return true;
		}
	}
}
