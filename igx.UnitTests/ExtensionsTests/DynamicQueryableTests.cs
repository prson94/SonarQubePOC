using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace igx.UnitTests.ExtensionsTests
{
    [Trait("Unit tests", "Dynamic Queryable Tests")]
    public class DynamicQueryableTests : BaseTest
    {
        [Fact]
        public void AsTableValuedParameter()
        {
            IEnumerable<string> data = new List<string> { "value1", "value2", "value3" };
            var results = data.AsTableValuedParameter(
                        "typeName",
                        new List<string>() { "columnName" });
            Assert.True(results != null);
        }
    }
}
