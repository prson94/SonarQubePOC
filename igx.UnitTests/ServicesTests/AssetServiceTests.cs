using System.Collections.Generic;
using System.Text;
using AutoFixture;
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
            var f = new Fixture();


            // ReSharper disable JoinDeclarationAndInitializer 
            AssetType assetType;
            string expectedResult;

            assetType = new AssetType() { Object = "ArtifactType", Class = AssetTypeClass.BusinessAsset, Name = f.Create<string>() };
            expectedResult = $"{CommonNames.AssetTypeClass_Business}: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "ArtifactType", Class = AssetTypeClass.TechnicalAsset, Name = f.Create<string>() };
            expectedResult = $"{CommonNames.AssetTypeClass_Technical}: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "ArtifactType", Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"{assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "PolicyType", Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"{CommonNames.AssetTypeClass_Policy}: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "ReferenceItemType", Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"Reference: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "RuleType", Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"{CommonNames.AssetTypeClass_Rule}: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "TaxonomyType", Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"{CommonNames.AssetTypeClass_Model}: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "AttributeType", Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"Attribute: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "GroupType", Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"Group: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "OrganizationType", Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"Organization: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = "ResourceType", Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"Resource: {assetType.Name}";
            yield return new object[] { assetType, expectedResult };

            assetType = new AssetType() { Object = It.IsAny<string>(), Class = It.IsAny<AssetTypeClass>(), Name = f.Create<string>() };
            expectedResult = $"{assetType.Name}";
            yield return new object[] { assetType, expectedResult };
        }
    }
}
