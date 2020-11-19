using d360.core;
using d360.core.queue;
using d360.core.enums;
using d360.extensions.search;
using d360.extensions.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using d360.core.entities;
using d360.extensions.info;
using d360.extensions.caching;
using d360.model;

namespace igx.jobs.indexer
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();

            /*
             * Timer execution disabled (GOV-10646 - Lower / Stop rebuild of search indexes every weekend)
             * To enable, uncomment the following line
             */
            //config.UseTimers();

            //We should only process one reindex queue item at a time
            config.Queues.BatchSize = 1;

#if DEBUG
            config.UseDevelopmentSettings();
#endif

            System.Net.ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    internal interface IPagedQuerySqlModel
    {
        long AssetID { get; set; }
    }

    internal class FieldSqlModel : IPagedQuerySqlModel
    {
        public long AssetID { get; set; }
        public string Name { get; set; }
        public string FormattedValue { get; set; }
    }

    internal class TagSqlModel : IPagedQuerySqlModel
    {
        public long AssetID { get; set; }
        public Guid AssetUID { get; set; }
        public Guid TagUID { get; set; }
        public string Value { get; set; }
    }

    internal class ResponsibilitySqlModel : IPagedQuerySqlModel
    {
        public long AssetID { get; set; }
        public string SecurityAsset { get; set; }
        public int SecurityAssetID { get; set; }
    }

    public static class Indexer
    {
        private static int _defaultQueryCommandTimeout = 180;
        const string functionName = "Indexing_ReIndex";
#if DEBUG
        const string timerSettings = "0 */15 * * * *";
#else
        const string timerSettings = "0 0 17 * * 6";
#endif

        const string fieldsSql = @"select F.AssetID, T.Name, F.FormattedValue from Field F " +
            " inner join FieldType T on T.ID = F.FieldTypeID and F.ObjectType = @t and F.FormattedValue is not null and F.FormattedValue <> '' and " +
            " T.[Type] not in('DateTime','Color','FilteredLookup','ComplexRelationLookup','OwnershipLookup','Relationship','FieldFromRelationship','RefListRelationship','JSON')";
        const string tagsSql = @"SELECT a.ID as AssetID, a.uid AS AssetUID, t.uid AS TagUID, t.Value FROM [dbo].[AssetTag] at " +
            "INNER JOIN [dbo].[Tag] t ON at.TagID = t.ID INNER JOIN [dbo].[Asset] a ON at.AssetID = a.ID";

        public static void RunViaTimer([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.UpdateRebuildRequestByCurrentSlot(CompanyRebuildJobToken.SearchIndex);

                var queue = new AzureQueueSource();
                companies.ForEach(c =>
                {
                    queue.CreateMessage(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel { CompanyID = c });
                });

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

            CoreFunction.AIFlush();
        }

        public static async Task RunViaQueue([QueueTrigger("%SearchIndexQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
        {
            var c = JsonConvert.DeserializeObject<ReindexModel>(myQueueItem);

            try
            {
                var source = new ElasticSearchSource();
                using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID))
                {
                    await ProcessCompany(source, company, c);
                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }
        }

        public static async Task ProcessCompanyDeprecated(ElasticSearchSource source, SqlConnection company, ReindexModel c)
        {
            IEnumerable<IndexObjectModel> models = null;

            company.Open();
            List<CompanySetting> settings = CompanyConnectionUtils.GetCompanySettings(c.CompanyID);
            bool fusionEnabled = (settings.Any(i => i.SettingID == 70) ? bool.Parse(settings.Single(i => i.SettingID == 70).Value) : true);

            int SuggestedIndexLimit = SuggestIndexLimit(company);
            if (SuggestedIndexLimit > 1000)
            {
                source.IndexFieldLimit = SuggestedIndexLimit;
            }

            source.ClearIndex(c.CompanyID);

            LogReindexStart("BusinessAssets", c.CompanyID);

            try
            {
                models = LoadArtifacts(company, c.CompanyID, source, AssetTypeClass.BusinessAsset);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            LogReindexStart("TechnicalAssets", c.CompanyID);

            try
            {
                models = LoadArtifacts(company, c.CompanyID, source, AssetTypeClass.TechnicalAsset);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            LogReindexStart("Models", c.CompanyID);

            try
            {
                models = LoadModels(company, c.CompanyID, source);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            LogReindexStart("Policies", c.CompanyID);

            try
            {
                models = LoadPolicies(company, c.CompanyID, source);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            if (fusionEnabled)
            {
                LogReindexStart("Fusion Types", c.CompanyID);

                try
                {
                    models = LoadFusionTypes(company, c.CompanyID, source);
                    source.AddToIndex(models);
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                }
            }

            LogReindexStart("Reference Item Types", c.CompanyID);

            try
            {
                models = LoadReferenceItemTypes(company, c.CompanyID, source);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            LogReindexStart("Groups", c.CompanyID);

            try
            {
                models = LoadGroups(company, c.CompanyID, source);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            LogReindexStart("Rules", c.CompanyID);

            try
            {
                models = LoadRules(company, c.CompanyID, source);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            if (fusionEnabled)
            {
                LogReindexStart("FusionAttributes", c.CompanyID);

                try
                {
                    models = LoadFusionAttributes(company, c.CompanyID, source);
                    source.AddToIndex(models);
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                }
            }

            LogReindexStart("Artifact Synonyms", c.CompanyID);

            try
            {
                models = LoadArtifactSynonyms(company, c.CompanyID, source);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            LogReindexStart("Custom Synonyms", c.CompanyID);

            try
            {
                models = LoadCustomSynonyms(company, c.CompanyID, source);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            LogReindexStart("Users", c.CompanyID);

            try
            {
                models = LoadUsers(company, c.CompanyID, source);
                source.AddToIndex(models);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            await LogCompanyReindexComplete(c.CompanyID);
        }

        public static async Task ProcessCompany(ElasticSearchSource source, SqlConnection company, ReindexModel c)
        {
            company.Open();
            List<CompanySetting> settings = CompanyConnectionUtils.GetCompanySettings(c.CompanyID);
            bool fusionEnabled = (settings.Any(i => i.SettingID == 70) ? bool.Parse(settings.Single(i => i.SettingID == 70).Value) : true);

            int SuggestedIndexLimit = SuggestIndexLimit(company);
            if (SuggestedIndexLimit > 1000)
            {
                source.IndexFieldLimit = SuggestedIndexLimit;
            }

            List<AssetTypeClass> classes = new List<AssetTypeClass> {
                AssetTypeClass.BusinessAsset,
                AssetTypeClass.TechnicalAsset,
                AssetTypeClass.Model,
                AssetTypeClass.Policy,
                AssetTypeClass.Rule,
                AssetTypeClass.ReferenceItemType,
                AssetTypeClass.Group,
                AssetTypeClass.User
            };

            if(fusionEnabled)
            {
                classes.Add(AssetTypeClass.Fusion);
                classes.Add(AssetTypeClass.FusionAttribute);
            }
            
            SearchIndexer indexer = new SearchIndexer(company, c.CompanyID, source);
            source.ClearIndex(c.CompanyID);

            classes.ForEach(cls => {
                LogReindexStart(cls.ToString(), c.CompanyID);
                try
                {
                    indexer.IndexAssetClass(cls);
                }
                catch (Exception ex)
                {
                    CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                }

            });


            LogReindexStart("Artifact Synonyms", c.CompanyID);
            try
            {
                indexer.IndexObjectType("Intersect", false);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            LogReindexStart("Custom Synonyms", c.CompanyID);
            try
            {
                indexer.IndexObjectType("Synonym", false);
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }

            await LogCompanyReindexComplete(c.CompanyID);
        }

        private static async Task LogCompanyReindexComplete(int companyID)
        {
            CoreFunction.AITrackTrace(functionName, $"Completed reindex for company {companyID}", companyId: companyID);

            #region Create EF connection

            var _c = CoreFunction.GetCompaniesByCurrentSlot()
                .FirstOrDefault(x => x.CompanyID == companyID);

            var sec = new UriSecurityContextProvider()
            {
                CompanyID = companyID,
                ResourceID = 0,
                CompanyPrefix = _c.UrlPrefix,
                IsAdministrator = true
            };
            var cache = new DummyCachingProvider();
            var queue = new AzureQueueSource();
            var community = new CommunityContext(cache, queue, sec);

            #endregion

            await community.UpdateRebuildJobStatus(CompanyRebuildJobToken.SearchIndex, CompanyRebuildJobStatusState.Inactive);
        }

        private static void LogReindexStart(string typeName, int companyID)
        {
            CoreFunction.AITrackTrace(functionName, $"Starting {typeName} reindex for company {companyID}", companyId: companyID);
        }


        #region Supporting Functions

        private static int SuggestIndexLimit(SqlConnection context) {
            var sql = "SELECT COUNT(*) FROM [dbo].[FieldType]";
            int FieldTypeCount = context.Query<int>(sql).FirstOrDefault();
            double MultiplicationFactor = 1.2;
            return Convert.ToInt16(FieldTypeCount * MultiplicationFactor);
        }

        private static IEnumerable<IndexObjectModel> LoadArtifactSynonyms(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            /* Intersect Synonym query columns:
             *  ID
             *  Direction
             *  Synonym
             *  SynonymObjectType
             *  SynonymObjectID
             *  SynonymAssetID
             *  SynonymFor
             *  SynonymForObject
             *  SynonymForObjectID
             *  Url
             *  SynonymForObjectType
             *  PredicateName
             */
            var sql = ElasticSearchSource.INTERSECT_SYNONYM_QUERY + " order by SynonymFor";

            return getData(context, sql, companyID, source, "", false, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = "Synonym",
                    CompanyID = companyID,
                    AssetType = "Synonym",
                    ItemUniqueID = $"intersect|{o.ID}|{o.Direction}",
                    RelativeUrl = o.Url,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Synonym },
                        { "NymType", o.PredicateName },
                        { "SynonymFor", o.SynonymFor },
                        { "SynonymForObject", o.SynonymForObject },
                        { "SynonymForObjectType", o.SynonymForObjectType }
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> LoadCustomSynonyms(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"
select 
	s.Name as 'Synonym'
	,d.DisplayValue as 'SynonymFor'
	,s.[Object] as 'SynonymForObject'
	,s.[ObjectID] as 'SynonymForObjectID'
	,dbo.GenerateAssetUrl(a.ID) as 'Url'
	,t.Name as 'SynonymForObjectType'	
    ,p.Name as 'PredicateName'    
    ,s.ID as 'ID'                
from
	[dbo].[nym] s
    inner join [dbo].Asset a on a.object = s.object and a.objectid = s.objectid
	inner join [dbo].AssetType t on a.assettypeid = t.id
	inner join [dbo].AssetDisplayValue d on d.assetid = a.id
    inner join [dbo].[predicate] p on (s.predicateid = p.id)";

            return getData(context, sql, companyID, source, "", false, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = "Synonym",
                    CompanyID = companyID,
                    AssetType = "Synonym",
                    ItemUniqueID = $"custom|{o.ID}",
                    RelativeUrl = o.Url,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Synonym },
                        { "NymType", o.PredicateName },
                        { "SynonymFor", o.SynonymFor },
                        { "SynonymForObject", o.SynonymForObject },
                        { "SynonymForObjectType", o.SynonymForObjectType }
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> LoadFusionAttributes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"select
	                        f.ID,
	                        f.Name,
	                        f.FusionAttributeTypeID,
	                        ft.Name as FusionAttributeTypeName,
	                        fu.Name as FusionName,
							a.id as AssetID,
                            dbo.GenerateAssetUrl(a.id) as 'Url'
                        from fusionattribute f
	                        inner join fusionattributetype ft on (f.fusionattributetypeid = ft.id)
	                        inner join fusion fu on (f.fusionid = fu.id)
                            inner join asset a on a.object = 'FusionAttribute' and f.id = a.objectid
                        where f.Deleted = 0";

            foreach (var a in context.Query(sql, new { compid = companyID }, buffered:false,commandTimeout: _defaultQueryCommandTimeout))
            {
                var item = new IndexObjectModel
                {
                    Category = "FusionAttributes",
                    CompanyID = companyID,
                    AssetType = $"{a.FusionName} {a.FusionAttributeTypeName}",
                    ID = a.ID,
                    AssetID = a.AssetID,
                    RelativeUrl = a.Url,
                    Fields = new Dictionary<string, string>() {
                        { "Name", a.Name }
                    }
                };                

                yield return item;
            }
        }
        
        private static IEnumerable<IndexObjectModel> LoadRules(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            int assettypeclass = (int)AssetTypeClass.Rule;
            var sql = $@"SELECT
                    A.ID as AssetID,
                    A.ObjectID as ID,
                    D.DisplayValue as Name,
                    T.Name as RuleType,
                    T.uid as AssetTypeUid,
                    a.uid as Uid,
                    [dbo].GenerateAssetUrl(A.ID) as [Url]
                FROM [dbo].[Asset] A
				INNER JOIN [dbo].AssetType T on A.AssetTypeID = T.id
				INNER JOIN [dbo].AssetDisplayValue D on D.AssetID = A.ID
	                WHERE T.Class = {assettypeclass.ToString()}
                AND A.State = 1
                ORDER BY A.ID";

            var sType = SystemObjects.Rule.ToString();

            return getData(context, sql, companyID, source, sType, true, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    AssetID = o.AssetID,
                    AssetType = o.RuleType,
                    RelativeUrl = o.Url,
                    Uid = o.Uid,
                    AssetTypeUid = o.AssetTypeUid,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "Description", o.Description }
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> LoadGroups(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"SELECT
                        g.[ID],
                        g.[Name],
                        g.[Description],
                        a.ID as AssetID,
                        dbo.GenerateAssetUrl(a.ID) as 'Url'
                    FROM [Group] g
                    INNER JOIN [Asset] a ON a.[Object] = 'Group' AND a.ObjectID = g.ID";

            var sType = "Group";
            return getData(context, sql, companyID, source, sType, false, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    AssetType = sType,
                    AssetID = o.AssetID,
                    RelativeUrl = o.Url,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "Description", o.Description }
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> LoadUsers(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"SELECT ResourceID, Email AS Username, LastName, FirstName, Email,
                        CASE
                        WHEN Email not like '%@data3sixty.com' and Email not like '%@infogix.com'
                            THEN '0'
                            ELSE '1'
                        END as Data3SixtyUser
                        FROM reporting.global_resource";

            var sType = "Resource";
            return getData(context, sql, companyID, source, sType, false, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = sType,
                    CompanyID = companyID,
                    AssetType = "User",
                    ID = o.ResourceID,
                    RelativeUrl = $"resource/{o.ResourceID}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", $"{o.FirstName} {o.LastName}" },
                        { "Email", o.Email },
                        { "Username", o.Username },
                        { "Data3SixtyUser", o.Data3SixtyUser },
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> LoadReferenceItemTypes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            int assettypeclass = (int)AssetTypeClass.Reference;
            var sql = $@"SELECT
                    ObjectID as ID,
                    Name,
                    Description,
                    uid as AssetTypeUid
                FROM [dbo].[AssetType]
                WHERE Class = {assettypeclass.ToString()}
                AND State = 1";
            var sType = "Reference";
            return getData(context, sql, companyID, source, sType, false, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    AssetType = "Reference List",
                    RelativeUrl = $"reference/{o.ID}",
                    AssetTypeUid = o.AssetTypeUid,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "Description", o.Description }
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> LoadPolicies(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            int assettypeclass = (int)AssetTypeClass.Policy;
            var sql = $@"SELECT
	                A.ID as AssetID,
	                A.ObjectID as ID,
	                D.DisplayValue as [Name],
	                D.DisplayValue as TextPath,
	                T.Name as PolicyType,
                    a.uid as Uid,
                    T.uid as AssetTypeUid,
	                [dbo].GenerateAssetUrl(A.ID) as [Url]
                FROM [dbo].[Asset] A
                INNER JOIN [dbo].AssetType T on A.AssetTypeID = T.id
				INNER JOIN [dbo].AssetDisplayValue D on D.AssetID = A.ID
                WHERE T.Class = {assettypeclass.ToString()}
                AND A.State = 1
                ORDER BY A.ID";

            var sType = SystemObjects.Policy.ToString();

            return getData(context, sql, companyID, source, sType, true, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    AssetID = o.AssetID,
                    AssetType = o.PolicyType,
                    RelativeUrl = o.Url,
                    Uid = o.Uid,
                    AssetTypeUid = o.AssetTypeUid,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "TextPath", o.TextPath ?? "" }
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> LoadArtifacts(SqlConnection context, int companyID, ElasticSearchSource source, AssetTypeClass ArtifactClass)
        {
            int assettypeclass = (int)ArtifactClass;
            var sql = $@"
select
    A.ID as AssetID,
	cast(A.ID as varchar) as ItemUniqueID,
	A.ObjectID as ID,
	att.ObjectID as TypeID,
	adv.DisplayValue,
	att.Name as TypeName,
    att.uid as AssetTypeUid,
	a.uid as Uid,
    dbo.GenerateAssetUrl(a.ID) as 'Url'
from
	[dbo].Asset a
	inner join [dbo].assettype att on a.assettypeid = att.id
	inner join [dbo].assetdisplayvalue adv on adv.assetid = a.id
where
	att.[Object] = 'ArtifactType' and a.[state] = 1 and att.[Class] = {assettypeclass.ToString()}
ORDER BY A.ID";

            var sType = ArtifactClass.ToString();

            return getData(context, sql, companyID, source, SystemObjects.Artifact.ToString(), true, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    AssetID = o.AssetID,
                    ItemUniqueID = o.ItemUniqueID,
                    AssetType = o.TypeName,
                    RelativeUrl = o.Url,
                    Uid = o.Uid,
                    AssetTypeUid = o.AssetTypeUid,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.DisplayValue },
                        { "Description", "" },
                        { "Status", "Active" },
                        { "Taxonomy", "" }
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> LoadModels(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"
SELECT	A.ID as AssetID,
        A.ObjectID as ID,
		T.ObjectID as TypeID,
		D.DisplayValue,
		T.Name as TypeName,
        T.uid as AssetTypeUid,
		A.uid as Uid,
        dbo.GenerateAssetUrl(a.ID) as 'Url'
FROM	[dbo].Asset A
		INNER JOIN [dbo].AssetType T on A.AssetTypeID = T.id
		INNER JOIN [dbo].AssetDisplayValue D on D.AssetID = A.ID
WHERE	T.Object = 'TaxonomyType'
		and A.State = 1
ORDER BY A.ID";


            var sType = SystemObjects.Taxonomy.ToString();

            return getData(context, sql, companyID, source, sType, true, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    AssetID = o.AssetID,
                    AssetType = o.TypeName,
                    RelativeUrl = o.Url,
                    Uid = o.Uid,
                    AssetTypeUid = o.AssetTypeUid,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.DisplayValue },
                        { "Description", "" },
                        { "TextPath", o.DisplayValue ?? "" }
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> LoadFusionTypes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"
select  f.id as ID,
	    f.Name as FusionName,
	    f.Description as FusionDescription,			                                                
	    ft.Name as FusionTypeName,
	    ft.Description as FusionTypeDescription,
        ft.ID as FusionTypeID
from    fusion f		                                                
        inner join fusiontype ft on f.fusiontypeid = ft.id";

            var sType = SystemObjects.FusionType.ToString();
            return getData(context, sql, companyID, source, sType, false, (dynamic o) =>
            {
                return new IndexObjectModel
                {
                    Category = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    AssetType = o.FusionTypeName,
                    RelativeUrl = $"fusion/{o.ID}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.FusionName },
                        { "Description", o.FusionDescription }
                    }
                };
            });
        }

        private static IEnumerable<IndexObjectModel> getData(SqlConnection context, string sql, int companyID, ElasticSearchSource source, string type, bool loadFields, Func<dynamic, IndexObjectModel> convertToDictionary)
        {
            if (loadFields)
            {
                return getDataWithFields(context, sql, companyID, source, type, convertToDictionary);
            }
            
            return getDataWithoutFields(context, sql, companyID, source, type, convertToDictionary);            
        }

        private static IEnumerable<IndexObjectModel> getDataWithoutFields(SqlConnection context, string sql, int companyID, ElasticSearchSource source, string type, Func<dynamic, IndexObjectModel> convertToDictionary)
        {
            return context.Query(sql, commandTimeout: _defaultQueryCommandTimeout, buffered:false).ToList().Select(a => (IndexObjectModel)convertToDictionary(a));
        }

        private static IEnumerable<IndexObjectModel> getDataWithFields(SqlConnection context, string sql, int companyID, ElasticSearchSource source, string type, Func<dynamic, IndexObjectModel> convertToDictionary)
        {
            var FieldQuery = new PagedQuery<FieldSqlModel>(context, fieldsSql, new { t = type });
            var TagsQuery = new PagedQuery<TagSqlModel>(context, tagsSql);
            var ResponsibilityQuery = new PagedQuery<ResponsibilitySqlModel>(context, ElasticSearchSource.GetAssetResponsibilityQuery());
            var list = getDataWithoutFields(context, sql, companyID, source, type, convertToDictionary);

            foreach (var item in list)
            {
                var subset = FieldQuery.GetByAssetID(item.AssetID);
                foreach (var f in subset)
                {
                    item.Fields[f.Name] = f.FormattedValue;
                }
                if(item.Uid.HasValue && item.Uid != Guid.Empty)
                {
                    item.Tags = TagsQuery.GetByAssetID(item.AssetID).ToDictionary(x => x.TagUID.ToString(), x => x.Value);
                }
                var secset = ResponsibilityQuery.GetByAssetID(item.AssetID);
                item.NoRead = new Dictionary<string, List<int>> {
                    { "R" , secset.Where(r => r.SecurityAsset == "R").Select(r => r.SecurityAssetID).ToList() },
                    { "G" , secset.Where(r => r.SecurityAsset == "G").Select(r => r.SecurityAssetID).ToList() },
                    { "O" , secset.Where(r => r.SecurityAsset == "O").Select(r => r.SecurityAssetID).ToList() }
                };
                yield return item;
            }
        }

        #endregion
    }

    internal interface IPagedQuery<T>
    {
        List<T> GetByAssetID(long AssetID);
    }
    internal class PagedQuery<T> : IPagedQuery<T> where T : IPagedQuerySqlModel
    {
        private static readonly int PageSize = 50000;
        private long CurrentHighID = 0;
        private List<T> _data;
        private SqlConnection _connection;
        private readonly string _query;
        public DynamicParameters _param;
        private bool LastPage = false;
        private static readonly int _defaultQueryCommandTimeout = 180;

        /// <summary>
        /// Performs a paged/chunked query
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="query">Query string</param>
        /// <param name="param"></param>
        public PagedQuery(SqlConnection connection, string query, object param = null)
        {
            _connection = connection;
            _query = "SELECT TOP (@PageSize) pagedquery.* FROM (" + query + ") pagedquery WHERE pagedquery.AssetID >= @AssetID ORDER BY pagedquery.AssetID"; ;
            _param = new DynamicParameters();
            if(param != null)
            {
                foreach(var p in param.GetType().GetProperties())
                {
                    _param.Add(p.Name, p.GetValue(param, null));
                }
            }
            _data = new List<T>();
        }

        /// <summary>
        /// Fetches the next "page" of data. Starting with the requested AssetID
        /// No need to get any records with a lower AssetID's
        /// </summary>
        /// <param name="AssetID"></param>
        private void FetchDataPage(long AssetID)
        {
            if (LastPage)
                return;

            _param.Add("AssetID", AssetID);
            _param.Add("PageSize", PageSize);
            _data = _connection.Query<T>(_query, _param, commandTimeout: _defaultQueryCommandTimeout).ToList();
            if (_data.Count() < PageSize)
            {
                //If we fetched less than PageSize, this is the last page of data
                LastPage = true;
            } else
            {
                long MinAssetID = _data.Min(i => i.AssetID);
                long MaxAssetID = _data.Max(i => i.AssetID);
                if (MinAssetID == MaxAssetID)
                {
                    //If min and max AssetID is the same, the whole "page" is the same asset and it can't be guaranteed that all records for one asset has been fetched
                    throw new Exception("Search of " + typeof(T) + " got more than " + PageSize + " results for one AssetID");
                }
                else
                {
                    //The page may have an incomplete set of records for the highest Asset ID, so remove those from the data stored.
                    _data.RemoveAll(i => i.AssetID == MaxAssetID);
                    CurrentHighID = _data.Max(i => i.AssetID);
                }
            }
        }

        /// <summary>
        /// Fetches records from the query for the provided Asset ID
        /// </summary>
        /// <param name="AssetID"></param>
        /// <returns></returns>
        public List<T> GetByAssetID(long AssetID)
        {
            //If requested ID is higher than what is current, and last page has not been reached, fetch the next data page
            if (!LastPage && AssetID > CurrentHighID)
                FetchDataPage(AssetID);

            return _data.Where(i => i.AssetID == AssetID).ToList();
        }
    }
}
