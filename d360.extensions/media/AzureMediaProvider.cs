using d360.core;
using Microsoft.WindowsAzure.MediaServices.Client;
using System.IO;

namespace d360.extensions.storage
{
    public class AzureMediaProvider : IMediaProvider
    {
        private MediaServicesCredentials getCredentials()
        {
            var acctName = constants.AZURE_STORAGE_NAME;
            var keyValue = constants.AZURE_STORAGE_KEY;
            return new MediaServicesCredentials(acctName, keyValue);
        }
        

        public void UploadAsset(string path)
        {
            var credentials = getCredentials();
            var context = new CloudMediaContext(credentials);

            var assetName = Path.GetFileNameWithoutExtension(path);
            var inputAsset = context.Assets.Create(assetName, AssetCreationOptions.StorageEncrypted);
            var assetFile = inputAsset.AssetFiles.Create(Path.GetFileName(path));
            assetFile.Upload(path);
        }

    }
}
