using d360.core.enums;
using d360.extensions.search;
using System.Collections.Generic;
using Xunit;

namespace igx.UnitTests.SearchTests
{
    [Trait("Unit tests", "Search Extention Class - Models")]
    public class SearchIndexerTests : BaseTest
    {
        public SearchIndexerTests()
        {

        }

        public static IEnumerable<object[]> GetAssetTypeClasses()
        {
            yield return new object[] { "BusinessAsset", 1 };
            yield return new object[] { "Model", 2 };
            yield return new object[] { "Policy", 6 };
            yield return new object[] { "Rule", 7 };
            yield return new object[] { "TechnicalAsset", 8 };
            yield return new object[] { "User", 11 };
            yield return new object[] { "Group", 12 };
            yield return new object[] { "Diagram", 15 };
        }

        [Theory]
        [MemberData(nameof(GetAssetTypeClasses))]
        public void SearchIndexerIsIndexable(string classOrObjectName, int classId)
        {
            Assert.True(SearchIndexer.IsIndexable(classOrObjectName), classOrObjectName + " should be indexable");
        }

        [Theory]
        [InlineData("Invalid", false)]
        public void SearchIndexerIsNotIndexable(string classOrObjectName, bool expected)
        {
            Assert.Equal(expected, SearchIndexer.IsIndexable(classOrObjectName));
        }

        [Theory]
        [MemberData(nameof(GetAssetTypeClasses))]
        [InlineData("ReferenceItemType", 14)]
        public void SearchIndexerGetClassFromCategory(string classOrObjectName, int classId)
        {
            Assert.Equal(classId, SearchIndexer.GetClassFromCategory(classOrObjectName));
        }

        [Theory]
        [MemberData(nameof(GetAssetTypeClasses))]
        [InlineData("Reference", 14)]
        [InlineData("Generic", 0)]
        [InlineData("", 50)]
        public void SearchIndexerGetCategoryFromClass(string classOrObjectName, int classId)
        {
            Assert.Equal(classOrObjectName, SearchIndexer.GetCategoryFromClass(classId));
        }

        [Theory]
        [MemberData(nameof(GetAssetTypeClasses))]
        [InlineData("Reference", 14)]
        [InlineData("Generic", 0)]
        [InlineData("", 50)]
        public void SearchIndexerGetCategoryFromClassByAssetTypeClass(string classOrObjectName, int classId)
        {
            Assert.Equal(classOrObjectName, SearchIndexer.GetCategoryFromClass((AssetTypeClass)classId));
        }

        [Theory]
        [InlineData(50)]
        public void SearchIndexerGetCategoryFromClass_Invalid(int classId)
        {
            Assert.Equal("", SearchIndexer.GetCategoryFromClass(classId));
        }


        [Theory]
        [InlineData("", "")]
        [InlineData("BusinessAsset", "BusinessAsset")]
        [InlineData("Taxonomy", "Model")]
        [InlineData("Resource", "User")]
        [InlineData("ReferenceItemType", "Reference")]
        public void SearchIndexerGetCategoryFromObject(string objectName, string expected)
        {
            Assert.Equal(expected, SearchIndexer.GetCategoryFromObject(objectName));
        }

    }
}




