using d360.core;
using d360.core.entities;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace d360.test.jobs
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

        void processSourceLevels(List<TestModel> list, int id)
        {
            var level = list.Single(i => i.ID == id && i.Type == "S").Level + 1;
            list.Where(i => i.ID == id && i.Type == "O").ToList().ForEach(i => {
                i.Level = level;
                processSourceLevels(list, i.O, i.OID, level);
            });
        }
        void processSourceLevels(List<TestModel> list, string obj, int objID, int level)
        {
            list.Where(i => i.O == obj && i.OID == objID && i.Type == "S" && i.Level == 0).ToList().ForEach(i => {
                i.Level = level;
                processSourceLevels(list, i.ID);
            });
        }


        public class SourcesToObjectModel
        {
            public int ID { get; set; }
            public int IntersectID { get; set; }

            public int SubjectIntersectNodeID { get; set; }
            public string SourceTypeName { get; set; }
            public string SourceObjectName { get; set; }
            public string SourceObject { get; set; }
            public int SourceObjectID { get; set; }
            public string SourceIconBackColor { get; set; }
            public string SourceIconForeColor { get; set; }
            public int SourceLevel { get; set; }

            public int ObjectIntersectNodeID { get; set; }
            public string TargetTypeName { get; set; }
            public string TargetObjectName { get; set; }
            public string TargetObject { get; set; }
            public int TargetObjectID { get; set; }
            public string TargetIconBackColor { get; set; }
            public string TargetIconForeColor { get; set; }
            public int TargetLevel { get; set; }

            public int PredicateID { get; set; }
            public string Predicate { get; set; }
        }

        public class TestModel
        {
            public int ID { get; set; }
            public string Type { get; set; }
            public bool IsStart { get; set; }
            public bool IsEnd { get; set; }
            public int Level { get; set; }
            public int NodeID { get; set; }
            public string TypeName { get; set; }
            public string ObjectName { get; set; }
            public string O { get; set; }
            public int OID { get; set; }
            public string BackColor { get; set; }
            public string ForeColor { get; set; }
        }

        [TestMethod]
        public void LoadLineageDiagramData()
        {
            var cnn = getCompanyConnection(15);
         
            #region SQL

            var sql = @"";

            #endregion

            var list = cnn.Query<TestModel>(sql, new { type = "Artifact", id = 113 }).ToList();

            list.Where(i => i.Level == 1).ToList().ForEach(i => {
                processSourceLevels(list, i.ID); //assumes type is "O"
            });

            var builder = new StringBuilder("");
            list.OrderBy(i => i.ID).ToList().ForEach(i => {
                builder.AppendLine($"ID: {i.ID}, Type: {i.Type}, Level: {i.Level}, Name: {i.ObjectName}     ");
            });

            Assert.IsNotNull(list);
        }
    }
}
