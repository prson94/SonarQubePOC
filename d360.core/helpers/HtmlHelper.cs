using HtmlAgilityPack;
using System.Collections.Generic;

namespace d360.core.helpers
{
    public static class HtmlHelper
    {
        public static string RemoveTags(string data)
        {
            if (data.Length < 6) return data;
            var document = new HtmlDocument();
            document.LoadHtml(data);

            // if selectnodes doesnt find any matches return an empty collection not null
            // https://github.com/zzzprojects/html-agility-pack/issues/23
            document.OptionEmptyCollection = true; 

            var nodes = new Queue<HtmlNode>(document.DocumentNode.SelectNodes("./*|./text()"));

            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                var parentNode = node.ParentNode;

                if (node.Name != "#text")
                {
                    var childNodes = node.SelectNodes("./*|./text()");

                    if (childNodes != null)
                    {
                        foreach (var child in childNodes)
                        {
                            nodes.Enqueue(child);
                            parentNode.InsertBefore(child, node);
                        }
                    }

                    parentNode.RemoveChild(node);
                }
            }

            return document.DocumentNode.InnerHtml;
        }
    }
}
