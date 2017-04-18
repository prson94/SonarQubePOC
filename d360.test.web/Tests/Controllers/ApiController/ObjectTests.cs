using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.core;
using System.Linq;
using System.Net;
using System;
using d360.web.Models;
using d360.core.entities;

namespace d360.test.web.Tests.Controllers.ApiController
{
    [TestClass]
    public class ObjectTests : BaseApiTest
    {
        private SystemObjects testObject;
        private int testObjectId;
        private SystemObjects testType;
        private int testTypeId;

        public ObjectTests() : base()
        {
            //TODO: need to mock these objects or use objects created specifically for unit testing
            testObject = SystemObjects.Artifact;
            testObjectId = 4651;
            testType = SystemObjects.ArtifactType;
            testTypeId = 1;
        }

        [TestMethod, TestCategory("ApiController")]
        public void GetObjectDetails()
        {
            ObjectDetail result;

            result = controller.GetObjectDetail(testObject, testObjectId);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.ID, testObjectId);
            Assert.AreEqual(result.ParentType, testObject.ToString());

            result = controller.GetObjectDetail(SystemObjects.Artifact, -1);
            Assert.IsNull(result);

        }
        
        [TestMethod, TestCategory("ApiController")]
        public void GetFieldForObject()
        {
            IQueryable<DisplayField> result;
            try
            {
                result = controller.GetFieldForObject(testObject, testObjectId);
                Assert.IsNotNull(result);
                result = controller.GetFieldForObject(SystemObjects.ArtifactType, 1);
                Assert.IsNotNull(result);
                result = controller.GetFieldForObject(SystemObjects.Artifact, -1);
                Assert.IsNotNull(result);
                result = controller.GetFieldForObject(SystemObjects.ArtifactType, -1);
                Assert.IsNotNull(result);
            } catch (Exception ex)
            {
                Assert.Fail(ex.GetFullExceptionData());
            }
        }

        [TestMethod, TestCategory("ApiController")]
        public void GetObjectStyle()
        {
            var result = controller.GetObjectStyle(testType, testTypeId);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.ObjectType, testType.ToString());
            Assert.AreEqual(result.ObjectID, testTypeId);

            result = controller.GetObjectStyle(testType, -1);
            Assert.IsNull(result);
        }

        [TestMethod, TestCategory("ApiController")]
        public void GetObjectDetailFields()
        {
            var result = controller.GetObjectDetailFields(testObject, testObjectId);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.rows.Count > 0);
            
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Statistics")]
        public void GetTileObjectStatistics()
        {

            var result = controller.GetTileObjectStatistics(testObject, testObjectId);

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Items);
        }

        [TestMethod, TestCategory("ApiController")]
        public void GetEditableFieldLookupData()
        {
            var take = 5;

            var result = controller.GetEditableFieldLookupData(testObject, testObjectId, take);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count <= take);

            result = controller.GetEditableFieldLookupData(testType, testTypeId, take);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count <= take);

        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Fields")]
        public void GetFieldTypesByObject()
        {
            var result = controller.GetFieldTypesByObject(testType, testTypeId);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Followers")]
        public void GetFollowers()
        {

            var result = controller.GetFollowers(testObject, testObjectId).ToList();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);

            var detail = result.First();

            Assert.AreEqual(detail.ObjectID, testObjectId);
            Assert.AreEqual(detail.ObjectType, testObject.ToString());
            Assert.AreEqual(detail.TypeID, testTypeId);
            Assert.AreEqual(detail.Type, testType.ToString());
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Responsibilities")]
        public void GetResponsibilitiesByObject()
        {
            var result = controller.GetResponsibilitiesByObject(testObject, testObjectId).ToList();

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.IsFalse(result.Any(r => !r.Visible));

            var detail = result.First();

            Assert.AreEqual(detail.ObjectID, testObjectId);
            Assert.AreEqual(detail.ObjectType, testObject.ToString());
            Assert.AreEqual(detail.ObjectTypeID, testTypeId);

        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Permissions")]
        public void GetPermissionsObObject()
        {
            var result = controller.GetPermissionsObObject(testObject, testObjectId);

            Assert.IsNotNull(result);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Relationships")]
        public void GetRelationships()
        {

            var result = controller.GetRelationships(testObject, testObjectId).ToList();

            Assert.IsNotNull(result);
        }

        //[TestMethod, TestCategory("ApiController"), TestCategory("Relationships")]
        //public void GetCriticalRelations()
        //{
        //    var result = controller.GetCriticalRelations(testObject, testObjectId).ToList();

        //    Assert.IsNotNull(result);
        //}

        //[TestMethod, TestCategory("ApiController"), TestCategory("Relationships")]
        //public void RelationshipsForObjectByTargetType()
        //{
        //    var targetType = SystemObjects.Artifact;
        //    var targetId = 4648;
        //    var intersectTypeId = 8306;

        //    var result = controller.RelationshipsForObjectByTargetType(testObject, testObjectId, targetType, targetId, intersectTypeId, false).ToList();

        //    Assert.IsNotNull(result);
        //    Assert.IsTrue(result.Count > 0);
        //    Assert.IsFalse(result.Any(r => r.IntersectTypeID != intersectTypeId));
            
        //}

        [TestMethod, TestCategory("ApiController"), TestCategory("Relationships"), TestCategory("Exports")]
        public void RelationshipsForObjectByTargetTypeExportExcel()
        {
            var targetType = SystemObjects.Artifact;
            var targetId = 4648;
            var intersectTypeId = 8306;

            var result = controller.RelationshipsForObjectByTargetTypeExportExcel(testObject, testObjectId, targetType, targetId, intersectTypeId);

            //TODO: generic set of asserts for exports and/or HttpResponseMessage?
            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
            Assert.AreEqual(result.Content.Headers.ContentType.MediaType, "application/vnd.ms-excel");
            Assert.AreEqual(result.Content.Headers.ContentDisposition.DispositionType, "attachment");

        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Statistics")]
        public void GetStatisticDetails()
        {
            var result = controller.GetStatisticDetails(testObject, testObjectId).ToList();
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Nyms")]
        public void GetSynonymsByObject()
        {
            var predicateId = 22;

            var result = controller.GetSynonymsByObject(testObject, testObjectId, predicateId);

            Assert.IsNotNull(result);
            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
        }

        [TestMethod, TestCategory("ApiController"), TestCategory("Nyms")]
        public void GetNymAllocations()
        {
            var result = controller.GetNymAllocations(testObject, testObjectId);
            Assert.IsNotNull(result);
            Assert.AreEqual(result.StatusCode, HttpStatusCode.OK);
        }

    }
}
