using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;

namespace d360.core.search
{
    public static class Searcher
    {
        public static string Search(string searchText)
        {
            var grammar = new FactGrammar();
            Parser p = new Parser(grammar);
            var tree = p.Parse(searchText);
            string returnValue = grammar.RunSample(tree);
            return returnValue;
        }
    }
}
