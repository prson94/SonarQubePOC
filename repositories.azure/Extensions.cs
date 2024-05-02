using Dapper;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace repositories.azure
{
	internal class SortColumnOption
	{
		public SortColumnOption(string q, string db)
		{
			QueryStringPropertyName = q;
			DatabaseColumn = db;
		}

		public string QueryStringPropertyName { get; set; }
		public string DatabaseColumn { get; set; }
	}

	internal static class Extensions
	{
		public static bool CheckForIncludeTotal(this IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName = "_includetotal", bool defaultValue = true)
		{
			if (queryParams.ToList().Any(q => q.Key.ToLower() == parameterName))
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

		public static void CheckForQueryParameter<T>(this IEnumerable<KeyValuePair<string, string>> queryParams, 
			string filterPropertyName, string sqlColumn, string sqlParameterName,
			ref DynamicParameters dbArgs, ref List<string> queryFilters, T defaultValue = default)
		{
			var sqlparameternameArgs = sqlParameterName.Replace("@", "");
			if (queryParams.ToList().Any(q => q.Key.ToLower() == filterPropertyName))
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
						}
					}
					else if (typeof(T).Name == "int")
					{
						int parsedValue;
						if (int.TryParse(rawValue, out parsedValue))
						{
							dbArgs.Add(sqlparameternameArgs, parsedValue);
							queryFilters.Add($"{sqlColumn} = {sqlParameterName}");
						}
					}
					else 
					{
						dbArgs.Add(sqlparameternameArgs, rawValue);
						queryFilters.Add($"{sqlColumn} = {sqlParameterName}");
					}
				}
			}

			if (defaultValue is not null && !dbArgs.ParameterNames.Contains(sqlparameternameArgs))
			{
				dbArgs.Add(sqlparameternameArgs, defaultValue);
			}
		}

		public static int CheckForPageSize(this IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName = "_pagesize", int defaultPageSize = 250)
		{
			int value = defaultPageSize;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == parameterName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					int parsedValue;
					if (int.TryParse(rawValue, out parsedValue))
					{
						value = parsedValue;
					}
				}
			}

			return value;
		}

		public static int CheckForPageNumber(this IEnumerable<KeyValuePair<string, string>> queryParams, string parameterName = "_pagenum", int defaultPageNum = 1)
		{
			int value = defaultPageNum;

			if (queryParams.ToList().Any(q => q.Key.ToLower() == parameterName))
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
			if (queryParams.ToList().Any(q => q.Key.ToLower() == parameterName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					var option = options.SingleOrDefault(c => c.QueryStringPropertyName == rawValue.ToLowerInvariant());
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
			if (queryParams.ToList().Any(q => q.Key.ToLower() == parameterName))
			{
				var rawValue = (queryParams.ToList().FirstOrDefault(q => q.Key.ToLower() == parameterName).Value ?? "").Trim();
				if (!string.IsNullOrEmpty(rawValue))
				{
					defaultValue = rawValue.ToLowerInvariant() == "desc" ? "desc" : "asc";
				}
			}

			return defaultValue;
		}

		public static bool ValidateForQueryParameter<T>(this IEnumerable<KeyValuePair<string, string>> queryParams, string filterPropertyName, ref string parameterValue)
		{
			bool isValid = true;
			parameterValue = "";
			if (queryParams.ToList().Any(q => q.Key.ToLower() == filterPropertyName))
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
			bool isValid = true;
			if (queryParams.ToList().Any(q => q.Key.ToLower() == filterPropertyName))
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
