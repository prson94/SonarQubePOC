using d360.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.CoreEnumTests
{
    [Trait("Unit tests", "Core SystemObjects enum tests")]
    public class SystemObjectsTests : BaseTest
    {

        public SystemObjectsTests()
        {
        }

        [Fact]
        public void SystemObjectsExcludedDataType()
        {            
            Assert.True(SystemObjects.ArtifactType.ExcludeDataType() == DataType.System);
            Assert.True(SystemObjects.Artifact.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.AttributeTypeCategory.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Claim.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ConnectorLabel.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.EmailTemplate.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ExportTemplate.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Field.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.FieldType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Group.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.GroupType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.Score | DataType.Html | DataType.Link | DataType.System));
            Assert.True(SystemObjects.Intersect.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.IntersectType.ExcludeDataType() == (DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Issue.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.IssueType.ExcludeDataType() == (DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.IssueTypeRelation.ExcludeDataType() == (DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Load.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Map.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.MapType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Monitor.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Policy.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.System));
            Assert.True(SystemObjects.PolicyType.ExcludeDataType() == DataType.System);
            Assert.True(SystemObjects.Predicate.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ReferenceItem.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ReferenceItemType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.Report.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Resource.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ResourceType.ExcludeDataType() == (DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ResponseType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Responsibility.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ResponsibilityType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ResponsibilityTypeClaim.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Rule.ExcludeDataType() == DataType.System);
            Assert.True(SystemObjects.RuleDimension.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.RuleType.ExcludeDataType() == DataType.System);
            Assert.True(SystemObjects.Score.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ScoreType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ScoreTypeMetric.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ShoppingCart.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.ShoppingCartType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.SurveyType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Synonym.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.SynonymType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Tag.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Task.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.TaskType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Taxonomy.ExcludeDataType() == (DataType.Tag | DataType.System));
            Assert.True(SystemObjects.TaxonomyType.ExcludeDataType() == DataType.System);
            Assert.True(SystemObjects.TooltipTemplate.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.Unknown.ExcludeDataType() == (DataType.JSON | DataType.Path | DataType.Tag | DataType.Counter | DataType.System));
            Assert.True(SystemObjects.WorkflowTypeRelation.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter | DataType.System));
        }

    }
}
