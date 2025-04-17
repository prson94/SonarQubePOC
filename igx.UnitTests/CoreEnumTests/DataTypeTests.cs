using d360.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.CoreEnumTests
{
    [Trait("Unit tests", "Core DataType enum tests")]
    public class DataTypeTests : BaseTest
    {

        public DataTypeTests()
        {

        }

        [Fact]
        public void DataTypeConversionOptions()
        {
            var allowedConversions = DataType.Text.GetAllowedConversionOptions();

            Assert.True(allowedConversions.Count == 7);

            Assert.True(allowedConversions[0].FromType == "Boolean" && allowedConversions[0].ToType == "Text");
            Assert.True(allowedConversions[1].FromType == "Date" && allowedConversions[1].ToType == "DateWithTime");
            Assert.True(allowedConversions[2].FromType == "Decimal" && allowedConversions[2].ToType == "Percentage");
            Assert.True(allowedConversions[3].FromType == "Number" && allowedConversions[3].ToType == "Decimal");
            Assert.True(allowedConversions[4].FromType == "Number" && allowedConversions[4].ToType == "Percentage");
            Assert.True(allowedConversions[5].FromType == "Text" && allowedConversions[5].ToType == "Html");
            Assert.True(allowedConversions[6].FromType == "ComplexRelationLookup" && allowedConversions[6].ToType == "Relationship");
        }


        [Fact]
        public void DataTypesTotal()
        {
            var dataTypes = DataType.Text.GetDataTypeInfoList();

            Assert.True(dataTypes.Count == 24, "Data types have been added / removed make sure they are unit tested.");
        }

        [Fact]
        public void DataTypeProp()
        {
            var dataTypes = DataType.Text.GetDataTypeInfoList();

            this.ValidateDataTypes(dataTypes);
        }

        private void ValidateDataTypes(List<DataTypeInfo> dataTypes)
        {
			int ix = 0;
            Assert.True(dataTypes[ix].ID == DataType.Boolean);
            Assert.True(dataTypes[ix].Description == "True/False");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Boolean");

			ix = 1;
			Assert.True(dataTypes[ix].ID == DataType.Date);
            Assert.True(dataTypes[ix].Description == "Date");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Date");

			ix = 2;
			Assert.True(dataTypes[ix].ID == DataType.DateTime);
            Assert.True(dataTypes[ix].Description == "Date With Time");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "DateTime");

			ix = 3;
			Assert.True(dataTypes[ix].ID == DataType.Hidden);
            Assert.True(dataTypes[ix].Description == "Hidden");
            Assert.True(dataTypes[ix].ReadOnly == true);
            Assert.True(dataTypes[ix].Name == "Hidden");

			ix = 4;
			Assert.True(dataTypes[ix].ID == DataType.Html);
            Assert.True(dataTypes[ix].Description == "Html/Richtext");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Html");

			ix = 5;
			Assert.True(dataTypes[ix].ID == DataType.Number);
            Assert.True(dataTypes[ix].Description == "Number");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Number");

			ix = 6;
			Assert.True(dataTypes[ix].ID == DataType.Decimal);
            Assert.True(dataTypes[ix].Description == "Decimal Number");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Decimal");

			ix = 7;
			Assert.True(dataTypes[ix].ID == DataType.Lookup);
            Assert.True(dataTypes[ix].Description == "List");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Lookup");

			ix = 8;
			Assert.True(dataTypes[ix].ID == DataType.Text);
            Assert.True(dataTypes[ix].Description == "Simple Text");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Text");

			ix = 9;
			Assert.True(dataTypes[ix].ID == DataType.Link);
            Assert.True(dataTypes[ix].Description == "Link");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Link");

			ix = 10;
			Assert.True(dataTypes[ix].ID == DataType.Color);
            Assert.True(dataTypes[ix].Description == "Color Picker");
            Assert.True(dataTypes[ix].ReadOnly == true);
            Assert.True(dataTypes[ix].Name == "Color");

			ix = 11;
			Assert.True(dataTypes[ix].ID == DataType.Path);
            Assert.True(dataTypes[ix].Description == "Asset Path");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Path");

			ix = 12;
			Assert.True(dataTypes[ix].ID == DataType.ComplexRelationLookup);
            Assert.True(dataTypes[ix].Description == "Relation Lookup");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "ComplexRelationLookup");

			ix = 13;
			Assert.True(dataTypes[ix].ID == DataType.OwnershipLookup);
            Assert.True(dataTypes[ix].Description == "Ownership Lookup");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "OwnershipLookup");

			ix = 14;
			Assert.True(dataTypes[ix].ID == DataType.Relationship);
            Assert.True(dataTypes[ix].Description == "Relationship");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Relationship");

			ix = 15;
			Assert.True(dataTypes[ix].ID == DataType.FieldFromRelationship);
            Assert.True(dataTypes[ix].Description == "Field from Relationship");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "FieldFromRelationship");

			ix = 16;
			Assert.True(dataTypes[ix].ID == DataType.JSON);
            Assert.True(dataTypes[ix].Description == "JSON");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "JSON");

			ix = 17;
			Assert.True(dataTypes[ix].ID == DataType.JsonElement);
            Assert.True(dataTypes[ix].Description == "JSON Attribute");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "JsonElement");

			ix = 18;
			Assert.True(dataTypes[ix].ID == DataType.Tag);
            Assert.True(dataTypes[ix].Description == "Tag");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Tag");

			ix = 19;
			Assert.True(dataTypes[ix].ID == DataType.Score);
            Assert.True(dataTypes[ix].Description == "Score");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Score");

			ix = 20;
			Assert.True(dataTypes[ix].ID == DataType.Counter);
            Assert.True(dataTypes[ix].Description == "Counter");
            Assert.True(dataTypes[ix].ReadOnly == false);
            Assert.True(dataTypes[ix].Name == "Counter");
        }

        [Fact]
        public void DataTypePropExcludedTaxonomy()
        {
            var dataTypes = DataType.Text.GetDataTypeInfoList(SystemObjects.TaxonomyType);

            this.ValidateDataTypes(dataTypes);
        }
    }
}
