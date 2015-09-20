using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions
{
    public static class StorageProviderFolders
    { 
        public static string RESOURCE_IMAGE_CONTAINER = "d3s-resource-image";
    }
    public interface IStorageProvider
    {
        void CreateFolder(string name);
        void CreateFile(string folderName, string fileName, Stream file);
        void CreateFile(string folderName, string fileName, string content);
        void DeleteFile(string folderName, string fileName);
        Stream GetFile(string folderName, string fileName);
        byte[] GetFileAsBytes(string folderName, string fileName);
        string GetFileSecureUrl(string folderName, string fileName);

        bool ReleaseLockOnBlobFile(string folderName, string fileName);
    }
}
