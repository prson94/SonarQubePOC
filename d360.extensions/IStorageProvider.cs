using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions
{
    public static class StorageProviderFolders
    {
        public static string RESOURCE_IMAGE_CONTAINER = "d3s-resource-image";
    }

    public class StorageFileInfo
    {
        public string Name { get; set; }
        public DateTimeOffset? LastModified { get; set; }
    }

    public interface IStorageProvider
    {
        Task CreateFolder(string name);
        Task CreateFile(string folderName, string fileName, Stream file, string contentType = null, bool cache = true);
        Task CreateFile(string folderName, string fileName, string content, string contentType = null, bool cache = true);
        Task SerializeJsonObjectToBlobAsync(string folderName, string fileName, object obj);
        Task<T> DeserializeJsonObjectFromBlobAsync<T>(string folderName, string fileName);
        Task DeleteFile(string folderName, string fileName);
        string GetFileContentsAsString(string folderName, string fileName);
        Uri GetBaseUri(string folderName);
        Task<string> GetFileContentsAsString(string folderName, string fileName, Encoding encoding);
        Task GetFileStream(string folderName, string fileName, Stream sr);
        List<string> ListFilenamesByPrefix(string folderName, string prefix);
        List<StorageFileInfo> ListFiles(string folderName);
        Task<DateTime> GetFileLastModifiedDate(string folderName, string fileName);
    }
}
