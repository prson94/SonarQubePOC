using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace d360.test.web.Tests.Controllers.ApiController
{
    [TestClass]
    public class ComplexLookupTests : BaseApiTest
    {
        [TestMethod, TestCategory("ApiController"), TestCategory("ComplexLookup")]
        public void GetResults()
        {
            var response = controller.GetComplexLookupGridField("Artifact", 4651, 52811);
            dynamic value = response.Content;
        }
    }
}
