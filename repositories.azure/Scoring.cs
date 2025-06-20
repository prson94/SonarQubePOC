using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Scoring : Repository, IScoring
	{
		public Scoring(DapperConnectionProvider provider) : base(provider) { }

		public async Task<RepositoryResponse<List<Guid>>> ReadAssetUidsAssociatedToPolicyAsync(Guid uid)
		{
			RepositoryResponse<List<Guid>> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var ruleId = await connection.QueryFirstAsync<int>(
					"select Id from [security].[Rule] where Uid = @uid;", new { uid }
					);

				if (ruleId == 0)
				{
					return new(404, "No matching security policy found based on uid.");
				}

				// Get the impacted assets that we need to rescore.
				var assetUids = await connection.QueryAsync<Guid>(@"
declare @assetTypes table(Id int);

insert into @assetTypes
	select	A.ID
	from	security.[Rule] P
			inner join security.[Role] O on O.Id = P.RoleId and P.Id = @ruleId
			inner join metrics.Allocation Al on Al.ScoreType = 1 and Al.IsExternallyCalculated = 0
			inner join dbo.AssetType A on A.Uid = Al.AssetTypeUid
			inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
			inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
				and ( 
					(@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
					(@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
					) 
				and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
				and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = O.Uid
				and V.Definition <> '{}';

create table #assets (Uid uniqueidentifier);

insert into #assets
	select  A.Uid
	from    security.[Rule] R
			inner join security.RuleAssignment RA on RA.RuleId = R.Id and R.Id = @ruleId and RA.AssetId = 0
			inner join dbo.Asset A on A.AssetTypeId = RA.AssetTypeId and A.AssetTypeId in (select Id from @assetTypes);

insert into #assets
	select  A.Uid
	from    security.[Rule] R
			inner join security.RuleAssignment RA on RA.RuleId = R.Id and R.Id = @ruleId
			inner join dbo.Asset A on A.AssetTypeId = RA.AssetTypeId and A.Id = RA.AssetId and A.AssetTypeId in (select Id from @assetTypes);

select Uid from #assets group by Uid", new { ruleId, today = DateTime.UtcNow.Date });

				response = new(assetUids.ToList(), 200, true);
			}

			return response;
		}

		public async Task<RepositoryResponse<List<Guid>>> ReadAssetUidsAssociatedToRoleAsync(Guid uid)
		{
			RepositoryResponse<List<Guid>> response;

			using (var connection = (SqlConnection)ConnectionProvider.Connect(true))
			{
				var roleId = await connection.QueryFirstAsync<int>(
					"select Id from [security].[Role] where Uid = @uid;", new { uid }
					);

				if (roleId == 0)
				{
					return new(404, "No matching role found based on uid.");
				}

				// Get the impacted assets that we need to rescore.
				var assetUids = await connection.QueryAsync<Guid>(@"
declare @assetTypes table(Id int);

insert into @assetTypes
	select	A.ID
	from	security.[Role] O
			inner join metrics.Allocation Al on Al.ScoreType = 1 and Al.IsExternallyCalculated = 0  and O.Id = @roleId
			inner join dbo.AssetType A on A.Uid = Al.AssetTypeUid
			inner join metrics.Asset M on M.AllocationUid = Al.Uid and M.State = 1 and M.IsGroup = 0
			inner join metrics.AssetVersion V on V.AssetUid = M.Uid 
				and ( 
					(@today between V.EffectiveDate and V.EffectiveEndDate and V.EffectiveEndDate is not null) or 
					(@today >= V.EffectiveDate and V.EffectiveEndDate is null) 
					) 
				and JSON_VALUE(V.Definition, '$.Governance.Check') = 'Owner'
				and JSON_VALUE(V.Definition, '$.Governance.Owner.ResponsibilityTypeUid') = O.Uid
				and V.Definition <> '{}';

select	A.Uid
into	#assets
from	dbo.Asset A
		inner join security.[Override] O on O.RoleId = @roleId and O.AssetID = A.ID;

insert into #assets
	select  A.Uid
	from    security.[Rule] R
			inner join security.RuleAssignment RA on RA.RuleId = R.Id and R.RoleId = @roleId and RA.AssetId = 0
			inner join dbo.Asset A on A.AssetTypeId = RA.AssetTypeId and A.AssetTypeId in (select Id from @assetTypes);

insert into #assets
	select  A.Uid
	from    security.[Rule] R
			inner join security.RuleAssignment RA on RA.RuleId = R.Id and R.RoleId = @roleId
			inner join dbo.Asset A on A.AssetTypeId = RA.AssetTypeId and A.Id = RA.AssetId and A.AssetTypeId in (select Id from @assetTypes);

select Uid from #assets group by Uid", new { roleId, today = DateTime.UtcNow.Date });

				response = new(assetUids.ToList(), 200, true);
			}

			return response;
		}
	}
}
