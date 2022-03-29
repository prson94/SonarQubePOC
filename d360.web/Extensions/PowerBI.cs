using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;

using Newtonsoft.Json;

namespace d360.web.Extensions
{
    public class DataSourceCredentials
    {
        public string credentialType { get; set; }

        public BasicCredentials basicCredentials { get; set; }
    }

    public class PowerBI
    {
        private static readonly string apiEndpointUri = "https://api.powerbi.com";
        private static readonly string pbiAuthorityUrl = "https://login.microsoftonline.com/02292cae-2fe6-4371-8da1-b03d14808575";
        private static readonly string pbiResourceUrl = "https://analysis.windows.net/powerbi/api";
        public const string PowerBiServiceRootUrl = "https://api.powerbi.com/v1.0/myorg/";

        /// <summary>
        /// Imports a Power BI Desktop file (pbix) into the Power BI Embedded service
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <param name="clientId"></param>
        /// <param name="groupId"></param>
        /// <param name="datasetName"></param>
        /// <param name="fileStream"></param>
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
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <param name="clientId"></param>
        /// <param name="groupId"></param>
        /// <param name="datasetId"></param>
        /// <returns></returns>
        public static async Task DeleteDataset(string user, string pwd, string clientId, string groupId, string datasetId)
        {
            using (var client = await CreateClient(user, pwd, clientId))
            {
                await client.Datasets.DeleteDatasetByIdAsync(groupId, datasetId);
            }
        }

        public static async Task<PowerBIClient> CreateClient(string user, string pwd, string clientId, AuthenticationResult auth = null)
        {
            if (auth == null)
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
            else
            {
                var tokenCredentials = new TokenCredentials(auth.AccessToken, "Bearer");

                return new PowerBIClient(new Uri(apiEndpointUri), tokenCredentials);
            }
        }

        public static async Task<Group> CreateWorkspace(string pbiUser, string pbiPwd, string clientId, string groupName)
        {
            // Create a provision token required to create a new workspace within your collection            
            using (var client = await CreateClient(pbiUser, pbiPwd, clientId))
            {
                var grp = new GroupCreationRequest(groupName);

                // Create a new workspace within the specified collection
                var createdGrp = await client.Groups.CreateGroupAsync(grp);
                var caps = await client.Capacities.GetCapacitiesAsync();

                if (!caps.Value.Any())
                {
                    throw new ArgumentException("CANNOT FIND ANY CAPACITY GROUPS");
                }

                client.Groups.AssignToCapacity(createdGrp.Id, new AssignToCapacityRequest { CapacityId = caps.Value.FirstOrDefault().Id });

                return createdGrp;
            }
        }

        public static async Task UpdateConnectionCredentials(string pbiUser, string pbiPwd, string clientId, string groupId, string username, string password, string connectionString = "")
        {
            var credential = new UserPasswordCredential(pbiUser, pbiPwd);
            var authenticationContext = new AuthenticationContext(pbiAuthorityUrl);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(pbiResourceUrl, clientId, credential);

            if (authenticationResult == null)
            {
                throw new Exception("authentication failed");
            }

            using (var client = await CreateClient(pbiUser, pbiPwd, clientId, authenticationResult))
            {
                // Get the newly created dataset from the previous import process
                var datasets = await client.Datasets.GetDatasetsAsync(groupId);

                if (datasets == null || datasets.Value == null)
                {
                    return;
                }

                //update the first sql data source..
                foreach (var dataset in datasets.Value)
                {
                    // Optionally udpate the connectionstring details if preent
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        ConnectionDetails det = new ConnectionDetails
                        {
                            ConnectionString = connectionString
                        };

                        await client.Datasets.SetAllDatasetConnectionsAsync(groupId, dataset.Id, det);
                    }

                    try
                    {
                        // Get the datasources from the dataset
                        var datasources = await client.Datasets.GetDatasourcesInGroupAsync(groupId, dataset.Id);

                        if ((datasources.Value[datasources.Value.Count - 1].DatasourceType ?? "").ToUpper() == "SQL")
                        {
                            var gatewayId = datasources.Value[datasources.Value.Count - 1].GatewayId;
                            var datasourceId = datasources.Value[datasources.Value.Count - 1].DatasourceId;

                            // create URL with pattern myorg/gateways/{gateway_id}/datasources/{datasource_id}
                            string restUrlPatchCredentials =
                              PowerBiServiceRootUrl +
                              "gateways/" + gatewayId + "/" +
                              "datasources/" + datasourceId + "/";

                            // create C# object with credential data
                            DataSourceCredentials dataSourceCredentials =
                              new DataSourceCredentials
                              {
                                  credentialType = "Basic",
                                  basicCredentials = new BasicCredentials
                                  {
                                      Username = username,
                                      Password = password
                                  }
                              };

                            // serialize C# object into JSON
                            string jsonDelta = JsonConvert.SerializeObject(dataSourceCredentials);

                            // add JSON to HttpContent object and configure content type
                            HttpContent patchRequestBody = new StringContent(jsonDelta);
                            patchRequestBody.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

                            // prepare PATCH request
                            var method = new HttpMethod("PATCH");
                            var request = new HttpRequestMessage(method, restUrlPatchCredentials)
                            {
                                Content = patchRequestBody
                            };

                            request.Headers.Add("Accept", "application/json");
                            request.Headers.Add("Authorization", "Bearer " + authenticationResult.AccessToken);

                            await client.HttpClient.SendAsync(request);
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
