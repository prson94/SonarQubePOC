using d360.core.entities;
using d360.core.entities.ChangeLog;
using Dapper;
using repositories.azure.extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class History : Repository, IHistory
	{
		public History(DapperConnectionProvider provider) : base(provider) { }

		public async Task<PagedApiBaseViewModel<dynamic>> ReadLogsAsync(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			PagedApiBaseViewModel<dynamic> response;

			int pageNumber = queryParams.CheckForPageNumber();
			int pageSize = queryParams.CheckForPageSize();

			var parameters = new DynamicParameters();
			parameters.Add("@pageNumber", pageNumber);
			parameters.Add("@pageSize", pageSize);
			parameters.Add("@CurrentUserId", CurrentUserId);
			//parameters.Add("@pageNumber", pageNumber);

			//queryParams.CheckForQueryParameter<Guid?>("assetTypeUid", "O.Uid", "@uid");

			using (var connection = ConnectionProvider.Connect(true))
			{
				response = new PagedApiBaseViewModel<dynamic> { pageNum = pageNumber, pageSize = pageSize, total = 1 };

				var query = await connection.QueryMultipleAsync(@"select * from ChangeLog", parameters);
				int total = await query.ReadSingleAsync<int>();
				response.items = (await query.ReadAsync<ChangeLog>()).ToList();
			}

			return response;
		}
	}
}
