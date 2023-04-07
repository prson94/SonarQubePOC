using AngleSharp.Dom;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace d360.model.helpers.filters
{
	public class FilterExpressionTokenizer
	{
		private readonly Regex MatchOperators = new Regex(@"( ct | eq | in | nct | neq | nin | ne )");
		private readonly Regex MatchExpressionStart = new Regex(@"( and | or |\()");
		private readonly Regex MatchExpressionEnd = new Regex(@"( and | or |\))");

		private readonly List<TokenMatch> Tokens = new List<TokenMatch>();
		private readonly string FilterExpressionString;
		private string FilterExpressionStringParsed;
		private readonly Dictionary<string, string> escapeCharMap = new Dictionary<string, string>();
		public FilterExpressionTokenizer(string value)
		{
			escapeCharMap.Add("(", "#escaped_character_1");
			escapeCharMap.Add(")", "#escaped_character_2");

			this.FilterExpressionString = EscapeString(value);

			if (!IsValid(this.FilterExpressionString))
			{
				throw new Exception("Not valid filter expression");
			}

		}

		private string EscapeString(string str)
		{
			escapeCharMap.ForEach(x =>
			{
				str = str.Replace("\\" + x.Key, x.Value);
			});

			return str;
		}

		private string NormalizeString(string str)
		{
			escapeCharMap.ForEach(x =>
			{
				str = str.Replace(x.Value, x.Key);
			});

			return str;
		}

		public List<TokenMatch> GetTokens()
		{
			this.Tokenize(this.FilterExpressionString);
			return this.Tokens;
		}

		public string GetFilterExpressionStringParsed()
		{
			if (string.IsNullOrEmpty(this.FilterExpressionStringParsed))
			{
				throw new Exception("Make sure that filter expression is not empty and that GetTokens() has been executed");
			}
			return this.FilterExpressionStringParsed;
		}

		public void Tokenize(string str)
		{

			int lastVisitedIndex = 0;
			MatchCollection matches = MatchOperators.Matches(str);
			int matchIdx = 1;
			foreach (Match match in matches)
			{
				var operatorIndex = match.Index;

				var prevSubstring = str.Substring(lastVisitedIndex, operatorIndex - lastVisitedIndex);
				MatchCollection startMatches = MatchExpressionStart.Matches(prevSubstring);

				var startIdx = startMatches.Count > 0 ? startMatches.Cast<Match>().Max(x => x.Index + x.Value.Length) + lastVisitedIndex : lastVisitedIndex;

				var nextSubString = str.Substring(operatorIndex);
				MatchCollection endMatches = MatchExpressionEnd.Matches(nextSubString);
				var endIdx = endMatches.Count > 0 ? endMatches.Cast<Match>().Min(x => x.Index) + operatorIndex : str.Length;

				var length = endIdx - startIdx;

				lastVisitedIndex = endIdx;
				var tokenMatch = new TokenMatch
				{
					MatchIdx = matchIdx,
					TokenString = "##expressionMatch" + matchIdx,
					Match = str.Substring(startIdx, length).Trim()
				};

				this.Tokens.Add(tokenMatch);

				matchIdx++;
			}

			FilterExpressionStringParsed = str;
			this.Tokens.ForEach(token =>
			{
				FilterExpressionStringParsed = FilterExpressionStringParsed.Replace(token.Match, token.TokenString);
			});

			this.Tokens.ForEach((token) =>
			{
				token.Match = NormalizeString(token.Match);
				string[] splitExpression = Regex.Split(token.Match, MatchOperators.ToString()).Select(x => x.Trim()).ToArray();
				token.Token = new TokenizerObject
				{
					Field = splitExpression[0],
					Operator = splitExpression[1],
					Value = splitExpression[2]
				};
			} 
			);
		}

		public bool IsValid(string str)
		{
			int parenthesisCount = 0;
			foreach (char c in str)
			{
				if (c == '(') parenthesisCount++;
				if (c == ')') parenthesisCount--;
			}

			return parenthesisCount == 0;
		}
	}

	public class TokenMatch
	{
		public int MatchIdx { get; set; }
		public string TokenString { get; set; }
		public string Match { get; set; }

		public TokenizerObject Token { get; set; }
	}

	public class TokenizerObject
	{
		public string Field { get; set; }
		public string Operator { get; set; }
		public string Value { get; set; }
	}

}
