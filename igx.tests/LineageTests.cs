using d360.core;
using d360.core.entities;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace igx.tests
{
    [TestClass]
    public class LineageTests
    {
        SqlConnection getCompanyConnection(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.Open();
            var db = cnn.Query<DatabaseServer>(
                @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                new { id = companyID }
            ).SingleOrDefault();
            cnn.Close();
            cnn.Dispose();

            if (db != null)
            {
                cnn = new SqlConnection(
                    string.Format("server={0};Database=D3S_{1};User ID={2};Password={3}", db.Server, companyID, db.Username, db.Password)
                );
                db = null;
            }
            return cnn;
        }

        public class TestModel
        {
            public int ID { get; set; }
            public long SubjectAssetID { get; set; }
            public string Subject { get; set; }
            public int SubjectID { get; set; }
            public string SubjectName { get; set; }
            public long ObjectAssetID { get; set; }
            public string Object { get; set; }
            public int ObjectID { get; set; }
            public string ObjectName { get; set; }
            public string Predicate { get; set; }
        }

        public class Node
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class Link
        {
            public string from { get; set; }
            public string to { get; set; }
        }

        [TestMethod]
        public void LoadLineageDiagramData()
        {
            var cnn = getCompanyConnection(15);
         
            #region SQL

            var sql = @"
select	I.ID,
		SA.ID as SubjectAssetID,
        I.Subject,
		I.SubjectID,
		utility.GetAssetDisplayValueWrapper(SA.ID) as SubjectName,
		OA.ID as ObjectAssetID,
        I.Object,
		I.ObjectID,
		utility.GetAssetDisplayValueWrapper(OA.ID) as ObjectName,
		P.Name as [Predicate]
from	[Intersect] I
		inner join IntersectType T on T.ID = I.IntersectTypeID
		inner join Asset SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
		inner join Asset OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID
		inner join [Predicate] P on P.ID = T.PredicateID and P.Type = 1";

            #endregion

            var list = cnn.Query<TestModel>(sql).ToList();

            var nodes = list.Select(i => new Node { id = $"{i.ID}.{i.SubjectAssetID}", name = i.SubjectName }).ToList();
            nodes.AddRange(
                list.Select(i => new Node { id = $"{i.ID}.{i.ObjectAssetID}", name = i.ObjectName })
                );

            var links = list.Select(i => new Link { from = $"{i.ID}.{i.SubjectAssetID}", to = $"{i.ID}.{i.ObjectAssetID}" }).ToList();

            Assert.IsNotNull(list);
        }
    }
}
