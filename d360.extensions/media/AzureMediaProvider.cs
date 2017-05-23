using d360.core;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace d360.extensions.storage
{
    public class AzureMediaProvider : IMediaProvider
    {
        private CloudMediaContext context;

        public AzureMediaProvider()
        {
            var credentials = getCredentials();
            context = new CloudMediaContext(credentials);
        }

        private MediaServicesCredentials getCredentials()
        {
            var acctName = constants.AZURE_MEDIA_SERVICES_ACCOUNT_NAME;
            var keyValue = constants.AZURE_MEDIA_SERVICES_KEY;
            return new MediaServicesCredentials(acctName, keyValue);
        }
        

        public void UploadAsset(string path)
        {
            var assetName = Path.GetFileNameWithoutExtension(path);
            var inputAsset = context.Assets.Create(assetName, AssetCreationOptions.StorageEncrypted);
            var assetFile = inputAsset.AssetFiles.Create(Path.GetFileName(path));


            assetFile.Upload(path);
            inputAsset = EncodeToAdaptiveBitrateMP4Set(inputAsset, assetName + "-encoded");
            ConfigureClearAssetDeliveryPolicy(inputAsset);
            BuildStreamingURLs(inputAsset);
            //GetLatestMediaProcessorByName("Media Encoder Standard");
        }

        public IAsset EncodeToAdaptiveBitrateMP4Set(IAsset asset, string filename)
        {
            // Declare a new job.
            IJob job = context.Jobs.Create("Media Encoder Standard Job");
            // Get a media processor reference, and pass to it the name of the 
            // processor to use for the specific task.
            IMediaProcessor processor = GetLatestMediaProcessorByName("Media Encoder Standard");

            // Create a task with the encoding details, using a string preset.
            // In this case "Adaptive Streaming" preset is used.
            ITask task = job.Tasks.AddNew("My encoding task",
                processor,
                "Adaptive Streaming",
                TaskOptions.None);

            // Specify the input asset to be encoded.
            task.InputAssets.Add(asset);
            // Add an output asset to contain the results of the job. 
            // This output is specified as AssetCreationOptions.None, which 
            // means the output asset is not encrypted. 
            task.OutputAssets.AddNew(filename,
                AssetCreationOptions.StorageEncrypted);

            //job.StateChanged += new EventHandler<JobStateChangedEventArgs>(JobStateChanged);
            job.Submit();
            job.GetExecutionProgressTask(CancellationToken.None).Wait();

            return job.OutputMediaAssets[0];
        }

        private void BuildStreamingURLs(IAsset asset)
        {
            
            // Create a 30-day readonly access policy. 
            // You cannot create a streaming locator using an AccessPolicy that includes write or delete permissions.
            IAccessPolicy policy = context.AccessPolicies.Create("Streaming policy",
                TimeSpan.FromDays(36500),
                AccessPermissions.Read);
           
            // Create a locator to the streaming content on an origin. 
            ILocator originLocator = context.Locators.CreateLocator(LocatorType.OnDemandOrigin, asset,
                policy,
                DateTime.UtcNow.AddMinutes(-5));

            // Display some useful values based on the locator.
            Console.WriteLine("Streaming asset base path on origin: ");
            Console.WriteLine(originLocator.Path);
            Console.WriteLine();

            // Get a reference to the streaming manifest file from the  
            // collection of files in the asset. 
            var manifestFile = asset.AssetFiles.Where(f => f.Name.ToLower().
                                        EndsWith(".ism")).
                                        FirstOrDefault();

            // Create a full URL to the manifest file. Use this for playback
            // in streaming media clients. 
            string urlForClientStreaming = originLocator.Path + manifestFile.Name + "/manifest";
            Console.WriteLine("URL to manifest for client streaming using Smooth Streaming protocol: ");
            Console.WriteLine(urlForClientStreaming);
            Console.WriteLine("URL to manifest for client streaming using HLS protocol: ");
            Console.WriteLine(urlForClientStreaming + "(format=m3u8-aapl)");
            Console.WriteLine("URL to manifest for client streaming using MPEG DASH protocol: ");
            Console.WriteLine(urlForClientStreaming + "(format=mpd-time-csf)");
            Console.WriteLine();
        }

        public void ConfigureClearAssetDeliveryPolicy(IAsset asset)
        {
            IAssetDeliveryPolicy policy =
            context.AssetDeliveryPolicies.Create("Clear Policy",
            AssetDeliveryPolicyType.NoDynamicEncryption,
            AssetDeliveryProtocol.HLS | AssetDeliveryProtocol.SmoothStreaming | AssetDeliveryProtocol.Dash, null);

            asset.DeliveryPolicies.Add(policy);
        }

        private IMediaProcessor GetLatestMediaProcessorByName(string mediaProcessorName)
        {

            //var processors = context.MediaProcessors.ToList();

            var processor = context.MediaProcessors.Where(p => p.Name == mediaProcessorName).
            ToList().OrderBy(p => new Version(p.Version)).LastOrDefault();

            if (processor == null)
                throw new ArgumentException(string.Format("Unknown media processor", mediaProcessorName));

            return processor;
        }

    }
}
