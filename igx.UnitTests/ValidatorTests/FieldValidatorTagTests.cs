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
    [Trait("Unit tests", "FieldAPI POST-Tag Model Validator")]
    public class PostFieldTagValidatorTests
    {
        private FieldTypesApiEditModel model = null;
        private TypeIdentifierInfoModel actionTypeModels = null;
        private TypeIdentifierInfoModel assetTypeModels = null;
        private TypeIdentifierInfoModel relationshipTypeModels = null;


        [Fact]
        public void PostInValidModel_AddTagToAction()
        {
            model = new FieldTypesApiEditModel();
            model.ActionTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Merge;
            actionTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Tag = new FieldTypeDataTypeTagApiViewModel()
                },
                Name = "test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }

        [Fact]
        public void PostInValidModel_AddTagToRelationship()
        {
            model = new FieldTypesApiEditModel();
            model.RelationshipTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Merge;
            relationshipTypeModels = new TypeIdentifierInfoModel();

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Tag = new FieldTypeDataTypeTagApiViewModel()
                },
                Name = "test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }

        [Fact]
        public void PostInValidModel_InvalidTypesCheck()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Merge;
            assetTypeModels = new TypeIdentifierInfoModel();
            assetTypeModels.Object = "ArtifactType";

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Tag = new FieldTypeDataTypeTagApiViewModel()

                },
                Name = "test",
                FriendlyName = "Test"
            });

            List<string> allTypes = new List<string>() {
                "ArtifactType",
                "AttributeType",
                "GroupType",
                "OrganizationType",
                "PolicyType",
                "ReferenceItemType",
                "ResourceType",
                "RuleType",
                "TaxonomyType"};

            List<string> allowedTypes = new List<string>() {
                "ArtifactType",
                "PolicyType",
                "RuleType",
                "TaxonomyType"};

            foreach (var type in allTypes)
            {
                assetTypeModels.Object = type;
                var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

                if (!allowedTypes.Contains(type))
                    Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
                else
                    Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);

            }
        }

    
        [Fact]
        public void PostInValidModel_AddExisitingTagWithSameName()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Merge;
            assetTypeModels = new TypeIdentifierInfoModel();
            assetTypeModels.Object = "ArtifactType";

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Tag = new FieldTypeDataTypeTagApiViewModel()

                },
                Name = "test",
                FriendlyName = "Test"
            });

            var fieldTypes = new List<FieldType>() { new FieldType() { Type = "Tag", Name = "test" } };

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }
        [Fact]
        public void PostInValidModel_AddExisitingTagWithDifferentName()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Merge;
            assetTypeModels = new TypeIdentifierInfoModel();
            assetTypeModels.Object = "ArtifactType";

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Tag = new FieldTypeDataTypeTagApiViewModel()

                },
                Name = "test1"
            });

            var fieldTypes = new List<FieldType>() { new FieldType() { Type = "Tag", Name = "test" } };

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels, fieldTypes);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.BadRequest, XMsg.BadResponseCode);
        }
        [Fact]
        public void PostValidTagModel()
        {
            model = new FieldTypesApiEditModel();
            model.AssetTypeUid = Guid.NewGuid();
            model.Action = FieldTypesApiEditAction.Merge;
            assetTypeModels = new TypeIdentifierInfoModel();
            assetTypeModels.Object = "ArtifactType";

            model.Fields = new List<FieldTypeApiEditModel>();
            model.Fields.Add(new FieldTypeApiEditModel()
            {
                Type = new FieldTypeDataTypeApiViewModel()
                {
                    Tag = new FieldTypeDataTypeTagApiViewModel()

                },
                Name = "test",
                FriendlyName = "Test"
            });

            var valResults = FieldApiModelValidator.ValidateModel(model, actionTypeModels, assetTypeModels, relationshipTypeModels);

            Assert.True(valResults.StatusCode == System.Net.HttpStatusCode.OK, XMsg.BadResponseCode);
        }
    }
}
