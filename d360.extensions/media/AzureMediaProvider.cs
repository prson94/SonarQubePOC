using d360.core;
using Microsoft.WindowsAzure.MediaServices.Client;
using Microsoft.WindowsAzure.MediaServices.Client.ContentKeyAuthorization;
using Microsoft.WindowsAzure.MediaServices.Client.DynamicEncryption;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace d360.extensions.storage
{
    public class AzureMediaProvider : IMediaProvider
    {
        private CloudMediaContext context;

        public AzureMediaProvider()
        {
            //var credentials = getCredentials();
            //context = new CloudMediaContext(credentials);
        }

        //private MediaServicesCredentials getCredentials()
        //{
        //    var acctName = constants.AZURE_MEDIA_SERVICES_ACCOUNT_NAME;
        //    var keyValue = constants.AZURE_MEDIA_SERVICES_KEY;
        //    return new MediaServicesCredentials(acctName, keyValue);
        //}


        public void UploadAsset(string path)
        {
            var assetName = Path.GetFileNameWithoutExtension(path);
            var inputAsset = context.Assets.Create(assetName, AssetCreationOptions.StorageEncrypted);
            var assetFile = inputAsset.AssetFiles.Create(Path.GetFileName(path));


            assetFile.Upload(path);
            inputAsset = EncodeToAdaptiveBitrateMP4Set(inputAsset, assetName + "-encoded");
            var key = CreateEnvelopeTypeContentKey(inputAsset);
            CreateAssetDeliveryPolicy(inputAsset, key);

            BuildStreamingURLs(inputAsset);

            var uri = key.GetKeyDeliveryUrl(ContentKeyDeliveryType.BaselineHttp);
            var aesKey = GetDeliveryKey(uri, null);
            //ConfigureClearAssetDeliveryPolicy(inputAsset);

            //GetLatestMediaProcessorByName("Media Encoder Standard");

            //var tokenTemplateString = AddTokenRestrictedAuthorizationPolicy(key);
            //TokenRestrictionTemplate tokenTemplate =
            //    TokenRestrictionTemplateSerializer.Deserialize(tokenTemplateString);
            //Guid rawkey = EncryptionUtils.GetKeyIdAsGuid(key.Id);

            //string testToken = TokenRestrictionTemplateSerializer.GenerateTestToken(tokenTemplate, null, rawkey);
            ////Console.WriteLine("The authorization token is:\nBearer {0}", testToken);
            //Console.WriteLine();





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

        private void CreateAssetDeliveryPolicy(IAsset asset, IContentKey key)
        {

            //  Get the Key Delivery Base Url by removing the Query parameter.  The Dynamic Encryption service will
            //  automatically add the correct key identifier to the url when it generates the Envelope encrypted content
            //  manifest.  Omitting the IV will also cause the Dynamice Encryption service to generate a deterministic
            //  IV for the content automatically.  By using the EnvelopeBaseKeyAcquisitionUrl and omitting the IV, this
            //  allows the AssetDelivery policy to be reused by more than one asset.
            //
            Uri keyAcquisitionUri = key.GetKeyDeliveryUrl(ContentKeyDeliveryType.BaselineHttp);
            UriBuilder uriBuilder = new UriBuilder(keyAcquisitionUri);
            uriBuilder.Query = string.Empty;
            keyAcquisitionUri = uriBuilder.Uri;


            // The following policy configuration specifies: 
            //   key url that will have KID=<Guid> appended to the envelope and
            //   the Initialization Vector (IV) to use for the envelope encryption.
            Dictionary<AssetDeliveryPolicyConfigurationKey, string> assetDeliveryPolicyConfiguration =
                new Dictionary<AssetDeliveryPolicyConfigurationKey, string>
            {
                    {
                        AssetDeliveryPolicyConfigurationKey.EnvelopeBaseKeyAcquisitionUrl, keyAcquisitionUri.ToString()
                    },
            };

            IAssetDeliveryPolicy assetDeliveryPolicy =
                context.AssetDeliveryPolicies.Create(
                            "AssetDeliveryPolicy",
                            AssetDeliveryPolicyType.DynamicEnvelopeEncryption,
                            AssetDeliveryProtocol.SmoothStreaming | AssetDeliveryProtocol.HLS | AssetDeliveryProtocol.Dash,
                            assetDeliveryPolicyConfiguration);

            // Add AssetDelivery Policy to the asset
            asset.DeliveryPolicies.Add(assetDeliveryPolicy);

            Console.WriteLine();
            Console.WriteLine("Adding Asset Delivery Policy: " + assetDeliveryPolicy.AssetDeliveryPolicyType);
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

        public IContentKey CreateEnvelopeTypeContentKey(IAsset asset)
        {
            // Create envelope encryption content key
            Guid keyId = Guid.NewGuid();
            byte[] contentKey = GetRandomBuffer(16);

            IContentKey key = context.ContentKeys.Create(
                                    keyId,
                                    contentKey,
                                    "ContentKey",
                                    ContentKeyType.EnvelopeEncryption);

            asset.ContentKeys.Add(key);

            return key;
        }

        private byte[] GetRandomBuffer(int size)
        {
            byte[] randomBytes = new byte[size];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }

            return randomBytes;
        }



        private string GenerateTokenRequirements()
        {
            TokenRestrictionTemplate template = new TokenRestrictionTemplate(TokenType.SWT);

            template.PrimaryVerificationKey = new SymmetricVerificationKey();
            template.AlternateVerificationKeys.Add(new SymmetricVerificationKey());
            template.Audience = "foo".ToString();
            template.Issuer = "bar".ToString();

            template.RequiredClaims.Add(TokenClaim.ContentKeyIdentifierClaim);

            return TokenRestrictionTemplateSerializer.Serialize(template);
        }

        private byte[] GetDeliveryKey(Uri keyDeliveryUri, string token)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(keyDeliveryUri);

            request.Method = "POST";
            request.ContentType = "text/xml";
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers[HttpRequestHeader.Authorization] = token;
            }
            request.ContentLength = 0;

            var response = request.GetResponse();

            var stream = response.GetResponseStream();
            if (stream == null)
            {
                throw new NullReferenceException("Response stream is null");
            }

            var buffer = new byte[256];
            var length = 0;
            while (stream.CanRead && length <= buffer.Length)
            {
                var nexByte = stream.ReadByte();
                if (nexByte == -1)
                {
                    break;
                }
                buffer[length] = (byte)nexByte;
                length++;
            }
            response.Close();

            // AES keys must be exactly 16 bytes (128 bits).
            var key = new byte[length];
            Array.Copy(buffer, key, length);
            return key;
        }

        public void AddOpenAuthorizationPolicy(IContentKey contentKey)
        {
            // Create ContentKeyAuthorizationPolicy with Open restrictions
            // and create authorization policy
            IContentKeyAuthorizationPolicy policy = context.
            ContentKeyAuthorizationPolicies.
            CreateAsync("Open Authorization Policy").Result;

            List<ContentKeyAuthorizationPolicyRestriction> restrictions =
                new List<ContentKeyAuthorizationPolicyRestriction>();

            ContentKeyAuthorizationPolicyRestriction restriction =
                new ContentKeyAuthorizationPolicyRestriction
                {
                    Name = "Testing Open Auth Policy",
                    KeyRestrictionType = (int)ContentKeyRestrictionType.Open,
                    Requirements = null
                };

            restrictions.Add(restriction);

            IContentKeyAuthorizationPolicyOption policyOption =
                context.ContentKeyAuthorizationPolicyOptions.Create(
                "policy",
                ContentKeyDeliveryType.BaselineHttp,
                restrictions,
                "");

            policy.Options.Add(policyOption);

            // Add ContentKeyAutorizationPolicy to ContentKey
            contentKey.AuthorizationPolicyId = policy.Id;
            IContentKey updatedKey = contentKey.UpdateAsync().Result;
            Console.WriteLine("Adding Key to Asset: Key ID is " + updatedKey.Id);
        }


        public string AddTokenRestrictedAuthorizationPolicy(IContentKey contentKey)
        {
            string tokenTemplateString = GenerateTokenRequirements();

            IContentKeyAuthorizationPolicy policy = context.
                                    ContentKeyAuthorizationPolicies.
                                    CreateAsync("Token restricted authorization policy").Result;

            List<ContentKeyAuthorizationPolicyRestriction> restrictions =
                    new List<ContentKeyAuthorizationPolicyRestriction>();

            ContentKeyAuthorizationPolicyRestriction restriction =
                    new ContentKeyAuthorizationPolicyRestriction
                    {
                        Name = "Token Authorization Policy",
                        KeyRestrictionType = (int)ContentKeyRestrictionType.TokenRestricted,
                        Requirements = tokenTemplateString
                    };

            restrictions.Add(restriction);

            //You could have multiple options 
            IContentKeyAuthorizationPolicyOption policyOption =
                context.ContentKeyAuthorizationPolicyOptions.Create(
                    "Token Policy Option",
                    ContentKeyDeliveryType.BaselineHttp,
                    restrictions,
                    null
                    );

            policy.Options.Add(policyOption);

            // Add ContentKeyAutorizationPolicy to ContentKey
            contentKey.AuthorizationPolicyId = policy.Id;
            IContentKey updatedKey = contentKey.UpdateAsync().Result;
            Console.WriteLine("Adding Key to Asset: Key ID is " + updatedKey.Id);

            return tokenTemplateString;
        }
    }
}
