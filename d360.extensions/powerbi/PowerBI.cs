using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerBI.Api.V1;
using Microsoft.PowerBI.Api.V1.Models;
using Microsoft.Rest;
using System.IO;
using Microsoft.PowerBI.Security;
using System.Threading;
using Microsoft.Rest.Serialization;
using System.Net.Http.Headers;
using System.Configuration;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace d360.extensions.powerbi
{
    public class PowerBI
    {
        static string apiEndpointUri = "https://api.powerbi.com";
        

        /// <summary>
        /// Imports a Power BI Desktop file (pbix) into the Power BI Embedded service
        /// </summary>        
        /// <param name="datasetName">The dataset name to apply to the uploaded dataset</param>
        /// <param name="filePath">A local file path on your computer</param>
        /// <returns></returns>
        public static async Task<Import> ImportPbix(string accessKey, string workspaceCollectionName, string workspaceId, string datasetName, Stream fileStream)
        {            
                // Create a dev token for import                
                using (var client = CreateClient(accessKey))
                {
                    // Import PBIX file from the file stream
                    var import = await client.Imports.PostImportWithFileAsync(workspaceCollectionName, workspaceId, fileStream, datasetName);

                    // Example of polling the import to check when the import has succeeded.
                    while (import.ImportState != "Succeeded" && import.ImportState != "Failed")
                    {
                        import = await client.Imports.GetImportByIdAsync(workspaceCollectionName, workspaceId, import.Id);
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
        public static async Task DeleteDataset(string accessKey, string workspaceCollectionName, string workspaceId, string datasetId)
        {           
            
            using (var client = CreateClient(accessKey))
            {
                await client.Datasets.DeleteDatasetByIdAsync(workspaceCollectionName, workspaceId, datasetId);
            }
        }


        public static IPowerBIClient CreateClient(string accessKey)
        {
            // Create a token credentials with "AppKey" type
            var credentials = new TokenCredentials(accessKey, "AppKey");

            // Instantiate your Power BI client passing in the required credentials
            var client = new PowerBIClient(credentials);
            
            // Override the api endpoint base URL.  Default value is https://api.powerbi.com
            client.BaseUri = new Uri(apiEndpointUri);

            return client;
        }
        
        public async static Task<Workspace> CreateWorkspace(string accessKey, string workspaceCollectionName)
        {
            // Create a provision token required to create a new workspace within your collection            
            using (var client = CreateClient(accessKey))
            {
                // Create a new workspace within the specified collection
                return await client.Workspaces.PostWorkspaceAsync(workspaceCollectionName);
            }
        }

        public static async Task UpdateConnectionCredentials(string accessKey, string workspaceCollectionName, string workspaceId, string username, string password, string connectionString = "")
        {            
            using (var client = CreateClient(accessKey))
            {
                // Get the newly created dataset from the previous import process
                var datasets = await client.Datasets.GetDatasetsAsync(workspaceCollectionName, workspaceId);

                if (datasets == null || datasets.Value == null) return;

                //update the first sql data source..
                foreach (var dataset in datasets.Value)
                {                    
                    // Optionally udpate the connectionstring details if preent
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        var connectionParameters = new Dictionary<string, object>
                    {
                        { "connectionString", connectionString }
                    };
                        await client.Datasets.SetAllConnectionsAsync(workspaceCollectionName, workspaceId, dataset.Id, connectionParameters);
                    }

                    // Get the datasources from the dataset
                    var datasources = await client.Datasets.GetGatewayDatasourcesAsync(workspaceCollectionName, workspaceId, dataset.Id);

                    if ((datasources.Value[datasources.Value.Count - 1].DatasourceType ?? "").ToUpper() == "SQL")
                    {
                        // Reset your connection credentials
                        var delta = new GatewayDatasource
                        {
                            CredentialType = "Basic",
                            BasicCredentials = new BasicCredentials
                            {
                                Username = username,
                                Password = password
                            }
                        };

                        // Update the datasource with the specified credentials
                        await client.Gateways.PatchDatasourceAsync(workspaceCollectionName, workspaceId, datasources.Value[datasources.Value.Count - 1].GatewayId, datasources.Value[datasources.Value.Count - 1].Id, delta);

                        return;
                    }
                }                
            }
        }
    }
}
