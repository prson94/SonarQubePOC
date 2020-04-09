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


            var nodes = new Queue<HtmlNode>(document.DocumentNode.SelectNodes("./*|./text()"));

            if (nodes == null) return data;

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
