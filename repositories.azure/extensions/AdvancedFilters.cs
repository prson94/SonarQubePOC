using Dapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace repositories.azure.extensions
{
	internal class FilterExpressionParserException : Exception
	{
		public HttpStatusCode StatusCode { get; }
		public FilterExpressionParserException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
			: base(message)
		{
			StatusCode = statusCode;
		}

		public FilterExpressionParserException(string message, Exception inner, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
			: base(message, inner)
		{
			StatusCode = statusCode;
		}
	}

	internal class TokenMatch
	{
		public int MatchIdx { get; set; }
		public string TokenString { get; set; }
		public string Match { get; set; }

		public TokenizerObject Token { get; set; }
	}

	internal class TokenizerObject
	{
		public string Field { get; set; }
		public string Operator { get; set; }
		public string Value { get; set; }
		public string Conjunction { get; set; }
	}

	internal enum SqlFieldType
	{
		Text,
		Boolean,
		Number,
		Decimal,
		Date,
		DateTime,
		Guid,
		AssetTypeClass,
		Xml
	}

	internal class FilterColumnOption
	{
		internal static Dictionary<SqlFieldType, string[]> FieldTypeOperators = new Dictionary<SqlFieldType, string[]>
		{
			{ SqlFieldType.Date, new string[] { "eq", "ne", "neq", "gt", "gte", "ge", "le", "lt", "lte" } },
			{ SqlFieldType.DateTime, new string[] { "eq", "ne", "neq", "gt", "gte", "ge", "le", "lt", "lte" } },
			{ SqlFieldType.Decimal, new string[] { "eq", "ne", "neq", "gt", "gte", "ge", "le", "lt", "lte" } },
			{ SqlFieldType.Guid, new string[] { "eq", "ne", "neq" } },
			{ SqlFieldType.Number, new string[] { "eq", "ne", "neq", "gt", "gte", "ge", "le", "lt", "lte" } },
			{ SqlFieldType.Text, new string[] { "ct", "nct", "eq", "ne", "neq", "sw", "ew" } }
		};

		public FilterColumnOption(string propertyName, string columnName, SqlFieldType type)
		{
			PropertyName = propertyName;
			DatabaseColumn = columnName;
			Type = type;
		}

		public string PropertyName { get; private set; }
		public string DatabaseColumn { get; private set; }
		public SqlFieldType Type { get; private set; }
	}

	internal static class AdvancedFilters
	{
		static readonly Regex ODataFilterRegex = new Regex(
		@"(\s+(?<conjunction>[Aa][Nn][Dd]|[Oo][Rr])\s+)*(?<field>\w+)\s+(?<operator>eq|neq|ne|gt|gte|ge|lt|le|lte|ct|nct|sw|ew|in|nin)\s+(?<value>(?:'[^']*'|[^\s']+))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

		internal static List<TokenMatch> ParseODataFilters(this IEnumerable<KeyValuePair<string, string>> queryParams, string filterParameterName = "_filter")
		{
			var filter = queryParams.ReadQueryParameterValue(filterParameterName);
			
			if (!string.IsNullOrEmpty(filter))
			{
				filter = filter.Trim();
				filter = filter.Replace("(", ""); // we are not supporting groups
				filter = filter.Replace(")", ""); // we are not supporting groups

				var matches = ODataFilterRegex.Matches(filter);
				var tokens = new List<TokenMatch>();

				foreach (Match match in matches)
				{
					if (match.Success)
					{
						tokens.Add(new TokenMatch
						{
							MatchIdx = tokens.Count + 1,
							TokenString = $"##expressionMatch{tokens.Count + 1}",
							Match = match.Value,
							Token = new TokenizerObject
							{
								Field = match.Groups["field"].Value,
								Operator = match.Groups["operator"].Value,
								Value = match.Groups["value"].Value.Trim('\''), // Remove single quotes around string values
								Conjunction = match.Groups["conjunction"].Value
							}
						});
					}
				}

				return tokens;
			}

			return [];
		}

		internal static string ConvertOperatorToSql(this string @operator)
		{
			switch (@operator.ToLower())
			{
				case "ct":
				case "ew":
				case "sw":
					return "LIKE";
				case "nct":
					return "NOT LIKE";
				case "eq":
					return "=";
				case "ne":
				case "neq":
					return "<>";
				case "gt":
					return ">";
				case "ge":
				case "gte":
					return ">=";
				case "lt":
					return "<";
				case "le":
				case "lte":
					return "<=";
				default:
					throw new FilterExpressionParserException($"Operator {@operator} is not supported for type.");
			}
		}

		internal static (DynamicParameters, List<string>) ConvertToSqlFilters(this List<TokenMatch> advancedFilters, List<FilterColumnOption> filterOptions)
		{
			var dbArgs = new DynamicParameters();
			var wheres = new List<string>();

			advancedFilters.ForEach(advancedFilter =>
			{
				if (filterOptions.Any(o => o.PropertyName.ToLower() == advancedFilter.Token.Field.ToLower()))
				{
					var filterOption = filterOptions.First(o => o.PropertyName.ToLower() == advancedFilter.Token.Field.ToLower());
					if (FilterColumnOption.FieldTypeOperators.ContainsKey(filterOption.Type))
					{
						string rawOperator = advancedFilter.Token.Operator.ToLower();

						if (FilterColumnOption.FieldTypeOperators[filterOption.Type].Contains(rawOperator))
						{
							string convertedOperator = advancedFilter.Token.Operator.ConvertOperatorToSql();

							switch (filterOption.Type)
							{
								case SqlFieldType.Decimal:
								case SqlFieldType.Number:
									if (double.TryParse(advancedFilter.Token.Value, out double numberValue))
									{
										dbArgs.Add($"@{filterOption.PropertyName}", numberValue);
										wheres.Add($"{filterOption.DatabaseColumn} {convertedOperator} @{filterOption.PropertyName}");
									}
									break;
								case SqlFieldType.Date:
								case SqlFieldType.DateTime:
									if (DateTime.TryParse(advancedFilter.Token.Value, out DateTime dateValue))
									{
										dbArgs.Add($"@{filterOption.PropertyName}", dateValue);
										wheres.Add($"{filterOption.DatabaseColumn} {convertedOperator} @{filterOption.PropertyName}");
									}
									break;
								case SqlFieldType.Guid:
									if (Guid.TryParse(advancedFilter.Token.Value, out Guid guidValue))
									{
										dbArgs.Add($"@{filterOption.PropertyName}", guidValue);
										wheres.Add($"{filterOption.DatabaseColumn} {convertedOperator} @{filterOption.PropertyName}");
									}
									break;
								case SqlFieldType.Text:
									string rawValue = advancedFilter.Token.Value;
									if (rawOperator.Equals("ew"))
									{
										rawValue = "%" + rawValue;
									}
									if (rawOperator.Equals("sw"))
									{
										rawValue = rawValue + "%";
									}

									dbArgs.Add($"@{filterOption.PropertyName}", rawValue);
									wheres.Add($"{filterOption.DatabaseColumn} {convertedOperator} @{filterOption.PropertyName}");
									break;
								default:
									//nothing - no filter
									break;
							}
						}
					}
				}
			});

			return (dbArgs, wheres);
		}
	}
}
