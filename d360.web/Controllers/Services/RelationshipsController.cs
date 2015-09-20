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
            var sql = "";

            switch (type)
            {
                case SystemObjects.Fusion:
                    #region
                    sql = string.Format(@"with h as (select ID from FusionAttribute where FusionID = {0})", id);
                    break;
                    #endregion
                case SystemObjects.FusionAttribute:
                    #region
                    sql = string.Format(
@"with h as	(
			select	ID, ParentID
			from	FusionAttribute
			where	ID = {0}
			union all
			select	C.ID,
					C.ParentID
			from	FusionAttribute C
					inner join h as P on P.ID = C.ParentID
			)", id);
                    break;
                    #endregion
                case SystemObjects.FusionAttributeType:
                    #region
                    sql = string.Format(@"with h as	(
			select	ID,
					ParentID
			from	FusionAttribute
			where	FusionAttributeTypeID = {0} and ( (ParentID = {1} and {1} > 0) OR ({1} = 0 and 1=1) )
			union all
			select	C.ID,
					C.ParentID
			from	FusionAttribute C
					inner join h as P on P.ID = C.ParentID
			)", id, parentAttributeID);
                    break;
                    #endregion
            }


            switch (targetType)
            { 
                case SystemObjects.IntersectType:
                    sql +=
@"select	FA.Name as FusionAttribute,
		    FA.TextPath as FusionAttributeTextPath,
		    FA.ID as FusionAttributeID,
		    FAT.Name as FusionAttributeType,
		    case 
                when R.SourceIntersectTypeNodeID = STN.ID then R.SourceObjectID
                else R.TargetObjectID
            end SourceID,
		    case 
                when R.SourceIntersectTypeNodeID = STN.ID then R.SourceType
                else R.TargetType
            end as SourceType,
		    case 
                when R.SourceIntersectTypeNodeID = STN.ID then R.SourceObjectName
                else R.TargetObjectName
            end as SourceName,
		    case 
                when R.SourceIntersectTypeNodeID = STN.ID then R.SourceTypeName
                else R.TargetTypeName
            end as SourceTypeName,
		    case 
                when R.SourceIntersectTypeNodeID = STN.ID then dbo.GenerateObjectUrl(R.SourceType, R.SourceTypeID, R.SourceObjectID)
                else dbo.GenerateObjectUrl(R.TargetType, R.TargetTypeID, R.TargetObjectID)
            end as SourceUrl,
		    case 
                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetObjectID
                else R.SourceObjectID
            end as TargetID,
		    case 
                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetType
                else R.SourceType
            end as TargetType,
		    case 
                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetObjectName
                else R.SourceObjectName
            end as TargetName,
		    case 
                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetTypeID
                else R.SourceTypeID
            end as TargetTypeID,
		    case
                when R.TargetIntersectTypeNodeID = TTN.ID then R.TargetTypeName
                else R.SourceTypeName
            end as TargetTypeName,
		    case 
                when R.TargetIntersectTypeNodeID = TTN.ID then dbo.GenerateObjectUrl(R.TargetType, R.TargetTypeID, R.TargetObjectID)
                else dbo.GenerateObjectUrl(R.SourceType, R.SourceTypeID, R.SourceObjectID)
            end as TargetUrl
from	    FusionAttribute FA
		    inner join  h on h.ID = FA.ID
		    inner join FusionAttributeType FAT on FAT.ID = FA.FusionAttributeTypeID
		    inner join cache.Relationships R on R.SourceObject = 'FusionAttribute' and R.SourceObjectID = FA.ID and R.TargetType = 'IntersectType' and R.TargetTypeID = @id
		    inner join IntersectTypeNode STN on STN.IntersectTypeID = R.IntersectTypeID and STN.[Order] = 1
		    inner join IntersectTypeNode TTN on TTN.IntersectTypeID = R.IntersectTypeID and TTN.[Order] = 2";
                    break;
                default:
                    sql +=
@"select	FA.Name as FusionAttribute,
		FA.TextPath as FusionAttributeTextPath,
		FA.ID as FusionAttributeID,
		FAT.Name as FusionAttributeType,
		R.TargetObjectID as TargetID,
		R.TargetObject as TargetType,
		R.TargetObjectName as TargetName,
		R.TargetTypeID,
		R.TargetTypeName,
		dbo.GenerateObjectUrl(R.TargetType, R.TargetTypeID, R.TargetObjectID) as TargetUrl
from	FusionAttribute FA
		inner join  h on h.ID = FA.ID
		inner join FusionAttributeType FAT on FAT.ID = FA.FusionAttributeTypeID
		inner join cache.Relationships R on R.SourceObject = 'FusionAttribute' and R.SourceObjectID = FA.ID and R.TargetType = @type and R.TargetTypeID = @id";
                    break;
            }

            return Company.Query<dynamic>(sql, new { type = targetType.ToString(), id = targetID }).AsQueryable();
        }
    }
}
