using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.queue;
using d360.model;
using d360.utils.company;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using d360.core.entities;
using d360.core.enums;
using d360.extensions.storage;

namespace igx.jobs.displayvalueupdateprocessor
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();

#if DEBUG
            config.UseDevelopmentSettings();
#endif
            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    public static class DisplayValueUpdateProcessor
    {
        const string functionName = "DisplayValueUpdateProcessor";
        
        public static async Task Run([QueueTrigger("%DisplayValueQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
        {
            var updateInfo = JsonConvert.DeserializeObject<DisplayUpdateInfo>(myQueueItem);

            try
            {
                #region Create EF connection
                var _c = CoreFunction.GetCompaniesByCurrentSlot()
                    .FirstOrDefault(x => x.CompanyID == updateInfo.CompanyID);

                var sec = new UriSecurityContextProvider()
                {
                    CompanyID = updateInfo.CompanyID,
                    ResourceID = 0,
                    CompanyPrefix = _c.UrlPrefix,
                    IsAdministrator = true
                };
                var cache = new DummyCachingProvider();
                var queue = new AzureQueueSource();
                var community = new CommunityContext(cache, queue, sec);
                var storage = new AzureStorageProvider();
                var company = new CompanyContext(community, cache, queue, sec, storage, true);

                #endregion

                using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(updateInfo.CompanyID))
                {
                    var assetTypeID = updateInfo.AssetTypeID;
                    if (updateInfo.ObjectTypeID > 0)
                    {
                        assetTypeID = await company.Database.Connection.QueryFirstOrDefaultAsync<int>($"select id from assettype where [object] = @obj and [objectid] = @objId", new { obj = new DbString { Value = updateInfo.ObjectType, IsFixedLength = true, Length = 20, IsAnsi = true }, objId = updateInfo.ObjectTypeID });
                    }
                    //if its an asset call the asset update proc
                    //if its a asset type call the asset type update proc
                    if (updateInfo.AssetID > 0)
                    {
                        await companyConnection.ExecuteAsync("exec GenerateAssetDisplayValue @assetID, null,-1", new { assetID = updateInfo.AssetID }, null, 2400);                        
                    }
                    else if(assetTypeID > 0)
                    {
                        await companyConnection.ExecuteAsync("exec GenerateAssetTypeDisplayValues @assetTypeID", new { assetTypeID }, null, 2400);
                    }
                    else if(updateInfo.RebuildAll)
                    {
                        try
                        {
                            await companyConnection.ExecuteAsync("exec GenerateAllAssetTypeDisplayValues", commandTimeout: 2400);
                        }
                        catch
                        {
                            throw;
                        }
                        finally 
                        {
                            await community.UpdateRebuildJobStatus(CompanyRebuildJobToken.DisplayValues, CompanyRebuildJobStatusState.Inactive);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, updateInfo.CompanyID);                
            }
        }
    }
}
