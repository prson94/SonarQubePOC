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
using d360.core.enums;
using d360.extensions.storage;
using Microsoft.Extensions.Hosting;

namespace igx.jobs.displayvalueupdateprocessor
{
    class Program
    {
        static async Task Main()
        {
            using (var host = CoreFunction.JobHostConfigBuilder().Build())
            {
                await host.RunAsync();
            }
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
                var _c = CoreFunction.GetCompaniesByCurrentSlot().FirstOrDefault(x => x.CompanyID == updateInfo.CompanyID);
                var company = JobDbContextCreator.CreateCompanyContext(updateInfo.CompanyID, 0, _c.UrlPrefix, true);

                using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(updateInfo.CompanyID))
                {
                    await companyConnection.OpenIfClosed();

                    var assetTypeID = updateInfo.AssetTypeID;
                    if (updateInfo.ObjectTypeID > 0)
                    {
                        assetTypeID = await companyConnection.QueryFirstOrDefaultAsync<int>($"select id from assettype where [object] = @obj and [objectid] = @objId", new { obj = new DbString { Value = updateInfo.ObjectType, IsFixedLength = true, Length = 20, IsAnsi = true }, objId = updateInfo.ObjectTypeID });
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
                            await company.UpdateRebuildJobStatus(CompanyRebuildJobToken.DisplayValues, CompanyRebuildJobStatusState.Inactive);
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
