using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace d360.core
{
    public class MimeTypes 
    {
        public static readonly string GIF = "image/gif";
        public static readonly string JPEG = "image/jpeg";
        public static readonly string ICON = "image/x-icon";
        public static readonly string PNG = "image/png";
        public static readonly string VISIO = "image/vnd.microsoft.icon";
    }
    public class MimeTypeExtensionsMap
    {
        static System.Text.RegularExpressions.Regex fileRegex = new System.Text.RegularExpressions.Regex(@"data:(?<mime>[\w/\-\.]+);(?<encoding>\w+),(?<data>.*)", System.Text.RegularExpressions.RegexOptions.Compiled);

        public static System.Text.RegularExpressions.Regex RegEx { get { return fileRegex; } }

        public static readonly Dictionary<string, byte[]> FileHeaders = new Dictionary<string, byte[]>
        {
            { MimeTypes.JPEG, new byte[]{ 0xFF, 0xD8 }},
            { MimeTypes.GIF, new byte[]{ 0x47, 0x49, 0x46 }},
            { MimeTypes.PNG, new byte[]{ 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }},
            { MimeTypes.ICON, new byte[]{ 0x00, 0x00, 0x01, 0x00 }},
            { MimeTypes.VISIO, new byte[]{ 0x00, 0x00, 0x01, 0x00 }},
        };

        public static Dictionary<string, byte[]> GetEmptyFileHeaderDictionary()
        {
            return new Dictionary<string, byte[]>();
        }

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

        public static string GetMimeType(string extension)
        {
            var item = items.SingleOrDefault(i => i.Extension.ToLower().Equals(extension.Trim().ToLower()));
            return (item == null) ? null : item.MimeType;
        }
    }

    public static class DataUrlExtensions
    {
        public static bool IsValidFileData(this string data, Dictionary<string, byte[]> headersToCheck)
        {
            if (string.IsNullOrEmpty(data))
            {
                return true;
            }
            var match = MimeTypeExtensionsMap.RegEx.Match(data);
            var imgMime = match.Groups["mime"].Value;

            if (headersToCheck.ContainsKey(imgMime))
            {
                var imgByteArray = Convert.FromBase64String(match.Groups["data"].Value);

                if (imgByteArray.Length >= headersToCheck[imgMime].Length)
                {
                    var slice = imgByteArray.Take(headersToCheck[imgMime].Length);
                    if (slice.SequenceEqual(headersToCheck[imgMime]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsValidImageData(this string data)
        {
            var headers = MimeTypeExtensionsMap.GetEmptyFileHeaderDictionary();
            headers.Add(MimeTypes.GIF, MimeTypeExtensionsMap.FileHeaders[MimeTypes.GIF]);
            headers.Add(MimeTypes.ICON, MimeTypeExtensionsMap.FileHeaders[MimeTypes.ICON]);
            headers.Add(MimeTypes.JPEG, MimeTypeExtensionsMap.FileHeaders[MimeTypes.JPEG]);
            headers.Add(MimeTypes.PNG, MimeTypeExtensionsMap.FileHeaders[MimeTypes.PNG]);
            headers.Add(MimeTypes.VISIO, MimeTypeExtensionsMap.FileHeaders[MimeTypes.VISIO]);

            return data.IsValidFileData(headers);
        }

        public static bool IsValidThemeImageData(this string data)
        {
            var headers = MimeTypeExtensionsMap.GetEmptyFileHeaderDictionary();
            headers.Add(MimeTypes.GIF, MimeTypeExtensionsMap.FileHeaders[MimeTypes.GIF]);
            headers.Add(MimeTypes.JPEG, MimeTypeExtensionsMap.FileHeaders[MimeTypes.JPEG]);
            headers.Add(MimeTypes.PNG, MimeTypeExtensionsMap.FileHeaders[MimeTypes.PNG]);

            return data.IsValidFileData(headers);
        }

        public static bool IsValidThemeIconData(this string data)
        {
            var headers = MimeTypeExtensionsMap.GetEmptyFileHeaderDictionary();
            headers.Add(MimeTypes.ICON, MimeTypeExtensionsMap.FileHeaders[MimeTypes.ICON]);
            headers.Add(MimeTypes.PNG, MimeTypeExtensionsMap.FileHeaders[MimeTypes.PNG]);

            return data.IsValidFileData(headers);
        }

        public static (string, MemoryStream) GetFileFromDataUrl(this string data)
        {
            var match = MimeTypeExtensionsMap.RegEx.Match(data);
            var imgMime = match.Groups["mime"].Value;
            var imgData = match.Groups["data"].Value;
            var imgExtension = MimeTypeExtensionsMap.GetExtension(imgMime);
            var imgByteArray = Convert.FromBase64String(imgData);
            return (imgExtension, new MemoryStream(imgByteArray));
        }

        public static string GetDataUrlFromStream(this byte[] data, string extension)
        {
            var b64String = Convert.ToBase64String(data);
            var contentType = MimeTypeExtensionsMap.GetMimeType(extension);
            var dataUrl = $"data:{contentType};base64," + b64String;

            return dataUrl;
        }
    }
}
