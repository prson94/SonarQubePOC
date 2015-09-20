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

namespace d360.extensions.storage.azure
{
    public class StorageProvider : IStorageProvider
    {
        string RESOURCE_IMAGE_CONTAINER = "d3s-resource-image";

        private StorageCredentials getCredentials()
        {
            var acctName = ConfigurationManager.AppSettings["AzureStorageAccountName"];
            var keyValue = ConfigurationManager.AppSettings["AzureStorageKeyValue"];
            return new StorageCredentials(acctName, keyValue);
        }
        
        CloudBlobContainer getContainer(string name)
        {
            var acct = new CloudStorageAccount(getCredentials(), true);
            return acct.CreateCloudBlobClient().GetContainerReference(name);
        }

        public void CreateFolder(string name)
        {
            var c = getContainer(name);
            c.CreateIfNotExists();
        }

        public void CreateFile(string folderName, string fileName, Stream file)
        {
            var c = getContainer(folderName);
            CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
            blockBlob.UploadFromStream(file);
        }

        public void DeleteFile(string folderName, string fileName)
        {
            var c = getContainer(folderName);
            CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
            blockBlob.Delete();
        }

        public Stream GetFile(string folderName, string fileName)
        {
            using (var stream = new MemoryStream())
            {
                var c = getContainer(folderName);
                CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
                blockBlob.DownloadToStream(stream);
                return stream;
            }
        }

        public string GetFileSecureUrl(string folderName, string fileName)
        {
            var c = getContainer(folderName);
            var blockBlob = c.GetBlockBlobReference(fileName);
            var creds = getCredentials();
            var uri = creds.TransformUri(blockBlob.StorageUri).ToString();
            return uri;
        }
    }
}
