using d360.core;
using d360.core.entities;
using d360.extensions;
using d360.extensions.storage;
using d360.utils.company;
using Dapper;
using igx.functions.fusion.Connector.Eagle;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace igx.functions
{
    public static class ProcessEagleMessageStreams
    {
        #region Field values

        static int EAGLE_MC_FUSION_TYPE = 17;
        static int MESSAGE_CENTER_FEED_FUSION_ATTRIBUTE_TYPE = 196;

        static string CLOUD_EXECUTION_TABLE = "[fusion].[StagingFile]";
        static string CLOUD_EXECUTION_JOB_DATA = "[fusion].[StagingFileItem]";

        #endregion

        const string functionName = "ProcessEagleMessageStreams";
        const string timerSettings = "0 0 1 * * *";
        //const string timerSettings = "*/10 * * * * *";

        [FunctionName(functionName)]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log) //   
        {
            // https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#schedule-examples

            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();

                companies.ForEach(c =>
                {
                    try
                    {
                        var storageProvider = new AzureStorageProvider();
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        company.OpenWithRetry(RetryPolicy.DefaultProgressive);

                        var streams = company.Query<FusionAttribute>("select * from fusionattribute where fusionattributetypeid = @t", new { t = MESSAGE_CENTER_FEED_FUSION_ATTRIBUTE_TYPE }).ToList();

                        foreach (var stream in streams)
                        {
                            AnalyzeStream(c.CompanyID, company, stream, storageProvider, log);
                        }
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }

                });

                CoreFunction.AITrackJobCompletedNoErrors(functionName);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }

        private static void AnalyzeStream(int companyID, SqlConnection companyConnection, FusionAttribute stream, IStorageProvider storageProvider, TraceWriter log)
        {
            // get details about this stream
            // stream rule file name
            // get cloud fusion execution details for date last run
            var streamDetails = companyConnection.Query<dynamic>("select ft.name as name,f.value as value from field f inner join fieldtype ft on(f.fieldtypeid = ft.id) where ft.object = 'FusionAttributeType' and ft.objectID = @t and f.objectID = @f", new { t = MESSAGE_CENTER_FEED_FUSION_ATTRIBUTE_TYPE, f = stream.ID }).ToList();

            //file and directory should be listed as attributes here if not log an error and bail
            var fileName = streamDetails.Where(s => String.Equals(s.name, "file", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            var directoryName = streamDetails.Where(s => String.Equals(s.name, "directory", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            var file = fileName != null ? fileName.value : string.Empty;
            var directory = directoryName != null ? directoryName.value : string.Empty;

            if (string.IsNullOrEmpty(file) || string.IsNullOrEmpty(directory))
            {
                log.Error($"No file / directory fields found for Client:[{companyID}] Message Stream:[{stream.Name}]");

                return;
            }

            var formatName = streamDetails.Where(s => String.Equals(s.name, "format", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            var directionName = streamDetails.Where(s => String.Equals(s.name, "direction", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            var formatValue = formatName != null ? formatName.value : string.Empty;
            var directionValue = directionName != null ? directionName.value : string.Empty;

            log.Error($"Checking stream direction and type for, company: {companyID}, stream: {stream.ID}.");

            // if it is not a bloomberg input stream ignore it             

            if ((formatValue.ToUpper() != "BLOOMBERG" && formatValue.ToUpper() != "CSV") || directionValue.ToUpper() != "I")
            {
                log.Error($"Ignoring Message Stream:[{stream.Name}] Client:[{companyID}] Format:[{formatValue}] Direction:[{directionValue}]");

                return;
            }

            log.Error($"Loading last run stats from db, company: {companyID}, stream: {stream.ID}.");

            // 2 - go to storage and load those / check dates against last modified date stored in cloudfusion analyzer stats
            var cloudLastRunDetails = companyConnection.Query<StagingFile>("select * from " + CLOUD_EXECUTION_TABLE + " where [FusionID] = @t and [FusionAttributeID] = @s", new { t = EAGLE_MC_FUSION_TYPE, s = stream.ID }).FirstOrDefault();

            directory = directory.TrimStart('\\');
            directory = directory.ToLower();
            file = file.ToLower();

            string azureDirectory = companyID + "." + EAGLE_MC_FUSION_TYPE + "/" + directory.Replace("\\", "/");

            if (!azureDirectory.EndsWith("/")) azureDirectory += '/';
            var azureFilePath = azureDirectory + file;

            log.Error($"Getting stream last modifieddate for company: {companyID}, stream: {stream.ID}.");

            DateTime lastModified = DateTime.MinValue;

            try
            {
                lastModified = storageProvider.GetFileLastModifiedDate(constants.AZURE_CLOUD_FUSION_CONTAINER, azureFilePath);
            }
            catch (Exception ex)
            {
                log.Error($"ERROR WHILE LOADING LAST MODIFIED INFO.  INGORMING STREAM {ex.Message}");

                return;
            }


            // 3 - if last modified date differs we need to analyze else continue
            if (cloudLastRunDetails != null && cloudLastRunDetails.UpdatedOn >= lastModified)
            {
                log.Error($"No changes made to stream:[{stream.Name}] client:[{companyID}] last scan update:[{cloudLastRunDetails.UpdatedOn}] last file update:[{lastModified}]");
                //log the file has not changed and move on
                return;
            }

            log.Error($"Interpreting ruleset  company: {companyID}, ruleset: {file}, directory: {azureDirectory}.");
            // if we are here this is the first run for this stream or it has changed either way we need to load the file
            // go to azure an try to get the file and compare to above details

            Ruleset ruleFile = null;

            try
            {
                ruleFile = Ruleset.Load(storageProvider, azureDirectory, file);
            }
            catch (Exception ex)
            {
                log.Error($"ERROR OCCURRED WHILE ANALYZING FILE [{file}] COMPANY [{companyID}] MESSAGE [{ex.Message}]");

                return;
            }

            if (ruleFile == null)
            {
                log.Error($"INVALID RULEFILE RESULTED FROM ANALYSIS. FOR FILE [{file}] COMPANY [{companyID}]");

                return;
            }


            var relationships = ruleFile.FlattendMappings.OrderBy(x => x.StarTag).ThenBy(x => x.Target).ToList();

            log.Info($"Loaded [{relationships.Count}] message center relationships");

            // 4 - compare relationships now to prior run
            // 5 - any removed need to be marked as such
            // 6 - any added need to be added
            bool bHasDifferences = false;
            if (cloudLastRunDetails != null)
            {
                var priorRunItems = companyConnection.Query<StagingFileItem>("select * from " + CLOUD_EXECUTION_JOB_DATA + " where [StagingFileID] = @jID order by tag, value", new { jID = cloudLastRunDetails.ID }).ToList();

                var oldRelationships = priorRunItems.Select(x => new GenericRelationship { Change = ChangeType.Delete, StarTag = x.Tag, Target = x.Value }).ToList();

                /// <todo> lets avoid n^2 merge!</todo>
                // lets avoid n^2
                // calc diffs between the two
                // two pointers one for old and one for new.  Both lists are assumed to be ordered lists ordered by the tag
                foreach (var item in relationships)
                {
                    //if the item doesnt exist in oldrelations it is new
                    //var old = oldRelationships.Find(x => x.StarTag == item.StarTag && x.Target == item.Target);
                    int index = oldRelationships.BinarySearch(item);

                    if (index < 0)
                    {
                        item.Change = ChangeType.Add;
                        bHasDifferences = true;
                        log.Info($"Found difference of type add tag[{item.StarTag}] value[{item.Target}]");
                    }
                    else
                    {
                        item.Change = ChangeType.None;
                        oldRelationships[index].Change = ChangeType.None; // mark prior so we know later it wasnt a delete
                    }
                }

                //any items that are in oldRelations list that have changetype of deleted

                List<GenericRelationship> deletedRelations = oldRelationships.Where(x => x.Change == ChangeType.Delete).ToList();

                if (deletedRelations.Count > 0)
                {
                    log.Info("Found difference of type delete");

                    bHasDifferences = true;

                    relationships.AddRange(deletedRelations); // add back in deleted               
                }
            }
            else
            {
                log.Info("First run all relationships being added as new");

                bHasDifferences = true;

                relationships.Select(c => { c.Change = ChangeType.Add; return c; }).ToList();
            }

            // 7 - log an entry in the cloud fusion status table with all the items we found this run               

            if (!bHasDifferences)
            {
                log.Info("Stream file was modified however no mapping differences were detected.  This could happen if a new version of the file was uploaded to the cloud storage with no substantial changes or changes to elements that do not impact relationships.");

                return;
            }


            if (bHasDifferences)
            {
                int newCloudJobId = 0;
                //insert execution details on a transaction for consistency
                using (var trans = companyConnection.BeginTransaction())
                {
                    companyConnection.Execute("delete from " + CLOUD_EXECUTION_TABLE + " where [FusionID] = @i and [FusionAttributeID] = @f", new { i = EAGLE_MC_FUSION_TYPE, f = stream.ID }, trans, 500);

                    if (cloudLastRunDetails != null)
                    {
                        companyConnection.Execute("delete from " + CLOUD_EXECUTION_JOB_DATA + " where StagingFileID = @id", new { id = cloudLastRunDetails.ID }, trans, 500);
                    }

                    companyConnection.Execute("insert into " + CLOUD_EXECUTION_TABLE + " ([FusionID], [FusionAttributeID], [File],[UpdatedOn]) values(@fus,@objID, @f, @now)", new { fus = EAGLE_MC_FUSION_TYPE, objID = stream.ID, f = file, now = DateTime.UtcNow }, trans);

                    newCloudJobId = companyConnection.Query<int>("select ID from " + CLOUD_EXECUTION_TABLE + " where FusionID = @f and FusionAttributeID = @o", new { f = EAGLE_MC_FUSION_TYPE, o = stream.ID }, trans).FirstOrDefault();
                    List<StagingFileItem> stagingItems = new List<StagingFileItem>();

                    // insert them into cloudfusion tables
                    foreach (var item in relationships)
                    {
                        stagingItems.Add(new StagingFileItem { ChangeType = item.Change, Tag = item.StarTag, Value = item.Target, StagingFileID = newCloudJobId, Description = item.Raw });
                    }

                    companyConnection.Execute("insert into " + CLOUD_EXECUTION_JOB_DATA + " ([StagingFileID],[Tag],[Value],[ChangeType],[Description]) values(@StagingFileID,@Tag,@Value,@ChangeType,@Description)", stagingItems, trans);

                    trans.Commit();
                }

                log.Info($"Calling proc fusion.ProcessEagleMCToBloombergRelations for company: {companyID}, ruleset: {file}, directory: {azureDirectory}.");
                // 8 - handle updates to intersects for these differences need to look up db columns from star tag
                //    need to look up bloomberg nmeonic  
                // fire off proc to do this
                if (newCloudJobId > 0)
                    companyConnection.Execute("EXEC fusion.ProcessEagleMCToBloombergRelations @id,@fId", new { id = newCloudJobId, fId = EAGLE_MC_FUSION_TYPE }, null, 7200);
                else
                    log.Error("UNABLE TO CALL PROCESSEAGLETOBBRELATIONS PROC DUE TO MISSING ID FROM STAGE FILE TABLE.");
            }
        }
    }
}
