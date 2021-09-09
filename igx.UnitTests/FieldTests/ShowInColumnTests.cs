using d360.core.entities;
using d360.model;
using d360.model.workflow;
using d360.web.Extensions;
using d360.web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.FieldsHelperTests
{
    [Trait("Unit tests", "Fields - Show In Column Tests")]
    public class ShowInColumnTests : BaseTest
    {

        [Fact]
        public void PropertyTests()
        {

            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeBooleanApiViewModel(), true), "Boolean property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedOwnershipLookupApiViewModel(), true), "Ownership Lookup property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedRelationshipFieldApiViewModel(), true), "Relationship Field property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedRelationshipLookupApiViewModel(), false), "Relation Lookup property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedRelationshipReferenceListApiViewModel(), false), "Relation Reference List property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeCounterApiViewModel(), true), "Counter property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeDateApiViewModel(), true), "Date property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeDateTimeApiViewModel(), true), "Date Time property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeDecimalApiViewModel(), true), "Decimal property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeHtmlApiViewModel(), true), "HTML property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeJsonApiViewModel(), false), "JSON property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeJsonElementApiViewModel(), false), "JSON Element property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeLinkApiViewModel(), true), "Link property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeLookupApiViewModel(), true), "Lookup property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeNumberApiViewModel(), true), "Number property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypePathApiViewModel(), true), "Path property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeRelationshipApiViewModel(), true), "Relationship property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeTextApiViewModel(), true), "Text property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeTagApiViewModel(), false), "Tag property check failed");
            Assert.True(IsPropAssumptionCorrect(new FieldTypeDataTypeComputedScoreApiViewModel(), true), "Score property check failed");

        }

        private bool IsPropAssumptionCorrect(object obj, bool expectProp)
        {
            var hasProp = obj.GetType().GetProperty("DisplayInColumn") != null;
            return hasProp == expectProp;
        }

        [Fact]
        public void TestAllInRows()
        {
            var fcmap = new List<FieldColumnMapping>();
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = false });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = null });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = false });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = false });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = null });

            var fcMapper = new FieldColumnMapper(fcmap, null);
            fcMapper.TransformRowsAndCols();

            Assert.True(fcMapper.FieldColumnMappings[0].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[0].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[1].Row == 2);
            Assert.True(fcMapper.FieldColumnMappings[1].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[2].Row == 3);
            Assert.True(fcMapper.FieldColumnMappings[2].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[3].Row == 4);
            Assert.True(fcMapper.FieldColumnMappings[3].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[4].Row == 5);
            Assert.True(fcMapper.FieldColumnMappings[4].Col == 1);
        }

        [Fact]
        public void TestAllInRowsWithDisplayInColumns()
        {
            var fcmap = new List<FieldColumnMapping>();
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = null });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = false });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });

            var fcMapper = new FieldColumnMapper(fcmap, null);
            fcMapper.TransformRowsAndCols();

            Assert.True(fcMapper.FieldColumnMappings[0].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[0].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[1].Row == 2);
            Assert.True(fcMapper.FieldColumnMappings[1].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[2].Row == 3);
            Assert.True(fcMapper.FieldColumnMappings[2].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[3].Row == 4);
            Assert.True(fcMapper.FieldColumnMappings[3].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[4].Row == 5);
            Assert.True(fcMapper.FieldColumnMappings[4].Col == 1);
        }

        [Fact]
        public void TestAllInOneRow()
        {
            var fcmap = new List<FieldColumnMapping>();
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });

            var fcMapper = new FieldColumnMapper(fcmap, null);
            fcMapper.TransformRowsAndCols();
            Assert.True(fcMapper.FieldColumnMappings[0].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[0].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[1].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[1].Col == 2);

            Assert.True(fcMapper.FieldColumnMappings[2].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[2].Col == 3);

            Assert.True(fcMapper.FieldColumnMappings[3].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[3].Col == 4);

            Assert.True(fcMapper.FieldColumnMappings[4].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[4].Col == 5);
        }

        [Fact]
        public void Test2Rows()
        {
            var fcmap = new List<FieldColumnMapping>();
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = false });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });

            var fcMapper = new FieldColumnMapper(fcmap, null);
            fcMapper.TransformRowsAndCols();

            Assert.True(fcMapper.FieldColumnMappings[0].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[0].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[1].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[1].Col == 2);

            Assert.True(fcMapper.FieldColumnMappings[2].Row == 2);
            Assert.True(fcMapper.FieldColumnMappings[2].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[3].Row == 3);
            Assert.True(fcMapper.FieldColumnMappings[3].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[4].Row == 3);
            Assert.True(fcMapper.FieldColumnMappings[4].Col == 2);
        }

        [Fact]
        public void TestAllInLastRow()
        {
            var fcmap = new List<FieldColumnMapping>();
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = null });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });

            var fcMapper = new FieldColumnMapper(fcmap, null);
            fcMapper.TransformRowsAndCols();

            Assert.True(fcMapper.FieldColumnMappings[0].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[0].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[1].Row == 2);
            Assert.True(fcMapper.FieldColumnMappings[1].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[2].Row == 2);
            Assert.True(fcMapper.FieldColumnMappings[2].Col == 2);

            Assert.True(fcMapper.FieldColumnMappings[3].Row == 2);
            Assert.True(fcMapper.FieldColumnMappings[3].Col == 3);

            Assert.True(fcMapper.FieldColumnMappings[4].Row == 2);
            Assert.True(fcMapper.FieldColumnMappings[4].Col == 4);
        }

        [Fact]
        public void TestAllInFirstRow()
        {
            var fcmap = new List<FieldColumnMapping>();
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { DisplayInColumn = false });

            var fcMapper = new FieldColumnMapper(fcmap, null);
            fcMapper.TransformRowsAndCols();

            Assert.True(fcMapper.FieldColumnMappings[0].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[0].Col == 1);

            Assert.True(fcMapper.FieldColumnMappings[1].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[1].Col == 2);

            Assert.True(fcMapper.FieldColumnMappings[2].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[2].Col == 3);

            Assert.True(fcMapper.FieldColumnMappings[3].Row == 1);
            Assert.True(fcMapper.FieldColumnMappings[3].Col == 4);

            Assert.True(fcMapper.FieldColumnMappings[4].Row == 2);
            Assert.True(fcMapper.FieldColumnMappings[4].Col == 1);
        }

        [Fact]
        public void UpdateModelRowsAndCols()
        {
            var fcmap = new List<FieldColumnMapping>();
            fcmap.Add(new FieldColumnMapping() { Name = "Field1", DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { Name = "Field2", DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { Name = "Field3", DisplayInColumn = false });
            fcmap.Add(new FieldColumnMapping() { Name = "Field4", DisplayInColumn = true });
            fcmap.Add(new FieldColumnMapping() { Name = "Field5", DisplayInColumn = true });

            DetailReadOnlyModel model = new DetailReadOnlyModel();
            var dynamiLoadedFields = new List<DetailReadOnlyRowModel>();
            dynamiLoadedFields.Add(new DetailReadOnlyRowModel { FirstColumnFields = new List<ReadOnlyField> { new ReadOnlyField { FieldName = "Field1" } } });
            dynamiLoadedFields.Add(new DetailReadOnlyRowModel { FirstColumnFields = new List<ReadOnlyField> { new ReadOnlyField { FieldName = "Field2" } } });
            dynamiLoadedFields.Add(new DetailReadOnlyRowModel { FirstColumnFields = new List<ReadOnlyField> { new ReadOnlyField { FieldName = "Field3" } } });
            dynamiLoadedFields.Add(new DetailReadOnlyRowModel { FirstColumnFields = new List<ReadOnlyField> { new ReadOnlyField { FieldName = "Field4" } } });
            dynamiLoadedFields.Add(new DetailReadOnlyRowModel { FirstColumnFields = new List<ReadOnlyField> { new ReadOnlyField { FieldName = "Field5" } } });

            var fcMapper = new FieldColumnMapper(fcmap, model);
            fcMapper.TransformRowsAndCols();
            fcMapper.ArrangeRowsAndCols(dynamiLoadedFields);

            Assert.True(model.rows[0].FirstColumnFields.Count == 2);
            Assert.True(model.rows[1].FirstColumnFields.Count == 1);
            Assert.True(model.rows[2].FirstColumnFields.Count == 2);
            
        }

    }
}