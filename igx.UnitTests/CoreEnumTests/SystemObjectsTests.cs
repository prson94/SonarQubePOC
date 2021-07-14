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
            Assert.True(SystemObjects.ArtifactType.ExcludeDataType() == DataType.None);
            Assert.True(SystemObjects.Artifact.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.AttributeTypeCategory.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.Claim.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.ConnectorLabel.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter));
            Assert.True(SystemObjects.Contract.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.EmailTemplate.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.ExportTemplate.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Field.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.FieldType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Fusion.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.FusionAttribute.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.FusionAttributeType.ExcludeDataType() == (DataType.OwnershipLookup | DataType.Path | DataType.RefListRelationship | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.FusionExecution.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.FusionQueryAttribute.ExcludeDataType() == (DataType.Path | DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.FusionQueryAttributeType.ExcludeDataType() == (DataType.ComplexRelationLookup | DataType.FieldFromRelationship | DataType.JSON | DataType.JsonElement | DataType.Link | DataType.Lookup | DataType.OwnershipLookup | DataType.Path | DataType.RefListRelationship | DataType.Relationship | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.FusionType.ExcludeDataType() == (DataType.ComplexRelationLookup | DataType.FieldFromRelationship | DataType.JSON | DataType.JsonElement |
            DataType.Link | DataType.Lookup | DataType.OwnershipLookup | DataType.Path | DataType.RefListRelationship | DataType.Relationship | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.Group.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.GroupType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Intersect.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.IntersectType.ExcludeDataType() == (DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Issue.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.IssueType.ExcludeDataType() == (DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.IssueTypeRelation.ExcludeDataType() == (DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.Load.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.Map.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.MapType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Monitor.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Organization.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.OrganizationDomain.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.OrganizationInvitation.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.OrganizationType.ExcludeDataType() == (DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.ComplexRelationLookup | DataType.Relationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.Policy.ExcludeDataType() == (DataType.JSON | DataType.JsonElement));
            Assert.True(SystemObjects.PolicyType.ExcludeDataType() == DataType.None);
            Assert.True(SystemObjects.Predicate.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.ReferenceItem.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.ReferenceItemType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.Report.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Resource.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.ResourceType.ExcludeDataType() == (DataType.FieldFromRelationship |
            DataType.OwnershipLookup | DataType.RefListRelationship | DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.ResponseType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Responsibility.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.ResponsibilityType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.ResponsibilityTypeClaim.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.Rule.ExcludeDataType() == DataType.None);
            Assert.True(SystemObjects.RuleDimension.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.RuleType.ExcludeDataType() == DataType.None);
            Assert.True(SystemObjects.Score.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.ScoreType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.ScoreTypeMetric.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.ShoppingCart.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.ShoppingCartType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
            Assert.True(SystemObjects.SurveyType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Synonym.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.SynonymType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Tag.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Counter));
            Assert.True(SystemObjects.Task.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter));
            Assert.True(SystemObjects.TaskType.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.ComplexRelationLookup | DataType.RefListRelationship | DataType.FieldFromRelationship | DataType.OwnershipLookup | DataType.Relationship | DataType.Counter));
            Assert.True(SystemObjects.Taxonomy.ExcludeDataType() == (DataType.Tag));
            Assert.True(SystemObjects.TaxonomyType.ExcludeDataType() == (DataType.None));
            Assert.True(SystemObjects.TooltipTemplate.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.Unknown.ExcludeDataType() == (DataType.JSON | DataType.Path | DataType.Tag | DataType.Counter));
            Assert.True(SystemObjects.WorkflowTypeRelation.ExcludeDataType() == (DataType.JSON | DataType.JsonElement | DataType.Path | DataType.Tag | DataType.Score | DataType.Counter));
        }

    }
}
