using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerBI.Api.Beta;
using Microsoft.PowerBI.Api.Beta.Models;
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
                var devToken = PowerBIToken.CreateDevToken(workspaceCollectionName, workspaceId);
                using (var client = CreateClient(devToken,accessKey))
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
            var devToken = PowerBIToken.CreateDevToken(workspaceCollectionName, workspaceId);
            using (var client = CreateClient(devToken,accessKey))
            {
                await client.Datasets.DeleteDatasetByIdAsync(workspaceCollectionName, workspaceId, datasetId);
            }
        }


        public static IPowerBIClient CreateClient(PowerBIToken token, string accessKey)
        {            
            // Generate a JWT token used when accessing the REST APIs
            var jwt = token.Generate(accessKey);

            // Create a token credentials with "AppToken" type
            var credentials = new TokenCredentials(jwt, "AppToken");

            // Instantiate your Power BI client passing in the required credentials
            var client = new PowerBIClient(credentials);

            // Override the api endpoint base URL.  Default value is https://api.powerbi.com
            client.BaseUri = new Uri(apiEndpointUri);

            return client;
        }
        
        public async static Task<Workspace> CreateWorkspace(string accessKey, string workspaceCollectionName)
        {
            // Create a provision token required to create a new workspace within your collection
            var provisionToken = PowerBIToken.CreateProvisionToken(workspaceCollectionName);
            using (var client = CreateClient(provisionToken, accessKey))
            {
                // Create a new workspace within the specified collection
                return await client.Workspaces.PostWorkspaceAsync(workspaceCollectionName);
            }
        }
    }
}
