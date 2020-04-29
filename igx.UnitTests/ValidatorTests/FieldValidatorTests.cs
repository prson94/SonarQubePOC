using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using d360.model.validators;
using d360.core.entities;

namespace igx.UnitTests.ValidatorTests
{
    [Trait("Unit tests", "FieldAPI POST-Model Validator")]
    public class PostFieldValidatorTests
    {
        private FieldTypesApiEditModel model = null;
        private TypeIdentifierInfoModel actionTypeModels = null;
        private TypeIdentifierInfoModel assetTypeModels = null;
        private TypeIdentifierInfoModel relationshipTypeModels = null;

        [Fact]
        public void PostInvalidModel_NullModel()
        {
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("No model found"), XMsg.BadResponseMessage);

        }
        [Fact]
        public void PostInvalidModel_NoUID()
        {
            model = new FieldTypesApiEditModel();
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("No Uid found"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void PostInvalidModel_NoType()
        {
            model = new FieldTypesApiEditModel();
            model.RelationshipTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Merge;
            relationshipTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                FriendlyName = "test",
                Type = null,
                Name = "test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }

        [Fact]
        public void PostInvalidModel_ActionNotFound()
        {
            model = new FieldTypesApiEditModel();
            model.ActionTypeUid = Guid.NewGuid();
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Type not found"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void PostInvalidModel_ActionAndAssetAdded()
        {
            model = new FieldTypesApiEditModel();
            model.ActionTypeUid = Guid.NewGuid();
            actionTypeModels = new TypeIdentifierInfoModel();
            model.AssetTypeUid = Guid.NewGuid();

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Parameter error"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void PostInvalidModel_NoAssetFound()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Type not found"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void PostInvalidModel_AssetAndRelationshipAdded()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            assetTypeModels = new TypeIdentifierInfoModel();
            model.RelationshipTypeUid = Guid.NewGuid();

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Parameter error"), XMsg.BadResponseMessage);


        }

        [Fact]
        public void PostInvalidModel_NoRelationshipFound()
        {
            model = new FieldTypesApiEditModel();
            model.RelationshipTypeUid = Guid.NewGuid();

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Type not found"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void PostInvalidModel_InvalidFieldJSON()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            assetTypeModels = new TypeIdentifierInfoModel();

            Assert.Throws<NullReferenceException>(() => FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels));

        }

        [Fact]
        public void PostInvalidModel_NoFieldAsPrimaryKey()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("No primary key defined"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void PostValidModel_FieldWithPrimaryKey()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Text = new FieldTypeDataTypeTextApiViewModel()
                    {
                        IsPartOfKey = true
                    }
                },
                Name = "test",
                FriendlyName = "Test"
            });


            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact]
        public void PostInvalidModel_DuplicateFieldTypes()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Text = new FieldTypeDataTypeTextApiViewModel()
                    {
                        IsPartOfKey = true
                    },
                    DateTime = new FieldTypeDataTypeDateTimeApiViewModel()
                    {

                    }
                },
                Name = "test",
                FriendlyName = "Test"
            });


            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Fields contain errors"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void PostInvalidModel_DuplicateNameFields()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Text = new FieldTypeDataTypeTextApiViewModel()
                    {
                        IsPartOfKey = true,
                    }
                },
                Name = "test",
                FriendlyName = "Test"
            });
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    DateTime = new FieldTypeDataTypeDateTimeApiViewModel()
                    {
                    }
                },
                Name = "test",
                FriendlyName = "Test2"
            });
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Duplicate field names"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void PostInvalidModel_InvalidNameField()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Name = "not.valid#!@$~"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Invalid Field Name"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void PostInvalidModel_TextTypeDecimalMaxLength()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Text = new FieldTypeDataTypeTextApiViewModel()
                    {
                        IsPartOfKey = true,
                        Validation = new FieldTypeDescriptionApiViewModel_ValidationText
                        {
                            MaximumLength = 1.2m
                        }
                    }
                },
                Name = "MaximumTest",
                FriendlyName = "Maximum Test"
            });
            
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }
        [Fact]
        public void PostInvalidModel_TextTypeDecimalMinLength()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Text = new FieldTypeDataTypeTextApiViewModel()
                    {
                        IsPartOfKey = true,
                        Validation = new FieldTypeDescriptionApiViewModel_ValidationText
                        {
                            MinimumLength = 1.1m
                        }
                    }
                },
                Name = "MinimumTest",
                FriendlyName = "Minimum Test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }

        [Fact]
        public void PostInvalidModel_HTMLTypeDecimalMaxLength()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Html = new FieldTypeDataTypeHtmlApiViewModel()
                    {
                        IsPartOfKey = true,
                        Validation = new FieldTypeDescriptionApiViewModel_ValidationText
                        {
                            MaximumLength = 1.2m
                        }
                    }
                },
                Name = "MaximumTest",
                FriendlyName = "Maximum Test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }
        [Fact]
        public void PostInvalidModel_HTMLTypeDecimalMinLength()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Html = new FieldTypeDataTypeHtmlApiViewModel()
                    {
                        IsPartOfKey = true,
                        Validation = new FieldTypeDescriptionApiViewModel_ValidationText
                        {
                            MinimumLength = 1.1m
                        }
                    }
                },
                Name = "MinimumTest",
                FriendlyName = "Minimum Test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }

        [Fact]
        public void PostInvalidModel_NumberTypeDecimalMaxValue()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Number = new FieldTypeDataTypeNumberApiViewModel()
                    {
                        IsPartOfKey = true,
                        Validation = new FieldTypeDescriptionApiViewModel_ValidationMinMaxValue
                        {
                            MaximumValue = 1.2m
                        }
                    }
                },
                Name = "MaximumTest",
                FriendlyName = "Maximum Test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }
        [Fact]
        public void PostInvalidModel_NumberTypeDecimalMinValue()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Number = new FieldTypeDataTypeNumberApiViewModel()
                    {
                        IsPartOfKey = true,
                        Validation = new FieldTypeDescriptionApiViewModel_ValidationMinMaxValue
                        {
                            MinimumValue = 1.1m
                        }
                    }
                },
                Name = "MinimumTest",
                FriendlyName = "Minimum Test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }

        [Fact]
        public void PostInvalidModel_NumberTypeDecimalIncrement()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Number = new FieldTypeDataTypeNumberApiViewModel()
                    {
                        IsPartOfKey = true,
                        Increment = 1.1m                        
                    }
                },
                Name = "IncrementTest",
                FriendlyName = "Increment Test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }

        [Fact]
        public void PostValidModel_DecimalTypeDecimalMaxValue()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Decimal = new FieldTypeDataTypeDecimalApiViewModel()
                    {
                        IsPartOfKey = true,
                        Validation = new FieldTypeDescriptionApiViewModel_ValidationDecimal
                        {
                            MaximumValue = 1.2m
                        }
                    }
                },
                Name = "MaximumTest",
                FriendlyName = "Maximum Test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }
        [Fact]
        public void PostValidModel_DecimalTypeDecimalMinValue()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Decimal = new FieldTypeDataTypeDecimalApiViewModel()
                    {
                        IsPartOfKey = true,
                        Validation = new FieldTypeDescriptionApiViewModel_ValidationDecimal
                        {
                            MinimumValue = 1.1m
                        }
                    }
                },
                Name = "MinimumTest",
                FriendlyName = "Minimum Test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }

        [Fact]
        public void PostValidModel()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Replace;
            assetTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Text = new FieldTypeDataTypeTextApiViewModel()
                    {
                        IsPartOfKey = true,
                    }
                },
                Name = "test",
                FriendlyName = "Test"
            });
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    DateTime = new FieldTypeDataTypeDateTimeApiViewModel()
                    {
                    }
                },
                Name = "test_good",
                FriendlyName = "Test Good"
            });
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }
    }

    [Trait("Unit tests", "FieldAPI Delete-Model Validator")]
    public class DeleteFieldValidatorTests
    {
        private FieldTypesApiDeleteModel model = null;
        private TypeIdentifierInfoModel actionTypeModels = null;
        private TypeIdentifierInfoModel assetTypeModels = null;
        private TypeIdentifierInfoModel relationshipTypeModels = null;

        [Fact]
        public void DeleteInvalidModel_NullModel()
        {
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("No model found"), XMsg.BadResponseMessage);


        }
        [Fact]
        public void DeleteInvalidModel_NoUID()
        {
            model = new FieldTypesApiDeleteModel();
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("No Uid found"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void DeleteInvalidModel_ActionNotFound()
        {
            model = new FieldTypesApiDeleteModel();
            model.ActionTypeUid = Guid.NewGuid();
            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Type not found"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void DeleteInvalidModel_ActionAndAssetAdded()
        {
            model = new FieldTypesApiDeleteModel();
            model.ActionTypeUid = Guid.NewGuid();
            actionTypeModels = new TypeIdentifierInfoModel();
            model.AssetTypeUid = Guid.NewGuid();

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Parameter error"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void DeleteInvalidModel_NoAssetFound()
        {
            model = new FieldTypesApiDeleteModel();
            model.AssetTypeUid = Guid.NewGuid();

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Type not found"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void DeleteInvalidModel_AssetAndRelationshipAdded()
        {
            model = new FieldTypesApiDeleteModel();
            model.AssetTypeUid = Guid.NewGuid();
            assetTypeModels = new TypeIdentifierInfoModel();
            model.RelationshipTypeUid = Guid.NewGuid();

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Parameter error"), XMsg.BadResponseMessage);

        }

        [Fact]
        public void DeleteInvalidModel_NoRelationshipFound()
        {
            model = new FieldTypesApiDeleteModel();
            model.RelationshipTypeUid = Guid.NewGuid();

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.NotFound, XMsg.BadResponseCode);
            Assert.True(valResults.Error.Contains("Type not found"), XMsg.BadResponseMessage);

        }

    }

}


