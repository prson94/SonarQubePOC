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

        public RelationshipsController(ICommunityContext community, ICompanyContext company)
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
