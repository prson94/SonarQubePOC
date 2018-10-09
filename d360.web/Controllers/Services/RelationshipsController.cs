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
using System.Web.Http.Results;
using Microsoft.Web.Http;

namespace d360.web.Controllers.Services
{
    [ApiVersion("1.0"), RoutePrefix("services/relationships"), Name("Relationships"), Authorize]
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


        public class LineageModel
        {
            public int IntersectID { get; set; }
            public int? IntersectGroupID { get; set; }
            public int IntersectTypeID { get; set; }

            public long SubjectAssetID { get; set; }
            public string Subject { get; set; }
            public int SubjectID { get; set; }
            public string SubjectName { get; set; }
            public string SubjectBackColor { get; set; }
            public string SubjectForeColor { get; set; }
            public string SubjectTypeName { get; set; }
            public string SubjectType { get; set; }
            public int SubjectTypeID { get; set; }
            public int SubjectAssetTypeID { get; set; }

            public long ObjectAssetID { get; set; }
            public string Object { get; set; }
            public int ObjectID { get; set; }
            public string ObjectName { get; set; }
            public string ObjectBackColor { get; set; }
            public string ObjectForeColor { get; set; }
            public string ObjectTypeName { get; set; }
            public string ObjectType { get; set; }
            public int ObjectTypeID { get; set; }
            public int ObjectAssetTypeID { get; set; }

            public State State { get; set; }

            public string Predicate { get; set; }

            public string SubjectPrefix { get; set; } = string.Empty;
            public string ObjectPrefix { get; set; } = string.Empty;
            public bool Processed { get; set; } = false;
        }

        public class Node
        {
            public string key { get; set; }
            public long assetId { get; set; }
            public string @object { get; set; }
            public int objectId { get; set; }
            public int intersectId { get; set; }
            public int state { get; set; }
            public int? intersectGroupId { get; set; }
            public string name { get; set; }
            public string backColor { get; set; }
            public string foreColor { get; set; }
            public string objectTypeName { get; set; }
            public string objectType { get; set; }
            public int objectTypeId { get; set; }
            public int assetTypeId { get; set; }

        }

        public class Link
        {
            public string from { get; set; }
            public string to { get; set; }
            public int intersectId { get; set; }
            public int intersectTypeId { get; set; }
            public int state { get; set; }
            public string predicate { get; set; }
        }

        public class LineageDiagramModel
        {
            public string Object { get; set; }
            public int ObjectID { get; set; }
            public List<Node> Nodes { get; set; }
            public List<Link> Links { get; set; }

            public List<Node> OriginalNodes { get; set; }
            public List<Link> OriginalLinks { get; set; }
        }

        [Route("{object}/{id:int}/lineage")]
        public HttpResponseMessage GetLineage(SystemObjects @object, int id)
        {
            #region SQL

            var sql = @"lineage.GetByObject @o, @oid";

            #endregion

            var list = Company.Query<string>(sql, new { o = @object.ToString(), oid = id }).ToList();

            var json = Newtonsoft.Json.JsonConvert.DeserializeObject(string.Join("",list));

            return Request.CreateResponse(HttpStatusCode.OK, json);
        }

        [Route("save/lineage"), HttpPost]
        public JsonResult<dynamic> SaveLineage(LineageDiagramModel model)
        {
            if (model == null || model.Object == null || model.ObjectID <= 0)
                return Json<dynamic>(new { type = "error", title = "Error", message = "Model is missing focal object data." });
            if (model.Links == null)
                return Json<dynamic>(new { type = "error", title = "Error", message = "Model is missing link data." });

            bool canCreate = Company.HasAssetPermission(model.Object, model.ObjectID, Permission.ModifyAsset);
            bool canUpdate = Company.HasAssetPermission(model.Object, model.ObjectID, Permission.ModifyAsset);
            bool canDelete = Company.HasAssetPermission(model.Object, model.ObjectID, Permission.DeleteAsset);

            try
            {
                model.OriginalLinks.ForEach(l =>
                {
                    if (l.intersectId <= 0)
                        return;

                    var link = model.Links.FirstOrDefault(k => k.intersectId == l.intersectId);

                    if (link == null && canDelete)
                    {
                        var intersect = Company.GetById<Intersect>(l.intersectId);
                        if (intersect != null)
                        {
                            Company.Delete(intersect);
                        }

                    }
                });

                model.Links.ForEach(l =>
                {
                    if (l.intersectId <= 0)
                    {
                        if (l.intersectTypeId > 0)
                        {
                            var from = model.Nodes.FirstOrDefault(n => n.key == l.from);
                            var to = model.Nodes.FirstOrDefault(n => n.key == l.to);

                            if (from == null || to == null)
                                return;

                            var intersect = new Intersect
                            {
                                IntersectTypeID = l.intersectTypeId,
                                Subject = from.@object,
                                SubjectID = from.objectId,
                                Object = to.@object,
                                ObjectID = to.objectId,
                            };

                            var existing = Company.Intersects.Where(i => 
                            i.Object == intersect.Object && 
                            i.ObjectID == intersect.ObjectID && 
                            i.Subject == intersect.Subject && 
                            i.SubjectID == intersect.SubjectID && 
                            i.IntersectTypeID == intersect.IntersectTypeID)
                            .FirstOrDefault();

                            if (existing != null)
                            {
                                if (existing.State == State.Deleted && canCreate)
                                {
                                    //this intersect was soft deleted
                                    //need to hard delete and re-add to trigger any potential workflows on relationships
                                    var copy = new Intersect()
                                    {
                                        IntersectTypeID = existing.IntersectTypeID,
                                        Subject = existing.Subject,
                                        SubjectID = existing.SubjectID,
                                        Object = existing.Object,
                                        ObjectID = existing.ObjectID,
                                    };

                                    Company.DeleteRelationship(existing.ID);
                                    Company.Add(copy);
                                }
                                return;
                            }
                                
                            if (canCreate)
                            {
                                Company.Add(intersect);
                                Company.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        var intersect = Company.GetById<Intersect>(l.intersectId);
                        if (intersect != null && intersect.IntersectTypeID != l.intersectTypeId && l.intersectTypeId > 0 && canUpdate)
                        {
                            intersect.IntersectTypeID = l.intersectTypeId;
                            Company.SaveOrUpdate(intersect);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Json<dynamic>(new { type = "error", title = "Error", message = ex.GetBaseException().Message });
            }
            return Json<dynamic>(new { type = "confirm", title = "Success", message = "Lineage saved successfully." });
        }
    }
}
