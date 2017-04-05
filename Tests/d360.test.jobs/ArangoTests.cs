using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Arango.Client;
using System.Collections.Generic;
using Dapper;
using System.Linq;
using Newtonsoft.Json;
using ArangoDB.Client;
using System.Net;
using ArangoDB.Client.Data;
using System.Xml.Linq;
//using Neo4jClient;

namespace d360.test.jobs
{
    [TestClass]
    public class ArangoTests : BaseTest
    {
        public int CompanyID { get { return 4; } }

        private ArangoDatabase getDatabase()
        {
            return new ArangoDatabase(new DatabaseSharedSetting { Url = "http://graph.eastus.cloudapp.azure.com", Database = $"D3S_{CompanyID}", Credential = new NetworkCredential("root", "fhgyt!htGHT!YR65234!") });
        }

        [TestMethod]
        public void GetGlossaryObject()
        {
            //var db = getDatabase();

            //var glossary = db.Document.Get("Items/152829");
        }

        [TestMethod]
        public void GlossarySearchResults_Success()
        {
            var db = getDatabase();

            //var items = db.Query<Item>().Where(i => i.ObjectID > 1000 && i.ObjectID <= 2500).ToList();

            //Assert.IsTrue(items.Count > 0);

//            var docs = db.Query.Aql(@"FOR doc IN @@collection
//FILTER doc.Status == @Status && doc.Name > @Start && doc.Name < @End
//SORT doc.Name
//RETURN {
//    Object: doc.Object,
//    ObjectID: doc.ObjectID,
//    Name: doc.Name,
//    Status: doc.Status
//}").BindVar("@collection", "Glossary")
//.BindVar("Start", "a")
//.BindVar("End", "g")
//.BindVar("Status", "Under Review")
//.ToDocuments();

            //Assert.IsTrue(docs.Value != null);
        }

        public class Artifact : Item { }
        public class FusionAttribute : Item { }
        public class Map : Item { }
        public class Policy : Item { }
        public class Rule : Item { }
        public class Taxonomy : Item { }

        public class Item: Dictionary<string, object>
        {
            //public string _id { get; set; }

            //public string _key { get; set; }

            //public string Object { get; set; }

            //public int ObjectID { get; set; }

            //public string ObjectType { get; set; }

            //public int ObjectTypeID { get; set; }


            //public string Name { get; set; }
        }

        public class ItemRaw
        {
            public string _id { get; set; }

            public string _key { get; set; }

            public string Object { get; set; }

            public int ObjectID { get; set; }

            public string ObjectType { get; set; }

            public int ObjectTypeID { get; set; }

            public string Name { get; set; }

            public string Fields { get; set; }

            public Dictionary<string, object> GetAsXml()
            {
                var d = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(Fields))
                {
                    var xml = XElement.Parse(Fields);

                    foreach (var f in xml.Elements("field"))
                    {
                        if (f.Element("Value") != null)
                            d.Add(f.Element("Name").Value, f.Element("Value").Value);
                    }
                }

                return d;
            }
        }

        public class Relationship
        {
            public string _key { get; set; }

            public string _from { get; set; }

            public string _to { get; set; }

            public int PredicateID { get; set; }

            public string Predicate { get; set; }

            public int IntersectTypeID { get; set; }
        }

        public class Field
        {
            //public string Object { get; set; }
            public int ObjectID { get; set; }
            public string Name { get; set; }

            public string Value { get; set; }
        }

        //var client = new GraphClient(new Uri("http://hobby-noligehmoeaggbkeildcicol.dbs.graphenedb.com:24789/db/data/"), username: "d3s-4", password: "b.dBm937tv6CPH.nQ2C4howmLywp9co");
        //client.Connect();

        [TestMethod]
        public void CreateArtifacts()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var rawItems = company.Query<ItemRaw>(@"select	cast(ID as varchar) as [_key], 
		ID as ObjectID, 
		ArtifactTypeID as ObjectTypeID, 
        Name,
		(
			select	F.FormattedValue as Value, 
					T.Name 
			from	Field F 
					inner join FieldType T on T.ID = F.FieldTypeID and F.ObjectType = 'Artifact' and F.ObjectID = A.ID
			for xml path('field'), elements, root('fields')
		) as Fields
from	Artifact A
").ToList(); // where ArtifactTypeID <> 2

            var items = new List<Artifact>();

            rawItems.ForEach(ir => {
                var i = new Artifact();// { Name = ir.Name, Object = ir.Object, ObjectID = ir.ObjectID, _id = ir._id, _key = ir._key };
                var d = ir.GetAsXml();
                i.Add("Name", ir.Name);
                i.Add("ObjectID", ir.ObjectID);
                i.Add("ObjectTypeID", ir.ObjectTypeID);
                i.Add("_key", ir._key);
                foreach (var k in d.Keys)
                {
                    i.Add(k, d[k]);
                }
                items.Add(i);
            });

            db.Advanced.BulkImport<Artifact>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        [TestMethod]
        public void CreateFusionAttributes()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var rawItems = company.Query<ItemRaw>(@"select cast(ID as varchar) as [_key], 
		ID as ObjectID, 
		FusionAttributeTypeID as ObjectTypeID, 
        Name,
		(
			select	F.FormattedValue as Value, 
					T.Name 
			from	Field F 
					inner join FieldType T on T.ID = F.FieldTypeID and F.ObjectType = 'Artifact' and F.ObjectID = A.ID
			for xml path('field'), elements, root('fields')
		) as Fields
from	FusionAttribute A
").ToList();

            var items = new List<FusionAttribute>();

            rawItems.ForEach(ir => {
                var i = new FusionAttribute();// { Name = ir.Name, Object = ir.Object, ObjectID = ir.ObjectID, _id = ir._id, _key = ir._key };
                var d = ir.GetAsXml();
                i.Add("Name", ir.Name);
                i.Add("ObjectID", ir.ObjectID);
                i.Add("ObjectTypeID", ir.ObjectTypeID);
                i.Add("_key", ir._key);
                foreach (var k in d.Keys)
                {
                    i.Add(k, d[k]);
                }
                items.Add(i);
            });

            db.Advanced.BulkImport<FusionAttribute>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        [TestMethod]
        public void CreateMaps()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var rawItems = company.Query<ItemRaw>(@"select	cast(ID as varchar) as [_key], 
		ID as ObjectID, 
		MapTypeID as ObjectTypeID, 
        Name
from	Map A
").ToList(); // where ArtifactTypeID <> 2

            var items = new List<Map>();

            rawItems.ForEach(ir => {
                var i = new Map();// { Name = ir.Name, Object = ir.Object, ObjectID = ir.ObjectID, _id = ir._id, _key = ir._key };
                var d = ir.GetAsXml();
                i.Add("Name", ir.Name);
                i.Add("ObjectID", ir.ObjectID);
                i.Add("ObjectTypeID", ir.ObjectTypeID);
                i.Add("_key", ir._key);
                //foreach (var k in d.Keys)
                //{
                //    i.Add(k, d[k]);
                //}
                items.Add(i);
            });

            db.Advanced.BulkImport<Map>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        [TestMethod]
        public void CreateModels()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var rawItems = company.Query<ItemRaw>(@"
select  cast(ID as varchar) as [_key], 
        ID as ObjectID,  
		TaxonomyTypeID as ObjectTypeID, 
        Name 
from    Taxonomy").ToList(); // where ArtifactTypeID <> 2

            var items = new List<Taxonomy>();

            rawItems.ForEach(ir => {
                var i = new Taxonomy();// { Name = ir.Name, Object = ir.Object, ObjectID = ir.ObjectID, _id = ir._id, _key = ir._key };
                var d = ir.GetAsXml();
                i.Add("Name", ir.Name);
                i.Add("ObjectID", ir.ObjectID);
                i.Add("ObjectTypeID", ir.ObjectTypeID);
                //i.Add("_id", ir._id);
                i.Add("_key", ir._key);
                //foreach (var k in d.Keys)
                //{
                //    i.Add(k, d[k]);
                //}
                items.Add(i);
            });

            db.Advanced.BulkImport<Taxonomy>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        [TestMethod]
        public void CreatePolicies()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var rawItems = company.Query<ItemRaw>(@"select	cast(ID as varchar) as [_key], 
		ID as ObjectID, 
		PolicyTypeID as ObjectTypeID, 
        Name,
		(
			select	F.FormattedValue as Value, 
					T.Name 
			from	Field F 
					inner join FieldType T on T.ID = F.FieldTypeID and F.ObjectType = 'Policy' and F.ObjectID = A.ID
			for xml path('field'), elements, root('fields')
		) as Fields
from	[Policy] A
").ToList();

            var items = new List<Policy>();

            rawItems.ForEach(ir => {
                var i = new Policy();// { Name = ir.Name, Object = ir.Object, ObjectID = ir.ObjectID, _id = ir._id, _key = ir._key };
                var d = ir.GetAsXml();
                i.Add("Name", ir.Name);
                i.Add("ObjectID", ir.ObjectID);
                i.Add("ObjectTypeID", ir.ObjectTypeID);
                i.Add("_key", ir._key);
                foreach (var k in d.Keys)
                {
                    i.Add(k, d[k]);
                }
                items.Add(i);
            });

            db.Advanced.BulkImport<Policy>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        [TestMethod]
        public void CreateRules()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var rawItems = company.Query<ItemRaw>(@"select	cast(ID as varchar) as [_key], 
		ID as ObjectID, 
		RuleTypeID as ObjectTypeID, 
        Name,
		(
			select	F.FormattedValue as Value, 
					T.Name 
			from	Field F 
					inner join FieldType T on T.ID = F.FieldTypeID and F.ObjectType = 'Rule' and F.ObjectID = A.ID
			for xml path('field'), elements, root('fields')
		) as Fields
from	[Rule] A
").ToList();

            var items = new List<Rule>();

            rawItems.ForEach(ir => {
                var i = new Rule();// { Name = ir.Name, Object = ir.Object, ObjectID = ir.ObjectID, _id = ir._id, _key = ir._key };
                var d = ir.GetAsXml();
                i.Add("Name", ir.Name);
                i.Add("ObjectID", ir.ObjectID);
                i.Add("ObjectTypeID", ir.ObjectTypeID);
                i.Add("_key", ir._key);
                foreach (var k in d.Keys)
                {
                    i.Add(k, d[k]);
                }
                items.Add(i);
            });

            db.Advanced.BulkImport<Rule>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        //        [TestMethod]
        //        public void CreateArtifactParentRelationships()
        //        {
        //            var db = getDatabase();
        //            var company = getCompanyConnection(CompanyID);

        //            var items = company.Query<Intersect>(@"
        //select  cast(I.ID as varchar) as [_key], 
        //        'Items/' + I.Subject + '_' + cast(I.SubjectID as varchar) as _from, 
        //        'Items/' + I.Object + '_' + cast(I.ObjectID as varchar) as _to,
        //        T.PredicateID,
        //        P.Name as [Predicate],
        //        I.IntersectTypeID
        //from    [Intersect] I
        //        inner join IntersectType T on T.ID = I.IntersectTypeID 
        //        left join [Predicate] P on P.ID = T.PredicateID
        //where   (I.Subject = 'Taxonomy' and I.Object = 'Taxonomy') 
        //        or (I.Subject = 'Artifact' and I.Object = 'Artifact') 
        //        or (I.Subject = 'Taxonomy' and I.Object = 'Artifact') 
        //        or (I.Subject = 'Artifact' and I.Object = 'Taxonomy')"
        //).ToList();

        //            db.Advanced.BulkImport<Intersect>(items, onDuplicate: ImportDuplicatePolicy.Update);
        //        }

        [TestMethod]
        public void CreateRelationships()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var items = company.Query<Relationship>(@"
select  cast(I.ID as varchar) as [_key], 
        I.Subject + '/' + cast(I.SubjectID as varchar) as _from, 
        I.Object + '/' + cast(I.ObjectID as varchar) as _to,
        T.PredicateID,
        P.Name as [Predicate],
        I.IntersectTypeID
from    [Intersect] I
        inner join IntersectType T on T.ID = I.IntersectTypeID 
        left join [Predicate] P on P.ID = T.PredicateID
"
).ToList();
            /*
             where   (I.Subject = 'Taxonomy' and I.Object = 'Taxonomy') 
                    or (I.Subject = 'Artifact' and I.Object = 'Artifact') 
                    or (I.Subject = 'Taxonomy' and I.Object = 'Artifact') 
                    or (I.Subject = 'Artifact' and I.Object = 'Taxonomy')
                         */
            db.Advanced.BulkImport<Relationship>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        [TestMethod]
        public void CreateArtifactParentChildRelationships()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var items = company.Query<Relationship>(@"
         with P as (
		select	'Artifact' + '/' + cast(ID as varchar) as [_key],
				ID,
				ParentID, 
				cast('' as varchar(100)) as [_from],
				cast('' as varchar(100)) as [_to]
		from	Artifact
		where	ParentID is null
		union all
		select	'Artifact' + '/' + cast(C.ID as varchar) as [_key],
				C.ID,
				C.ParentID, 
				cast(P._key as varchar(100)) as [_from],
				cast('Artifact' + '/' + cast(C.ID as varchar) as varchar(100)) as [_to]
		from	Artifact C
				inner join P on P.ID = C.ParentID
)

select	cast(ParentID as varchar) + '.' + cast(ID as varchar) as [_key],
		_from,
		_to,
		16 as PredicateID,
		'parent of' as [Predicate],
		0 as IntersectTypeID
from	P 
where	_to <> ''
"
).ToList();
            db.Advanced.BulkImport<Relationship>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        [TestMethod]
        public void CreateFusionAttributeParentChildRelationships()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var items = company.Query<Relationship>(@"
with P as (
		select	'FusionAttribute' + '/' + cast(ID as varchar) as [_key],
				ID,
				ParentID, 
				cast('' as varchar(100)) as [_from],
				cast('' as varchar(100)) as [_to]
		from	FusionAttribute
		where	ParentID is null
		union all
		select	'FusionAttribute' + '/' + cast(C.ID as varchar) as [_key],
				C.ID,
				C.ParentID, 
				cast(P._key as varchar(100)) as [_from],
				cast('FusionAttribute' + '/' + cast(C.ID as varchar) as varchar(100)) as [_to]
		from	FusionAttribute C
				inner join P on P.ID = C.ParentID
)

select	cast(ParentID as varchar) + '.' + cast(ID as varchar) as [_key],
		_from,
		_to,
		16 as PredicateID,
		'parent of' as [Predicate],
		0 as IntersectTypeID
from	P 
where	_to <> ''").ToList();

            db.Advanced.BulkImport<Relationship>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        [TestMethod]
        public void CreateTaxonomyParentChildRelationships()
        {
            var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var items = company.Query<Relationship>(@"
with P as (
		select	'Taxonomy' + '/' + cast(ID as varchar) as [_key],
				ID,
				ParentID, 
				cast('' as varchar(100)) as [_from],
				cast('' as varchar(100)) as [_to]
		from	Taxonomy
		where	ParentID is null
		union all
		select	'Taxonomy' + '/' + cast(C.ID as varchar) as [_key],
				C.ID,
				C.ParentID, 
				cast(P._key as varchar(100)) as [_from],
				cast('Taxonomy' + '/' + cast(C.ID as varchar) as varchar(100)) as [_to]
		from	Taxonomy C
				inner join P on P.ID = C.ParentID
)

select	cast(ParentID as varchar) + '.' + cast(ID as varchar) as [_key],
		_from,
		_to,
		16 as PredicateID,
		'parent of' as [Predicate],
		0 as IntersectTypeID
from	P 
where	_to <> ''").ToList();

            db.Advanced.BulkImport<Relationship>(items, onDuplicate: ImportDuplicatePolicy.Update);
        }

        [TestMethod]
        public void CreateArtifactTypes()
        {
            //var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

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

                //var createDocumentResult = db.Document
                //    .WaitForSync(true)
                //    .Create("Glossary", document);

                //if (createDocumentResult.Success)
                //{
                //    var id = createDocumentResult.Value.String("_id");
                //    var key = createDocumentResult.Value.String("_key");
                //    var revision = createDocumentResult.Value.String("_rev");
                //}
            }
        }

        [TestMethod]
        public void CreateArtifactToArtifactTypeEdges_TypeOf()
        {
            //var db = getDatabase();
            var company = getCompanyConnection(CompanyID);

            var items = company.Query<dynamic>(@"select ArtifactTypeID, ID from Artifact").ToList();

            foreach (var item in items)
            {
                var document = new Dictionary<string, object>();
                //document.Add("_key", $"{item.Object}-{item.ObjectID}");
                document.Add("Predicate", "has instance of");
                document.Add("Inverse", "is type of");

                //var createEdgeResult = db.Document
                //    .WaitForSync(true)
                //    .CreateEdge("GlossaryRelations", $"Glossary/ArtifactType-{item.ArtifactTypeID}", $"Glossary/Artifact-{item.ID}", document);
            }
        }
    }
}
