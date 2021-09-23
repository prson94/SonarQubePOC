using d360.core.entities;
using d360.model;
using System;
using Xunit;

namespace igx.UnitTests.ModelExtensionTests
{
    [Trait("Unit tests", "Model Extention Class - Ownership Lookup Definition")]
    public class ModelExtensionTestsOwnershipLookupDefinition : BaseTest
    {        
        [Fact]
        public void ParseLookupOwnershipDefinition()
        {
            FieldTypeLookup ftl = new FieldTypeLookup();

            ftl.Definition = "{ \"DisplayAsList\" : false, \"DisplayAssignmentSource\":false, \"ExpandGroupMembership\":false, \"ResponsibilityType\":1, \"ResponsibilityTypeUid\":\"fda2a7b4-caf6-4a4b-a887-af187d7e5243\" }";

            FieldTypeOwnershipLookupDefinition res = ftl.ParseOwnershipLookupDefinition();

            Assert.True(res != null, "FieldTypeOwnershipLookupDefinition is null and should not be.");

            Assert.True(!res.DisplayAsList, "FieldTypeOwnershipLookupDefinition DisplayAsList should be false");
            Assert.True(!res.DisplayAssignmentSource, "FieldTypeOwnershipLookupDefinition DisplayAssignmentSource should be false");
            Assert.True(!res.ExpandGroupMembership, "FieldTypeOwnershipLookupDefinition ExpandGroupMembership should be false");
            Assert.True(res.ResponsibilityType == 1, "FieldTypeOwnershipLookupDefinition ResponsibilityType should be 1");
            Assert.True(res.ResponsibilityTypeUid == new Guid("fda2a7b4-caf6-4a4b-a887-af187d7e5243"), "FieldTypeOwnershipLookupDefinition ResponsibilityTypeUid should be fda2a7b4-caf6-4a4b-a887-af187d7e5243");
        }

        [Fact]
        public void ParseLookupOwnershipDefinitionDefaults()
        {
            FieldTypeLookup ftl = new FieldTypeLookup();

            ftl.Definition = "{ }";

            FieldTypeOwnershipLookupDefinition res = ftl.ParseOwnershipLookupDefinition();

            Assert.True(res != null, "FieldTypeOwnershipLookupDefinition is null and should not be.");

            Assert.True(!res.DisplayAsList, "FieldTypeOwnershipLookupDefinition DisplayAsList should be false");
            Assert.True(res.DisplayAssignmentSource, "FieldTypeOwnershipLookupDefinition DisplayAssignmentSource should be true");
            Assert.True(res.ExpandGroupMembership, "FieldTypeOwnershipLookupDefinition ExpandGroupMembership should be true");
            Assert.True(!res.ResponsibilityType.HasValue, "FieldTypeOwnershipLookupDefinition ResponsibilityType should be null");
            Assert.True(!res.ResponsibilityTypeUid.HasValue, "FieldTypeOwnershipLookupDefinition ResponsibilityTypeUid should be null");
        }
    }
}