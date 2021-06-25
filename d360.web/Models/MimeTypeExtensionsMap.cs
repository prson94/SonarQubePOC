using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace d360.web.Models
{
    public class MimeTypeExtensionsMap
    {
        static System.Text.RegularExpressions.Regex fileRegex = new System.Text.RegularExpressions.Regex(@"data:(?<mime>[\w/\-\.]+);(?<encoding>\w+),(?<data>.*)", System.Text.RegularExpressions.RegexOptions.Compiled);

        public static System.Text.RegularExpressions.Regex RegEx { get { return fileRegex; } }


        internal class MimeTypeExtensionMapItem
        {
            public string MimeType { get; set; }
            public string Extension { get; set; }
        }

        private static List<MimeTypeExtensionMapItem> items = new List<MimeTypeExtensionMapItem> {
            new MimeTypeExtensionMapItem { Extension = ".gif", MimeType = "image/gif"},
            new MimeTypeExtensionMapItem { Extension = ".ico", MimeType = "image/vnd.microsoft.icon"},
            new MimeTypeExtensionMapItem { Extension = ".ico", MimeType = "image/x-icon"},
            new MimeTypeExtensionMapItem { Extension = ".jpg", MimeType = "image/jpeg"},
            new MimeTypeExtensionMapItem { Extension = ".png", MimeType = "image/png"},
            new MimeTypeExtensionMapItem { Extension = ".xlam", MimeType = "application/vnd.ms-excel.addin.macroEnabled.12"},
            new MimeTypeExtensionMapItem { Extension = ".xls", MimeType = "application/vnd.ms-excel"},
            new MimeTypeExtensionMapItem { Extension = ".xlsb", MimeType = "application/vnd.ms-excel.sheet.binary.macroEnabled.12"},
            new MimeTypeExtensionMapItem { Extension = ".xlsm", MimeType = "application/vnd.ms-excel.sheet.macroEnabled.12"},
            new MimeTypeExtensionMapItem { Extension = ".xlsx", MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
            new MimeTypeExtensionMapItem { Extension = ".xltm", MimeType = "application/vnd.ms-excel.template.macroEnabled.12"},
            new MimeTypeExtensionMapItem { Extension = ".xltx", MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.template"},
        };

        public static string GetExtension(string mimeType)
        {
            var item = items.SingleOrDefault(i => i.MimeType.ToLower().Equals(mimeType.Trim().ToLower()));
            return (item == null) ? null : item.Extension;
        }
    }
}