using Microsoft.VisualStudio.TestTools.UnitTesting;
using BrightstarDB.Client;
using Dapper;
using System.Linq;
using System.Text;

namespace d360.test.jobs
{
    [TestClass]
    public class Brightstar: BaseTest
    {
        IDataObjectContext getContext()
        {
            return BrightstarService.GetDataObjectContext("Type=rest;endpoint=http://104.45.137.220:8090/brightstar/;"); ;
        }

        IBrightstarService getLowlevelClient()
        {
            return BrightstarService.GetClient("Type=rest;endpoint=http://104.45.137.220:8090/brightstar/;"); ;
        }

        [TestMethod]
        public void Connection_Successful()
        {
            var context = getContext();
            Assert.IsTrue(context.DoesStoreExist("d3s4"));
        }

        public class RelationModel
        {
            public string SubjectUrl { get; set; }
            public string ObjectUrl { get; set; }
            public int PredicateID { get; set; }
        }


        [TestMethod]
        public void MakeDataObject_Successful()
        {
            var prefix = "https://d3s.com/";

            var context = getContext();
            IDataObjectStore myStore;
            if (!context.DoesStoreExist("d3s4"))
            {
                context.CreateStore("d3s4");
            }
            myStore = context.OpenStore("d3s4");

//            var cnn = getCompanyConnection(4);

//            var relations = cnn.Query<RelationModel>(@"
//select	SO.[Object] + '/' + cast(SO.ObjectTypeID as varchar) + '/' + cast(SO.ObjectID as varchar) as SubjectUrl,
//		OO.[Object] + '/' + cast(OO.ObjectTypeID as varchar) + '/' + cast(OO.ObjectID as varchar) as ObjectUrl,
//		P.ID as PredicateID
//from	IntersectMap IM
//		inner join IntersectNode S on S.ID = IM.SubjectIntersectNodeID
//		inner join cache.ObjectDetails SO on SO.Object = S.ObjectType and SO.ObjectID = S.ObjectID
//		inner join IntersectNode O on O.ID = IM.ObjectIntersectNodeID
//		inner join cache.ObjectDetails OO on OO.Object = O.ObjectType and OO.ObjectID = O.ObjectID
//		inner join Predicate P on P.ID = IM.PredicateID").ToList();
//            var nodes = relations.Select(i => i.SubjectUrl).Distinct().ToList();
//            nodes.AddRange(
//                relations.Select(i => i.ObjectUrl).Distinct().Except(nodes)
//            );
//            nodes.ForEach(n =>
//            {
//                myStore.MakeDataObject($"{prefix}{n}");
//            });
//            myStore.SaveChanges();

//            //var predicates = relations.Select(p => p.PredicateID).Distinct().ToList();
//            //predicates.ForEach(p =>
//            //{
//            //    myStore.MakeDataObject($"{prefix}Predicate/{p}");
//            //});
//            //myStore.SaveChanges();

//            relations.ForEach(r =>
//            {
//                var s = myStore.GetDataObject($"{prefix}{r.SubjectUrl}");
//                var o = myStore.GetDataObject($"{prefix}{r.ObjectUrl}");

//                s.AddProperty($"{prefix}Predicate/{r.PredicateID}", o);

//                //var obj = myStore.MakeDataObject($"https://d3s.com/{a.Url}");
//                //obj.AddProperty()
//            });

            //            var artifacts = cnn.Query<dynamic>(@"
            //select	Object + '/' + cast(ObjectTypeID as varchar) + '/' + cast(ObjectID as varchar) as Url
            //from	cache.ObjectDetails
            //where	ObjectType = 'ArtifactType' and ObjectTypeName is not null
            //");
            //            foreach (var a in artifacts)
            //            {
            //                myStore.MakeDataObject($"https://d3s.com/{a.Url}");
            //                //var obj = myStore.MakeDataObject($"https://d3s.com/{a.Url}");
            //                //obj.AddProperty()
            //            }

            var query = myStore.BindDataObjectsWithSparql(@"SELECT ?node WHERE {?node a <" + prefix + "Artifact>}").ToList();

            myStore.SaveChanges();
        }

        [TestMethod]
        public void LowLevel_SubmitTriples_Successful()
        {
            var prefix = "https://d3s.com/";

            var context = getLowlevelClient();

            var cnn = getCompanyConnection(4);

            var relations = cnn.Query<RelationModel>(@"
            select	SO.[Object] + '/' + cast(SO.ObjectTypeID as varchar) + '/' + cast(SO.ObjectID as varchar) as SubjectUrl,
            		OO.[Object] + '/' + cast(OO.ObjectTypeID as varchar) + '/' + cast(OO.ObjectID as varchar) as ObjectUrl,
            		P.ID as PredicateID
            from	IntersectMap IM
            		inner join IntersectNode S on S.ID = IM.SubjectIntersectNodeID
            		inner join cache.ObjectDetails SO on SO.Object = S.ObjectType and SO.ObjectID = S.ObjectID
            		inner join IntersectNode O on O.ID = IM.ObjectIntersectNodeID
            		inner join cache.ObjectDetails OO on OO.Object = O.ObjectType and OO.ObjectID = O.ObjectID
            		inner join Predicate P on P.ID = IM.PredicateID").ToList();
            //            var nodes = relations.Select(i => i.SubjectUrl).Distinct().ToList();
            //            nodes.AddRange(
            //                relations.Select(i => i.ObjectUrl).Distinct().Except(nodes)
            //            );
            //            nodes.ForEach(n =>
            //            {
            //                myStore.MakeDataObject($"{prefix}{n}");
            //            });
            //            myStore.SaveChanges();

            //            //var predicates = relations.Select(p => p.PredicateID).Distinct().ToList();
            //            //predicates.ForEach(p =>
            //            //{
            //            //    myStore.MakeDataObject($"{prefix}Predicate/{p}");
            //            //});
            //            //myStore.SaveChanges();

            var addTriples = new StringBuilder();
            relations.ForEach(r =>
            {
                addTriples.AppendLine($"<{prefix}{r.SubjectUrl}> <{prefix}Predicates/{r.PredicateID}> <{prefix}{r.ObjectUrl}>.");
            });
            var transactionData = new UpdateTransactionData { InsertData = addTriples.ToString() };
            var jobInfo = context.ExecuteTransaction("d3s4", transactionData);

        }
    }
}
