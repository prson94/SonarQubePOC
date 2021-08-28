using d360.core.entities;
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
    [Trait("Unit tests", "Fields - Show In Column Tests")]
    public class ShowInColumnTests : BaseTest
    {

        [Fact]
        public void PropertyTests()
        {

            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeBooleanApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedOwnershipLookupApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedRelationshipFieldApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedRelationshipLookupApiViewModel(), false));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedRelationshipReferenceListApiViewModel(), false));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeCounterApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeDateApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeDateTimeApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeDecimalApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeHtmlApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeJsonApiViewModel(), false));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeJsonElementApiViewModel(), false));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeLinkApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeLookupApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeNumberApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypePathApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeRelationshipApiViewModel(), false));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeTextApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeTagApiViewModel(), true));
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedScoreApiViewModel(), true));

        }

        private bool IsPropAssumptionCorrect(object obj, bool expectProp)
        {
            obj.get
        }

    }
}