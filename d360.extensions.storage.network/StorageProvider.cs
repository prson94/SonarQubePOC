using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions.storage.network
{
    public class StorageProvider: IStorageProvider
    {
        string RESOURCE_IMAGE_CONTAINER = "d3s-resource-image";

        public void CreateFolder(string name)
        {
            throw new NotImplementedException();
        }

        public void CreateFile(string folderName, string fileName, System.IO.Stream file)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(string folderName, string fileName)
        {
            throw new NotImplementedException();
        }

        public Stream GetFile(string folderName, string fileName)
        {
            throw new NotImplementedException();
        }

        public string GetFileSecureUrl(string folderName, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
