using d360.core;
using d360.core.helpers;
using Xunit;

namespace igx.UnitTests.CoreTests
{
    [Trait("Unit tests", "SystemObject helper tests")]
    public class SystemObjectHelperTests : BaseTest
    {

        public SystemObjectHelperTests()
        {

        }

        [Fact]
        public void SystemObjectHelperArtifact()
        {
            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.BusinessAsset) == SystemObjects.ArtifactType, "Expected BusinessAsset type to return ArtifactType system object");

            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.TechnicalAsset) == SystemObjects.ArtifactType, "Expected TechnicalAsset type to return ArtifactType system object");
        }

        [Fact]
        public void SystemObjectHelperOrganization()
        {
            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.Organization) == SystemObjects.OrganizationType, "Expected Organization type to return OrganizationType system object");
        }

        [Fact]
        public void SystemObjectHelperPolicy()
        {
            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.Policy) == SystemObjects.PolicyType, "Expected Policy type to return Policytype system object");
        }

        [Fact]
        public void SystemObjectHelperReference()
        {
            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.Reference) == SystemObjects.ReferenceItemType, "Expected Reference type to return ReferenceItemType system object");
        }

        [Fact]
        public void SystemObjectHelperRule()
        {
            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.Rule) == SystemObjects.RuleType, "Expected Reference type to return RuleType system object");
        }

        [Fact]
        public void SystemObjectHelperModel()
        {
            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.Model) == SystemObjects.TaxonomyType, "Expected Model type to return TaxonomyType system object");
        }

        [Fact]
        public void SystemObjectHelperFusionAttribute()
        {
            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.FusionAttribute) == SystemObjects.FusionAttributeType, "Expected FusionAttribute type to return FusionAttributeType system object");
        }


        [Fact]
        public void SystemObjectHelperDiagram()
        {
            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.Diagram) == SystemObjects.TaskType, "Expected Diagram type to return TaskType system object");
        }



        [Fact]
        public void SystemObjectHelperDefault()
        {
            Assert.True(SystemObjectHelper.GetSystemObjects(d360.core.enums.AssetTypeClass.User) == SystemObjects.ArtifactType, "Expected default to be ArtifactType");
        }
    }
}