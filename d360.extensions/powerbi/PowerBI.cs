using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace d360.extensions.powerbi
{
    public static class AsymmetricKeyEncryptionHelper
    {

        private const int SegmentLength = 85;
        private const int EncryptedLength = 128;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param> the datasouce user name
        /// <param name="password"></param> the datasource password
        /// <param name="gatewaypublicKeyExponent"></param> gateway publicKey Exponent field, you can get it from the get gateways api response json
        /// <param name="gatewaypublicKeyModulus"></param> gateway publicKey Modulus field, you can get it from the get gateways api response json
        /// <returns></returns>
        public static string EncodeCredentials(string userName, string password, string gatewaypublicKeyExponent, string gatewaypublicKeyModulus)
        {
            // using json serializer to handle escape characters in username and password
            var plainText = string.Format("{{\"credentialData\":[{{\"value\":{0},\"name\":\"username\"}},{{\"value\":{1},\"name\":\"password\"}}]}}", JsonConvert.SerializeObject(userName), JsonConvert.SerializeObject(password));
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(EncryptedLength * 8))
            {
                var parameters = rsa.ExportParameters(false);
                parameters.Exponent = Convert.FromBase64String(gatewaypublicKeyExponent);
                parameters.Modulus = Convert.FromBase64String(gatewaypublicKeyModulus);
                rsa.ImportParameters(parameters);
                return Encrypt(plainText, rsa);
            }
        }

        private static string Encrypt(string plainText, RSACryptoServiceProvider rsa)
        {
            byte[] plainTextArray = Encoding.UTF8.GetBytes(plainText);

            // Split the message into different segments, each segment's length is 85. So the result may be 85,85,85,20.
            bool hasIncompleteSegment = plainTextArray.Length % SegmentLength != 0;

            int segmentNumber = (!hasIncompleteSegment) ? (plainTextArray.Length / SegmentLength) : ((plainTextArray.Length / SegmentLength) + 1);

            byte[] encryptedData = new byte[segmentNumber * EncryptedLength];
            int encryptedDataPosition = 0;

            for (var i = 0; i < segmentNumber; i++)
            {
                int lengthToCopy;

                if (i == segmentNumber - 1 && hasIncompleteSegment)
                    lengthToCopy = plainTextArray.Length % SegmentLength;
                else
                    lengthToCopy = SegmentLength;

                var segment = new byte[lengthToCopy];

                Array.Copy(plainTextArray, i * SegmentLength, segment, 0, lengthToCopy);

                var segmentEncryptedResult = rsa.Encrypt(segment, true);

                Array.Copy(segmentEncryptedResult, 0, encryptedData, encryptedDataPosition, segmentEncryptedResult.Length);

                encryptedDataPosition += segmentEncryptedResult.Length;
            }

            return Convert.ToBase64String(encryptedData);
        }
    }

    public class PowerBI
    {
        static string apiEndpointUri = "https://api.powerbi.com";

        private static readonly string pbiAuthorityUrl = "https://login.windows.net/common/oauth2/authorize/";
        private static readonly string pbiResourceUrl = "https://analysis.windows.net/powerbi/api";
        


        /// <summary>
        /// Imports a Power BI Desktop file (pbix) into the Power BI Embedded service
        /// </summary>        
        /// <param name="datasetName">The dataset name to apply to the uploaded dataset</param>
        /// <param name="filePath">A local file path on your computer</param>
        /// <returns></returns>
        public static async Task<Import> ImportPbix(string user, string pwd, string clientId, string groupId, string datasetName, Stream fileStream)
        {            
                // Create a dev token for import                
                using (var client = await CreateClient(user, pwd, clientId))
                {
                    // Import PBIX file from the file stream
                    var import = await client.Imports.PostImportWithFileAsync(groupId, fileStream, datasetName);

                    // Example of polling the import to check when the import has succeeded.
                    while (import.ImportState != "Succeeded" && import.ImportState != "Failed")
                    {
                        import = await client.Imports.GetImportByIdAsync(groupId, import.Id);
                        Debug.WriteLine("Checking import state... {0}", import.ImportState);
                        Thread.Sleep(1000);
                    }

                    return import;
                }
            
        }


        /// <summary>
        /// Removes a published dataset from a given workspace.
        /// </summary>        
        /// <param name="datasetId">The Power BI dataset to delete</param>
        /// <returns></returns>
        public static async Task DeleteDataset(string user, string pwd, string clientId, string groupId, string datasetId)
        {           
            
            using (var client = await CreateClient(user,pwd,clientId))
            {
                await client.Datasets.DeleteDatasetByIdAsync(groupId, datasetId);
            }
        }


        public static async Task<PowerBIClient> CreateClient(string user, string pwd, string clientId)
        {
            var credential = new UserPasswordCredential(user, pwd);

            // Authenticate using created credentials
            var authenticationContext = new AuthenticationContext(pbiAuthorityUrl);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(pbiResourceUrl, clientId, credential);

            if (authenticationResult == null)
            {
                throw new Exception("authentication failed");
            }

            var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");

            return new PowerBIClient(new Uri(apiEndpointUri), tokenCredentials);
        }
        
        public async static Task<Group> CreateWorkspace(string pbiUser, string pbiPwd, string clientId, string groupName)
        {
            // Create a provision token required to create a new workspace within your collection            
            using (var client = await CreateClient(pbiUser, pbiPwd, clientId))
            {
                var grp = new GroupCreationRequest(groupName);
                
                // Create a new workspace within the specified collection
                var createdGrp =  await client.Groups.CreateGroupAsync(grp);
                var caps = await client.Capacities.GetCapacitiesAsync();

                if (!caps.Value.Any()) throw new Exception("CANNOT FIND ANY CAPACITY GROUPS");

                client.Groups.AssignToCapacity(createdGrp.Id, new AssignToCapacityRequest { CapacityId = caps.Value.FirstOrDefault().Id });

                return createdGrp;
            }
        }

        public static async Task UpdateConnectionCredentials(string pbiUser, string pbiPwd, string clientId, string groupId, string username, string password, string connectionString = "")
        {            
            using (var client = await CreateClient(pbiUser, pbiPwd, clientId))
            {
                // Get the newly created dataset from the previous import process
                var datasets = await client.Datasets.GetDatasetsAsync(groupId);

                if (datasets == null || datasets.Value == null) return;

                //update the first sql data source..
                foreach (var dataset in datasets.Value)
                {                    
                    // Optionally udpate the connectionstring details if preent
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        ConnectionDetails det = new ConnectionDetails();
                        det.ConnectionString = connectionString;
                        
                        await client.Datasets.SetAllDatasetConnectionsAsync(groupId, dataset.Id, det);
                    }

                    try
                    {
                        // Get the datasources from the dataset
                        var datasources = await client.Datasets.GetDatasourcesInGroupAsync(groupId, dataset.Id);

                        if ((datasources.Value[datasources.Value.Count - 1].DatasourceType ?? "").ToUpper() == "SQL")
                        {
                            // Reset your connection credentials
                            /*  var delta = new GatewayDatasource
                              {
                                  CredentialType = "Basic",
                                  BasicCredentials = new BasicCredentials
                                  {
                                      Username = username,
                                      Password = password
                                  }
                              };*/

                            var gatewayId = datasources.Value[datasources.Value.Count - 1].GatewayId;
                            var datasourceId = datasources.Value[datasources.Value.Count - 1].DatasourceId;
                            /*

                            
                            string uri = string.Format("https://api.powerbi.com/v1.0/myorg/gateways/{0}/datasources/{1}", gatewayId, datasourceId);

                            var patchUri = new Uri(uri);

                            var basic = $"{{\"credentialData\": [{{\"name\":\"username\",\"value\":\"{username}\"}},{{ \"name\":\"password\",\"value\":\"{password}\" }}]}}";
                            

                            using (var request = new HttpRequestMessage { Method = new HttpMethod("PATCH"), RequestUri = patchUri, Content = content })
                            {
                                var rep = client.HttpClient.SendAsync(request).Result;
                            }*/
                            
                            //var credentials = AsymmetricKeyEncryptionHelper.EncodeCredentials(username, password, gate.PublicKey.Exponent, gate.PublicKey.Modulus);
                               var plainText = string.Format("{{\"credentialData\":[{{\"value\":{0},\"name\":\"username\"}},{{\"value\":{1},\"name\":\"password\"}}]}}", JsonConvert.SerializeObject(username), JsonConvert.SerializeObject(password));

                              var delta = new UpdateDatasourceRequest
                              {
                                  CredentialDetails = new CredentialDetails(
                                  plainText,
                                  credentialType: "Basic",
                                  encryptedConnection: "Encrypted",
                                  encryptionAlgorithm: "None",
                                  privacyLevel: "None"
                                  )
                              };
                            
                            // Update the datasource with the specified credentials
                            await client.Gateways.UpdateDatasourceAsync(datasources.Value[datasources.Value.Count - 1].GatewayId, datasources.Value[datasources.Value.Count - 1].DatasourceId, delta);
                              
                            //return;
                        }
                    }
                    catch (HttpOperationException ex)
                    {
                        //Bad Request
                        var content = ex.Response.Content;
                        Console.WriteLine(content);
                    }
                }                
            }
        }
    }
}
