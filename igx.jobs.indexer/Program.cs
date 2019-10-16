using d360.core;
using d360.core.queue;
using d360.core.enums;
using d360.extensions.search;
using d360.extensions.queue;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace igx.jobs.indexer
{
    class Program
    {
        static void Main()
        {
            var config = CoreFunction.GetJobHostConfiguration();
            config.UseTimers();
            //We should only process one reindex queue item at a time
            config.Queues.BatchSize = 1;

#if DEBUG
            config.UseDevelopmentSettings();
 #endif

            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }

    internal class FieldSqlModel
    {
        public int ObjectID { get; set; }
        public string Name { get; set; }
        public string FormattedValue { get; set; }
    }

    internal class TagSqlModel
    {
        public Guid AssetUID { get; set; }
        public Guid TagUID { get; set; }
        public string Value { get; set; }
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

        const string fieldsSql = @"select F.ObjectID, T.Name, F.FormattedValue from Field F inner join FieldType T on T.ID = F.FieldTypeID and F.ObjectType = @t and F.FormattedValue is not null and F.FormattedValue <> '' and T.[Type] not in('DateTime','Color','FusionLookup','FilteredLookup','ComplexRelationLookup','OwnershipLookup','Relationship','FieldFromRelationship','RefListRelationship','JSON')";
        const string tagsSql = @"SELECT a.uid AS AssetUID, t.uid AS TagUID, t.Value FROM [dbo].[AssetTag] at INNER JOIN [dbo].[Tag] t ON at.TagID = t.ID INNER JOIN [dbo].[Asset] a ON at.AssetID = a.ID";

        public static void RunViaTimer([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();
                AzureQueueSource queue = new AzureQueueSource();


                companies.ForEach(c =>
                {
                    queue.CreateMessage(Config.GetValue<string>("SearchIndexQueue"), new ReindexModel { CompanyID = c.CompanyID });
                });

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

            CoreFunction.AIFlush();
        }

        public static void RunViaQueue([QueueTrigger("%SearchIndexQueue%"), StorageAccount("QueueStorageAccount")] string myQueueItem, TextWriter log)
        {
            var c = JsonConvert.DeserializeObject<ReindexModel>(myQueueItem);

            try
            {
                var source = new ElasticSearchSource();
                using (var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID))
                {
                    IEnumerable<AddToIndexModel> models = null;

                    company.OpenWithRetry(RetryPolicy.DefaultFixed);

                    int SuggestedIndexLimit = SuggestIndexLimit(company);
                    if(SuggestedIndexLimit > 1000)
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

                    LogReindexStart("Attributes", c.CompanyID);

                    try
                    {
                        models = LoadAttributes(company, c.CompanyID, source);
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

                    var users = new List<AddToIndexModel>();

                    #region Company Users

                    var sql = @"select ResourceID, Email as Username, LastName, FirstName, Email from reporting.global_resource";

                    users = company.Query(sql).ToList().Select(u => new AddToIndexModel
                    {
                        Group = "Resource",
                        CompanyID = c.CompanyID,
                        Type = "User",
                        ID = u.ResourceID,
                        RelativeUrl = $"#/resources/{u.ResourceID}",
                        Fields = new Dictionary<string, string>() {
                                    { "Name", $"{u.FirstName} {u.LastName}" },
                                    { "Type", "User" },
                                    { "Email", u.Email },
                                    { "Username", u.Username }
                                }
                    }).ToList();

                    source.AddToIndex(users);

                    #endregion

                    LogCompanyReindexComplete(c.CompanyID);

                }
            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex, c.CompanyID);
            }
        }

        private static void LogCompanyReindexComplete(int companyID)
        {
            CoreFunction.AITrackTrace(functionName, $"Completed reindex for company {companyID}", companyId: companyID);
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

        private static IEnumerable<AddToIndexModel> LoadArtifactSynonyms(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"                    	
                (select	
	                SubjectAdv.DisplayValue as 'Synonym',	
	                I.Subject as 'SynonymObjectType',
	                I.SubjectID as  'SynonymObjectID',
	                ObjectAdv.DisplayValue as 'SynonymFor',	
	                I.Object as 'SynonymForObject',
	                I.ObjectID as 'SynonymForObjectID',		
	                dbo.GenerateAssetUrl(ObjectAsset.ID) as 'Url',	
	                ArtType.Name as 'SynonymForObjectType',
                    P.Name as 'PredicateName'
                from [intersect] I
	                inner join IntersectType T on T.ID = I.IntersectTypeID 
                    inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
	                inner join Asset SubjectAsset on SubjectAsset.[Object] = 'Artifact' and SubjectAsset.ObjectID = I.SubjectID and I.Subject = 'Artifact'
					inner join [dbo].AssetDisplayValue SubjectAdv on SubjectAdv.AssetID = SubjectAsset.ID
	                inner join Asset ObjectAsset on ObjectAsset.[Object] = 'Artifact' and ObjectAsset.ObjectID = I.ObjectID and I.Object = 'Artifact'
					inner join [dbo].AssetDisplayValue ObjectAdv on ObjectAdv.AssetID = ObjectAsset.ID
	                inner join AssetType ArtType on ObjectAsset.AssetTypeID = ArtType.ID)
                Union
                (select	
	                SubjectAdv.DisplayValue as 'Synonym',	
	                I.Object as 'SynonymObjectType',
	                I.ObjectID as  'SynonymObjectID',
	                ObjectAdv.DisplayValue as 'SynonymFor',	
	                I.Subject as 'SynonymForObject',
	                I.SubjectID as 'SynonymForObjectID',		
	                dbo.GenerateAssetUrl(ObjectAsset.ID) as 'Url',	
	                ArtType.Name as 'SynonymForObjectType',
                    P.Name as 'PredicateName'	
                from [intersect] I
	                inner join IntersectType T on T.ID = I.IntersectTypeID 
                    inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
	                inner join Asset SubjectAsset on SubjectAsset.[Object] = 'Artifact' and SubjectAsset.ObjectID = I.ObjectID and I.Subject = 'Artifact'
					inner join [dbo].AssetDisplayValue SubjectAdv on SubjectAdv.AssetID = SubjectAsset.ID
	                inner join Asset ObjectAsset on ObjectAsset.[Object] = 'Artifact' and ObjectAsset.ObjectID = I.SubjectID and I.Object = 'Artifact'
					inner join [dbo].AssetDisplayValue ObjectAdv on ObjectAdv.AssetID = ObjectAsset.ID
	                inner join AssetType ArtType on ObjectAsset.AssetTypeID = ArtType.ID)
                order by SynonymFor";

            return getData(context, sql, companyID, source, "", false, (dynamic o) =>
            {
                return new AddToIndexModel
                {
                    Group = "Synonym",
                    
                    CompanyID = companyID,
                    Type = "Synonym",
                    ItemUniqueID = $"{o.SynonymObjectType}|{o.SynonymObjectID}|{o.SynonymForObjectType}|{o.SynonymForObjectID}",
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

        private static IEnumerable<AddToIndexModel> LoadCustomSynonyms(SqlConnection context, int companyID, ElasticSearchSource source)
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
                return new AddToIndexModel
                {
                    Group = "Synonym",
                    CompanyID = companyID,
                    Type = "Synonym",
                    ItemUniqueID = $"custom|{o.PredicateName}|{o.ID}",
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

        private static IEnumerable<AddToIndexModel> LoadFusionAttributes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"select
	                        f.ID,
	                        f.Name,
	                        f.FusionAttributeTypeID,
	                        ft.Name as FusionAttributeTypeName,
	                        fu.Name as FusionName
                        from fusionattribute f
	                        inner join fusionattributetype ft on (f.fusionattributetypeid = ft.id)
	                        inner join fusion fu on (f.fusionid = fu.id)
                        where f.Deleted = 0";

            foreach (var a in context.Query(sql, new { compid = companyID }, buffered:false,commandTimeout: _defaultQueryCommandTimeout))
            {
                var item = new AddToIndexModel { Group = "FusionAttributes",
                    CompanyID = companyID,
                    Type = a.FusionAttributeTypeName,
                    ID = a.ID,                    
                    RelativeUrl = $"/fusion/details/FusionAttribute/{a.ID}/{Uri.EscapeDataString(a.Name)}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", a.Name },
                        { "Type", $"{a.FusionName} {a.FusionAttributeTypeName}" }
                    }
                };                

                yield return item;
            }
        }
        
        private static IEnumerable<AddToIndexModel> LoadRules(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            int assettypeclass = (int)AssetTypeClass.Rule;
            var sql = $@"SELECT
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
                AND A.State = 1";

            var sType = SystemObjects.Rule.ToString();

            return getData(context, sql, companyID, source, sType, true, (dynamic o) =>
            {
                return new AddToIndexModel
                {
                    Group = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    Type = "Rule",
                    RelativeUrl = o.Url,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "Type", o.RuleType },
                        { "Description", o.Description },
                        { "Uid", o.Uid.ToString() },
                        { "AssetTypeUid", o.AssetTypeUid.ToString() }
                    }
                };
            });
        }

        private static IEnumerable<AddToIndexModel> LoadGroups(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"SELECT [ID],[Name],[Description] FROM [Group]";

            var sType = "Group";
            return getData(context, sql, companyID, source, sType, false, (dynamic o) =>
            {
                return new AddToIndexModel
                {
                    Group = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    Type = sType,
                    RelativeUrl = $"/groups/{o.ID}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "Type", sType },
                        { "Description", o.Description }
                    }
                };
            });
        }

        private static IEnumerable<AddToIndexModel> LoadReferenceItemTypes(SqlConnection context, int companyID, ElasticSearchSource source)
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
                return new AddToIndexModel
                {
                    Group = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    Type = "Reference List",
                    RelativeUrl = $"/reference/{o.ID}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "Type", "Reference List" },
                        { "Description", o.Description },
                        { "AssetTypeUid", o.AssetTypeUid.ToString() }

                    }
                };
            });
        }

        private static IEnumerable<AddToIndexModel> LoadPolicies(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            int assettypeclass = (int)AssetTypeClass.Policy;
            var sql = $@"SELECT
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
                AND A.State = 1";

            var sType = SystemObjects.Policy.ToString();

            return getData(context, sql, companyID, source, sType, true, (dynamic o) =>
            {
                return new AddToIndexModel
                {
                    Group = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    Type = o.PolicyType,
                    RelativeUrl = o.Url,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "Type", o.PolicyType },
                        { "TextPath", o.TextPath ?? "" },
                        { "Uid", o.Uid.ToString() },
                        { "AssetTypeUid", o.AssetTypeUid.ToString() }
                    }
                };
            });
        }

        private static IEnumerable<AddToIndexModel> LoadArtifacts(SqlConnection context, int companyID, ElasticSearchSource source, AssetTypeClass ArtifactClass)
        {
            int assettypeclass = (int)ArtifactClass;
            var sql = $@"
select
	cast(A.ID as varchar) as ItemUniqueID,
	A.ObjectID as ID,
	att.ObjectID as TypeID,
	adv.DisplayValue,
	att.Name as TypeName,
    att.uid as AssetTypeUid,
	a.uid as Uid
from
	[dbo].Asset a
	inner join [dbo].assettype att on a.assettypeid = att.id
	inner join [dbo].assetdisplayvalue adv on adv.assetid = a.id
where
	att.[Object] = 'ArtifactType' and a.[state] = 1 and att.[Class] = {assettypeclass.ToString()}";

            var sType = ArtifactClass.ToString();

            return getData(context, sql, companyID, source, SystemObjects.Artifact.ToString(), true, (dynamic o) =>
            {
                return new AddToIndexModel
                {
                    Group = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    ItemUniqueID = o.ItemUniqueID,
                    Type = o.TypeName,
                    RelativeUrl = $"/artifact/{o.TypeID}/{o.ID}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.DisplayValue },
                        { "Type", o.TypeName },
                        { "Uid", o.Uid.ToString() },
                        { "Description", "" },
                        { "Status", "Active" },
                        { "Taxonomy", "" },
                        { "AssetTypeUid", o.AssetTypeUid.ToString() }
                    }
                };
            });
        }

        private static IEnumerable<AddToIndexModel> LoadAttributes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"
select	AD.ID, AD.Name, AD.FormattedValue, OD.Url 
from	AttributeDetail AD 
        inner join cache.ObjectDetails OD on OD.[Object] = AD.ObjectType and  OD.ObjectID = AD.ObjectID and OD.[Object] in ('Artifact', 'Taxonomy')";

            var sType = SystemObjects.Attribute.ToString();

            return getData(context, sql, companyID, source, sType, false, (dynamic o) =>
            {
                return new AddToIndexModel
                {
                    Group = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    Type = o.Name,
                    RelativeUrl = o.Url,
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.FormattedValue },
                        { "Type", o.Name }
                    }
                };
            });
        }

        private static IEnumerable<AddToIndexModel> LoadModels(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"
SELECT	A.ObjectID as ID,
		T.ObjectID as TypeID,
		D.DisplayValue,
		T.Name as TypeName,
        T.uid as AssetTypeUid,
		A.uid as Uid
FROM	[dbo].Asset A
		INNER JOIN [dbo].AssetType T on A.AssetTypeID = T.id
		INNER JOIN [dbo].AssetDisplayValue D on D.AssetID = A.ID
WHERE	T.Object = 'TaxonomyType'
		and A.State = 1";


            var sType = SystemObjects.Taxonomy.ToString();

            return getData(context, sql, companyID, source, sType, true, (dynamic o) =>
            {
                return new AddToIndexModel
                {
                    Group = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    Type = o.TypeName,
                    RelativeUrl = $"/model/{o.TypeID};hierarchyId={o.ID}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.DisplayValue },
                        { "Type", o.TypeName },
                        { "Uid", o.Uid.ToString() },
                        { "Description", "" },
                        { "TextPath", o.DisplayValue ?? "" },
                        { "AssetTypeUid", o.AssetTypeUid.ToString() }
                    }
                };
            });
        }

        private static IEnumerable<AddToIndexModel> LoadFusionTypes(SqlConnection context, int companyID, ElasticSearchSource source)
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
                return new AddToIndexModel
                {
                    Group = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    Type = o.FusionTypeName,
                    RelativeUrl = $"/fusion/{o.ID}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.FusionName },
                        { "Type", o.FusionTypeName },
                        { "Description", o.FusionDescription }
                    }
                };
            });
        }

        private static IEnumerable<AddToIndexModel> getData(SqlConnection context, string sql, int companyID, ElasticSearchSource source, string type, bool loadFields, Func<dynamic, AddToIndexModel> convertToDictionary)
        {
            if (loadFields)
            {
                return getDataWithFields(context, sql, companyID, source, type, convertToDictionary);
            }
            
            return getDataWithoutFields(context, sql, companyID, source, type, convertToDictionary);            
        }

        private static IEnumerable<AddToIndexModel> getDataWithoutFields(SqlConnection context, string sql, int companyID, ElasticSearchSource source, string type, Func<dynamic, AddToIndexModel> convertToDictionary)
        {
            return context.Query(sql, commandTimeout: _defaultQueryCommandTimeout, buffered:false).ToList().Select(a => (AddToIndexModel)convertToDictionary(a));
        }

        private static IEnumerable<AddToIndexModel> getDataWithFields(SqlConnection context, string sql, int companyID, ElasticSearchSource source, string type, Func<dynamic, AddToIndexModel> convertToDictionary)
        {
            var fields = context.Query<FieldSqlModel>(fieldsSql, new { t = type }, commandTimeout: _defaultQueryCommandTimeout).ToList();
            var tags = context.Query<TagSqlModel>(tagsSql, null, commandTimeout: _defaultQueryCommandTimeout).ToList();
            var list = getDataWithoutFields(context, sql, companyID, source, type, convertToDictionary);
                        
            foreach (var item in list)
            {
                var subset = fields.Where(i => i.ObjectID == item.ID);
                foreach (var f in subset)
                {
                    item.Fields[f.Name] = f.FormattedValue;
                }
                if(item.Fields.ContainsKey("Uid"))
                {
                    item.Tags = tags.Where(i => i.AssetUID == Guid.Parse(item.Fields["Uid"])).ToDictionary(x => x.TagUID.ToString(), x => x.Value);
                }

                yield return item;
            }            
        }

#endregion
    }
}
