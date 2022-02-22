using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System.IO;
using Newtonsoft.Json;
using Azure.Storage.Blobs.Models;
using System.Configuration;

namespace d360.extensions.storage
{
    public class AzureStorageProvider : IStorageProvider
    {
        public string StorageConnectionString { get { return ConfigurationManager.AppSettings["AzureStorageConnectionString"]; } }

        private BlobContainerClient GetContainer(string name)
        {
            var client = new BlobServiceClient(StorageConnectionString);
            return client.GetBlobContainerClient(name);
        }

        private BlobClient GetBlob(string folderName, string fileName)
        {
            var container = GetContainer(folderName);
            return container.GetBlobClient(fileName);
        }

        public Uri GetBaseUri(string folderName)
        {
            var container = GetContainer(folderName);
            var uri = container.Uri;
            return uri;
        }

        public async Task CreateFolder(string name)
        {
            var container = GetContainer(name);
            await container.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        public async Task CreateFile(string folderName, string fileName, Stream file, string contentType = null, bool cache = true)
        {
            var blob = GetBlob(folderName, fileName);
            var headers = new BlobHttpHeaders();

            if (!cache)
            {
                headers.CacheControl = "private, max-age=0, no-cache, no-store";
            }
            if (!string.IsNullOrEmpty(contentType))
            {
                headers.ContentType = contentType;
            }

            file.Position = 0;
            await blob.UploadAsync(file, headers).ConfigureAwait(false);
        }

        public async Task CreateFile(string folderName, string fileName, string content, string contentType = null, bool cache = true)
        {
            await CreateFile(folderName, fileName, new MemoryStream(Encoding.UTF8.GetBytes(content)), contentType, cache).ConfigureAwait(false);
        }


        public async Task SerializeJsonObjectToBlobAsync(string folderName, string fileName, object obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
                using (JsonTextWriter jtw = new JsonTextWriter(sw))
                {
                    JsonSerializer ser = new JsonSerializer();
                    ser.Serialize(jtw, obj);
                }

                await CreateFile(folderName, fileName, new MemoryStream(ms.ToArray())).ConfigureAwait(false);
            }
        }

        public async Task<T> DeserializeJsonObjectFromBlobAsync<T>(string folderName, string fileName)
        {
            var blob = GetBlob(folderName, fileName);

            using (MemoryStream ms = new MemoryStream())
            {
                await blob.DownloadToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms, Encoding.UTF8))
                {
                    using (JsonTextReader jtr = new JsonTextReader(sr))
                    {
                        JsonSerializer ser = new JsonSerializer();
                        return ser.Deserialize<T>(jtr);
                    }
                }
            }
        }

        public async Task DeleteFile(string folderName, string fileName)
        {
            var blob = GetBlob(folderName, fileName);
            await blob.DeleteIfExistsAsync().ConfigureAwait(false);
        }


        public string GetFileContentsAsString(string folderName, string fileName)
        {
            return GetFileContentsAsString(folderName, fileName, Encoding.Default).Result;
        }

        public async Task GetFileStream(string folderName, string fileName, Stream sr)
        {
            var blob = GetBlob(folderName, fileName);

            if (!(await blob.ExistsAsync().ConfigureAwait(false)))
            {
                throw new FileNotFoundException();
            }

            await blob.DownloadToAsync(sr).ConfigureAwait(false);
        }

        public async Task<string> GetFileContentsAsString(string folderName, string fileName, Encoding encoding)
        {
            string str = null;
            using (var stream = new MemoryStream())
            {
                var blob = GetBlob(folderName, fileName);

                if (await blob.ExistsAsync().ConfigureAwait(false))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (StreamReader sr = new StreamReader(ms, encoding))
                        {
                            await blob.DownloadToAsync(ms).ConfigureAwait(false);
                            ms.Position = 0;
                            str = await sr.ReadToEndAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            return str;
        }

        public async Task<DateTime> GetFileLastModifiedDate(string folderName, string fileName)
        {
            using (var stream = new MemoryStream())
            {
                var blob = GetBlob(folderName, fileName);
                var props = await blob.GetPropertiesAsync().ConfigureAwait(false);
                return props?.Value?.LastModified.UtcDateTime ?? DateTime.MinValue;
            }
        }

        public List<string> ListFilenamesByPrefix(string folderName, string prefix)
        {
            var fileNames = new List<string>();
            var c = GetContainer(folderName);

            foreach (BlobItem item in c.GetBlobs(BlobTraits.None, BlobStates.None, prefix))
            {
                fileNames.Add(item.Name);
            }

            return fileNames;
        }

        public List<StorageFileInfo> ListFiles(string folderName)
        {
            //container is the first part of the path
            var containerName = folderName.Substring(0, folderName.IndexOf('/'));
            var files = new List<StorageFileInfo>();
            var c = GetContainer(containerName);

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

            return files;
        }
    }
}
