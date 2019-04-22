using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System.IO;
using d360.core;
using Microsoft.Azure;

namespace d360.extensions.storage
{
    public class AzureStorageProvider : IStorageProvider
    {
        public string StorageName { get { return CloudConfigurationManager.GetSetting("AzureStorageName"); } }
        public string StorageKey { get { return CloudConfigurationManager.GetSetting("AzureStorageKey"); } }

        private StorageCredentials getCredentials()
        {
            return new StorageCredentials(StorageName, StorageKey);
        }
        
        CloudBlobContainer getContainer(string name)
        {
            var acct = new CloudStorageAccount(getCredentials(), StorageName, "core.windows.net", true);
            return acct.CreateCloudBlobClient().GetContainerReference(name);
        }

        public void CreateFolder(string name)
        {
            var c = getContainer(name);
            c.CreateIfNotExists();
        }

        public void CreateFile(string folderName, string fileName, Stream file, string contentType = null, bool cache = true)
        {
            var c = getContainer(folderName);
            CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
            if (!cache) blockBlob.Properties.CacheControl = "private, max-age=0, no-cache, no-store";
            if (!string.IsNullOrEmpty(contentType)) blockBlob.Properties.ContentType = contentType;
            blockBlob.UploadFromStream(file);
        }

        public void CreateFile(string folderName, string fileName, string content, string contentType = null, bool cache = true)
        {
            var c = getContainer(folderName);
            CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
            if (!cache) blockBlob.Properties.CacheControl = "private, max-age=0, no-cache, no-store";
            if (!string.IsNullOrEmpty(contentType)) blockBlob.Properties.ContentType = contentType;
            blockBlob.UploadText(content);
        }

        public void DeleteFile(string folderName, string fileName)
        {
            var c = getContainer(folderName);
            CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
            blockBlob.Delete();
        }


        public string GetFileContentsAsString(string folderName, string fileName)
        {
            return GetFileContentsAsString(folderName, fileName, Encoding.Default);            
        }

        public string GetFileContentsAsString(string folderName, string fileName, Encoding encoding)
        {
            string str = null;
            using (var stream = new MemoryStream())
            {
                var c = getContainer(folderName);
                CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
                if (blockBlob.Exists())
                {
                    str = blockBlob.DownloadText(encoding);
                }
            }
            return str;
        }

        public DateTime GetFileLastModifiedDate(string folderName, string fileName)
        {            
            using (var stream = new MemoryStream())
            {
                var c = getContainer(folderName);
                CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
                blockBlob.FetchAttributes();
                return blockBlob.Properties.LastModified.HasValue? blockBlob.Properties.LastModified.Value.UtcDateTime : DateTime.MinValue;
            }            
        }

        public List<string> ListFilenamesByPrefix(string folderName, string prefix)
        {
            var c = getContainer(folderName);
            return c.ListBlobs(prefix, true, BlobListingDetails.Metadata).Select(i => i.Uri.LocalPath.Replace(folderName, "").Replace("/", "")).ToList();
        }

        public List<StorageFileInfo> ListFiles(string folderName)
        {
            //container is the first part of the path
            var containerName = folderName.Substring(0, folderName.IndexOf('/'));
                        
            var c = getContainer(containerName);
            
            string blobPrefix = (containerName.Length < folderName.Length ? folderName.Substring(folderName.IndexOf('/'), folderName.Length - folderName.IndexOf('/')) : null);
            blobPrefix = blobPrefix.TrimStart('/');

            bool useFlatBlobListing = true;

            List<StorageFileInfo> files =
                c.ListBlobs(blobPrefix, useFlatBlobListing, BlobListingDetails.Metadata).
                Select(b => new StorageFileInfo
                {
                    LastModified = (((CloudBlockBlob)b).Properties.LastModified),//put actual time
                    Name = b.Uri.LocalPath.Remove(0, folderName.Length + 2).Replace('/', '\\')
                }).
                ToList();

            return files;
        }
    }
}
