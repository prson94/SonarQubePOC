using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Azure.Storage;
using Azure.Storage.Blobs;
using System.Configuration;
using System.IO;
using d360.core;
using Microsoft.Azure;
using Newtonsoft.Json;
using Azure.Storage.Blobs.Models;

namespace d360.extensions.storage
{
    public class AzureStorageProvider : IStorageProvider
    {
        public string StorageName { get { return CloudConfigurationManager.GetSetting("AzureStorageName"); } }
        public string StorageKey { get { return CloudConfigurationManager.GetSetting("AzureStorageKey"); } }
        public string StorageConnectionString { get { return CloudConfigurationManager.GetSetting("AzureStorageConnectionString"); } }


        //private StorageCredentials getCredentials()
        //{
        //    return new StorageCredentials(StorageName, StorageKey);
        //}
        
        BlobContainerClient getContainer(string name)
        {
            var client = new BlobServiceClient(StorageConnectionString);
            return client.GetBlobContainerClient(name);
        }

        BlobClient getBlob(string folderName, string fileName)
        {
            var container = getContainer(folderName);
            return container.GetBlobClient(fileName);
        }

        public async Task CreateFolder(string name)
        {
            var c = getContainer(name);
            await c.CreateIfNotExistsAsync();
        }

        public async Task CreateFile(string folderName, string fileName, Stream file, string contentType = null, bool cache = true)
        {
            var blob = getBlob(folderName, fileName);
            var headers = new BlobHttpHeaders();

            if (!cache)
            {
                headers.CacheControl = "private, max-age=0, no-cache, no-store";
            }
            if (!string.IsNullOrEmpty(contentType))
            {
                headers.ContentType = contentType;
            }

            await blob.UploadAsync(file, headers);
            //CloudBlockBlob blockBlob = c.GetBlo(fileName);
            //if (!cache) blockBlob.Properties.CacheControl = "private, max-age=0, no-cache, no-store";
            //if (!string.IsNullOrEmpty(contentType)) blockBlob.Properties.ContentType = contentType;
            //blockBlob.UploadFromStream(file);
        }

        public async Task CreateFile(string folderName, string fileName, string content, string contentType = null, bool cache = true)
        {

            await CreateFile(folderName, fileName, new MemoryStream(Encoding.UTF8.GetBytes(content)), contentType, cache);

            //var c = getContainer(folderName);
            //CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
            //if (!cache) blockBlob.Properties.CacheControl = "private, max-age=0, no-cache, no-store";
            //if (!string.IsNullOrEmpty(contentType)) blockBlob.Properties.ContentType = contentType;
            //blockBlob.UploadText(content);
        }

        
        public async Task SerializeJsonObjectToBlobAsync(string folderName, string fileName, object obj)
        {
            //var c = getContainer(folderName);
            //CloudBlockBlob blob = c.GetBlockBlobReference(fileName);
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms,Encoding.UTF8))
                {
                    using (JsonTextWriter jtw = new JsonTextWriter(sw))
                    {
                        JsonSerializer ser = new JsonSerializer();
                        ser.Serialize(jtw, obj);
                        await CreateFile(folderName, fileName, ms);
                    }
                }
            }

            //using (Stream stream = await blob.OpenWriteAsync())
            //using (StreamWriter sw = new StreamWriter(stream))
            //using (JsonTextWriter jtw = new JsonTextWriter(sw))
            //{
            //    JsonSerializer ser = new JsonSerializer();
            //    ser.Serialize(jtw, obj);
            //}
        }

        public async Task<T> DeserializeJsonObjectFromBlobAsync<T>(string folderName, string fileName)
        {
            //var c = getContainer(folderName);
            //CloudBlockBlob blob = c.GetBlockBlobReference(fileName);

            var blob = getBlob(folderName, fileName);

            using (MemoryStream ms = new MemoryStream())
            {
                await blob.DownloadToAsync(ms);
                using (StreamReader sr = new StreamReader(ms, Encoding.UTF8))
                {
                    using (JsonTextReader jtr = new JsonTextReader(sr))
                    {
                        JsonSerializer ser = new JsonSerializer();
                        return ser.Deserialize<T>(jtr);
                    }
                }
            }


            //using (Stream stream = await blob.OpenReadAsync())
            //using (StreamReader sr = new StreamReader(stream))
            //using (JsonTextReader jtr = new JsonTextReader(sr))
            //{
            //    JsonSerializer ser = new JsonSerializer();
            //    return ser.Deserialize<T>(jtr);
            //}
        }

        public async Task DeleteFile(string folderName, string fileName)
        {
            var blob = getBlob(folderName, fileName);
            await blob.DeleteIfExistsAsync();

            //CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
            //blockBlob.Delete();
        }


        public string GetFileContentsAsString(string folderName, string fileName)
        {
            return GetFileContentsAsString(folderName, fileName, Encoding.Default).Result;          
        }

        public async Task<string> GetFileContentsAsString(string folderName, string fileName, Encoding encoding)
        {
            string str = null;
            using (var stream = new MemoryStream())
            {
                var blob = getBlob(folderName, fileName);
                if (await blob.ExistsAsync())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (StreamReader sr = new StreamReader(ms, encoding))
                        {
                            await blob.DownloadToAsync(ms);
                            str = await sr.ReadToEndAsync();
                        }
                    }
                }

                //var c = getContainer(folderName);
                //CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
                //if (blockBlob.Exists())
                //{
                //    str = blockBlob.DownloadText(encoding);
                //}
            }
            return str;
        }

        public async Task<DateTime> GetFileLastModifiedDate(string folderName, string fileName)
        {            
            using (var stream = new MemoryStream())
            {
                var blob = getBlob(folderName, fileName);
                var props = await blob.GetPropertiesAsync();
                return props?.Value?.LastModified.UtcDateTime ?? DateTime.MinValue;
                //var c = getContainer(folderName);
                //CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
                //blockBlob.FetchAttributes();
                //return blockBlob.Properties.LastModified.HasValue? blockBlob.Properties.LastModified.Value.UtcDateTime : DateTime.MinValue;
            }            
        }

        public List<string> ListFilenamesByPrefix(string folderName, string prefix)
        {
            var fileNames = new List<string>();
            var c = getContainer(folderName);

            foreach(BlobItem item in c.GetBlobs(BlobTraits.None, BlobStates.None, prefix))
            {
                fileNames.Add(item.Name);
            }

            return fileNames;
            //return c.ListBlobs(prefix, true, BlobListingDetails.Metadata).Select(i => i.Uri.LocalPath.Replace(folderName, "").Replace("/", "")).ToList();
        }

        public List<StorageFileInfo> ListFiles(string folderName)
        {
            //container is the first part of the path
            var containerName = folderName.Substring(0, folderName.IndexOf('/'));
            var files = new List<StorageFileInfo>();            
            var c = getContainer(containerName);
            
            string blobPrefix = (containerName.Length < folderName.Length ? folderName.Substring(folderName.IndexOf('/'), folderName.Length - folderName.IndexOf('/')) : null);
            blobPrefix = blobPrefix.TrimStart('/');

            foreach (BlobItem item in c.GetBlobs(BlobTraits.None, BlobStates.None, blobPrefix))
            {
                files.Add(new StorageFileInfo
                {
                    LastModified = item.Properties.LastModified,
                    Name = item.Name
                });
            }

            //List<StorageFileInfo> files =
            //    c.ListBlobs(blobPrefix, useFlatBlobListing, BlobListingDetails.Metadata).
            //    Select(b => new StorageFileInfo
            //    {
            //        LastModified = (((CloudBlockBlob)b).Properties.LastModified),//put actual time
            //        Name = b.Uri.LocalPath.Remove(0, folderName.Length + 2).Replace('/', '\\')
            //    }).
            //    ToList();

            return files;
        }
    }
}
