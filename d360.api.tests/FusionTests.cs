using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Xml.Linq;
using d360.extensions;

namespace d360.api.tests
{
    /// <summary>
    /// Tests Fusion API methods and routes, as well as CRUDing data via the API.
    /// </summary>
    [TestClass]
    public class FusionTests : BaseApiClient
    {
        public FusionTests()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {  
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        #region Create Tests

        #endregion

        #region Delete Tests

        #endregion

        #region Update Tests

        #endregion

        #region Read Tests

        [TestMethod]
        public void GetFusionConfigurations()
        {
            var jsonResponse = GetJsonContent("/fusion/1/configurations");
            var items = Newtonsoft.Json.Linq.JArray.Parse(jsonResponse);
            Assert.IsTrue(items.Count > 0);
        }

        #endregion

    }
}
