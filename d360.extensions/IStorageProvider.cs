using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        void CreateFolder(string name);
        void CreateFile(string folderName, string fileName, Stream file, string contentType = null, bool cache = true);
        void CreateFile(string folderName, string fileName, string content, string contentType = null, bool cache = true);
        void DeleteFile(string folderName, string fileName);
        bool FileExists(string folderName, string fileName);
        Stream GetFile(string folderName, string fileName);
        byte[] GetFileAsBytes(string folderName, string fileName);
        string GetFileSecureUrl(string folderName, string fileName);

        string GetFileContentsAsString(string folderName, string fileName);
        string GetFileContentsAsString(string folderName, string fileName, Encoding encoding);

        List<string> ListFilenamesByPrefix(string folderName, string prefix);

        bool ReleaseLockOnBlobFile(string folderName, string fileName);

        List<StorageFileInfo> ListFiles(string folderName);

        DateTime GetFileLastModifiedDate(string folderName, string fileName);
    }
}
