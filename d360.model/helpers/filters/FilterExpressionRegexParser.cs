using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace d360.model.helpers.filters
{
	public static class FilterExpressionRegexParser
	{
		private static readonly string filterExpressionRegex = @"([\w\s\-\/\:]+)\s(ct|eq|in|nct|neq|nin|ne)\s([\w\s\>\-\/\:\,\*\!\£\$\%\^\&\.\?\@\{\}\#\;\+\>\=\<\\\""\~\`\[\]\|]+)";
		public static MatchCollection ParseFullFilterExpression(string advancedFilterString)
		{
			return Regex.Matches(advancedFilterString, filterExpressionRegex);
		}
		public static Match ParseSingleFilterExpression(Match filterGrp)
		{
			return Regex.Match(filterGrp.Value, $"^{filterExpressionRegex}");
		}
	}
}
