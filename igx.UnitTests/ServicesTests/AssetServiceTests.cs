using System.Collections.Generic;
using System.Text;
using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using d360.web.Services;
using FluentAssertions;
using Moq;
using SmartFormat.Utilities;
using Xunit;

namespace igx.UnitTests.ServicesTests
{
    public class AssetServiceTests
    {
        private AssetService TestedObject { get; }

        public AssetServiceTests()
        {
            TestedObject = new AssetService();
        }

        [Theory, MemberData(nameof(GetAssetNameTestData))]
        internal void GetAssetName_Valid(AssetType entity, string expectedResult)
        {
            // assign

            // act
            var actualResult = TestedObject.GetAssetName(entity);

            // assert
            actualResult.Should().Be(expectedResult);
        }

        public static IEnumerable<object[]> GetAssetNameTestData()
        {
            return new List<object[]>
            {
                new object[] { new AssetType() { Object = "ArtifactType", Class = AssetTypeClass.BusinessAsset}, CommonNames.AssetTypeClass_Business },
                new object[] { new AssetType() { Object = "ArtifactType", Class = AssetTypeClass.TechnicalAsset}, CommonNames.AssetTypeClass_Technical},
                new object[] { new AssetType() { Object = "ArtifactType", Class = It.IsAny<AssetTypeClass>() }, string.Empty },
                new object[] { new AssetType() { Object = "PolicyType", Class = It.IsAny<AssetTypeClass>()}, CommonNames.AssetTypeClass_Policy },
                new object[] { new AssetType() { Object = "ReferenceItemType", Class = It.IsAny<AssetTypeClass>() }, "Reference: " },
                new object[] { new AssetType() { Object = "RuleType", Class = It.IsAny<AssetTypeClass>() }, CommonNames.AssetTypeClass_Rule },
                new object[] { new AssetType() { Object = "TaxonomyType", Class = It.IsAny<AssetTypeClass>() }, CommonNames.AssetTypeClass_Model },
                new object[] { new AssetType() { Object = "AttributeType", Class = It.IsAny<AssetTypeClass>() }, "Attribute: " },
                new object[] { new AssetType() { Object = "GroupType", Class = It.IsAny<AssetTypeClass>() }, "Group: " },
                new object[] { new AssetType() { Object = "OrganizationType", Class = It.IsAny<AssetTypeClass>() }, "Organization: " },
                new object[] { new AssetType() { Object = "ResourceType", Class = It.IsAny<AssetTypeClass>() }, "Resource: " },
                new object[] { new AssetType() { Object = It.IsAny<string>(), Class = It.IsAny<AssetTypeClass>() }, string.Empty },
            };
        }
    }
}
