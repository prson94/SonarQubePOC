using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace igx.UnitTests.ExtensionsTests
{
    [Trait("Unit tests", "Dynamic Helper Tests")]
    public class DynamicHelperTests : BaseTest
    {
        [Theory]
        [InlineData("string")]
        [InlineData(45)]
        [InlineData(true)]
        public void IsSimpleType(object obj)
        {
            var simpleTypeRes = obj.GetType().IsSimpleType();
            Assert.True(simpleTypeRes);
        }

        [Fact]
        public void ConvertToXml()
        {
            dynamic dynObject = new System.Dynamic.ExpandoObject();
            dynObject.property1 = "test";
            dynObject.property2 = 23;
            XElement result = DynamicHelper.ConvertToXml(dynObject, "element_name");
            Assert.True(result.Name.LocalName == "element_name");
        }

        [Fact]
        public void ToXml()
        {
            object obj = new { property1 = "test", property2 = 23 };
            XElement result = obj.ToXml();
            Assert.True(result.Name.LocalName == "object");
        }

        [Fact]
        public void ToXmlOverride()
        {
            object obj = new { property1 = "test", property2 = 23 };
            XElement result = obj.ToXml("element_name");
            Assert.True(result.Name.LocalName == "element_name");
        }

        [Fact]
        public void GetXElement()
        {
            var dict = new Dictionary<string, string>();
            dict.Add("key1", "value1");
            dict.Add("key2", "value2");
            XElement result = DynamicHelper.GetXElement("key1", dict);
            Assert.True(result.Name.LocalName == "key1");
            Assert.True(result.ToString() == "<key1 xmlns=\"value1\" />");
        }
    }
}
