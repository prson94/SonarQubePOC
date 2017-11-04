using d360.core.entities;
using d360.extensions;
using d360.model;
using d360.core;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData.Query;
using System.Web.Http.OData;
using System.Dynamic;
using d360.web.Models;
using d360.web.Models.Attributes;
using System.Web.Http.Description;
using d360.core.enums;

namespace d360.web.Controllers.Services
{
    [RoutePrefix("services/relationships"), Name("Relationships"), Authorize]
    public class RelationshipsController : BaseApiController
    {
        #region DI

        public RelationshipsController(CommunityContext community, CompanyContext company)
            : base(community, company)
        {
        }

        #endregion

        /// <summary>
        /// Allows for OData filtering on relationships types.
        /// </summary>
        /// <returns>A list of relationships types present in the system.</returns>
        [Route(""), HttpGet]
        public IQueryable<IntersectType> GetIntersectTypes()
        {
            return Company.Table<IntersectType>();
        }

        [Route("{type}/{id:int}/{targetType}/{targetID}/{parentAttributeID:int}")]
        public IQueryable<dynamic> GetDynamicRelationships(SystemObjects type, int id, SystemObjects targetType, int targetID, int parentAttributeID)
        {
            //            var sql = "";

            //            switch (type)
            //            {
            //                case SystemObjects.Fusion:
            //                    #region
            //                    sql = string.Format(@"with h as (select ID from FusionAttribute where FusionID = {0})", id);
            //                    break;
            //                    #endregion
            //                case SystemObjects.FusionAttribute:
            //                    #region
            //                    sql = string.Format(
            //@"with h as	(
            //			select	ID, ParentID
            //			from	FusionAttribute
            //			where	ID = {0}
            //			union all
            //			select	C.ID,
            //					C.ParentID
            //			from	FusionAttribute C
            //					inner join h as P on P.ID = C.ParentID
            //			)", id);
            //                    break;
            //                    #endregion
            //                case SystemObjects.FusionAttributeType:
            //                    #region
            //                    sql = string.Format(@"with h as	(
            //			select	ID,
            //					ParentID
            //			from	FusionAttribute
            //			where	FusionAttributeTypeID = {0} and ( (ParentID = {1} and {1} > 0) OR ({1} = 0 and 1=1) )
            //			union all
            //			select	C.ID,
            //					C.ParentID
            //			from	FusionAttribute C
            //					inner join h as P on P.ID = C.ParentID
            //			)", id, parentAttributeID);
            //                    break;
            //                    #endregion
            //            }


            //            switch (targetType)
            //            { 
            //                case SystemObjects.IntersectType:
            //                    sql +=
            //@"select	FA.Name as FusionAttribute,
            //		    FA.TextPath as FusionAttributeTextPath,
            //		    FA.ID as FusionAttributeID,
            //		    FAT.Name as FusionAttributeType,
            //		    case 
            //                when R.SourceIntersectTypeNodeID = STN.ID then R.SourceObjectID
            //                else R.TargetObjectID
            //            end SourceID,
            //		    case 
            //                when R.SourceIntersectTypeNodeID = STN.ID then R.SourceType
            //                else R.TargetType
            //            end as SourceType,
            //		    case 
            //                when R.SourceIntersectTypeNodeID = STN.ID then R.SourceObjectName
            //                else R.TargetObjectName
            //            end as SourceName,
            //		    case 
            //                when R.SourceIntersectTypeNodeID = STN.ID then R.SourceTypeName
            //                else R.TargetTypeName
            //            end as SourceTypeName,
            //		    case 
            //                when R.SourceIntersectTypeNodeID = STN.ID then dbo.GenerateObjectUrl(R.SourceType, R.SourceTypeID, R.SourceObjectID)
            //                else dbo.GenerateObjectUrl(R.TargetType, R.TargetTypeID, R.TargetObjectID)
            //            end as SourceUrl,
            //		    case 
            //                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetObjectID
            //                else R.SourceObjectID
            //            end as TargetID,
            //		    case 
            //                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetType
            //                else R.SourceType
            //            end as TargetType,
            //		    case 
            //                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetObjectName
            //                else R.SourceObjectName
            //            end as TargetName,
            //		    case 
            //                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetTypeID
            //                else R.SourceTypeID
            //            end as TargetTypeID,
            //		    case
            //                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetTypeName
            //                else R.SourceTypeName
            //            end as TargetTypeName,
            //		    case 
            //                when R.TargetIntersectTypeNodeID = TTN.ID then dbo.GenerateObjectUrl(R.TargetType, R.TargetTypeID, R.TargetObjectID)
            //                else dbo.GenerateObjectUrl(R.SourceType, R.SourceTypeID, R.SourceObjectID)
            //            end as TargetUrl
            //from	    FusionAttribute FA
            //		    inner join  h on h.ID = FA.ID
            //		    inner join FusionAttributeType FAT on FAT.ID = FA.FusionAttributeTypeID
            //		    inner join [Intersect] R on R.SourceObject = 'FusionAttribute' and R.SourceObjectID = FA.ID and R.TargetType = 'IntersectType' and R.TargetTypeID = @id
            //            inner join IntersectType RT on RT.ID = R.IntersectTypeID";
            //                    break;
            //                default:
            //                    sql +=
            //@"select	FA.Name as FusionAttribute,
            //		FA.TextPath as FusionAttributeTextPath,
            //		FA.ID as FusionAttributeID,
            //		FAT.Name as FusionAttributeType,
            //		R.TargetObjectID as TargetID,
            //		R.TargetObject as TargetType,
            //		R.TargetObjectName as TargetName,
            //		R.TargetTypeID,
            //		R.TargetTypeName,
            //		dbo.GenerateObjectUrl(R.TargetObject, R.TargetTypeID, R.TargetObjectID) as TargetUrl,
            //        R.[Description] as Description
            //from	FusionAttribute FA
            //		inner join  h on h.ID = FA.ID
            //		inner join FusionAttributeType FAT on FAT.ID = FA.FusionAttributeTypeID
            //		inner join cache.Relationships R on R.SourceObject = 'FusionAttribute' and R.SourceObjectID = FA.ID and R.TargetType = @type and R.TargetTypeID = @id";
            //                    break;
            //            }

            return null;//Company.Query<dynamic>(sql, new { type = targetType.ToString(), id = targetID }).AsQueryable();
        }

        [Route("technical/{type}/{id:int}/{targetType}/{targetID}")]
        public IQueryable<dynamic> GetTechnicalRelationships(SystemObjects type, int id, SystemObjects targetType, int targetID)
        {
            return Company.Query<dynamic>("EXEC GetTechnicalRelationshipsByObject @ResponsibleObjectType, @ResponsibleObjectID, @ObjectType, @ObjectID"
                , new
                {
                    ResponsibleObjectType = type.ToString(),
                    ResponsibleObjectID = id,
                    ObjectType = targetType.ToString(),
                    ObjectID = targetID
                }).AsQueryable();
           // return null;
        }


        public class TestModel
        {
            public int ID { get; set; }
            public long SubjectAssetID { get; set; }
            public string Subject { get; set; }
            public int SubjectID { get; set; }
            public string SubjectName { get; set; }
            public string SubjectBackColor { get; set; }
            public string SubjectForeColor { get; set; }
            public string SubjectTypeName { get; set; }

            public long ObjectAssetID { get; set; }
            public string Object { get; set; }
            public int ObjectID { get; set; }
            public string ObjectName { get; set; }
            public string ObjectBackColor { get; set; }
            public string ObjectForeColor { get; set; }
            public string ObjectTypeName { get; set; }

            public string Predicate { get; set; }

            public string SubjectPrefix { get; set; } = string.Empty;
            public string ObjectPrefix { get; set; } = string.Empty;
            public bool Processed { get; set; } = false;
        }

        public class Node
        {
            public string id { get; set; }
            public string name { get; set; }
            public string back { get; set; }
            public string fore { get; set; }
            public string type { get; set; }
        }

        public class Link
        {
            public string from { get; set; }
            public string to { get; set; }
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //private string RandomString(int length)
        //{
        //    var rand = new Random();
        //    const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
        //    var chars = Enumerable.Range(0, length).Select(x => pool[rand.Next(0, pool.Length)]);
        //    return new string(chars.ToArray());
        //}

        void Process(TestModel current, List<TestModel> list, List<Node> nodes, bool forward = true)
        {
            //var linkIndicator = RandomString(5);
            if (forward)
            {
                if (string.IsNullOrEmpty(current.ObjectPrefix))
                {
                    current.ObjectPrefix = RandomString(5);
                }
                
                //Add to node collection.
                if (!nodes.Any(i => i.id == $"{current.ObjectPrefix}.{current.ObjectAssetID}"))
                {
                    nodes.Add(new Node { id = $"{current.ObjectPrefix}.{current.ObjectAssetID}", name = current.ObjectName, back = current.ObjectBackColor, fore = current.ObjectForeColor, type = current.ObjectTypeName });
                }

                current.Processed = true;
                foreach (var o in list.Where(i => i.SubjectAssetID == current.ObjectAssetID)) //!i.Processed && 
                {
                    o.SubjectPrefix = current.ObjectPrefix;
                    Process(o, list, nodes, forward);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(current.SubjectPrefix))
                {
                    current.SubjectPrefix = RandomString(5);
                }

                //Add to node collection.
                if (!nodes.Any(i => i.id == $"{current.SubjectPrefix}.{current.SubjectAssetID}"))
                {
                    nodes.Add(new Node { id = $"{current.SubjectPrefix}.{current.SubjectAssetID}", name = current.SubjectName, back = current.SubjectBackColor, fore = current.SubjectForeColor, type = current.SubjectTypeName });
                }

                current.Processed = true;

                foreach (var o in list.Where(i => i.ObjectAssetID == current.SubjectAssetID)) //!i.Processed && 
                {
                    o.ObjectPrefix = current.SubjectPrefix;
                    Process(o, list, nodes, forward);
                }
            }
        }

        [Route("{object}/{id:int}/lineage")]
        public HttpResponseMessage GetLineage(SystemObjects @object, int id)
        {
            #region SQL

            var sql = @"
select	I.ID,
		SA.ID as SubjectAssetID,
        I.Subject,
		I.SubjectID,
		utility.GetAssetDisplayValueWrapper(SA.ID) as SubjectName,
        SA.BackColor as SubjectBackColor,
        SA.ForeColor as SubjectForeColor,
        SA.TypeName as SubjectTypeName,

        OA.ID as ObjectAssetID,
        I.Object,
		I.ObjectID,
		utility.GetAssetDisplayValueWrapper(OA.ID) as ObjectName,
        OA.BackColor as ObjectBackColor,
        OA.ForeColor as ObjectForeColor,
        OA.TypeName as ObjectTypeName,

        P.Name as [Predicate]
from	[Intersect] I
		inner join IntersectType T on T.ID = I.IntersectTypeID
		inner join AssetDetail SA on SA.Object = I.Subject and SA.ObjectID = I.SubjectID
		inner join AssetDetail OA on OA.Object = I.Object and OA.ObjectID = I.ObjectID
		inner join [Predicate] P on P.ID = T.PredicateID and P.Type = 1";

            #endregion

            var list = Company.Query<TestModel>(sql).ToList();

            var nodes = new List<Node>();
            var links = new List<Link>();

            list.ForEach(current =>
            {
                if (!current.Processed)
                {
                    Process(current, list, nodes, true);
                    Process(current, list, nodes, false);
                }
            });

            //var current = list[0];
            //Process(current, list, nodes, true);
            //Process(current, list, nodes, false);

            links = list.Select(i => new Link { from = $"{i.SubjectPrefix}.{i.SubjectAssetID}", to = $"{i.ObjectPrefix}.{i.ObjectAssetID}" }).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, new
            {
                nodes,
                links
            });

            //return Request.CreateResponse(HttpStatusCode.OK, list);
        }
    }
}
