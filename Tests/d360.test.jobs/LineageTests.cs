using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.SqlClient;
using d360.core;
using d360.core.entities;
using System.Collections.Generic;
using System.Linq;
using Dapper;

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

        void processSourceLevels(List<SourcesToObjectModel> list, int level, string obj = null, int? objID = null)
        {
            if (!objID.HasValue)
            {
                list.ForEach(i => {
                    if (!list.Any(t => t.TargetObject == i.SourceObject && t.TargetObjectID == i.SourceObjectID))
                    {
                        i.SourceLevel = level;
                        i.TargetLevel = level + 1;
                    }
                });

                list.ForEach(i => {
                    if (!list.Any(t => t.TargetObject == i.SourceObject && t.TargetObjectID == i.SourceObjectID))
                    {
                        processSourceLevels(list, level + 1, i.TargetObject, i.TargetObjectID);
                    }
                });
            }
            else
            {
                list.Where(i => i.SourceLevel == 0 || i.TargetLevel == 0 && i.SourceObject == obj && i.SourceObjectID == objID).ToList().ForEach(i => {
                    i.SourceLevel = level;
                    i.TargetLevel = level + 1;
                    processSourceLevels(list, level + 1, i.TargetObject, i.TargetObjectID);
                });
            }
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

        [TestMethod]
        public void LoadLineageDiagramData()
        {
            var cnn = getCompanyConnection(22);
         
            #region SQL

            var sql = @"
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
		inner join Predicate P on P.ID = M.PredicateID";

            #endregion

            var list = cnn.Query<SourcesToObjectModel>(sql, new { type = "Artifact", id = 854 }).ToList();
            processSourceLevels(list, 1);

            Assert.IsNotNull(list);
        }
    }
}
