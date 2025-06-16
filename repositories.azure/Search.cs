using d360.core;
using d360.core.entities;
using d360.core.enums;
using Dapper;
using repositories.azure.extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Search: Repository, ISearch
	{
		public Search(DapperConnectionProvider provider) : base(provider) { }

		private string buildFullTextSql(
			bool searchByUid, bool includeAggregations, bool isFreeText, 
			bool includeFields, bool includePath, bool includeScores, 
			List<int> classes = null, List<int> assetTypeIds = null)
		{
			string sql = @"
	drop table if exists #aggregations;
	drop table if exists #results;
	create table #aggregations (Uid uniqueidentifier, [Class] int, Name nvarchar(250), ResultCount int);
	create table #results ([Rank] int, Id bigint);
";

			string ftTableFunction = isFreeText ? "FREETEXTTABLE" : "CONTAINSTABLE";

			string cteFilterSql = "";
			if (classes != null || assetTypeIds != null)
			{
				cteFilterSql = "inner join dbo.Asset a_s on a_s.ID = a.AssetId";
				if (assetTypeIds != null && assetTypeIds.Count > 0)
				{
					cteFilterSql += $" and a_s.AssetTypeId in ({string.Join(",", assetTypeIds)})";
				}
				if (classes != null && classes.Count > 0)
				{
					cteFilterSql += $" inner join dbo.AssetType a_st on a_st.ID = a_s.AssetTypeId and a_st.Class in ({string.Join(",", classes)})";
				}
			}

			Action<int, bool> appendCte = (int id, bool useFilter) =>
			{
				string permissionJoin = "";
				string appliedFilter = useFilter ? cteFilterSql : "";
				if (!CurrentUserIsAdmin)
				{
					permissionJoin = 
						" inner join dbo.Asset ap on ap.ID = a.AssetId" +
						" inner join dbo.AssetType apt on apt.Id = ap.AssetTypeId" +
						" where (" +
						"	apt.DefaultPermissions & 1 = 1 " +
						"	or exists (select 1 from dbo.ResponsibilityDetail where (AssetID = ap.ID OR (AssetID = 0 AND AssetTypeID = apt.ID)) and ResourceID = @CurrentUserId and PermissionsBitMask & 1 = 1)" +
						")";
				}

				if (searchByUid)
				{
					var whereType = permissionJoin == "" ? "where" : "and";
					sql += $@"
;with cte{id} as (
	select	100 as [Rank],
			a.Id
	from	dbo.Asset a
			{appliedFilter.Replace("a.AssetId", "a.id")} {permissionJoin.Replace("a.AssetId", "a.id")}
	{whereType}	a.Uid = @uid
			and a.State = 1
)
";
				}
				else 
				{
					sql += $@"
;with cte{id} as (
	select		2000 as [Rank],
				a.AssetId as Id
	from		AssetDisplayValue a
				inner join dbo.Asset a_state on a.AssetID = a_state.ID and a_state.State = 1 and a.DisplayValue like @sqlPhrase
				{appliedFilter} {permissionJoin}
	union
	select		s.[Rank]*4 as [Rank],
				a.AssetId as Id
	from		AssetDisplayValue a
				inner join dbo.Asset a_state on a.AssetID = a_state.ID and a_state.State = 1
				inner join {ftTableFunction}(AssetDisplayValue, DisplayValue, @phrase) s on s.[Key] = a.AssetID 
				{appliedFilter} {permissionJoin}
	union
	select		s.[Rank]*3 as [Rank],
				a.AssetId as Id
	from		Field a 
				inner join dbo.Asset a_state on a.AssetID = a_state.ID and a_state.State = 1
				inner join {ftTableFunction}(Field, FormattedValue, @phrase) s on s.[Key] = a.ID and a.AssetID is not null 
				{appliedFilter} {permissionJoin}
	union
	select		s.[Rank],
				a.AssetID as Id
	from		AssetTag a
				inner join dbo.Asset a_state on a.AssetID = a_state.ID and a_state.State = 1
				inner join Tag t on t.ID = a.TagID
				inner join {ftTableFunction}(Tag, [Value], @phrase) s on s.[Key] = t.ID 
				{appliedFilter} {permissionJoin}
	union
	select	s.[Rank]*2 as [Rank],
			a.AssetId as Id
	from	dbo.Asset o
			inner join AssetDataProfile a on a.AssetId = o.Id and a.ProfileSetDate = (select top 1 max(ProfileSetDate) from AssetDataProfile where AssetId = o.Id) and o.State = 1
			inner join Semantic st on st.Qualifier = a.TypeQualifier and st.EffectiveDate <= a.ProfileSetDate
			inner join CONTAINSTABLE(Semantic, Qualifier, @phrase) s on s.[Key] = st.ID
			{appliedFilter} {permissionJoin}
)
";				
				}
			};

			if (includeAggregations)
			{
				appendCte(1, false);

				sql += $@"
insert into #aggregations
	select	t.Uid, t.Class, t.Name, count(1) as ResultCount
	from	Asset a
			inner join AssetType t on t.ID = a.AssetTypeID
	where	a.ID in (select Id from cte1)
	group by t.Uid, t.Class, t.Name;";
			}
			
			appendCte(2, true);
			sql += $@"
insert into #results ([Rank], [Id])
	select	ir.[Rank], ir.Id
	from	(
			select	row_number() over (partition by Id order by [Rank] desc) as RowNum, [Rank], Id
			from	cte2
			) ir 
	where	ir.RowNum = 1
	order by [Rank] desc offset @offset rows fetch next @take rows only;";

			
			string includeFieldSql = "select '[]'";
			if (includeFields)
			{
				includeFieldSql = @"
select	ft.Name,
		ft.Type,
		ft.FriendlyName as [Label],
		ft.SearchPrefix as [Prefix],
		ft.SearchSuffix as [Suffix],
		f.FormattedValue as [Value],
		cast(iif(ft.ID is null, 1, 0) as bit) as [Empty]
from	FieldType ft
		left join Field f on f.FieldTypeID = ft.ID and f.AssetID = a.ID
where	ft.AssetTypeID = a.AssetTypeID
		and ft.SearchAddToResult = 1
order by ft.SearchDisplayOrder, ft.FriendlyName
for json path";
			}

			string includePathSql = "select '[]'";
			if (includePath)
			{
				includePathSql = @"
select	graph.GetPathAsJson(pa.Segments) 
from	AssetPath pa
where	pa.ID = a.ID";
			}

			string includeScoresSql = "select '[]'";
			if (includeScores)
			{
				includeScoresSql = @"
select	al.ScoreType,
		al.LowerThreshold,
		al.UpperThreshold,
		sc.[Value]
from	(
		select	isc.AllocationUid,
				isc.[Value],
				row_number() over (partition by isc.AllocationUid order by isc.EffectiveDate desc) as RowNum
		from	metrics.Score isc 
		where	isc.AssetUid = a.Uid 
				and isc.EndDate is null
		) sc
		inner join metrics.Allocation al on al.uid = sc.AllocationUid and sc.RowNum = 1
for json path";
			}

	sql += $@"
select * from #aggregations;

select	a.Uid,
		a.Object + '|' + cast(a.ID as varchar(50)) as ID,
		a.Object,
		a.ObjectId,
		adv.DisplayValue as [Name],
		t.Class,
		t.Name as [Type],
		t.Uid as AssetTypeUid,
		s.Icon,
		pr.HasProfiling,
		({includeFieldSql}) as _Fields,
		({includePathSql}) as _AssetPath,
		({includeScoresSql}) as _Scores,
		case when t.Class = 15 then dbo.GenerateAssetUrl(a.ID) else null end as Url
from	Asset a
		inner join AssetDisplayValue adv on adv.AssetID = a.ID
		inner join AssetType t on t.ID = a.AssetTypeID
		left join AssetTypeStyle s on s.ID = a.AssetTypeID
		inner join #results r on r.Id = a.Id
		cross apply (
			select iif(exists(select top 1 1 from AssetDataProfile where AssetID = a.ID), 1, 0) as HasProfiling 
		) pr
order by r.[Rank] desc;";

			return sql;
		}

		public async Task<RepositoryResponse<SearchModel>> ReadResultsAsync(
			string phrase, 
			bool includeFields, bool includePath, bool includeScore, bool includeAggregations,
			List<AssetTypeClass> _classes = null, List<Guid> _types = null,
			int offset = 0, int take = 250)
		{
			RepositoryResponse<SearchModel> response = new(200);

			if (take <= 0)
			{
				take = 25;
			}
			if (offset <= 0) 
			{
				offset = 0;
			}

			var parameters = new DynamicParameters();
			parameters.Add("@offset", offset);
			parameters.Add("@take", take);
			parameters.Add("@CurrentUserId", CurrentUserId);

			List<int> classes = null;
			if (_classes != null && _classes.Count > 0)
			{
				classes = _classes.Select(o => (int)o).ToList();
			}

			List<int> types = null;
			if (_types != null && _types.Count > 0)
			{
				using (var connection = ConnectionProvider.Connect(true))
				{
					types = (await connection.QueryAsync<int>("select ID from AssetType where Uid in @uids", new { uids = _types })).ToList();
				}
			}

			//If filtering both on Class and AssetType, convert Class to AssetTypes to simplify filter
			if (classes != null && types != null && classes.Any() && types.Any())
			{
				List<int> classTypes = null;
				using (var connection = ConnectionProvider.Connect(true))
				{
					classTypes = (await connection.QueryAsync<int>("select ID from AssetType where [Class] in @classes", new { classes })).ToList();
				}
				types.AddRange(classTypes);
				types = types.Distinct().ToList();
				classes = null;
			}

			// If user passed in a Uid, pass it into the sql to NOT bother performing a full-text search and instead, return the asset directly. In same format as search.
			bool searchByUid = false;
			if (Guid.TryParse(phrase, out Guid uid))
			{
				parameters.Add("@uid", uid);
				searchByUid = true;
			}

			bool isFreeText = false;
			var sqlPhrase = phrase.EscapeForLike().Replace("*", "%");
			phrase = phrase.ConvertPhraseToFullTextSearch();


			// Generate the SQL to run.
			var sql = buildFullTextSql(searchByUid, includeAggregations, isFreeText, includeFields, includePath, includeScore, classes, types);

			parameters.Add("@phrase", phrase);
			parameters.Add("@sqlPhrase", sqlPhrase);

			using (var connection = ConnectionProvider.Connect(true))
			{
				var dbResponse = await connection.QueryMultipleAsync(sql, parameters);

				var aggregations = (await dbResponse.ReadAsync<SearchResultAggregation>()).ToList();
				var results = (await dbResponse.ReadAsync<SearchResult>()).ToList();

				int total = (includeAggregations) ? 
					aggregations.Where(a => (
						(_classes == null || _classes.Count == 0) && (_types == null || _types.Count == 0) ||
						(_classes != null &&_classes.Contains(a.Class))) || (_types != null && _types.Contains((Guid)a.Uid))
					).Sum(a => a.ResultCount) : 
					results.Count;

				var classAggregations = aggregations
					.GroupBy(a => a.Class)
					.Select(a => new SearchResultAggregation { 
						Class = a.Key, 
						Name = a.Key.GetName(),
						DisplayName = a.Key.GetDisplayName(),
						ResultCount = a.Sum(r => r.ResultCount) 
					})
					.OrderBy(a => a.Name)
					.ToList();

				classAggregations.ForEach(p =>
				{
					p.Items = aggregations.Where(a => a.Class == p.Class).ToList();
					p.Items.ForEach(i =>
					{
						i.DisplayName = i.Name;
					});
				});

				aggregations = classAggregations;

				response.Data = new SearchModel
				{
					Matches = total,
					Aggregations = aggregations,
					Results = results
				};
			}

			return response;
		}
	}
}
