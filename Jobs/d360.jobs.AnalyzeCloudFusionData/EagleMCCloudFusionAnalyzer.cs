using d360.core;
using d360.core.entities;
using d360.extensions;
using d360.jobs.AnalyzeCloudFusionData.eagle.messageCenter;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace d360.jobs.AnalyzeCloudFusionData
{
    

    public class StagingFile
    {
        public int ID { get; set; }
        public int FusionID { get; set; }
        public int FussionAttributeID { get; set; }
        public string File { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    public class StagingFileItem
    {
        public int StagingFileID { get; set; }
        public string Tag { get; set; }
        public string Value { get; set; }
    }

    public class EagleMCCloudFusionAnalyzer : FunctionsBase
    {
        static int EAGLE_MC_FUSION_TYPE = 17;
        static int MESSAGE_CENTER_FEED_FUSION_ATTRIBUTE_TYPE = 171;

        static string CLOUD_EXECUTION_TABLE = "[fusion].[StagingFile]";
        static string CLOUD_EXECUTION_JOB_DATA = "[fusion].[StagingFileItem]";
        

        public static void Analyze(int companyID)
        {
            IStorageProvider storageProvider = new d360.extensions.storage.AzureStorageProvider();
            // 1-  load any eagle message center streams that exist as fusion data
            var companyConnection = GetCompanyConnection(companyID);
             companyConnection.Open();
                        
            var streams = companyConnection.Query<FusionAttribute>("select * from fusionattribute where fusionattributetypeid = @t", new { t = MESSAGE_CENTER_FEED_FUSION_ATTRIBUTE_TYPE }).ToList();

            // if there are no streams we are done
            if (streams.Count == 0)
            {
                Console.WriteLine("No Eagle Message Center streams found for company id:{0}", companyID);

                return;
            }

            foreach (var stream in streams)
            {
                // get details about this stream
                // stream rule file name
                // get cloud fusion execution details for date last run
                var streamDetails = companyConnection.Query<dynamic>("select ft.name as name,f.value as value from field f inner join fieldtype ft on(f.fieldtypeid = ft.id) where ft.object = 'FusionAttributeType' and ft.objectID = @t and f.objectID = @f", new { t = MESSAGE_CENTER_FEED_FUSION_ATTRIBUTE_TYPE, f = stream.ID }).ToList();

                //file and directory should be listed as attributes here if not log an error and bail
                var fileName = streamDetails.Find(f => f.name == "file");
                var directoryName = streamDetails.Find(f => f.name == "directory");

                var file = fileName != null ? fileName.value : string.Empty;
                var directory = directoryName != null ? directoryName.value : string.Empty;

                if(string.IsNullOrEmpty(file) || string.IsNullOrEmpty(directory))
                {
                    Console.WriteLine("No file / directory fields found for Client:[{0}] Message Stream:[{1}]", companyID, stream.Name);

                    continue;
                }
                var formatName = streamDetails.Find(f => f.name == "format");
                var directionName = streamDetails.Find(f => f.name == "direction");

                var formatValue = formatName != null ? formatName.value : string.Empty;
                var directionValue = directionName != null ? directionName.value : string.Empty;

                // if it is not a bloomberg input stream ignore it             

                if (formatValue.ToUpper() != "BLOOMBERG" && directionValue.ToUpper() != "I")
                {
                    Console.WriteLine("Ignoring Message Stream:[{0}] Client:[{1}] Format:[{2}] Direction:[{3}]", stream.Name, companyID, formatName, directionName);

                    continue;
                }

                // 2 - go to storage and load those / check dates against last modified date stored in cloudfusion analyzer stats
                var cloudLastRunDetails = companyConnection.Query<StagingFile>("select * from " + CLOUD_EXECUTION_TABLE + " where [FusionID] = @t and [FusionAttributeID] = @s", new { t = EAGLE_MC_FUSION_TYPE, s = stream.ID }).FirstOrDefault();

                directory = directory.TrimStart('\\');
                directory = directory.ToLower();
                file = file.ToLower();

                var azureDirectory = companyID + "." + EAGLE_MC_FUSION_TYPE + "/" + directory.Replace("\\", "/");
                var azureFilePath = azureDirectory + file;

                DateTime lastModified = storageProvider.GetFileLastModifiedDate(constants.AZURE_CLOUD_FUSION_CONTAINER, azureFilePath);

                // 3 - if last modified date differs we need to analyze else continue
                if (cloudLastRunDetails != null && cloudLastRunDetails.UpdatedOn >= lastModified)
                {
                    Console.WriteLine("No changes made to stream:[{0}] client:[{1}] last scan update:[{2}] last file update:[{3}]", stream.Name, companyID, cloudLastRunDetails.UpdatedOn, lastModified);
                    //log the file has not changed and move on
                    continue;
                }

                // if we are here this is the first run for this stream or it has changed either way we need to load the file
                // go to azure an try to get the file and compare to above details

                Ruleset ruleFile = Ruleset.Load(storageProvider, azureDirectory, file);

                var relationships = ruleFile.FlattendMappings.OrderBy(x=>x.StarTag).ThenBy(x=>x.Target).ToList();

                Console.WriteLine("Loaded [{0}] message center relationships", relationships.Count);
                                
                // 4 - compare relationships now to prior run
                // 5 - any removed need to be marked as such
                // 6 - any added need to be added
                bool bHasDifferences = false;
                if (cloudLastRunDetails != null)
                {
                    var priorRunItems = companyConnection.Query<StagingFileItem>("select * from " + CLOUD_EXECUTION_JOB_DATA  + " where [StagingFileID] = @jID order by tag, value", new { jID = stream.ID }).ToList();

                    List<GenericRelationship> oldRelationships = priorRunItems.Select(x => new GenericRelationship { Change = ChangeType.Delete, StarTag = x.Tag, Target = x.Value }).ToList();

                    /// <todo> lets avoid n^2 merge!</todo>
                    // lets avoid n^2
                    // calc diffs between the two
                    // two pointers one for old and one for new.  Both lists are assumed to be ordered lists ordered by the tag
                    foreach (var item in relationships)
                    {
                        //if the item doesnt exist in oldrelations it is new
                        //var old = oldRelationships.Find(x => x.StarTag == item.StarTag && x.Target == item.Target);
                        int index = oldRelationships.BinarySearch(item);

                        if(index < 0)
                        {
                            item.Change = ChangeType.Add;
                            bHasDifferences = true;
                            Console.WriteLine("Found difference of type add tag[{0}] value[{1}]", item.StarTag, item.Target);
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
                        Console.WriteLine("Found difference of type delete");

                        bHasDifferences = true;
                    }

                    relationships.AddRange(deletedRelations); // add back in deleted                    
                }
                else
                {
                    Console.WriteLine("First run all relationships being added as new");

                    bHasDifferences = true;

                    relationships.Select(c => { c.Change = ChangeType.Add; return c; }).ToList();
                }

                // 7 - log an entry in the cloud fusion status table with all the items we found this run               

                if(bHasDifferences)
                {
                    int newCloudJobId = 0;
                    //insert execution details on a transaction for consistency
                    using (var trans = companyConnection.BeginTransaction())
                    {
                        companyConnection.Execute("delete from " + CLOUD_EXECUTION_TABLE + " where [FusionID] = @i and [FusionAttributeID] = @f", new { i = EAGLE_MC_FUSION_TYPE, f= stream.ID}, trans, 500);

                        if (cloudLastRunDetails != null)
                        {                            
                            companyConnection.Execute("delete from " + CLOUD_EXECUTION_JOB_DATA + " where StagingFileID = @id", new { id = cloudLastRunDetails.ID }, trans, 500);
                        }

                        companyConnection.Execute("insert into " + CLOUD_EXECUTION_TABLE + " ([FusionID], [FusionAttributeID], [File],[UpdatedOn]) values(@fus,@objID, @f, @now)", new { fus = EAGLE_MC_FUSION_TYPE, objID = stream.ID, f = file, now = DateTime.UtcNow }, trans);

                        newCloudJobId = companyConnection.Query<int>("select ID from " + CLOUD_EXECUTION_TABLE + " where FusionID = @f and FusionAttributeID = @o", new { f = EAGLE_MC_FUSION_TYPE, o = stream.ID }, trans).FirstOrDefault();

                        // insert them into cloudfusion tables
                        foreach (var item in relationships)
                        {
                            companyConnection.Execute("insert into " + CLOUD_EXECUTION_JOB_DATA + " values(@id,@tag,@value,@c)", new { id = newCloudJobId, tag = item.StarTag, value = item.Target, c = item.Change }, trans);                            
                        }
                        trans.Commit();
                    }

                    // 8 - handle updates to intersects for these differences need to look up db columns from star tag
                    //    need to look up bloomberg nmeonic  
                    // fire off proc to do this
                    if (newCloudJobId > 0)
                        companyConnection.Execute("EXEC fusion.ProcessEagleMCToBloombergRelations @id,@fId", new { id = newCloudJobId, fId  = EAGLE_MC_FUSION_TYPE}, null, 7200);
                    else
                        Console.WriteLine("UNABLE TO CALL PROCESSEAGLETOBBRELATIONS PROC DUE TO MISSING ID FROM STAGE FILE TABLE.");
                }

            }

        }
    }
}
