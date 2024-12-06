using d360.core;
using d360.core.entities;
using d360.core.entities.Membership;
using d360.core.enums;
using d360.core.helpers;
using d360.core.queue;
using d360.core.resources;
using d360.core.validators;
using d360.extensions;
using d360.featureflags;
using d360.model.DataAccessLayer.repositories;
using d360.model.helpers.filters;
using Dapper;
using MoreLinq;
using repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
	public class MembershipRepository : BaseRepository, IMembershipRepository
	{
		internal IAssetRepository AssetRepository;
		internal IQueueSource QueueSource;
		internal IStorageProvider StorageProvider;

		public MembershipRepository(
			ICompanyContext companyContext, 
			ISecurityContextProvider securityContext,
			IAssetRepository assetRepository, 
			IQueueSource queueSource, 
			IStorageProvider storageProvider, IFeatureFlagService ff)
			: base(companyContext, securityContext, ff)
		{
			AssetRepository = assetRepository;
			QueueSource = queueSource;
			StorageProvider = storageProvider;
		}

		public async Task<GroupApiModels> GetGroups(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			var dbArgs = new DynamicParameters();
			bool listColorsAsJSON = false;
			List<string> condition = new List<string>();
			string resourceString = "";
			string paginationStatement = "";

			var fieldColumns = new DynamicQuerySelects();
			var fieldJoins = new DynamicQueryJoins();

			if (queryParams != null)
			{
				if (queryParams.ToList().Any(q => q.Key.ToLower() == "uid"))
				{
					var uidString = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "uid").Value;

					if (Guid.TryParse(uidString, out Guid uid))
					{
						if (uid != Guid.Empty)
						{
							condition.Add("A.Uid = @Uid");
							dbArgs.Add("uid", uid);
						}

					}
				}

				if (queryParams.ToList().Any(q => q.Key.ToLower() == "name"))
				{

					var name = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "name").Value.Trim();

					if (!string.IsNullOrEmpty(name))
					{

						condition.Add("G.Name like  @name");
						dbArgs.Add("name", name + '%');
					}
				}

				if (queryParams.ToList().Any(q => q.Key.ToLower() == "resourceuid"))
				{

					var user = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == "resourceuid").Value.Trim();

					if (!string.IsNullOrEmpty(user))
					{
						resourceString = @"left join Asset U on U.[uid] = @user
										left join[dbo].[ResourceGroup] RG on RG.[ResourceID] = U.ObjectID ";
						condition.Add("RG.[GroupID] = G.ID");
						dbArgs.Add("user", user);
					}
				}

				var pageSize = queryParams.FirstOrDefault(q => q.Key == "_pageSize");
				var pageNum = queryParams.FirstOrDefault(q => q.Key == "_pageNum");

				if (int.TryParse(pageSize.Value, out int _pageSize) && int.TryParse(pageNum.Value, out int _pageNum))
				{
					paginationStatement = $"offset {_pageSize * (_pageNum - 1)} rows fetch next {_pageSize} rows only";
				}
			}

			if (queryParams.ToList().Any(k => k.Key.ToLower() == "_listcolorsasjson"))
			{
				bool.TryParse(queryParams.FirstOrDefault(k => k.Key.ToLower() == "_listcolorsasjson").Value, out listColorsAsJSON);
			}

			var groupIdList = CompanyContext.AssetTypes.Where(a => a.Class == AssetTypeClass.Group).Select(s => s.ID);

			var fieldTypes = CompanyContext.FieldTypes.Where(f => groupIdList.Contains(f.AssetTypeID.Value)).ToList();
			getFieldSql(fieldTypes, dbArgs, fieldJoins, fieldColumns, listColorsAsJSON: listColorsAsJSON);

			if (queryParams != null)
			{
				if (queryParams.ToList().Any(x => x.Key.ToLower() == "_simplefilter"))
				{
					var simpleFilter = queryParams.FirstOrDefault(x => x.Key.ToLower() == "_simplefilter").Value.Trim();

					if (!string.IsNullOrEmpty(simpleFilter))
					{
						simpleFilter = CompanyContext.GetEscapedFilterString(simpleFilter);

						dbArgs.Add("@simpleFilter", simpleFilter);

						List<string> simpleFilters = new List<string>();

						//There may be multiple OwnershipLookup fields, but they all look to the same table for filtering, so that will be dealt with below
						var fields = fieldTypes.Zip(fieldColumns.Selects(), (type, column) => (type, column))
							.Where(x => x.type.IsListable == true && x.type.Type != DataType.OwnershipLookup.ToString());

						foreach (var (ft, column) in fields)
						{
							simpleFilters.Add($"{column.FilterStatement} like @simpleFilter");
						}

						simpleFilters.Add($"G.Name like @simpleFilter");

						condition.Add($"({string.Join(" or ", simpleFilters)})");
					}
				}
			}

			var sqlOrderBy = CompanyContext.ParseOrderColumn(queryParams, Enumerable
				.Zip(
					fieldTypes,
					fieldColumns.Selects(),
					(type, column) => new DefaultFilter(type.Name, column.FilterStatement, SqlFieldType.Text))
				.Concat(new[] { new DefaultFilter("Name", "G.Name", SqlFieldType.Text) })
				.ToList(),
				"Name");

			var sqlOrderDirection = this.CompanyContext.ParseOrderDirection(queryParams, "asc");

			var whereStatements = condition.Count != 0 ? $" where  {string.Join(" and ", condition)}" : "";
			var sql = $@"
				   Select 
					   A.Uid,
					   {(fieldColumns.GetStatements().Count > 0 ? string.Join(",\n", fieldColumns.GetStatements()) + "," : "")}
					   G.Name,
					   G.Description,
					   gr1.uid as PrimaryOwnerUid,
					   gr2.uid as SecondaryOwnerUid,
					   G.IsActiveDirectoryGroup
					   from [Group] G
						   inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
						   left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
						   left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
						   {(fieldJoins.Count > 0 ? string.Join("\n", fieldJoins.SQLJoinStatement) : "")}
						   {resourceString} 
						   {whereStatements}  
						   order by {sqlOrderBy} {sqlOrderDirection}
						   {paginationStatement}";

			var countSql = $@"Select count(*) from [Group] G
			inner join Asset A on A.[Object]='Group' and A.ObjectID = G.ID
			left join [reporting].[Global_Resource] gr1 on gr1.ResourceID = G.PrimaryOwnerResourceID
			left join [reporting].[Global_Resource] gr2 on gr2.ResourceID = G.SecondaryOwnerResourceID
			   {(fieldJoins.Count > 0 ? string.Join("\n", fieldJoins.GetStatements()) : "")}
				{resourceString} 
				{whereStatements}  ";

			var countResults = await CompanyContext.QueryAsync<int>(countSql, dbArgs, ApiTimeout);
			var count = countResults.First();

			var results = await CompanyContext.QueryAsync<dynamic>(sql, dbArgs, ApiTimeout);

			return new GroupApiModels() { items = results, Total = count };
		}

		public List<GroupResponseResult> UpdateGroups(ApiExecution execution, List<UpdateGroupModel> groups)
		{
			List<GroupResponseResult> results = null;

			try
			{
				results = CompanyContext.UpsertGroups(execution, groups);
				CompanyContext.CompleteApiExecutionAndGetCounts(execution.ExecutionID, ApiExecutionAction.PutGroups);
			}
			catch (Exception ex)
			{
				CompanyContext.UpdateExecutionWithErrorFromException(execution, ex);
			}

			return results;
		}

		public List<GroupResponseResult> AddGroups(ApiExecution execution, List<UpdateGroupModel> groups)
		{
			List<GroupResponseResult> results = null;

			try
			{
				results = CompanyContext.UpsertGroups(execution, groups);
				CompanyContext.CompleteApiExecutionAndGetCounts(execution.ExecutionID, ApiExecutionAction.PostGroups);
			}
			catch (Exception ex)
			{
				CompanyContext.UpdateExecutionWithErrorFromException(execution, ex);
			}

			return results;
		}

		[Obsolete]
		public async Task ClearFavorites(int resourceID)
		{
			await CompanyContext.DeleteAsync<Favorite>(i => i.ResourceID == resourceID && !i.IsHomePage);
		}

		public async Task DeleteFavorites(int resourceID, List<int> favoriteIds)
		{
			await CompanyContext.DeleteAsync<Favorite>(i => i.ResourceID == resourceID && favoriteIds.Contains(i.ID));
		}
	}
}
