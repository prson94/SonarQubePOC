using System;
using System.Linq;
using d360.core.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using d360.model;
using d360.core.enums;

namespace igx.functions.consumption
{
    public class DisplayValueUpdateProcessor
    {
        const string functionName = "DisplayValueUpdateProcessor";
        private CoreFunction CoreFunction;

        [FunctionName("DisplayValueUpdateProcessor")]
        public async Task Run([QueueTrigger("%DisplayValueQueue%"), StorageAccount("AzureWebJobsStorage")] string myQueueItem, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                   .SetBasePath(context.FunctionAppDirectory)
                   .AddJsonFile("appSettings.json", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables()
                   .Build();

            CoreFunction = new CoreFunction(config);

            var updateInfo = JsonConvert.DeserializeObject<DisplayUpdateInfo>(myQueueItem);

            try
            {
                var _c = CoreFunction.GetCompaniesByCurrentSlot().FirstOrDefault(x => x.CompanyID == updateInfo.CompanyID);
                var community = JobDbContextCreator.CreateCommunityContext(updateInfo.CompanyID, 0, _c.UrlPrefix, true, CoreFunction.GetConnectionString("CommunityContext"));

                using (var companyConnection = CompanyConnectionUtils.GetCompanyConnection(updateInfo.CompanyID, CoreFunction.GetConnectionString("CommunityContext")))
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
                    else if (assetTypeID > 0)
                    {
                        await companyConnection.ExecuteAsync("exec GenerateAssetTypeDisplayValues @assetTypeID", new { assetTypeID }, null, 2400);
                    }
                    else if (updateInfo.RebuildAll)
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
