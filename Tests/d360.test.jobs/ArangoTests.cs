using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Arango.Client;
using System.Collections.Generic;
using Dapper;
using System.Linq;
using Newtonsoft.Json;

namespace d360.test.jobs
{
    [TestClass]
    public class ArangoTests : BaseTest
    {
        private ADatabase getDatabase()
        {
            if (!ASettings.HasConnection("D3S_4"))
                ASettings.AddConnection("D3S_4", "arangous1.eastus.cloudapp.azure.com", 8529, false, "D3S_4", "root", "fhgyt!htGHT!YR65234!");

            var db = new ADatabase("D3S_4");

            return db;
        }

        [TestMethod]
        public void GetGlossaryObject()
        {
            var db = getDatabase();

            var glossary = db.Document.Get("Items/152829");
        }

        [TestMethod]
        public void GlossarySearchResults_Success()
        {
            var db = getDatabase();

            var docs = db.Query.Aql(@"FOR doc IN @@collection
FILTER doc.Status == @Status && doc.Name > @Start && doc.Name < @End
SORT doc.Name
RETURN {
    Object: doc.Object,
    ObjectID: doc.ObjectID,
    Name: doc.Name,
    Status: doc.Status
}").BindVar("@collection", "Glossary")
.BindVar("Start", "a")
.BindVar("End", "g")
.BindVar("Status", "Under Review")
.ToDocuments();

            Assert.IsTrue(docs.Value != null);
        }

        [TestMethod]
        public void CreateArtifacts()
        {
            var db = getDatabase();
            var company = getCompanyConnection(4);

            var items = company.Query<dynamic>(@"select 'Artifact' as Object, ID as ObjectID, Name from Artifact").ToList(); // where ArtifactTypeID <> 2

            //var json = JsonConvert.SerializeObject(items);

            //var results = db.Document
            //    .WaitForSync(true)
            //    .Create("Glossary", json);

            foreach (var item in items)
            {
                var document = new Dictionary<string, object>();
                //document.Add("_id", $"{item.Object}-{item.ObjectID}");
                document.Add("_key", $"{item.Object}-{item.ObjectID}");
                document.Add("Object", item.Object);
                document.Add("ObjectID", item.ObjectID);
                document.Add("Name", item.Name);

                var createDocumentResult = db.Document
                    .WaitForSync(true)
                    .Create("Items", document);

                if (createDocumentResult.Success)
                {
                    var id = createDocumentResult.Value.String("_id");
                    var key = createDocumentResult.Value.String("_key");
                    var revision = createDocumentResult.Value.String("_rev");
                }
            }
        }

        [TestMethod]
        public void CreateArtifactTypes()
        {
            var db = getDatabase();
            var company = getCompanyConnection(4);

            var items = company.Query<dynamic>(@"select 'ArtifactType' as Object, ID as ObjectID, Name, Description from ArtifactType").ToList();

            //var json = JsonConvert.SerializeObject(items);

            //var results = db.Document
            //    .WaitForSync(true)
            //    .Create("Glossary", json);

            foreach (var item in items)
            {
                var document = new Dictionary<string, object>();
                document.Add("_key", $"{item.Object}-{item.ObjectID}");
                document.Add("Object", item.Object);
                document.Add("ObjectID", item.ObjectID);
                document.Add("Name", item.Name);
                document.Add("Description", item.Description);

                var createDocumentResult = db.Document
                    .WaitForSync(true)
                    .Create("Glossary", document);

                if (createDocumentResult.Success)
                {
                    var id = createDocumentResult.Value.String("_id");
                    var key = createDocumentResult.Value.String("_key");
                    var revision = createDocumentResult.Value.String("_rev");
                }
            }
        }

        [TestMethod]
        public void CreateArtifactToArtifactTypeEdges_TypeOf()
        {
            var db = getDatabase();
            var company = getCompanyConnection(4);

            var items = company.Query<dynamic>(@"select ArtifactTypeID, ID from Artifact").ToList();

            foreach (var item in items)
            {
                var document = new Dictionary<string, object>();
                //document.Add("_key", $"{item.Object}-{item.ObjectID}");
                document.Add("Predicate", "has instance of");
                document.Add("Inverse", "is type of");

                var createEdgeResult = db.Document
                    .WaitForSync(true)
                    .CreateEdge("GlossaryRelations", $"Glossary/ArtifactType-{item.ArtifactTypeID}", $"Glossary/Artifact-{item.ID}", document);
            }
        }
    }
}
