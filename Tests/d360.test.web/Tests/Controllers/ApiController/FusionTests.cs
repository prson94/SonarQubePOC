using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.core;
using System.Linq;
using System.Net;
using System;
using d360.web.Models;
using d360.core.entities;
using System.Web.Http;

namespace d360.test.web.Tests.Controllers.ApiController
{
    [TestClass]
    public class FusionTests : BaseApiTest
    {
        private int testFusionTypeId;
        private int testFusionId;
        private int testFusionAttributeId;
        private int testFusionAttributeTypeId;

        private int testRuleId;
        private int testRuleStepId;

        public FusionTests() : base()
        {
            //TODO: mock these items or items in DB specific to unit testing
            testFusionTypeId = 14;
            testFusionId = 22;
            testFusionAttributeId = 82033;
            testFusionAttributeTypeId = 726;
            testRuleId = 86;
            testRuleStepId = 196;
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetFusionTypes()
        {
            var result = controller.GetFusionTypes();
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetFusionType()
        {
            var result = controller.GetFusionType(testFusionTypeId);
            Assert.IsNotNull(result);

            bool threw = false;
            try
            {
                result = controller.GetFusionType(-1);
            } catch (Exception ex)
            {
                threw = true;
                Assert.IsInstanceOfType(ex, typeof(HttpResponseException));
            }
            Assert.IsTrue(threw);
        }


        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetFusionConfigurationsByType()
        {
            var result = controller.GetFusionConfigurationsByType(testFusionTypeId);

            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetFusionConfiguration()
        {
            var result = controller.GetFusionConfiguration(testFusionTypeId, testFusionId);

            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetFusionConfigurationFromFusionAttribute()
        {
            var result = controller.GetFusionConfigurationFromFusionAttribute(testFusionAttributeId);

            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Breadcrumbs")]
        public void GetSelectedFusionBreadcrumb()
        {
            var result = controller.GetSelectedFusionBreadcrumb(82040);

            Assert.IsNotNull(result);

            bool threw = false;
            try
            {
                result = controller.GetSelectedFusionBreadcrumb(-1);
            } catch
            {
                threw = true;
            }
            Assert.IsTrue(threw);
        }


        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetOwnershipChildAttributeNodes()
        {
            var result = controller.GetOwnershipChildAttributeNodes(testFusionId, testFusionAttributeTypeId, testRuleId);

            Assert.IsNotNull(result);

            try
            {
                result = controller.GetOwnershipChildAttributeNodes(0, 0, 0);
            } catch (Exception ex)
            {
                Assert.Fail(ex.GetFullExceptionData());
            }
            

        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetPromotionChildAttributeNodes()
        {
            var result = controller.GetPromotionChildAttributeNodes(testFusionId, testFusionAttributeTypeId, testRuleId);

            Assert.IsNotNull(result);

            try
            {
                result = controller.GetPromotionChildAttributeNodes(0, 0, 0);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.GetFullExceptionData());
            }
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetPromotionFusionQueryAttributes()
        {
            var result = controller.GetPromotionFusionQueryAttributes(testRuleId);

            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetFilterByFusion()
        {
            //typeID not used??
            var result = controller.GetFilterByFusion(testFusionTypeId, testFusionId);

            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Relationships")]
        public void GetAllowedIntersectTypesForFusionOwnership()
        {
            //typeID not used??
            var result = controller.GetAllowedIntersectTypesForFusionOwnership(testFusionTypeId);

            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules")]
        public void GetFusionRules()
        {
            var result = controller.GetFusionRules(testFusionId);
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules")]
        public void GetFusionRuleSteps()
        {
            var result = controller.GetFusionRuleSteps(testRuleId);
            Assert.IsNotNull(result);

            bool threw = false;

            try
            {
                result = controller.GetFusionRuleSteps(-1);
            } catch (Exception ex)
            {
                threw = true;
                Assert.IsInstanceOfType(ex, typeof(HttpResponseException));
                Assert.AreEqual(HttpStatusCode.NotFound, ((HttpResponseException)ex).Response.StatusCode);
            }

            Assert.IsTrue(threw);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules")]
        public void GetRuleSteps()
        {
            var result = controller.GetRuleSteps(testRuleId, testRuleStepId);
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules")]
        public void GetActions()
        {
            var result = controller.GetActions();
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules")]
        public void GetFusionAttributeTypes()
        {
            var result = controller.GetFusionAttributeTypes();
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules")]
        public void GetRuleFusionOwners()
        {
            var result = controller.GetRuleFusionOwners(testFusionId);
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules"), TestCategory("Relationships")]
        public void GetIntersectTypes()
        {
            var result = controller.GetIntersectTypes();
            Assert.IsNotNull(result);
        }

        //[TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules"), TestCategory("Relationships"), TestCategory("Lineage")]
        //public void GetIntersectRoles()
        //{
        //    var result = controller.GetIntersectRoles();
        //    Assert.IsNotNull(result);
        //}

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules"), TestCategory("Relationships")]
        public void GetDirectObjectRelateTypes()
        {
            var result = controller.GetDirectObjectRelateTypes();
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Fusion Rules")]
        public void GetFusionRuleDirectOptions()
        {
            var result = controller.GetFusionRuleDirectOptions(SystemObjects.ArtifactType, 1);
            Assert.IsNotNull(result);

            result = controller.GetFusionRuleDirectOptions(SystemObjects.Artifact, 4651);
            Assert.IsNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion"), TestCategory("Lineage")]
        public void GetFusionTechnicalMappings()
        {
            var result = controller.GetFusionTechnicalMappings();
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetArtifactsOwningFusion()
        {
            var result = controller.GetArtifactsOwningFusion();
            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fusion")]
        public void GetFusionTextpathsAutocomplete()
        {
            var count = 10;
            var result = controller.GetFusionTextpathsAutocomplete("s", count);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count() <= count);

            try
            {
                result = controller.GetFusionTextpathsAutocomplete("", count);
                result = controller.GetFusionTextpathsAutocomplete("%", count);
                result = controller.GetFusionTextpathsAutocomplete("%$(*&@)!*_#[]", count);
            } catch (Exception ex)
            {
                Assert.Fail(ex.GetFullExceptionData());
            }
        }

    }
}
