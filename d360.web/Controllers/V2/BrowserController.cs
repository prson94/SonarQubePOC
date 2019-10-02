using d360.core.entities;
using d360.model;
using Microsoft.Web.Http;
using System;
using System.Web.Http;
using d360.core;
using System.Linq;
using System.Data.SqlClient;
using d360.core.enums;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;

namespace d360.web.Controllers.V2
{
    /// <summary>
    /// This service supports all asset browser functionality for Lineage version 3.
    /// </summary>
    [
        ApiVersion("2.0"),
        RoutePrefix("api/v{version:apiVersion}/browser"),
        Authorize
    ]
    public class BrowserController : BaseV2ApiController
    {
        public BrowserController(ICommunityContext community, ICompanyContext company) : base(community, company)
        {
        }

        internal class DirectLineageRow
        {
            public int Hop { get; set; }
            public long FromID { get; set; }
            public long ToID { get; set; }
        }

        internal class HierarchyLineageRow
        {
            public int Hop { get; set; }
            public string FromTo { get; set; }
            public long ID { get; set; }
            public int HierarchyLevel { get; set; }
            public int AssetTypeID { get; set; }
            public long AssetID { get; set; }
            public string DisplayValue { get; set; }
            public AssetTypeClass Class { get; set; }
            public string AssetTypeName { get; set; }
        }

        public class AssetBrowserLineageApiRelationshipModel
        {
            public Guid intersectUid { get; set; }
            public Guid subjectUid { get; set; }
            public Guid objectUid { get; set; }
            public string predicate { get; set; }
            public Guid predicateUid { get; set; }
            public string backColor { get; set; }
            public string foreColor { get; set; }
        }

        public class AssetBrowserLineageApiItemModel
        {
            public Guid assetUid { get; set; }
            public string displayValue { get; set; }
            public List<AssetBrowserLineageApiItemModel> items { get; set; }
        }

        public class AssetBrowserLineageApiTopItemModel: AssetBrowserLineageApiItemModel
        {
            public string backColor { get; set; }
            public string foreColor { get; set; }
        }

        public class AssetBrowserLineageApiModel
        {
            public Guid focalAssetUid { get; set; }
            public List<AssetBrowserLineageApiTopItemModel> assets { get; set; } = new List<AssetBrowserLineageApiTopItemModel>();
            public List<AssetBrowserLineageApiRelationshipModel> intersects { get; set; } = new List<AssetBrowserLineageApiRelationshipModel>();
        }

        [Route("{assetUid: Guid}"), HttpGet]
        public HttpResponseMessage GetAssetLineage(Guid assetUid)
        {
            try
            {
                var asset = Company.Filter<Asset>(i => i.uid == assetUid).FirstOrDefault();

                if (asset == null)
                {
                    return ReturnApiError(HttpStatusCode.NotFound, $"Asset with uid of {assetUid.ToString()} could not be found.");
                }

                var reader = Company.QueryMultipleAsync(@"
drop table if exists #tbl
create table #tbl ([Hop] int, Uid uniqueidentifier, FromID bigint, FromPath nvarchar(2500), FromSegment xml, ToID bigint, ToPath nvarchar(2500), ToSegment xml)
create index ix_TempTbl on #tbl ([Hop] desc, ToID asc)

declare	@level int = 0,
		@current int = 1,
		@max int = @hops*2 -- (* 2) because we have to include the hop through the transformation asset

insert into #tbl ([Hop], ToID) values (@level, @assetId)

while @current <= @max
begin
	set @level = @level + 1

	insert into #tbl
		select	@level as [Hop],
				E.Uid,
				
                S.ID as FromID,
				S.[Path] as FromPath,
				S.[Segments] as FromSegments,
				
                T.ID as ToID,
				T.[Path] as ToPath,
				T.[Segments] as ToSegments
		from	graph.AssetNode S,
				graph.AssetEdge E,
				graph.AssetNode T,
				#tbl J
		where	MATCH(S-(E)->T)
				and J.[Hop] = @level-1 
				and J.ToID = S.ID

	set @current = @current+1
end

drop table if exists #hierarchies
create table #hierarchies ([Hop] int, FromTo varchar(1), ID bigint, HierarchyLevel int, AssetTypeID int, AssetID bigint, DisplayValue nvarchar(500), Class int, AssetTypeName nvarchar(250))
create index ix_TempHierarchies on #hierarchies ([Hop] asc, HierarchyLevel asc)

-- From hierarchies
insert into #hierarchies
	select	T.[Hop],
			'F',
			T.FromID,
			doc.c.value('@level', 'int') as [HierarchyLevel],
			doc.c.value('@assetTypeId', 'int') as [assetTypeId],
			doc.c.value('@assetId', 'bigint') as [assetId],
			d.DisplayValue,
			[AT].Class,
			[AT].Name as AssetTypeName,
			null
	from	#tbl T
			cross apply T.FromSegment.nodes('/path/segment') doc(c)
			inner join AssetDisplayValue d on d.AssetID = doc.c.value('@assetId', 'bigint')
			inner join AssetType [AT] on [AT].ID = doc.c.value('@assetTypeId', 'int');

-- To hierarchies
insert into #hierarchies
	select	T.[Hop],
			'T',
			T.ToID,
			doc.c.value('@level', 'int') as [HierarchyLevel],
			doc.c.value('@assetTypeId', 'int') as [assetTypeId],
			doc.c.value('@assetId', 'bigint') as [assetId],
			d.DisplayValue,
			[AT].Class,
			[AT].Name as AssetTypeName,
			null
	from	#tbl T
			cross apply T.ToSegment.nodes('/path/segment') doc(c)
			inner join AssetDisplayValue d on d.AssetID = doc.c.value('@assetId', 'bigint')
			inner join AssetType [AT] on [AT].ID = doc.c.value('@assetTypeId', 'int');

delete #tbl where Hop = 0;

-- Direct lineage
select * from #tbl;

-- Select hierarchies
select * from #hierarchies;
", new { assetId = asset.ID, hops = 3 }).Result;

                var hops = reader.Read<DirectLineageRow>().ToList();
                var hierarchies = reader.Read<HierarchyLineageRow>().OrderBy(i => i.Hop).ThenBy(i => i.HierarchyLevel).ToList();

                var model = new AssetBrowserLineageApiModel {
                    focalAssetUid = assetUid
                };

                hierarchies.ForEach(h =>
                {
                    if (h.Hop == 1)
                    {

                    }
                });

                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            catch (Exception ex)
            {
                return ReturnApiError(HttpStatusCode.InternalServerError, ex.GetFullExceptionData(false));
            }
        }

        //private 
    }
}
