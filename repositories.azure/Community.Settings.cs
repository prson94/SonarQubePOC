using d360.core.enums;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Community: ICommunity
	{
		public async Task<Dictionary<string, string>> ReadSettingsAsDictionaryAsync(int companyId)
		{
			return (await ReadSettingsAsync(companyId)).ToDictionary(k => k.ID.ToString(), v => v.Value);
		}

		public async Task<SettingInfo> ReadSettingAsync(int companyId, Setting setting)
		{
			string sql = "select ID, Value from CompanySetting where CompanyId = @companyId and ID = @id";
			var model = setting.AsInfoModel();
			dynamic @override;
			using (var connection = (SqlConnection)Connect(true))
			{
				@override = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { companyId, id = (int)setting });
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

		public async Task<List<SettingInfo>> ReadSettingsAsync(int companyId)
		{
			// Get the list of settings from the D3S_###.dbo.Setting table.
			// Get the full list of settings from the Setting enum.
			// Return a list of SettingInfo, merging the values present from the environment into the SettingInfo.Value property.

			List<dynamic> overrides;
			string sql = "select ID, Value from CompanySetting where CompanyId = @companyId";
			using (var connection = (SqlConnection)Connect(true))
			{
				overrides = (await connection.QueryAsync<dynamic>(sql, new { companyId })).ToList();
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

		public async Task<T> ReadSettingValueAsync<T>(int companyId, Setting setting)
		{
			SettingInfo info = await ReadSettingAsync(companyId, setting);

			var checkType = default(T);

			if (checkType is Guid)
			{
				Guid guid = Guid.Parse(info.Value);

				return (T)Convert.ChangeType(guid, typeof(T));
			}

			return (T)Convert.ChangeType(info.Value, typeof(T));
		}

		public async Task<RepositoryResponse<bool>> RemoveSettingAsync(int companyId, Setting setting)
		{
			var dbArgs = new DynamicParameters();
			dbArgs.Add("id", (int)setting);
			dbArgs.Add("companyId", companyId);


			string sql = "delete CompanySetting where CompanyID = @companyId and ID = @id";

			var response = new RepositoryResponse<bool>(false, 0, false, "");
			using (var connection = Connect())
			{
				await connection.ExecuteAsync(sql, dbArgs);
				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}
			return response;
		}

		public async Task<RepositoryResponse<bool>> UpsertSettingAsync(int companyId, Setting setting, string value)
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
if exists(select 1 from [CompanySetting] where CompanyID = @companyId and ID = @id) 
begin 
	update [CompanySetting] set [Value] = @value where CompanyID = @companyId and ID = @id 
end 
else 
begin 
	insert [CompanySetting](CompanyID,ID,[Value]) values (@companyId, @id, @value) 
end";

			using (var connection = (SqlConnection)Connect())
			{
				await connection.ExecuteAsync(sql, new { companyId, id = (int)setting, value });
				response.IsSuccess = true;
				response.StatusCode = 200;
				response.Data = true;
			}

			return response;
		}
	}
}
