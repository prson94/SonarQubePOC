using ApplicationInsights.Helpers.WebJobs;
using d360.core;
using d360.core.queue;
using d360.extensions.search;
using d360.utils.company;
using Dapper;
using Microsoft.Azure.WebJobs;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace igx.jobs.indexer
{
    class Program
    {
        static void Main()
        {
            var config = new JobHostConfiguration {
                DashboardConnectionString = CoreFunction.GetConfigValueByKey("WebJobsAccount"),
                StorageConnectionString = CoreFunction.GetConfigValueByKey("WebJobsAccount"),
                NameResolver = new QueueNameResolver()
            };

            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
            }

            config.UseApplicationInsights();
            config.UseCore();
            //config.UseServiceBus();
            config.UseTimers();

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

    public static class Indexer
    {
        private static int _defaultQueryCommandTimeout = 180;
        const string functionName = "Indexing_ReIndex";
        const string timerSettings = "0 0 4 * * 6";
        //const string timerSettings = "*/1 * * * * *";

        const string fieldsSql = @"select F.ObjectID, T.Name, F.FormattedValue from Field F inner join FieldType T on T.ID = F.FieldTypeID and F.ObjectType = @t and F.FormattedValue is not null and F.FormattedValue <> ''";

        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TextWriter log)
        {
            try
            {
                CoreFunction.AITrackJobStart(functionName);
                var companies = CoreFunction.GetCompaniesByCurrentSlot();


                companies.ForEach(c =>
                {
                    try
                    {
                        var source = new ElasticSearchSource();
                        var company = CompanyConnectionUtils.GetCompanyConnection(c.CompanyID, c.Server, c.Username, c.Password);
                        IEnumerable<AddToIndexModel> models = null;

                        company.OpenWithRetry(RetryPolicy.DefaultFixed);

                        #region Load Assets

                        //var assetTypes = company.Query<AssetType>("select * from AssetType").ToList();
                        //var assetSql = "select * from AssetDetail where AssetTypeID = @t State = 1";
                        //var fieldSql = "select	F.* from FieldDetail F inner join Asset A on A.ID = F.AssetID and A.AssetTypeID = @t";
                        //foreach (var at in assetTypes)
                        //{
                        //    var assets = company.Query<AssetDetail>(assetSql, new { t = at.ID });
                        //    var fields = company.Query<FieldDetail>(fieldSql, new { t = at.ID });
                        //    var adds = new List<AddToIndexModel>();
                        //    var urlFormat = "";
                        //    switch (at.Class)
                        //    {
                        //        case AssetTypeClass.FusionAttribute:
                        //            urlFormat = "";
                        //            break;
                        //        case AssetTypeClass.Glossary:
                        //            urlFormat = "/artifact/{0}/{1}";
                        //            break;
                        //        case AssetTypeClass.Group:
                        //            urlFormat = "";
                        //            break;
                        //        case AssetTypeClass.Model:
                        //            urlFormat = "/model/{0};hierarchyId={1}";
                        //            break;
                        //        case AssetTypeClass.Policy:
                        //            urlFormat = "/policy/{0};hierarchyId={1}";
                        //            break;
                        //        case AssetTypeClass.Reference:
                        //            urlFormat = "";
                        //            break;
                        //        case AssetTypeClass.Rule:
                        //            urlFormat = "";
                        //            break;
                        //        default:
                        //            urlFormat = "";
                        //            break;
                        //    }

                        //    if (!string.IsNullOrEmpty(urlFormat))
                        //    {
                        //        foreach (var a in assets)
                        //        {
                        //            var theseFields = fields.Where(f => f.AssetID == a.ID).ToDictionary(k => k.Name, v => v.FormattedValue);
                        //            if (!theseFields.ContainsKey("Name"))
                        //            {
                        //                theseFields.Add("Name", a.DisplayValue);
                        //            }
                        //            if (!theseFields.ContainsKey("Description"))
                        //            {
                        //                var description = string.Join("; ", theseFields.Values);
                        //                theseFields.Add("Description", description);
                        //            }
                        //            adds.Add(new AddToIndexModel {
                        //                Group = at.Class.ToString(),
                        //                CompanyID = c.CompanyID,
                        //                Type = a.TypeName,
                        //                ID = a.ObjectID,
                        //                ItemUniqueID = a.ID.ToString(),
                        //                RelativeUrl = string.Format(urlFormat, a.TypeID, a.ObjectID),
                        //                Fields = theseFields
                        //            });
                        //        }

                        //        source.AddToIndex(models);
                        //    }
                        //}


                        #endregion


                        source.ClearIndex(c.CompanyID);

                        try
                        {
                            models = LoadArtifacts(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadAttributes(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadModels(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadPolicies(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadFusionTypes(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadReferenceItemTypes(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadGroups(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadRules(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadFusionAttributes(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadArtifactSynonyms(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        try
                        {
                            models = LoadCustomSynonyms(company, c.CompanyID, source);
                            source.AddToIndex(models);
                        }
                        catch (Exception ex)
                        {
                            CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        }

                        var users = new List<AddToIndexModel>();

                        #region Company Users

                        var sql = @"select ResourceID, Email as Username, LastName, FirstName, Email from reporting.global_resource";

                        users = company.Query(sql).ToList().Select(u => new AddToIndexModel
                        {
                            Group = "Users",
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

                        company.Close();
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                    }
                });

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
            }

            CoreFunction.AIFlush();
        }


        #region Supporting Functions

        private static List<AddToIndexModel> LoadArtifactSynonyms(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"                    	
                (select	
	                SubjectArt.DisplayValue as 'Synonym',	
	                I.Subject as 'SynonymObjectType',
	                I.SubjectID as  'SynonymObjectID',
	                ObjectArt.DisplayValue as 'SynonymFor',	
	                I.Object as 'SynonymForObject',
	                I.ObjectID as 'SynonymForObjectID',		
	                dbo.GenerateObjectUrl('Artifact', ArtType.ID, ObjectArt.ID) as 'Url',	
	                ArtType.Name as 'SynonymForObjectType',
                    P.Name as 'PredicateName'
                from [intersect] I
	                inner join IntersectType T on T.ID = I.IntersectTypeID 
                    inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
	                inner join Artifact SubjectArt on SubjectArt.ID = I.SubjectID and I.Subject = 'Artifact'
	                inner join Artifact ObjectArt on ObjectArt.ID = I.ObjectID and I.Object = 'Artifact'
	                inner join ArtifactType ArtType on ObjectArt.ArtifactTypeID = ArtType.ID)
                Union
                (select	
	                SubjectArt.DisplayValue as 'Synonym',	
	                I.Object as 'SynonymObjectType',
	                I.ObjectID as  'SynonymObjectID',
	                ObjectArt.DisplayValue as 'SynonymFor',	
	                I.Subject as 'SynonymForObject',
	                I.SubjectID as 'SynonymForObjectID',		
	                dbo.GenerateObjectUrl('Artifact', ArtType.ID, ObjectArt.ID) as 'Url',	
	                ArtType.Name as 'SynonymForObjectType',
                    P.Name as 'PredicateName'	
                from [intersect] I
	                inner join IntersectType T on T.ID = I.IntersectTypeID 
                    inner join Predicate P on P.ID = T.PredicateID and P.Type = 6
	                inner join Artifact SubjectArt on SubjectArt.ID = I.ObjectID and I.Subject = 'Artifact'
	                inner join Artifact ObjectArt on ObjectArt.ID = I.SubjectID and I.Object = 'Artifact'
	                inner join ArtifactType ArtType on ObjectArt.ArtifactTypeID = ArtType.ID)
                order by ObjectArt.DisplayValue";

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

        private static List<AddToIndexModel> LoadCustomSynonyms(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"
select 
	s.Name as 'Synonym'
	,c.Name as 'SynonymFor'
	,s.[Object] as 'SynonymForObject'
	,s.[ObjectID] as 'SynonymForObjectID'
	,dbo.GenerateObjectUrl(s.[Object], c.ObjectTypeID, s.[ObjectID]) as 'Url'
	,c.ObjectTypeName as 'SynonymForObjectType'	
    ,p.Name as 'PredicateName'    
    ,s.ID as 'ID'                
from
	[dbo].[nym] s
	inner join [cache].[objectdetails] c on (s.[Object] = c.[Object] and s.[ObjectID] = c.[ObjectID])
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

        private static List<AddToIndexModel> LoadFusionAttributes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"select
	                            f.ID,
	                            f.Name,
	                            f.FusionAttributeTypeID,
	                            ft.Name as FusionAttributeTypeName,
	                            fu.Name as FusionName
                            from fusionattribute f
	                            inner join fusionattributetype ft on (f.fusionattributetypeid = ft.id)
	                            inner join fusion fu on (f.fusionid = fu.id)";

            return getData(context, sql, companyID, source, SystemObjects.FusionAttribute.ToString(), false, (dynamic o) =>
            {
                return new AddToIndexModel
                {
                    Group = "FusionAttributes",
                    CompanyID = companyID,
                    ID = o.ID,
                    Type = o.FusionAttributeTypeName,
                    RelativeUrl = $"/fusion/details/FusionAttribute/{o.ID}/{Uri.EscapeDataString(o.Name)}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "Type", $"{o.FusionName} {o.FusionAttributeTypeName}" }
                    }
                };
            });
        }

        private static List<AddToIndexModel> LoadRules(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"SELECT R.[ID]
                                    ,R.DisplayValue as [Name]    
                                    ,T.Name as [RuleType]
								    ,[dbo].GenerateNgObjectUrl('Rule',R.RuleTypeID,R.ID) as [Url]
                                FROM [dbo].[Rule] R inner join RuleType T on T.ID = R.RuleTypeID";

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
                        { "Type", $"{o.RuleType} Rule" },
                        { "Description", o.Description }
                    }
                };
            });
        }

        private static List<AddToIndexModel> LoadGroups(SqlConnection context, int companyID, ElasticSearchSource source)
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

        private static List<AddToIndexModel> LoadReferenceItemTypes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"select ID, Name, [Description] from ReferenceItemType";
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
                        { "Description", o.Description }
                    }
                };
            });
        }

        private static List<AddToIndexModel> LoadPolicies(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"select  p.ID,
                                p.DisplayValue as Name,
                                p.TextPath,
                                pt.Name as [PolicyType],
                                p.PolicyTypeID as [PolicyTypeID]
                       from     [Policy] p 
                                inner join PolicyType pt on p.PolicyTypeID = pt.ID";

            var sType = SystemObjects.Policy.ToString();

            return getData(context, sql, companyID, source, sType, true, (dynamic o) =>
            {
                return new AddToIndexModel
                {
                    Group = sType,
                    CompanyID = companyID,
                    ID = o.ID,
                    Type = o.PolicyType,
                    RelativeUrl = $"/policy/{o.PolicyTypeID};hierarchyId={o.ID}",
                    Fields = new Dictionary<string, string>() {
                        { "Name", o.Name },
                        { "Type", o.PolicyType },
                        { "Description", o.Description ?? "" },
                        { "TextPath", o.TextPath ?? "" }
                    }
                };
            });
        }

        private static List<AddToIndexModel> LoadArtifacts(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"
select	cast(ID as varchar) as ItemUniqueID,
        ObjectID as ID,
		TypeID,
		DisplayValue,
		TypeName
from	AssetDetail
where	Type = 'ArtifactType'
		and State = 1";

            var sType = SystemObjects.Artifact.ToString();

            return getData(context, sql, companyID, source, sType, true, (dynamic o) =>
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
                        { "Description", "" },
                        { "Status", "Active" },
                        { "Taxonomy", "" }
                    }
                };
            });
        }

        private static List<AddToIndexModel> LoadAttributes(SqlConnection context, int companyID, ElasticSearchSource source)
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

        private static List<AddToIndexModel> LoadModels(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"
select	ObjectID as ID,
		TypeID,
		DisplayValue,
		TypeName
from	AssetDetail
where	Type = 'TaxonomyType'
		and State = 1";


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
                        { "Description", "" },
                        { "TextPath", o.DisplayValue ?? "" }
                    }
                };
            });
        }

        private static List<AddToIndexModel> LoadFusionTypes(SqlConnection context, int companyID, ElasticSearchSource source)
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

        private static List<AddToIndexModel> getData(SqlConnection context, string sql, int companyID, ElasticSearchSource source, string type, bool loadFields, Func<dynamic, AddToIndexModel> convertToDictionary)
        {
            var list = context.Query(sql, commandTimeout: _defaultQueryCommandTimeout).ToList().Select(a => (AddToIndexModel)convertToDictionary(a)).ToList();

            if (loadFields)
            {
                var fields = context.Query<FieldSqlModel>(fieldsSql, new { t = type }, commandTimeout: _defaultQueryCommandTimeout).ToList();
                list.ForEach(item =>
                {
                    var subset = fields.Where(i => i.ObjectID == item.ID);
                    foreach (var f in subset)
                    {
                        item.Fields[f.Name] = f.FormattedValue;
                    }
                });
                fields = null;
            }

            return list;
        }

        #endregion
    }
}
