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

namespace d360.extensions.storage
{
    public class AzureStorageProvider : IStorageProvider
    {
        //string RESOURCE_IMAGE_CONTAINER = "d3s-resource-image";

        private StorageCredentials getCredentials()
        {
            var acctName = constants.AZURE_STORAGE_NAME;
            var keyValue = constants.AZURE_STORAGE_KEY;
            return new StorageCredentials(acctName, keyValue);
        }
        
        CloudBlobContainer getContainer(string name)
        {
            var acct = new CloudStorageAccount(
                getCredentials(), 
                new Uri(string.Format(@"https://{0}.blob.core.windows.net/", constants.AZURE_STORAGE_NAME)),
                new Uri(string.Format(@"https://{0}.queue.core.windows.net/", constants.AZURE_STORAGE_NAME)),
                new Uri(string.Format(@"https://{0}.table.core.windows.net/", constants.AZURE_STORAGE_NAME)),
                new Uri(string.Format(@"https://{0}.blob.core.windows.net/", constants.AZURE_STORAGE_NAME))
            );
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

        public void CreateFile(string folderName, string fileName, string content)
        {
            var c = getContainer(folderName);
            CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
            blockBlob.UploadText(content);
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

        public byte[] GetFileAsBytes(string folderName, string fileName)
        {
            byte[] bytes = null;
            using (var stream = new MemoryStream())
            {
                var c = getContainer(folderName);
                CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
                blockBlob.DownloadToStream(stream);
                bytes = new byte[(int)stream.Length];
                stream.Write(bytes, 0, (int)stream.Length);
            }
            return bytes;
        }

        public string GetFileSecureUrl(string folderName, string fileName)
        {
            var c = getContainer(folderName);
            var blockBlob = c.GetBlockBlobReference(fileName);

            var policy = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddMinutes(1), 
                Permissions = SharedAccessBlobPermissions.Read
            };

            //var permissions = new BlobContainerPermissions();
            //permissions.SharedAccessPolicies.Add("1minute", policy);
            //permissions.PublicAccess = BlobContainerPublicAccessType.Off;
            //c.SetPermissions(permissions);

            var signature = blockBlob.GetSharedAccessSignature(policy);

            //var creds = getCredentials();
            var uri = blockBlob.Uri.AbsoluteUri + signature;
            return uri;
        }


        public bool ReleaseLockOnBlobFile(string folderName, string fileName)
        {
            var c = getContainer(folderName);
            CloudBlockBlob blockBlob = c.GetBlockBlobReference(fileName);
            var timespan  = blockBlob.BreakLease();
            return true;
        }
    }
}
