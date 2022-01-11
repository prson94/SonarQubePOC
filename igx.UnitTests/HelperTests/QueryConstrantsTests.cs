using d360.model;
using d360.model.workflow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.HtmlHelperTests
{
    [Trait("Unit tests", "Query Constants")]
    public class QueryConstantsTests : BaseTest
    {        

        [Fact]
        public void CheckStringExistance()
        {
            Assert.True(!string.IsNullOrEmpty(QueryConstants.HighLevelTypeCaseStatement));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.ArtifactActivityAllDateCountList));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.ShoppingCartItemList));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.ArtifactActivitySpecificDateCountList));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.GroupResourceInfoList));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.InformationCatalogDiagramData));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.LookupAllocations));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.ObjectNymTypes));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.ObjectRelationshipAllCountsWithZero));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.ObjectRelationships));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.ObjectRelationshipTypeIDs));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.PolicySettingsItem));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.ReferenceListTypeRelationshipsAllCountsWithZero));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.RuleSettingsItem));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.ShoppingCartItemList));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.SiteNavPermissions));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.SynonymOptions));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.SynonymsByObjectList));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.SynonymTypes));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.TaxonomySettingsItem));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.WorkflowAssignments));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.WorkflowDiagramLinks));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.WorkflowDiagramNodes));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.WorkflowItemSteps));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.WorkflowList));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.WorkflowObjectTypes));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.WorkflowTypeList));
            Assert.True(!string.IsNullOrEmpty(QueryConstants.WorkflowVersionStepHistory));
        }
    }
}