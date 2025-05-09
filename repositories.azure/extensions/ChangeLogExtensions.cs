using d360.core.entities.ChangeLog;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;

namespace repositories.azure.extensions
{
	internal static class ChangeLogExtensions
	{
		static string VersionSql(string whereSuffix)
		{
			return $@"
declare @version int = 0;
select @version = max([Version])+1 from ChangeLog where {whereSuffix}
if @version is null
begin
	set @version = 1
end";
		}

		static string ChangeLogSql(string column, string parameter)
		{
			return $@"
insert into dbo.ChangeLog ({column}, ChangedBy, ChangeObject, ChangeAction, Changes, [Version])
values ({parameter}, @currentUserId, @obj, @action, @json, @version)";
		}

		internal static async Task UpdateChangeLogForAsset(this SqlConnection connection, long assetId, int currentUserId, ChangeLogObject obj, ChangeLogAction action, dynamic data, SqlTransaction trans = null)
		{
			await connection.ExecuteAsync(
				$"{VersionSql("AssetId = @assetId")}" +
				$"{ChangeLogSql("assetId", "@assetId")}",
				new { assetId, currentUserId, obj, action, json = JsonConvert.SerializeObject(data) }, 
				trans);
		}

	}
}
