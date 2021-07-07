using d360.core.entities;
using d360.model;
using System;
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

    }
}