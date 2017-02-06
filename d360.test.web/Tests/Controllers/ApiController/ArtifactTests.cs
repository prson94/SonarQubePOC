using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.Http;
using System.Net;
using d360.web.Models;
using System;
using System.Collections.Generic;

namespace d360.test.web.Tests.Controllers.ApiController
{
    [TestClass]
    public class ArtifactTests : BaseApiTest
    {

        public ArtifactTests(): base()
        { }

        [TestMethod, TestCategory("ApiController"), TestCategory("Artifacts")]
        public void GetArtifact()
        {
            var testArtifactId = 4651;
            var result = controller.GetArtifact(testArtifactId);

            Assert.IsNotNull(result);
            Assert.AreEqual(result["ID"].ToString(), testArtifactId.ToString());

        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Artifacts")]
        public void GetArtifactNegative()
        {
            bool threw = false;
            ArtifactModelRequest result = null;

            try
            {
                result = controller.GetArtifact(-1);
            } catch (Exception ex)
            {
                threw = true;
                Assert.IsInstanceOfType(ex, typeof(HttpResponseException));
                Assert.AreEqual(((HttpResponseException)ex).Response.StatusCode, HttpStatusCode.NotFound);
            }
            Assert.IsNull(result);
            Assert.IsTrue(threw);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Artifacts")]
        public void GetArtifactType()
        {
            var testArtifactTypeId = 1;

            var result = controller.GetArtifactType(testArtifactTypeId);

            Assert.IsNotNull(result);
            Assert.AreEqual(result["ID"].ToString(), testArtifactTypeId.ToString());
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Artifacts")]
        public void GetArtifactTypeNegative()
        {
            bool threw = false;
            Dictionary<string, object> result = null;

            try
            {
                result = controller.GetArtifactType(-1);
            } catch (Exception ex)
            {
                threw = true;
                Assert.IsInstanceOfType(ex, typeof(HttpResponseException));
                Assert.AreEqual(((HttpResponseException)ex).Response.StatusCode, HttpStatusCode.NotFound);
            }

            Assert.IsNull(result);
            Assert.IsTrue(threw);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Artifacts")]
        public void GetArtifactTypes()
        {
            var result = controller.GetArtifactTypes();
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Artifacts")]
        public void GetArtifacts()
        {
            var testArtifactTypeId = 1;
            var count = 5;
            var result = controller.GetArtifacts(testArtifactTypeId, count);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count <= count);

            result = controller.GetArtifacts(-1, count);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Count, 0);

        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Artifacts")]
        public void GetArtifactTypePossibleOwners()
        {
            var testArtifactTypeId = 1;
            var result = controller.GetArtifactTypePossibleOwners(testArtifactTypeId);

            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
        }

    }
}
