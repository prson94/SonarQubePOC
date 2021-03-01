using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Data;
using d360.core.resources;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using d360.core.entities;
using Newtonsoft.Json;

namespace d360.core
{
    public static class JsonExtensions
    {
        public static int GetNumberOfDecimalPlaces(this decimal d)
        {
            int count = BitConverter.GetBytes(decimal.GetBits(d)[3])[2];
            return count;
        }
        public static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this string o)
        {
            var token = JToken.Parse(o);
            return token.ParseJsonIntoJsonPropertiesCollection();
        }

        public static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this JToken o)
        {
            List<FieldJsonProperty> properties = new List<FieldJsonProperty>();

            if (o is JArray)
            {
                properties = (o as JArray).ParseJsonIntoJsonPropertiesCollection();
            }
            else if (o is JObject)
            {
                properties = (o as JObject).ParseJsonIntoJsonPropertiesCollection(0);
            }

            return properties;
        }

        private static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this JArray o)
        {
            List<FieldJsonProperty> properties = new List<FieldJsonProperty>();

            int pos = 0;
            foreach (JToken c in o)
            {
                properties.AddRange(
                    (c as JObject).ParseJsonIntoJsonPropertiesCollection(pos)
                    );
                pos++;
            }

            return properties;
        }

        private static List<FieldJsonProperty> ParseJsonIntoJsonPropertiesCollection(this JObject o, int position = 0)
        {
            List<FieldJsonProperty> properties = new List<FieldJsonProperty>();

            foreach (JProperty p in o.Properties())
            {
                if (p.Value is JArray)
                {
                    properties.Add(new FieldJsonProperty { IsArray = true, Name = p.Name, Path = p.Path, Position = position });

                    int pos = 0;
                    foreach (JToken c in p.Value)
                    {
                        if (c is JObject)
                        {
                            properties.AddRange(
                                (c as JObject).ParseJsonIntoJsonPropertiesCollection(pos)
                                );
                        }
                        pos++;
                    }
                }
                else if (p.Value is JObject)
                {
                    properties.Add(new FieldJsonProperty { IsArray = false, Name = p.Name, Path = p.Path, Position = position });
                    properties.AddRange(
                        (p.Value as JObject).ParseJsonIntoJsonPropertiesCollection(position)
                        );
                }
                else
                {
                    properties.Add(new FieldJsonProperty
                    {
                        IsArray = false,
                        Name = p.Name,                        
                        Path = p.Path,
                        Position = position,
                        Value = p.Value.ToString()
                    });
                }
            }
        
            return properties;
        }
    }

    public static class StringExtensions
    {
        public static bool In<T>(this T t, params T[] values)
        {
            return values.Contains(t);
        }

        public static string FormatBooleanReadOnlyValue(this bool b)
        {
            return b ? Values.BooleanTrue : Values.BooleanFalse;
        }

        /// <summary>
        /// Parses a string made up of one or more field tokens.
        /// </summary>
        /// <param name="tokenizedString">The string, for example: {FIELD_NAME}.{FIELD_NAME} - {FIELD_NAME}</param>
        /// <returns></returns>
        public static List<string> ParseTokens(this string tokenizedString)
        {
            var list = new List<string>();
            var r = new Regex(@"\{[\d\w]*\}", RegexOptions.Singleline);
            foreach (Match m in r.Matches(tokenizedString))
            {
                list.Add(m.Value);
            }

            return list;
        }

        public static string CleanForSql(this string text)
        {
            try
            {
                text = Regex.Replace(text, "'", "''");
            }
            catch
            { }

            return text;
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static string ReplaceLast(this string Source, string Find, string Replace)
        {
            int place = Source.LastIndexOf(Find);

            if (place == -1)
                return Source;

            return Source.Remove(place, Find.Length).Insert(place, Replace);
        }


        public static byte[] GetSha1Hash(this string inputString)
        {
            HashAlgorithm algorithm = SHA1.Create();  //or use SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetSha1HashString(this string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetSha1Hash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public static byte[] GetD3sHash(this string inputString)
        {
            HashAlgorithm algorithm = SHA256.Create();  //or use SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetD3sHashString(this string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetD3sHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
        /// <summary>
        /// Returns truncated string to 31 characters to accomodate xlsx sheet name limit
        /// </summary>
        public static string GetSafeSheetName(this string inputString)
        {
            if (inputString == null) return "";
            if (inputString.Length < 31)
                return inputString;

            return inputString.Substring(0, 28) + "...";
        }

    }

    public static class XMLExtensions
    {
        /// <summary>
        /// Replaces field token with their actual values from system lookups.
        /// </summary>
        /// <param name="xml">The XML that contains all the field names and values.</param>
        public static string ReplaceTokenWithValues(this string tokenFormatString, Dictionary<string, string> fields)
        {
            List<string> tokens = tokenFormatString.ParseTokens();
            // Format the text based on the tokens.
            foreach (string token in tokens)
            {
                var n = token.Substring(1, token.Length - 2);   // Name of the element to find in the XML.
                if (fields.ContainsKey(n))
                    tokenFormatString = tokenFormatString.Replace(token, fields[n]);
            }

            return tokenFormatString;
        }

        public static XElement StripNamespaces(this XElement root)
        {
            var attributes = root.Attributes();
            attributes.Where(i => i.Name == "xmlns").Remove();
            return new XElement(
                root.Name.LocalName,
                attributes.Where(i => i.Name != "xmlns"),
                root.HasElements ?
                    root.Elements().Select(el => StripNamespaces(el)) :
                    (object)root.Value
            );
        }

        public static string GetSafeFilename(this string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return string.Empty;

            //restricted characters check
            var fn = string.Join("_", filename.Split(Path.GetInvalidFileNameChars()));

            // max filename check
            if (fn.Length > 250)
                fn = fn.Substring(0, 250);

            return fn;
        }

        public static XElement ToXElement(this string xml)
        {
            return XElement.Parse(xml);
        }
    }

    public static class GeneralExtensions
    {
        public static string GetFullExceptionData(this Exception ex, bool includeStacktrace = true)
        {
            if (ex.InnerException != null && ex.InnerException.InnerException != null && ex.InnerException.InnerException.GetType() == typeof(SqlException))
            {
                SqlException sqlException = (SqlException)ex.InnerException.InnerException;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                foreach (SqlError sqlError in sqlException.Errors)
                {
                    if (sb.Length > 0) sb.Append(" ");
                    sb.Append(sqlError.Message);
                }

                return sb.ToString();
            }

            string error = "";

            if (!ex.Message.Contains("inner exception for details")) error += ex.Message;

            var iex = ex.InnerException;
            while (iex != null)
            {
                error += $";  {iex.Message}{(includeStacktrace ? "-----" + iex.StackTrace : "")}";
                iex = iex.InnerException;
            }

            return error;
        }

        public static string AsJson<T>(this T item)
        {
            var json = JsonConvert.SerializeObject(item);
            return json;
        }

        public static T CloneThis<T>(this T item)
        {
            var json = JsonConvert.SerializeObject(item);
            T newItem = JsonConvert.DeserializeObject<T>(json);
            return newItem;
        }
    }

    public static class FullTextSearchExtensions
    {
        #region SQL Full-text Search

        /// <summary>
        /// Query term forms.
        /// </summary>
        protected enum TermForms
        {
            Inflectional,
            Thesaurus,
            Literal,
        }

        /// <summary>
        /// Term conjunction types.
        /// </summary>
        protected enum ConjunctionTypes
        {
            And,
            Or,
            Near,
        }

        /// <summary>
        /// Common interface for expression nodes
        /// </summary>
        protected interface INode
        {
            /// <summary>
            /// Indicates this term (or both child terms) should be excluded from
            /// the results
            /// </summary>
            bool Exclude { get; set; }

            /// <summary>
            /// Indicates this term is enclosed in parentheses
            /// </summary>
            bool Grouped { get; set; }
        }

        /// <summary>
        /// Terminal (leaf) expression node class.
        /// </summary>
        private class TerminalNode : INode
        {
            // Interface members
            public bool Exclude { get; set; }
            public bool Grouped { get; set; }

            // Class members
            public string Term { get; set; }
            public TermForms TermForm { get; set; }

            // Convert node to string
            public override string ToString()
            {
                string fmt = String.Empty;
                if (TermForm == TermForms.Inflectional)
                    fmt = "{0}FORMSOF(INFLECTIONAL, {1})";
                else if (TermForm == TermForms.Thesaurus)
                    fmt = "{0}FORMSOF(THESAURUS, {1})";
                else if (TermForm == TermForms.Literal)
                    fmt = "{0}\"{1}\"";
                return String.Format(fmt,
                    Exclude ? "NOT " : String.Empty,
                    Term);
            }
        }

        /// <summary>
        /// Internal (non-leaf) expression node class
        /// </summary>
        private class InternalNode : INode
        {
            // Interface members
            public bool Exclude { get; set; }
            public bool Grouped { get; set; }

            // Class members
            public INode Child1 { get; set; }
            public INode Child2 { get; set; }
            public ConjunctionTypes Conjunction { get; set; }

            // Convert node to string
            public override string ToString()
            {
                return String.Format(Grouped ? "({0} {1} {2})" : "{0} {1} {2}",
                    Child1.ToString(),
                    Conjunction.ToString().ToUpper(),
                    Child2.ToString());
            }
        }

        /// <summary>
        /// blackbeltcoder.com/articles/strings/a-text-parsing-helper-class
        /// www.blackbeltcoder.com/Articles/data/easy-full-text-search-queries
        /// </summary>
        internal class TextParser
        {
            private string _text;
            private int _pos;

            public string Text { get { return _text; } }
            public int Position { get { return _pos; } }
            public int Remaining { get { return _text.Length - _pos; } }
            public static char NullChar = (char)0;

            public TextParser()
            {
                Reset(null);
            }

            public TextParser(string text)
            {
                Reset(text);
            }

            /// <summary>
            /// Resets the current position to the start of the current document
            /// </summary>
            public void Reset()
            {
                _pos = 0;
            }

            /// <summary>
            /// Sets the current document and resets the current position to the start of it
            /// </summary>
            /// <param name="html"></param>
            public void Reset(string text)
            {
                _text = (text != null) ? text : String.Empty;
                _pos = 0;
            }

            /// <summary>
            /// Indicates if the current position is at the end of the current document
            /// </summary>
            public bool EndOfText
            {
                get { return (_pos >= _text.Length); }
            }

            /// <summary>
            /// Returns the character at the current position, or a null character if we're
            /// at the end of the document
            /// </summary>
            /// <returns>The character at the current position</returns>
            public char Peek()
            {
                return Peek(0);
            }

            /// <summary>
            /// Returns the character at the specified number of characters beyond the current
            /// position, or a null character if the specified position is at the end of the
            /// document
            /// </summary>
            /// <param name="ahead">The number of characters beyond the current position</param>
            /// <returns>The character at the specified position</returns>
            public char Peek(int ahead)
            {
                int pos = (_pos + ahead);
                if (pos < _text.Length)
                    return _text[pos];
                return NullChar;
            }

            /// <summary>
            /// Extracts a substring from the specified position to the end of the text
            /// </summary>
            /// <param name="start"></param>
            /// <returns></returns>
            public string Extract(int start)
            {
                return Extract(start, _text.Length);
            }

            /// <summary>
            /// Extracts a substring from the specified range of the current text
            /// </summary>
            /// <param name="start"></param>
            /// <param name="end"></param>
            /// <returns></returns>
            public string Extract(int start, int end)
            {
                return _text.Substring(start, end - start);
            }

            /// <summary>
            /// Moves the current position ahead one character
            /// </summary>
            public void MoveAhead()
            {
                MoveAhead(1);
            }

            /// <summary>
            /// Moves the current position ahead the specified number of characters
            /// </summary>
            /// <param name="ahead">The number of characters to move ahead</param>
            public void MoveAhead(int ahead)
            {
                _pos = Math.Min(_pos + ahead, _text.Length);
            }

            /// <summary>
            /// Moves to the next occurrence of the specified string
            /// </summary>
            /// <param name="s">String to find</param>
            /// <param name="ignoreCase">Indicates if case-insensitive comparisons
            /// are used</param>
            public void MoveTo(string s, bool ignoreCase = false)
            {
                _pos = _text.IndexOf(s, _pos, ignoreCase ?
                    StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                if (_pos < 0)
                    _pos = _text.Length;
            }

            /// <summary>
            /// Moves to the next occurrence of the specified character
            /// </summary>
            /// <param name="c">Character to find</param>
            public void MoveTo(char c)
            {
                _pos = _text.IndexOf(c, _pos);
                if (_pos < 0)
                    _pos = _text.Length;
            }

            /// <summary>
            /// Moves to the next occurrence of any one of the specified
            /// characters
            /// </summary>
            /// <param name="chars">Array of characters to find</param>
            public void MoveTo(char[] chars)
            {
                _pos = _text.IndexOfAny(chars, _pos);
                if (_pos < 0)
                    _pos = _text.Length;
            }

            /// <summary>
            /// Moves to the next occurrence of any character that is not one
            /// of the specified characters
            /// </summary>
            /// <param name="chars">Array of characters to move past</param>
            public void MovePast(char[] chars)
            {
                while (IsInArray(Peek(), chars))
                    MoveAhead();
            }

            /// <summary>
            /// Determines if the specified character exists in the specified
            /// character array.
            /// </summary>
            /// <param name="c">Character to find</param>
            /// <param name="chars">Character array to search</param>
            /// <returns></returns>
            protected bool IsInArray(char c, char[] chars)
            {
                foreach (char ch in chars)
                {
                    if (c == ch)
                        return true;
                }
                return false;
            }

            /// <summary>
            /// Moves the current position to the first character that is part of a newline
            /// </summary>
            public void MoveToEndOfLine()
            {
                char c = Peek();
                while (c != '\r' && c != '\n' && !EndOfText)
                {
                    MoveAhead();
                    c = Peek();
                }
            }

            /// <summary>
            /// Moves the current position to the next character that is not whitespace
            /// </summary>
            public void MovePastWhitespace()
            {
                while (Char.IsWhiteSpace(Peek()))
                    MoveAhead();
            }
        }

        public class EasyFts
        {
            // Characters not allowed in unquoted search terms
            protected const string Punctuation = "~\"`!@#$%^&*()-+=[]{}\\|;:,.<>?/";

            /// <summary>
            /// Collection of stop words. These words will not
            /// be included in the resulting query unless quoted.
            /// </summary>
            public HashSet<string> StopWords { get; set; }

            // Class constructor
            public EasyFts()
            {
                StopWords = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            }

            /// <summary>
            /// Converts an "easy" search term to a full-text search term.
            /// </summary>
            /// <param name="query">Search term to convert</param>
            /// <returns>A valid full-text search query</returns>
            public string ToFtsQuery(string query)
            {
                INode node = FixUpExpressionTree(ParseNode(query, ConjunctionTypes.And), true);
                return (node != null) ? node.ToString() : String.Empty;
            }

            /// <summary>
            /// Parses a query segment and converts it to an expression
            /// tree.
            /// </summary>
            /// <param name="query">Query segment to convert</param>
            /// <param name="defaultConjunction">Implicit conjunction type</param>
            /// <returns>Root node of expression tree</returns>
            private INode ParseNode(string query, ConjunctionTypes defaultConjunction)
            {
                TermForms termForm = TermForms.Inflectional;
                bool termExclude = false;
                ConjunctionTypes conjunction = defaultConjunction;
                bool resetState = true;
                INode root = null;
                INode node;
                string term;

                TextParser parser = new TextParser(query);
                while (!parser.EndOfText)
                {
                    if (resetState)
                    {
                        // Reset modifiers
                        termForm = TermForms.Inflectional;
                        termExclude = false;
                        conjunction = defaultConjunction;
                        resetState = false;
                    }

                    parser.MovePastWhitespace();
                    if (!parser.EndOfText &&
                        !Punctuation.Contains(parser.Peek()))
                    {
                        // Extract query term
                        int start = parser.Position;
                        parser.MoveAhead();
                        while (!parser.EndOfText &&
                            !Punctuation.Contains(parser.Peek()) &&
                            !Char.IsWhiteSpace(parser.Peek()))
                            parser.MoveAhead();

                        // Allow trailing wildcard
                        if (parser.Peek() == '*')
                        {
                            parser.MoveAhead();
                            termForm = TermForms.Literal;
                        }

                        // Interpret token
                        term = parser.Extract(start, parser.Position);
                        if (String.Compare(term, "AND", true) == 0)
                            conjunction = ConjunctionTypes.And;
                        else if (String.Compare(term, "OR", true) == 0)
                            conjunction = ConjunctionTypes.Or;
                        else if (String.Compare(term, "NEAR", true) == 0)
                            conjunction = ConjunctionTypes.Near;
                        else if (String.Compare(term, "NOT", true) == 0)
                            termExclude = true;
                        else
                        {
                            root = AddNode(root, term, termForm, termExclude, conjunction);
                            resetState = true;
                        }
                        continue;
                    }
                    else if (parser.Peek() == '"')
                    {
                        // Match next term exactly
                        termForm = TermForms.Literal;
                        // Extract quoted term
                        term = ExtractQuote(parser);
                        root = AddNode(root, term.Trim(), termForm, termExclude, conjunction);
                        resetState = true;
                    }
                    else if (parser.Peek() == '(')
                    {
                        // Parse parentheses block
                        term = ExtractBlock(parser, '(', ')');
                        node = ParseNode(term, defaultConjunction);
                        root = AddNode(root, node, conjunction, true);
                        resetState = true;
                    }
                    else if (parser.Peek() == '<')
                    {
                        // Parse angle brackets block
                        term = ExtractBlock(parser, '<', '>');
                        node = ParseNode(term, ConjunctionTypes.Near);
                        root = AddNode(root, node, conjunction);
                        resetState = true;
                    }
                    else if (parser.Peek() == '-')
                    {
                        // Match when next term is not present
                        termExclude = true;
                    }
                    else if (parser.Peek() == '+')
                    {
                        // Match next term exactly
                        termForm = TermForms.Literal;
                    }
                    else if (parser.Peek() == '~')
                    {
                        // Match synonyms of next term
                        termForm = TermForms.Thesaurus;
                    }
                    // Advance to next character
                    parser.MoveAhead();
                }
                return root;
            }

            /// <summary>
            /// Fixes any portions of the expression tree that would produce an invalid SQL Server full-text
            /// query.
            /// </summary>
            /// <remarks>
            /// While our expression tree may be properly constructed, it may represent a query that
            /// is not supported by SQL Server. This method traverses the expression tree and corrects
            /// problem expressions as described below.
            /// 
            ///     NOT term1 AND term2         Subexpressions swapped.
            ///     NOT term1                   Expression discarded.
            ///     NOT term1 AND NOT term2     Expression discarded if node is grouped (parenthesized)
            ///                                 or is the root node; otherwise, the parent node may
            ///                                 contain another subexpression that will make this one
            ///                                 valid.
            ///     term1 OR NOT term2          Expression discarded.
            ///     term1 NEAR NOT term2        NEAR conjunction changed to AND.*
            ///
            /// * This method converts all NEAR conjunctions to AND when either subexpression is not
            /// an InternalNode with the form TermForms.Literal.
            /// </remarks>
            /// <param name="node">Node to fix up</param>
            /// <param name="isRoot">True if node is the tree's root node</param>
            INode FixUpExpressionTree(INode node, bool isRoot = false)
            {
                // Test for empty expression tree
                if (node == null) return null;

                // Special handling for internal nodes
                if (node is InternalNode)
                {
                    // Fix up child nodes
                    var internalNode = node as InternalNode;
                    internalNode.Child1 = FixUpExpressionTree(internalNode.Child1);
                    internalNode.Child2 = FixUpExpressionTree(internalNode.Child2);

                    // Correct subexpressions incompatible with conjunction type
                    if (internalNode.Conjunction == ConjunctionTypes.Near)
                    {
                        // If either subexpression is incompatible with NEAR conjunction then change to AND
                        if (IsInvalidWithNear(internalNode.Child1) || IsInvalidWithNear(internalNode.Child2))
                            internalNode.Conjunction = ConjunctionTypes.And;
                    }
                    else if (internalNode.Conjunction == ConjunctionTypes.Or)
                    {
                        // Eliminate subexpressions not valid with OR conjunction
                        if (IsInvalidWithOr(internalNode.Child1))
                            internalNode.Child1 = null;
                        if (IsInvalidWithOr(internalNode.Child2))
                            internalNode.Child1 = null;
                    }

                    // Handle eliminated child expressions
                    if (internalNode.Child1 == null && internalNode.Child2 == null)
                    {
                        // Eliminate parent node if both child nodes were eliminated
                        return null;
                    }
                    else if (internalNode.Child1 == null)
                    {
                        // Child1 eliminated so return only Child2
                        node = internalNode.Child2;
                    }
                    else if (internalNode.Child2 == null)
                    {
                        // Child2 eliminated so return only Child1
                        node = internalNode.Child1;
                    }
                    else
                    {
                        // Determine if entire expression is an exclude expression
                        internalNode.Exclude = (internalNode.Child1.Exclude && internalNode.Child2.Exclude);
                        // If only first child expression is an exclude expression,
                        // then simply swap child expressions
                        if (!internalNode.Exclude && internalNode.Child1.Exclude)
                        {
                            var temp = internalNode.Child1;
                            internalNode.Child1 = internalNode.Child2;
                            internalNode.Child2 = temp;
                        }
                    }
                }
                // Eliminate expression group if it contains only exclude expressions
                return ((node.Grouped || isRoot) && node.Exclude) ? null : node;
            }

            /// <summary>
            /// Determines if the specified node is invalid on either side of a NEAR conjuction.
            /// </summary>
            /// <param name="node">Node to test</param>
            bool IsInvalidWithNear(INode node)
            {
                // NEAR is only valid with TerminalNodes with form TermForms.Literal
                return !(node is TerminalNode) || ((TerminalNode)node).TermForm != TermForms.Literal;
            }

            /// <summary>
            /// Determines if the specified node is invalid on either side of an OR conjunction.
            /// </summary>
            /// <param name="node">Node to test</param>
            bool IsInvalidWithOr(INode node)
            {
                // OR is only valid with non-null, non-excluded (NOT) subexpressions
                return node == null || node.Exclude == true;
            }

            /// <summary>
            /// Creates an expression node and adds it to the
            /// give tree.
            /// </summary>
            /// <param name="root">Root node of expression tree</param>
            /// <param name="term">Term for this node</param>
            /// <param name="termForm">Indicates form of this term</param>
            /// <param name="termExclude">Indicates if this is an excluded term</param>
            /// <param name="conjunction">Conjunction used to join with other nodes</param>
            /// <returns>The new root node</returns>
            INode AddNode(INode root, string term, TermForms termForm, bool termExclude, ConjunctionTypes conjunction)
            {
                if (term.Length > 0 && !IsStopWord(term))
                {
                    INode node = new TerminalNode
                    {
                        Term = term,
                        TermForm = termForm,
                        Exclude = termExclude
                    };
                    root = AddNode(root, node, conjunction);
                }
                return root;
            }

            /// <summary>
            /// Adds an expression node to the given tree.
            /// </summary>
            /// <param name="root">Root node of expression tree</param>
            /// <param name="node">Node to add</param>
            /// <param name="conjunction">Conjunction used to join with other nodes</param>
            /// <returns>The new root node</returns>
            INode AddNode(INode root, INode node, ConjunctionTypes conjunction, bool group = false)
            {
                if (node != null)
                {
                    node.Grouped = group;
                    if (root != null)
                        root = new InternalNode
                        {
                            Child1 = root,
                            Child2 = node,
                            Conjunction = conjunction
                        };
                    else
                        root = node;
                }
                return root;
            }

            /// <summary>
            /// Extracts a block of text delimited by the specified open and close
            /// characters. It is assumed the parser is positioned at an
            /// occurrence of the open character. The open and closing characters
            /// are not included in the returned string. On return, the parser is
            /// positioned at the closing character or at the end of the text if
            /// the closing character was not found.
            /// </summary>
            /// <param name="parser">TextParser object</param>
            /// <param name="openChar">Start-of-block delimiter</param>
            /// <param name="closeChar">End-of-block delimiter</param>
            /// <returns>The extracted text</returns>
            private string ExtractBlock(TextParser parser, char openChar, char closeChar)
            {
                // Track delimiter depth
                int depth = 1;

                // Extract characters between delimiters
                parser.MoveAhead();
                int start = parser.Position;
                while (!parser.EndOfText)
                {
                    if (parser.Peek() == openChar)
                    {
                        // Increase block depth
                        depth++;
                    }
                    else if (parser.Peek() == closeChar)
                    {
                        // Decrease block depth
                        depth--;
                        // Test for end of block
                        if (depth == 0)
                            break;
                    }
                    else if (parser.Peek() == '"')
                    {
                        // Don't count delimiters within quoted text
                        ExtractQuote(parser);
                    }
                    // Move to next character
                    parser.MoveAhead();
                }
                return parser.Extract(start, parser.Position);
            }

            /// <summary>
            /// Extracts a block of text delimited by double quotes. It is
            /// assumed the parser is positioned at the first quote. The
            /// quotes are not included in the returned string. On return,
            /// the parser is positioned at the closing quote or at the end of
            /// the text if the closing quote was not found.
            /// </summary>
            /// <param name="parser">TextParser object</param>
            /// <returns>The extracted text</returns>
            private string ExtractQuote(TextParser parser)
            {
                // Extract contents of quote
                parser.MoveAhead();
                int start = parser.Position;
                while (!parser.EndOfText && parser.Peek() != '"')
                    parser.MoveAhead();
                return parser.Extract(start, parser.Position);
            }

            /// <summary>
            /// Determines if the given word has been identified as
            /// a stop word.
            /// </summary>
            /// <param name="word">Word to check</param>
            protected bool IsStopWord(string word)
            {
                return StopWords.Contains(word);
            }
        }

        #endregion

        /// <summary>
        /// Converts an "easy" search term to a full-text search term.
        /// </summary>
        /// <param name="query">Search term to convert</param>
        /// <returns>A valid full-text search query</returns>
        public static string ToSqlFullTextSearchPhrase(this string query)
        {
            var model = new EasyFts();
            return model.ToFtsQuery(query);
        }
    }
}
