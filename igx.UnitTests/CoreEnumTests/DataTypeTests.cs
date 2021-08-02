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

            Assert.True(dataTypes.Count == 27, "Data types have been added / removed make sure they are unit tested.");
        }

        [Fact]
        public void DataTypeProp()
        {
            var dataTypes = DataType.Text.GetDataTypeInfoList();

            this.ValidateDataTypes(dataTypes);
        }

        private void ValidateDataTypes(List<DataTypeInfo> dataTypes)
        {
            Assert.True(dataTypes[0].ID == DataType.Boolean);
            Assert.True(dataTypes[0].Description == "True/False");
            Assert.True(dataTypes[0].ReadOnly == false);
            Assert.True(dataTypes[0].Name == "Boolean");

            Assert.True(dataTypes[1].ID == DataType.Date);
            Assert.True(dataTypes[1].Description == "Date");
            Assert.True(dataTypes[1].ReadOnly == false);
            Assert.True(dataTypes[1].Name == "Date");

            Assert.True(dataTypes[2].ID == DataType.DateTime);
            Assert.True(dataTypes[2].Description == "Date With Time");
            Assert.True(dataTypes[2].ReadOnly == false);
            Assert.True(dataTypes[2].Name == "DateTime");

            Assert.True(dataTypes[3].ID == DataType.File);
            Assert.True(dataTypes[3].Description == "File");
            Assert.True(dataTypes[3].ReadOnly == true);
            Assert.True(dataTypes[3].Name == "File");

            Assert.True(dataTypes[4].ID == DataType.Hidden);
            Assert.True(dataTypes[4].Description == "Hidden");
            Assert.True(dataTypes[4].ReadOnly == true);
            Assert.True(dataTypes[4].Name == "Hidden");

            Assert.True(dataTypes[5].ID == DataType.Html);
            Assert.True(dataTypes[5].Description == "Html/Richtext");
            Assert.True(dataTypes[5].ReadOnly == false);
            Assert.True(dataTypes[5].Name == "Html");

            Assert.True(dataTypes[6].ID == DataType.Number);
            Assert.True(dataTypes[6].Description == "Number");
            Assert.True(dataTypes[6].ReadOnly == false);
            Assert.True(dataTypes[6].Name == "Number");

            Assert.True(dataTypes[7].ID == DataType.Decimal);
            Assert.True(dataTypes[7].Description == "Decimal Number");
            Assert.True(dataTypes[7].ReadOnly == false);
            Assert.True(dataTypes[7].Name == "Decimal");

            Assert.True(dataTypes[8].ID == DataType.Lookup);
            Assert.True(dataTypes[8].Description == "List");
            Assert.True(dataTypes[8].ReadOnly == false);
            Assert.True(dataTypes[8].Name == "Lookup");

            Assert.True(dataTypes[9].ID == DataType.Text);
            Assert.True(dataTypes[9].Description == "Simple Text");
            Assert.True(dataTypes[9].ReadOnly == false);
            Assert.True(dataTypes[9].Name == "Text");

            Assert.True(dataTypes[10].ID == DataType.Password);
            Assert.True(dataTypes[10].Description == "Password");
            Assert.True(dataTypes[10].ReadOnly == true);
            Assert.True(dataTypes[10].Name == "Password");

            Assert.True(dataTypes[11].ID == DataType.Link);
            Assert.True(dataTypes[11].Description == "Link");
            Assert.True(dataTypes[11].ReadOnly == false);
            Assert.True(dataTypes[11].Name == "Link");

            Assert.True(dataTypes[12].ID == DataType.UncLink);
            Assert.True(dataTypes[12].Description == "UNC/File Link");
            Assert.True(dataTypes[12].ReadOnly == true);
            Assert.True(dataTypes[12].Name == "UncLink");

            Assert.True(dataTypes[13].ID == DataType.Color);
            Assert.True(dataTypes[13].Description == "Color Picker");
            Assert.True(dataTypes[13].ReadOnly == true);
            Assert.True(dataTypes[13].Name == "Color");

            Assert.True(dataTypes[14].ID == DataType.Path);
            Assert.True(dataTypes[14].Description == "Asset Path");
            Assert.True(dataTypes[14].ReadOnly == false);
            Assert.True(dataTypes[14].Name == "Path");

            Assert.True(dataTypes[15].ID == DataType.ComplexRelationLookup);
            Assert.True(dataTypes[15].Description == "Relation Lookup");
            Assert.True(dataTypes[15].ReadOnly == false);
            Assert.True(dataTypes[15].Name == "ComplexRelationLookup");

            Assert.True(dataTypes[16].ID == DataType.Percentage);
            Assert.True(dataTypes[16].Description == "Percentage");
            Assert.True(dataTypes[16].ReadOnly == true);
            Assert.True(dataTypes[16].Name == "Percentage");

            Assert.True(dataTypes[17].ID == DataType.DataTableSelect);
            Assert.True(dataTypes[17].Description == "DataTableSelect");
            Assert.True(dataTypes[17].ReadOnly == true);
            Assert.True(dataTypes[17].Name == "DataTableSelect");

            Assert.True(dataTypes[18].ID == DataType.OwnershipLookup);
            Assert.True(dataTypes[18].Description == "Ownership Lookup");
            Assert.True(dataTypes[18].ReadOnly == false);
            Assert.True(dataTypes[18].Name == "OwnershipLookup");

            Assert.True(dataTypes[19].ID == DataType.Relationship);
            Assert.True(dataTypes[19].Description == "Relationship");
            Assert.True(dataTypes[19].ReadOnly == false);
            Assert.True(dataTypes[19].Name == "Relationship");

            Assert.True(dataTypes[20].ID == DataType.FieldFromRelationship);
            Assert.True(dataTypes[20].Description == "Field from Relationship");
            Assert.True(dataTypes[20].ReadOnly == false);
            Assert.True(dataTypes[20].Name == "FieldFromRelationship");

            Assert.True(dataTypes[21].ID == DataType.RefListRelationship);
            Assert.True(dataTypes[21].Description == "Reference Item List from Relationship");
            Assert.True(dataTypes[21].ReadOnly == false);
            Assert.True(dataTypes[21].Name == "RefListRelationship");

            Assert.True(dataTypes[22].ID == DataType.JSON);
            Assert.True(dataTypes[22].Description == "JSON");
            Assert.True(dataTypes[22].ReadOnly == false);
            Assert.True(dataTypes[22].Name == "JSON");

            Assert.True(dataTypes[23].ID == DataType.JsonElement);
            Assert.True(dataTypes[23].Description == "JSON Attribute");
            Assert.True(dataTypes[23].ReadOnly == false);
            Assert.True(dataTypes[23].Name == "JsonElement");

            Assert.True(dataTypes[24].ID == DataType.Tag);
            Assert.True(dataTypes[24].Description == "Tag");
            Assert.True(dataTypes[24].ReadOnly == false);
            Assert.True(dataTypes[24].Name == "Tag");

            Assert.True(dataTypes[25].ID == DataType.Score);
            Assert.True(dataTypes[25].Description == "Score");
            Assert.True(dataTypes[25].ReadOnly == false);
            Assert.True(dataTypes[25].Name == "Score");

            Assert.True(dataTypes[26].ID == DataType.Counter);
            Assert.True(dataTypes[26].Description == "Counter");
            Assert.True(dataTypes[26].ReadOnly == false);
            Assert.True(dataTypes[26].Name == "Counter");
        }

        [Fact]
        public void DataTypePropExcludedTaxonomy()
        {
            var dataTypes = DataType.Text.GetDataTypeInfoList(SystemObjects.TaxonomyType);

            this.ValidateDataTypes(dataTypes);
        }

        [Fact]
        public void DataTypePropExcludedOrganizationType()
        {
            var dataTypes = DataType.Text.GetDataTypeInfoList(SystemObjects.OrganizationType);

            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.OwnershipLookup);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.FieldFromRelationship);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.RefListRelationship);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.ComplexRelationLookup);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.Relationship);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.JSON);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.JsonElement);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.Path);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.Tag);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.Score);
            Assert.DoesNotContain(dataTypes, x => x.ID == DataType.Counter);

        }
    }
}
