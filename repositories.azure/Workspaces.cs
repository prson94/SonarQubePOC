using d360.core.entities;
using d360.core.enums;
using Dapper;
using Dapper.Contrib.Extensions;
using DocumentFormat.OpenXml.EMMA;
using repositories.resources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace repositories.azure
{
	public class Workspaces : Repository, IWorkspaces
	{
		public int CompanyId { get; set; }
		public string WorkspaceId { get; set; }

		public Workspaces(DapperConnectionProvider provider): base(provider) { }
		
		public async Task<Dictionary<string, string>> ReadSettingsAsDictionaryAsync()
		{
			return (await ReadSettingsAsync()).ToDictionary(k => k.ID.ToString(), v => v.Value);
		}

		public async Task<SettingInfo> ReadSettingAsync(Setting setting)
		{
			string sql = "select * from Setting where ID = @id";
			var model = setting.AsInfoModel();
			dynamic @override;
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				@override = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { id = (int)setting });
			}

			if (@override != null)
			{
				if (@override.Value == "True" || @override.Value == "False")
				{
					@override.Value = @override.Value.ToLowerInvariant();
				}
				model.Value = @override.Value;
			}
			else
			{
				model.Value = model.DefaultValue;
			}

			return model;
		}

		public async Task<List<SettingInfo>> ReadSettingsAsync()
		{
			// Get the list of settings from the D3S_###.dbo.Setting table.
			// Get the full list of settings from the Setting enum.
			// Return a list of SettingInfo, merging the values present from the environment into the SettingInfo.Value property.

			List<dynamic> overrides;
			string sql = "select * from Setting";
			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				overrides = (await connection.QueryAsync<dynamic>(sql)).ToList();
			}

			List<SettingInfo> settings = [.. Setting.ActionMessage.GetAsList().OrderBy(s => (int)s.ID)];

			settings.ForEach(s =>
			{
				string defaultValue = s.DefaultValue;

				if (defaultValue == "True" || defaultValue == "False")
				{
					defaultValue = defaultValue.ToLowerInvariant();
				}

				if (overrides.Any(o => o.ID == (int)s.ID))
				{
					s.Value = overrides.First(o => o.ID == (int)s.ID).Value;

					if (s.Value == "True" || s.Value == "False")
					{
						s.Value = s.Value.ToLowerInvariant();
					}
				}
				else
				{
					s.Value = defaultValue;
				}
			});

			return settings;
		}

		public async Task<T> ReadSettingValueAsync<T>(Setting setting)
		{
			SettingInfo info = await ReadSettingAsync(setting);

			var checkType = default(T);

			if (checkType is Guid)
			{
				Guid guid = Guid.Parse(info.Value);

				return (T)Convert.ChangeType(guid, typeof(T));
			}

			return (T)Convert.ChangeType(info.Value, typeof(T));
		}
		
		public async Task<RepositoryResponse<bool>> RemoveSettingAsync(Setting setting)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("id", (int)setting);

			string sql = "delete Setting where ID = @id";

			var response = new RepositoryResponse<bool>(false, 0, false, "");
			using (var connection = ConnectionProvider.Connect())
			{
				await connection.ExecuteAsync(sql, dbArgs);
				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}
			return response;
		}

		public async Task<RepositoryResponse<bool>> UpsertSettingAsync(Setting setting, string value)
		{
			var userErrorMessages = new List<string>();

			var response = new RepositoryResponse<bool>(false, 0, false, "");

			if (userErrorMessages.Count > 0)
			{
				response.Message = string.Join("; ", userErrorMessages);
				response.StatusCode = 400;

				return response;
			}

			var sql = @"
if exists(select 1 from [Setting] where ID = @id) 
begin 
	update [Setting] set [Value] = @value where ID = @id 
end 
else 
begin 
	insert [Setting] values (@id, @value) 
end";

			using (var connection = (SqlConnection)ConnectionProvider.Connect())
			{
				await connection.ExecuteAsync(sql, new { id = (int)setting, value });
				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}

			return response;
		}
	}
}
