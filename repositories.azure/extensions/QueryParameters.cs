using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace repositories.azure.extensions
{
	internal class SortColumnOption
	{
		public SortColumnOption(string q, string db)
		{
			QueryStringPropertyName = q;
			DatabaseColumn = db;
		}

		public string QueryStringPropertyName { get; private set; }
		public string DatabaseColumn { get; private set; }
	}

	internal static class QueryParameters
	{
		public static bool CheckForIncludeTotal(this IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName = "_includetotal", bool defaultValue = true)
		{
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					bool parsedValue;
					if (bool.TryParse(rawValue, out parsedValue))
					{
						return parsedValue;
					}
				}
			}

			return defaultValue;
		}

		public static bool CheckForQueryParameter<T>(this IEnumerable<KeyValuePair<string, string>> queryParams, 
			string filterPropertyName, string sqlColumn, string sqlParameterName,
			ref DynamicParameters dbArgs, ref List<string> queryFilters, T defaultValue = default)
		{
			var isValidParameter = true;

			var sqlparameternameArgs = sqlParameterName.Replace("@", "");
			if (queryParams.IsQueryParameterPresent(filterPropertyName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == filterPropertyName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					if (typeof(T).Name == "Guid")
					{
						Guid parsedValue;
						if (Guid.TryParse(rawValue, out parsedValue))
						{
							dbArgs.Add(sqlparameternameArgs, parsedValue);
							queryFilters.Add($"{sqlColumn} = {sqlParameterName}");
							if (parsedValue == Guid.Empty)
							{
								isValidParameter = false;
							}
						}
						else
						{
							if (defaultValue is not null)
							{
								dbArgs.Add(sqlparameternameArgs, defaultValue);
							}
							isValidParameter = false;
						}
					}
					else if (typeof(T).Name == "int")
					{
						int parsedValue;
						if (int.TryParse(rawValue, out parsedValue))
						{
							dbArgs.Add(sqlparameternameArgs, parsedValue);
							queryFilters.Add($"{sqlColumn} = {sqlParameterName}");
							isValidParameter = true;
						}
						else
						{
							isValidParameter = false;
						}
					}
					else
					{
						dbArgs.Add(sqlparameternameArgs, rawValue);
						queryFilters.Add($"{sqlColumn} = {sqlParameterName}");
						isValidParameter = true;
					}
				}
			}
			else
			{
				// If no filter provided, then we are good here.
				isValidParameter = true;
			}

			if (defaultValue is not null && !dbArgs.ParameterNames.Contains(sqlparameternameArgs))
			{
				dbArgs.Add(sqlparameternameArgs, defaultValue);
				isValidParameter = true;
			}
      
			return isValidParameter;
		}

		public static int CheckForPageSize(this IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName = "_pagesize", int defaultPageSize = 250)
		{
			var value = defaultPageSize;

			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					int parsedValue;
					if (int.TryParse(rawValue, out parsedValue))
					{
						value = parsedValue;
						if (value <= 0)
						{
							value = 1;
						}
					}
				}
			}

			return value;
		}

		/// <summary>
		/// Stores an @offset parameter into the database arguments object.
		/// </summary>
		public static void LoadOffsetDatabaseParameter(this DynamicParameters dbArgs, int pageNumber, int pageSize)
		{
			dbArgs.Add("@offset", (pageNumber - 1) * pageSize);
		}

		/// <summary>
		/// Stores an @size parameter into the database arguments object.
		/// </summary>
		public static void LoadPageSizeDatabaseParameter(this DynamicParameters dbArgs, int pageSize)
		{
			dbArgs.Add("@size", pageSize);
		}

		public static int CheckForPageNumber(this IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName = "_pagenum", int defaultPageNum = 1)
		{
			var value = defaultPageNum;

			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					int parsedValue;
					if (int.TryParse(rawValue, out parsedValue))
					{
						value = parsedValue;
						if (value < 1)
						{
							value = 1;
						}
					}
				}
			}

			return value;
		}

		public static string CheckForSortColumn(this IEnumerable<KeyValuePair<string, string>> queryParams, List<SortColumnOption> options, string defaultColumn, string parameterName = "_order")
		{
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					var option = options.SingleOrDefault(c => c.QueryStringPropertyName.ToLowerInvariant() == rawValue.ToLowerInvariant());
					if (option != null)
					{
						defaultColumn = option.DatabaseColumn;
					}
				}
			}

			return defaultColumn;
		}

		public static string CheckForSortDirection(this IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName = "_direction", string defaultValue = "asc")
		{
			if (queryParams.IsQueryParameterPresent(parameterName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					defaultValue = rawValue.ToLowerInvariant() == "desc" ? "desc" : "asc";
				}
			}

			return defaultValue;
		}

		public static bool IsQueryParameterPresent(this IEnumerable<KeyValuePair<string, string>> queryParams, string name)
		{
			return queryParams.ToList().Any(x => x.Key.ToLower() == name.ToLower());
		}

		public static string ReadQueryParameterValue(this IEnumerable<KeyValuePair<string, string>> queryParams, string name)
		{
			var value = queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == name.ToLower()).Value;
			if (!string.IsNullOrEmpty(value))
			{
				value = value.Trim();
			}
			return value;
		}

		public static bool ValidateForQueryParameter<T>(this IEnumerable<KeyValuePair<string, string>> queryParams, string filterPropertyName, ref string parameterValue)
		{
			var isValid = true;
			parameterValue = "";
			if (queryParams.IsQueryParameterPresent(filterPropertyName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == filterPropertyName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					parameterValue = rawValue;
					if (typeof(T).Name == "Guid")
					{
						Guid parsedValue;
						if (Guid.TryParse(rawValue, out parsedValue))
						{
							if (parsedValue == null || parsedValue == Guid.Empty)
							{
								isValid = false;
							}
						}
						else
						{
							isValid = false;
						}

					}
					else
					{
						if (filterPropertyName == "_direction")
						{
							string[] allowedDirections = ["asc","desc"];

							if (!allowedDirections.Contains(rawValue.Trim().ToLower()))
							{
								isValid = false;
							}
						}
					}
				}
			}
			return isValid;
		}

		public static bool ValidateForQueryParameterFromList(this IEnumerable<KeyValuePair<string, string>> queryParams, string filterPropertyName, List<string> ValidFieldList, ref string parameterValue)
		{
			var isValid = true;
			if (queryParams.IsQueryParameterPresent(filterPropertyName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == filterPropertyName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					parameterValue = rawValue;
					if (!ValidFieldList.Contains(rawValue.ToLowerInvariant()))
					{
						isValid = false;
					}
				}
			}
			return isValid;
		}
	}
}
