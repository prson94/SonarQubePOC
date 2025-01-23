using d360.core;
using d360.core.entities;
using d360.core.enums;
using d360.core.resources;
using Dapper;
using Newtonsoft.Json.Linq;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace repositories.azure
{
	public partial class Catalog
	{
		public async Task<bool> DeleteConnectorLabels(List<ConnectorLabelApiDeleteModel> model)
		{
			try
			{
				IEnumerable<Guid> labelUids = model.Select(m => m.uid);

				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var parameters = new
					{
						State = (int)State.Deleted,
						Uids = labelUids
					};

					await connection.ExecuteAsync(
						"UPDATE dbo.ConnectorLabel SET State = @State WHERE uid IN @Uids",
						parameters);
				}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<ConnectorLabelApiModelWrapper> GetLabels(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			try
			{
				ConnectorLabelApiModelWrapper results = new ConnectorLabelApiModelWrapper();
				int pageSize = 250;
				int pageNum = 0;

				bool disablePaging = false;

				var dbArgs = new DynamicParameters();

				List<string> queryFilters = new List<string>();

				dbArgs.Add("@state", State.Active);
				queryFilters.Add($"t.[state] = @state");

				if (queryParams.ToList().Any(q => string.Equals(q.Key, "uid", StringComparison.OrdinalIgnoreCase)))
				{
					Guid uid = new Guid();

					var tagUidString = queryParams.ToList().FirstOrDefault(q => string.Equals(q.Key, "uid", StringComparison.OrdinalIgnoreCase)).Value;
					if (Guid.TryParse(tagUidString, out uid))
					{
						dbArgs.Add("@uid", uid);
						queryFilters.Add($"t.[UID] = @uid");
					}
				}

				if (queryParams.ToList().Any(q => string.Equals(q.Key, "_pagesize", StringComparison.OrdinalIgnoreCase)))
				{
					if (int.TryParse(queryParams.ToList().Find(q => string.Equals(q.Key, "_pagesize", StringComparison.OrdinalIgnoreCase)).Value, out pageSize))
					{
						if (pageSize < 1)
						{
							pageSize = 1;
						}
					}

					if (pageSize > 250)
					{
						pageSize = 250; // max page size is 250 people.
					}
				}

				if (queryParams.ToList().Any(q => string.Equals(q.Key, "_pagenum", StringComparison.OrdinalIgnoreCase)))
				{
					if (int.TryParse(queryParams.ToList().FirstOrDefault(q => string.Equals(q.Key, "_pagenum", StringComparison.OrdinalIgnoreCase)).Value, out pageNum))
					{
						if (pageNum < 1)
						{
							pageNum = 1;
						}
					}
				}

				string whereClause = $"WHERE t.State = 1";
				if (queryFilters.Count > 0)
				{
					whereClause += $" and ({string.Join(" AND ", queryFilters)})";
				}

				var sql = $@"drop table if exists #labelUidMap
						create table #labelUidMap(
							uid uniqueidentifier
						)

						insert into #labelUidMap
						select LabelUid from ProcessExpandedData
						where LabelUid is not null

						select
						Labels.count as UseCount,
						t.uid,
						t.Value,
						t.CreatedOn,
						created.uid as CreatedByUid,
						adv_created.DisplayValue as CreatedByName,
						t.CreatedOn,
						updated.uid as UpdatedByUid,
						adv_updated.DisplayValue as UpdatedByName,
						t.UpdatedOn
						from ConnectorLabel t
						  left join asset created on created.Object = 'Resource' and created.ObjectID = t.CreatedBy
						  left join AssetDisplayValue adv_created on adv_created.AssetID = created.ID
						  left join asset updated on updated.Object = 'Resource' and updated.ObjectID = t.CreatedBy
						  left join AssetDisplayValue adv_updated on adv_updated.AssetID = updated.ID
						  cross apply (select count(*) from #labelUidMap where uid = t.uid)Labels (count)
						{whereClause}";

				var countSql = @"select count(*)
							from ConnectorLabel";

				sql += " order by t.[ID] ASC"; // admin screen will most likely order results however it sees fit

				if (pageSize < 1)
				{
					pageSize = 1;
				}

				if (pageNum < 1)
				{
					pageNum = 1;
				}

				if (!disablePaging)
				{
					sql += $" offset {pageSize * (pageNum - 1)} rows fetch next {pageSize} rows only";
				}

				results.pageNum = pageNum;
				results.pageSize = pageSize;
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					int labelsCount = await connection.ExecuteScalarAsync<int>(countSql);
					results.total = labelsCount;

					if (results.total > 0)
					{
						results.items = (await connection.QueryAsync<ConnectorLabelApiModel>(sql, dbArgs, commandTimeout: CommandTimeout));
					}
				}
				return results;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<ConnectorLabelApiModel> CreateConnectorLabel(ConnectorLabelPostModel model)
		{
			try
			{
				var result = new ConnectorLabelApiModel
				{
					Value = model.Value
				};

				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var parameters = new
					{
						State = (int)State.Deleted,
						Uid = Guid.NewGuid(),
						Value = model.Value.Trim()
					};

					var label = await connection.QueryFirstOrDefaultAsync<ConnectorLabel>("select c.uid, c.Value, c.State from dbo.ConnectorLabel c where c.Value = @Value and c.State = @State", parameters, commandTimeout: CommandTimeout);

					if (label == null)
					{
						label = new ConnectorLabel { Value = model.Value };
						await connection.ExecuteAsync("insert into dbo.ConnectorLabel (uid, Value) values (@Uid, @Value)", parameters, commandTimeout: CommandTimeout);
						label.uid = parameters.Uid;
					}
					else
					{
						label.State = State.Active;
						label.CreatedBy = label.UpdatedBy = CurrentUserId;
						label.CreatedOn = label.UpdatedOn = DateTime.UtcNow;
					}

					var user = await GetUser(CurrentUserId);

					result.uid = label.uid;
					result.UpdatedOn = label.UpdatedOn.GetValueOrDefault();
					result.UpdatedByUid = user.Uid;
					result.CreatedOn = label.CreatedOn.GetValueOrDefault();
					result.CreatedByUid = user.Uid;

					return result;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<ConnectorLabelApiModel> UpdateConnectorLabel(Guid uid, ConnectorLabelPostModel model, ConnectorLabel existingLabel)
		{
			var result = new ConnectorLabelApiModel();
			try
			{
				var parameters = new
				{
					Uid = existingLabel.uid,
					Value = model.Value.Trim(),
					UpdatedOn = existingLabel.UpdatedOn.GetValueOrDefault(),
					CreatedOn = existingLabel.CreatedOn.GetValueOrDefault(),
				};
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					await connection.ExecuteAsync(
					@"UPDATE dbo.ConnectorLabel
					  SET Value = @Value,
					  UpdatedOn = @UpdatedOn,
					  CreatedOn = @CreatedOn
					  where uid = @Uid",
					parameters,
					commandTimeout: CommandTimeout);

					result.Value = model.Value;
					result.uid = existingLabel.uid;
					result.UpdatedOn = existingLabel.UpdatedOn.GetValueOrDefault();
					result.CreatedOn = existingLabel.CreatedOn.GetValueOrDefault();
					int count = await connection.ExecuteScalarAsync<int>("select count(*) from ProcessExpandedData where LabelUid = @Uid", parameters, commandTimeout: CommandTimeout);
					result.UseCount = count;
				}

				var createUser = await GetUser(existingLabel.CreatedBy);
				if (createUser != null)
				{
					result.CreatedByUid = createUser.Uid;
				}
				var updateUser = await GetUser(CurrentUserId);
				if (updateUser != null)
				{
					result.UpdatedByUid = updateUser.Uid;
				}

				return result;
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<bool> DoesLabelExists(Guid uid)
		{
			try
			{
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var parameters = new { Uid = uid };
					var count = await connection.ExecuteScalarAsync<int>(
						$"SELECT TOP 1 1 FROM dbo.ConnectorLabel WHERE uid = @Uid", parameters, commandTimeout: CommandTimeout);
					return count > 0;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<bool> DoesLabelExists(string value)
		{
			try
			{
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var parameters = new
					{
						Value = value,
						State = (int)State.Active
					};
					var count = await connection.ExecuteScalarAsync<int>(
						$"SELECT TOP 1 1 FROM dbo.ConnectorLabel WHERE Value = @Value AND State = @State", parameters, commandTimeout: CommandTimeout);
					return count > 0;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<bool> DoesLabelExists(Guid existingUid, ConnectorLabelPostModel model)
		{
			try
			{
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var parameters = new
					{
						Value = model.Value.Trim(),
						Uid = existingUid,
						State = (int)State.Active
					};
					var count = await connection.ExecuteScalarAsync<int>(
						$"SELECT TOP 1 1 FROM dbo.ConnectorLabel WHERE Value = @Value AND uid != @Uid AND State = @State", parameters, commandTimeout: CommandTimeout);
					return count > 0;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public Task<dynamic> GetConnectorLabelsForExcel(IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			throw new NotImplementedException();
		}

		public async Task<(byte[], string)> GetExcelFromConnectorLabelUsage(ConnectorLabel label, IEnumerable<dynamic> response)
		{
			return await Task.Run(() => GetExcelFromConnectionLabelUsageAsync(label, response));

			(byte[], string) GetExcelFromConnectionLabelUsageAsync(ConnectorLabel label, IEnumerable<dynamic> response)
			{
				var fileName = $"Where Used report for Connector Label '{label.Value}'";
				var fields = new List<FieldType>
			{
				new FieldType { Type = "string", Name = "Diagram", FriendlyName = "Diagram" },
				new FieldType { Type = "string", Name = "AssetTypeName", FriendlyName = "Asset Type" },
				new FieldType { Type = "string", Name = "Occurrences", FriendlyName = "Occurrences" },
				new FieldType { Type = "string", Name = "AssetUid", FriendlyName = "UID" },
				new FieldType { Type = "string", Name = "AssetId", FriendlyName = "Asset ID" },
				new FieldType { Type = "string", Name = "url", FriendlyName = "URL" }
			};

				var document = new SLDocument();
				const string sheetName = "Where Used";

				#region Populate Excel Document

				document.RenameWorksheet(SLDocument.DefaultFirstSheetName, sheetName);
				int index = 1;

				foreach (var field in fields)
				{
					document.SetCellValue(1, index++, field.FriendlyName.GetSafeXLSColumnValue());
				}

				int rowNumber = 1;

				foreach (var row in response)
				{
					index = 1;
					rowNumber++;
					var rowValues = (row as IDictionary<string, object>);
					foreach (var field in fields)
					{
						if (rowValues.ContainsKey(field.Name))
						{
							var val = rowValues[field.Name];
							setCellValueFromField(document, rowNumber, index, field, val);
						}
						index++;
					}
				}

				#endregion Populate Excel Document

				var stream = new MemoryStream();
				document.SaveAs(stream);
				return (stream.ToArray(), fileName);
			}
		}

		public async Task<bool> IsAuthorizedToEditConnectorLabel(Guid connectorLabelUid)
		{
			try
			{
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var parameters = new
					{
						ConnectorLabelUid = connectorLabelUid
					};
					var connectorLabel = await connection.QueryFirstOrDefaultAsync<ConnectorLabel>(
						$"SELECT * from dbo.ConnectorLabel c WHERE c.uid = @ConnectorLabelUid", parameters, commandTimeout: CommandTimeout);

					if (connectorLabel == null)
					{
						return false;
					}

					var user = await GetUser(CurrentUserId);

					return user.IsAdministrator || CurrentUserId == connectorLabel.CreatedBy;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<IEnumerable<dynamic>> GetConnectorLabelUsage(Guid labelUid, IEnumerable<KeyValuePair<string, string>> queryParams)
		{
			try
			{
				var dbArgs = new DynamicParameters();
				List<string> whereClauses = new List<string>();
				string sortField = "";
				string sortOrder = "";
				string whereOperater = " and ";
				int useCount = 0;
				dbArgs.Add("labelUid", labelUid);

				foreach (var qitem in queryParams.Where(x => !string.IsNullOrEmpty(x.Value)))
				{
					switch (qitem.Key.ToLower())
					{
						case "globalsearch":
							dbArgs.Add("global", $"%{qitem.Value.ToLower()}%");
							whereClauses.Add("an.DisplayPath like @global");
							whereClauses.Add("Type.AssetTypeName like @global");
							whereClauses.Add("STR(Count) like @global");

							whereOperater = " or ";

							break;

						case "diagram":
							if (!string.IsNullOrEmpty(qitem.Value))
							{
								dbArgs.Add("diagram", $"%{qitem.Value.ToLower()}%");
								whereClauses.Add("an.DisplayPath like @diagram");
							}

							break;

						case "occurrences":
							if (int.TryParse(qitem.Value, out useCount))
							{
								dbArgs.Add("occurrences", $"%{qitem.Value.ToLower()}%");
								whereClauses.Add("STR(Count) like @occurrences");
							}

							break;

						case "assettypename":
							if (!string.IsNullOrEmpty(qitem.Value))
							{
								dbArgs.Add("assettypename", $"%{qitem.Value.ToLower()}%");
								whereClauses.Add("Type.AssetTypeName like @assettypename");
							}
							break;

						case "sortby":
							if (string.Equals(qitem.Value, "diagram", StringComparison.OrdinalIgnoreCase))
							{
								sortField = "an.DisplayPath";
							}

							if (string.Equals(qitem.Value, "assettypename", StringComparison.OrdinalIgnoreCase))
							{
								sortField = "Type.AssetTypeName";
							}

							if (string.Equals(qitem.Value, "occurrences", StringComparison.OrdinalIgnoreCase))
							{
								sortField = "count";
							}

							break;

						case "sortorder":
							int val = int.Parse(qitem.Value);
							if (val >= 0)
							{
								sortOrder = "ASC";
							}
							else
							{
								sortOrder = "DESC";
							}

							break;
					}
				}

				string sortClause = !string.IsNullOrEmpty(sortField) ? $"ORDER BY {sortField} {sortOrder}" : "";

				string whereClause = $"";
				if (whereClauses.Count > 0)
				{
					whereClause += $"where ({string.Join(whereOperater, whereClauses)})";
				}

				var labelsSql = $@";with usage as(
								select DiagramAssetUid, count(*) as count from dbo.processexpandeddata ped
								where ped.labeluid = @labelUid
								group by ped.DiagramAssetUid)
								select	an.DisplayPath as Diagram,
										Count as Occurrences,
										u.diagramassetuid as AssetUid,
										a.ID as AssetId,
										'asset/' + lower(cast(a.uid as nvarchar(36))) as url,
										Type.AssetTypeName,
										a.Object,
										a.ObjectID
								from	usage u
										inner join asset a on a.uid = u.diagramassetuid
										inner join AssetPath an on an.ID = a.ID
										inner join assettype ast on a.assettypeid = ast.id
								cross apply (
									select
										CASE AST.[Class]
											WHEN 2 THEN '{Label.AssetTypeClass_Model.CleanForSql()}' + ' > ' +  AST.Name
											WHEN 1 THEN '{Label.AssetTypeClass_Business.CleanForSql()}'+  ' > ' + AST.Name
											WHEN 8 THEN '{Label.AssetTypeClass_Technical.CleanForSql()}'+  ' > ' + AST.Name
											WHEN 6 THEN '{Label.AssetTypeClass_Policy.CleanForSql()}'+  ' > ' + AST.Name
											WHEN 7 THEN '{Label.AssetTypeClass_Rule.CleanForSql()}'+  ' > ' + AST.Name
											ELSE '' + AST.Name
									   END AS AssetTypeName)Type
								{whereClause}
								{sortClause}";

				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var response = await connection.QueryAsync<dynamic>(labelsSql, dbArgs, commandTimeout: CommandTimeout);
					return response;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<dynamic> GetLabels(string q = null, bool isExact = false, bool getUseCount = false, Guid? exceptUid = null)
		{
			try
			{
				string labelsSql = string.Empty;

				if (isExact)
				{
					labelsSql = $@"SELECT top 10 uid, Value
                                {(getUseCount ? ", Labels.cnt as UseCount" : "")}
                                  FROM [dbo].[ConnectorLabel] cl
                                {(getUseCount ? "cross apply (select count(*) from ProcessExpandedData where LabelUid = cl.uid)Labels(cnt)" : "")}
                                where Value = @q and state = 1
                                {(exceptUid.HasValue ? " and cl.uid <> @exceptUid" : "")}
                                order by Value";
				}
				else
				{
					if (!string.IsNullOrEmpty(q))
					{
						q = $"%{q}%";
					}

					labelsSql = $@"SELECT top 10 uid, Value
                                    {(getUseCount ? ", Labels.cnt as UseCount" : "")}
                                  FROM [dbo].[ConnectorLabel] cl
                                {(getUseCount ? "cross apply (select count(*) from ProcessExpandedData where LabelUid = cl.uid)Labels(cnt)" : "")}
                                where state = 1
                                {(!string.IsNullOrEmpty(q) ? " and Value like @q" : "")}
                                {(exceptUid.HasValue ? " and cl.uid <> @exceptUid" : "")}
                                order by Value";
				}
				var parameters = new
				{
					q
				};

				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var result = await connection.QueryAsync<dynamic>(labelsSql, parameters, commandTimeout: CommandTimeout);
					return result;
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<ConnectorLabel> GetLabel(Guid parentGuid)
		{
			try
			{
				using (var connection = (SqlConnection)ConnectionProvider.Connect())
				{
					var parameters = new
					{
						Uid = parentGuid
					};

					return await connection.QueryFirstOrDefaultAsync<ConnectorLabel>(
						$"Select * from dbo.ConnectorLabel c where c.uid = @Uid", parameters, commandTimeout: CommandTimeout);
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		protected void setCellValueFromField(SLDocument document, int rowIndex, int colIndex, FieldType field, object value)
		{
			var valueString = value?.ToString() ?? "";
			switch ((field.Type ?? "").ToUpper())
			{
				case "DECIMAL":
					double dVal = 0;
					if (double.TryParse(valueString, out dVal))
					{
						document.SetCellValue(rowIndex, colIndex, dVal);
					}
					else
					{
						document.SetCellValue(rowIndex, colIndex, valueString);
					}
					break;

				case "NUMBER":
					int intVal = 0;
					if (int.TryParse(valueString, out intVal))
					{
						document.SetCellValue(rowIndex, colIndex, intVal);
					}
					else
					{
						document.SetCellValue(rowIndex, colIndex, valueString);
					}
					break;

				case "DATE":
					if (DateTime.TryParse(valueString, out DateTime dateVal))
					{
						document.SetCellValue(rowIndex, colIndex, dateVal);

						SLStyle style = document.CreateStyle();
						style.FormatCode = "m/d/yyyy";
						document.SetCellStyle(rowIndex, colIndex, style);
					}
					break;

				case "HTML":
					var txt = (value as string).ReplaceHtmlEntities().GetSafeXLSColumnValue();
					if (txt.StartsWith("="))
					{
						txt = "'" + txt;
					}
					document.SetCellValue(rowIndex, colIndex, txt);
					break;

				case "OWNERSHIPLOOKUP":
					if (value != null)
					{
						string owners = "";
						if (value.GetType() == typeof(JArray))
						{
							var ownerships = ((JArray)value).ToObject<List<dynamic>>();
							owners = string.Join(" | ", ownerships.OrderBy(o => o.ResourceName).Select(o => $"{o.ResourceName} ({o.ResponsibilityTypes})"));
						}
						document.SetCellValue(rowIndex, colIndex, owners.GetSafeXLSColumnValue());
					}
					break;

				case "PATH":
					if (value != null)
					{
						document.SetCellValue(rowIndex, colIndex, WebUtility.HtmlDecode(valueString).GetSafeXLSColumnValue());
					}
					break;

				default:
					if (valueString.StartsWith("="))
					{
						valueString = "'" + valueString;
					}
					document.SetCellValue(rowIndex, colIndex, valueString.GetSafeXLSColumnValue());
					break;
			}
		}
	}
}