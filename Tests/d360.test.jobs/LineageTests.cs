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

            var sql = @"
declare @tbl table	(
					IntersectID int, ID int, 
					SubjectNodeID int, SubjectTypeName nvarchar(1000), SubjectObjectName nvarchar(1000), Subject varchar(50), SubjectID int, SubjectBackColor varchar(10), SubjectForeColor varchar(10),  
					ObjectNodeID int, ObjectTypeName nvarchar(1000), ObjectObjectName nvarchar(1000), Object varchar(50), ObjectID int, ObjectBackColor varchar(10), ObjectForeColor varchar(10),
					PredicateID int, Predicate nvarchar(250)
					)
insert into @tbl
	select	distinct
			R.IntersectID,
			M.ID,
			M.SubjectIntersectNodeID,
			R.SourceTypeName,
			R.SourceObjectName,
			R.SourceObject,
			R.SourceObjectID,
			SD.[IconBackColor] as SourceIconBackColor,
			SD.[IconForeColor] as SourceIconForeColor,
			M.ObjectIntersectNodeID,
			R.TargetTypeName,
			R.TargetObjectName,
			R.TargetObject,
			R.TargetObjectID,
			TD.[IconBackColor] as TargetIconBackColor,
			TD.[IconForeColor] as TargetIconForeColor,
			M.PredicateID,
			P.Name as Predicate
	from	IntersectMap M
			inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and M.[Type] = 1
			inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
			inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
			inner join Predicate P on P.ID = M.PredicateID
			inner join [cache].[Relationship] SR on SR.SourceObject = @type and SR.SourceObjectID = @id and SR.TargetObject = R.SourceObject and SR.TargetObjectID = R.SourceObjectID
			inner join [cache].[Relationship] TR on TR.SourceObject = @type and TR.SourceObjectID = @id and TR.TargetObject = R.TargetObject and TR.TargetObjectID = R.TargetObjectID
	union
	select	distinct
			R.IntersectID,
			M.ID,
			M.SubjectIntersectNodeID,
			R.SourceTypeName,
			R.SourceObjectName,
			R.SourceObject,
			R.SourceObjectID,
			SD.[IconBackColor] as SourceIconBackColor,
			SD.[IconForeColor] as SourceIconForeColor,
			M.ObjectIntersectNodeID,
			R.TargetTypeName,
			R.TargetObjectName,
			R.TargetObject,
			R.TargetObjectID,
			TD.[IconBackColor] as TargetIconBackColor,
			TD.[IconForeColor] as TargetIconForeColor,
			M.PredicateID,
			P.Name as Predicate
	from	IntersectMap M
			inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.SourceObject = @type and R.SourceObjectID = @id and M.[Type] = 1
			inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
			inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
			inner join Predicate P on P.ID = M.PredicateID
	union
	select	distinct
			R.IntersectID,
			M.ID,
			M.SubjectIntersectNodeID,
			R.SourceTypeName,
			R.SourceObjectName,
			R.SourceObject,
			R.SourceObjectID,
			SD.[IconBackColor] as SourceIconBackColor,
			SD.[IconForeColor] as SourceIconForeColor,
			M.ObjectIntersectNodeID,
			R.TargetTypeName,
			R.TargetObjectName,
			R.TargetObject,
			R.TargetObjectID,
			TD.[IconBackColor] as TargetIconBackColor,
			TD.[IconForeColor] as TargetIconForeColor,
			M.PredicateID,
			P.Name as Predicate
	from	IntersectMap M
			inner join [cache].[Relationships] R on M.SubjectIntersectNodeID = R.SourceIntersectNodeID and M.ObjectIntersectNodeID = R.TargetintersectNodeID and R.TargetObject = @type and R.TargetObjectID = @id and M.[Type] = 1
			inner join [cache].ObjectDetails SD on SD.[Object] = R.SourceObject and SD.ObjectID = R.SourceObjectID
			inner join [cache].ObjectDetails TD on TD.[Object] = R.TargetObject and TD.ObjectID = R.TargetObjectID
			inner join Predicate P on P.ID = M.PredicateID

declare @h table	(
					ID int, [Type] varchar(1), IsStart bit, IsEnd bit,
					[Level] int, NodeID int, TypeName nvarchar(1000), ObjectName nvarchar(1000), O varchar(50), OID int, BackColor varchar(10), ForeColor varchar(10)
					)

insert into @h
	select	ID, 'S', 0, 0, 0, SubjectNodeID, SubjectTypeName, SubjectObjectName, Subject, SubjectID, SubjectBackColor, SubjectForeColor from	@tbl

insert into @h
	select	ID, 'O', 0, 0, 0, ObjectNodeID, ObjectTypeName, ObjectObjectName, Object, ObjectID, ObjectBackColor, ObjectForeColor from	@tbl

update	T
set		T.[Level] = 1,
		T.IsStart = 1
from	@h T
		left join @h S on S.O = T.O and S.OID = T.OID and S.[Type] = 'O'
where	T.[Type] = 'S'
		and S.ID is null

update	T
set		T.IsEnd = 1
from	@h T
		left join @h S on S.O = T.O and S.OID = T.OID and S.[Type] = 'S'
where	T.[Type] = 'O'
		and S.ID is null

select * from @h";

            #endregion

            //var deepestHierarchy = new List<DeepestHierarchyModel>();

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
