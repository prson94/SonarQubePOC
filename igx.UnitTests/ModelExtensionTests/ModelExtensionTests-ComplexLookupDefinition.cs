using d360.core.entities;
using d360.model;
using d360.model.helpers;
using igx.UnitTests.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace igx.UnitTests.ModelExtensionTests
{
    [Trait("Unit tests", "Model Extention Class - Complex Lookup Definition")]
    public class ModelExtensionTestsComplexLookupDefinition : BaseTest
    {

        public ModelExtensionTestsComplexLookupDefinition()
        {

        }

        [Fact]
        public void ParseLookupComplexDefinition()
        {
            FieldTypeLookup ftl = new FieldTypeLookup();

            ftl.Definition = "{ \"DisplayAsList\" : false, \"DisplayAssignmentSource\":false, \"ExpandGroupMembership\":false, \"ResponsibilityType\":1, \"ResponsibilityTypeUid\":\"fda2a7b4-caf6-4a4b-a887-af187d7e5243\" }";

            FieldTypeComplexLookupDefinition res = ftl.ParseComplexLookupDefinition();

            Assert.True(res != null, "FieldTypeComplexLookupDefinition is null and should not be.");

            Assert.True(res.Relations == null, "FieldTypeComplexLookupDefinition Relations is not null and should be");
            Assert.True(res.Fields == null, "FieldTypeComplexLookupDefinition Fields is not null and should be");
        }

        [Fact]
        public void GetFriendlyNameJSONContract()
        {
            FieldTypeLookup ftl = new FieldTypeLookup();

            ftl.Definition = "{ \"DisplayAsList\" : false, \"DisplayAssignmentSource\":false, \"ExpandGroupMembership\":false, \"ResponsibilityType\":1, \"ResponsibilityTypeUid\":\"fda2a7b4-caf6-4a4b-a887-af187d7e5243\" }";

            FieldTypeComplexLookupDefinition res = ftl.ParseComplexLookupDefinition();

            var field = new FieldTypeComplexLookupDefinitionField();
            field.AssetTypeUid = Guid.Parse(DataConstants.ValidGUID);
            field.FieldTypeID = 1;
            field.FieldTypeName = "Related Item.";

            var relation = new FieldTypeComplexLookupDefinitionRelation();
            relation.AssetTypeUid = Guid.Parse(DataConstants.ValidGUID);
            res.Fields = new List<FieldTypeComplexLookupDefinitionField>();
            res.Relations = new List<FieldTypeComplexLookupDefinitionRelation>();

            res.Fields.Add(field);
            res.Relations.Add(relation);

            CustomJSONContractResolver contract = res.GetFriendlyNameJSONContract();

            Assert.True(contract != null, "CustomJSONContractResolver is null and should not be.");
            Assert.True(contract.DynamicCodeGeneration == true, "DynamicCodeGeneration is not true and should not be.");
            Assert.True(contract.GetResolvedPropertyName("H1_Uid") == "Asset.[0].Uid", "Property H1_Uid not returning correct value.");
            Assert.True(contract.GetResolvedPropertyName("H1_1_IntersectTypeUid") == "Asset.[0].RelatedItems.[0].IntersectTypeUid", "Property H1_1_IntersectTypeUid not returning correct value.");
        }

        [Fact]
        public void GetFieldMapings()
        {
            FieldTypeLookup ftl = new FieldTypeLookup();

            ftl.Definition = "{ \"DisplayAsList\" : false, \"DisplayAssignmentSource\":false, \"ExpandGroupMembership\":false, \"ResponsibilityType\":1, \"ResponsibilityTypeUid\":\"fda2a7b4-caf6-4a4b-a887-af187d7e5243\" }";

            FieldTypeComplexLookupDefinition res = ftl.ParseComplexLookupDefinition();

            var field = new FieldTypeComplexLookupDefinitionField();
            field.AssetTypeUid = Guid.Parse(DataConstants.ValidGUID);
            field.FieldTypeID = 1;
            field.FieldTypeName = "Related Item.";

            var relation = new FieldTypeComplexLookupDefinitionRelation();
            relation.AssetTypeUid = Guid.Parse(DataConstants.ValidGUID);
            res.Fields = new List<FieldTypeComplexLookupDefinitionField>();
            res.Relations = new List<FieldTypeComplexLookupDefinitionRelation>();

            res.Fields.Add(field);
            res.Relations.Add(relation);

            Dictionary<string, FieldTypeComplexLookupDefinitionField> mappings = res.GetFieldMapings();

            var i = mappings.First();

            var assetTypeUid = mappings["H1_1_IntersectTypeUid"].AssetTypeUid;

            Assert.True(mappings != null, "FieldMapings is null and should not be.");
            Assert.True(assetTypeUid == Guid.Parse("f8bf1431-0d7b-4381-9cec-dd32c05e0158"), "AssetTypeUid in FieldMapings is not a match and should be.");
            Assert.True(mappings.Count == 6, "FieldMapings count is not 6 and should be.");
            Assert.True(i.Value == null, "First Value in FieldMapings is not null and should be.");
        }

        [Fact]
        public void GetFriendlyNamesMapping()
        {
            FieldTypeLookup ftl = new FieldTypeLookup();

            ftl.Definition = "{ \"DisplayAsList\" : false, \"DisplayAssignmentSource\":false, \"ExpandGroupMembership\":false, \"ResponsibilityType\":1, \"ResponsibilityTypeUid\":\"fda2a7b4-caf6-4a4b-a887-af187d7e5243\" }";

            FieldTypeComplexLookupDefinition res = ftl.ParseComplexLookupDefinition();

            var field = new FieldTypeComplexLookupDefinitionField();
            field.AssetTypeUid = Guid.Parse(DataConstants.ValidGUID);
            field.FieldTypeID = 1;
            field.FieldTypeName = "Related Item.";
            field.RelationIndex = 1000;

            var relation = new FieldTypeComplexLookupDefinitionRelation();
            relation.AssetTypeUid = Guid.Parse(DataConstants.ValidGUID);
            res.Fields = new List<FieldTypeComplexLookupDefinitionField>();
            res.Relations = new List<FieldTypeComplexLookupDefinitionRelation>();

            res.Fields.Add(field);
            res.Relations.Add(relation);

            Dictionary<string, string> mappings = res.GetFriendlyNamesMapping();

            var i = mappings["H1001_1_IntersectTypeUid"];

            Assert.True(mappings != null, "FriendlyNamesMapping is null and should not be.");
            Assert.True(mappings["H1001_1_DisplayValue"] == "Asset.[1000].RelatedItems.[0].DisplayValue", "One of the FriendlyNamesMapping values is incorrect");
            Assert.True(mappings["H1001_1_IntersectTypeUid"] == "Asset.[1000].RelatedItems.[0].IntersectTypeUid", "One of the FriendlyNamesMapping values is incorrect");
            Assert.True(mappings.Count == 6, "FriendlyNamesMapping count is not 6 and should be.");
        }

        [Fact]
        public void UnflattenJsonDefault()
        {
            FieldTypeLookup ftl = new FieldTypeLookup();

            ftl.Definition = "{ }";

            FieldTypeComplexLookupDefinition res = ftl.ParseComplexLookupDefinition();

            List<dynamic> values = new List<dynamic>();

            res.UnflattenJson(values);

            Assert.True(res.Relations == null, "FieldTypeComplexLookupDefinition Relations is not null and should be after UnflattenJson");
            Assert.True(res.Fields == null, "FieldTypeComplexLookupDefinition Fields is not null and should be after UnflattenJson");
        }

    }
}