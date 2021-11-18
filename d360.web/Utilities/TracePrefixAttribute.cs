using System;

namespace d360.web.Utilities
{
    public class TracePrefixAttribute : Attribute
    {
        public string Text { get; }

        public TracePrefixAttribute(string text)
        {
            Text = text;
        }
    }
}