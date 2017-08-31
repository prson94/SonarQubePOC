using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using d360.utils.company;
using d360.core;
using System.Configuration;
using System.Linq;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Dapper;
using d360.extensions.search;
using System.Collections.Generic;
using d360.core.queue;
using d360.core.entities;
using System.Data.SqlClient;
using igx.functions.Core;

namespace igx.functions.Indexing
{
    public static class Engine
    {
        private static int _defaultQueryCommandTimeout = 180;
        const string functionName = "ReIndexer";
        const string timerSettings = "0 0 4 * * 6";
        //const string timerSettings = "*/10 * * * * *";

        [FunctionName(functionName)]
        public static void Run([TimerTrigger(timerSettings)]TimerInfo myTimer, TraceWriter log)
        {
            //https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer#schedule-examples

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

                        company.OpenWithRetry(RetryPolicy.DefaultFixed);

                        source.ClearIndex(c.CompanyID);
                        source.AddToIndex(LoadArtifacts(company, c.CompanyID, source));
                        source.AddToIndex(LoadAttributes(company, c.CompanyID, source));
                        source.AddToIndex(LoadModels(company, c.CompanyID, source));
                        source.AddToIndex(LoadPolicies(company, c.CompanyID, source));
                        source.AddToIndex(LoadFusionTypes(company, c.CompanyID, source));
                        source.AddToIndex(LoadDomains(company, c.CompanyID, source));
                        source.AddToIndex(LoadGroups(company, c.CompanyID, source));
                        source.AddToIndex(LoadRules(company, c.CompanyID, source));
                        source.AddToIndex(LoadFusionAttributes(company, c.CompanyID, source));
                        source.AddToIndex(LoadArtifactSynonyms(company, c.CompanyID, source));
                        source.AddToIndex(LoadCustomSynonyms(company, c.CompanyID, source));

                        #region Company Users

                        var sql = @"select 
	                        ResourceID,
	                        Email as Username,
	                        LastName,
	                        FirstName,
	                        Email
                        from
	                        reporting.global_resource";

                        var users = new List<AddToIndexModel>();
                        foreach (var a in company.Query(sql))
                        {
                            var item = new AddToIndexModel { Group = "Users", CompanyID = c.CompanyID, Type = "User", ID = a.ResourceID, RelativeUrl = $"#/resources/{a.ResourceID}" };
                            item.Fields = new Dictionary<string, string>();
                            item.Fields.Add("Name", $"{a.FirstName} {a.LastName}");
                            item.Fields.Add("Type", "User");
                            item.Fields.Add("Email", a.Email);
                            item.Fields.Add("Username", a.Username);

                            users.Add(item);
                        }

                        source.AddToIndex(users);

                        #endregion

                        company.Close();
                    }
                    catch (Exception ex)
                    {
                        CoreFunction.AITrackException(functionName, ex, c.CompanyID);
                        log.Error($"Company [{c.CompanyID}]: [{ex.GetFullExceptionData()}]");
                    }
                });

            }
            catch (Exception ex)
            {
                CoreFunction.AITrackException(functionName, ex);
                log.Error($"General Exception: {ex.GetFullExceptionData()}");
            }

            CoreFunction.AIFlush();
        }


        #region Supporting Functions

        private static IEnumerable<AddToIndexModel> LoadArtifactSynonyms(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"                    	
                (select	
	                SubjectArt.Name as 'Synonym',	
	                I.Subject as 'SynonymObjectType',
	                I.SubjectID as  'SynonymObjectID',
	                ObjectArt.Name as 'SynonymFor',	
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
	                SubjectArt.Name as 'Synonym',	
	                I.Object as 'SynonymObjectType',
	                I.ObjectID as  'SynonymObjectID',
	                ObjectArt.Name as 'SynonymFor',	
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
                order by ObjectArt.Name
            ";

            foreach (var a in context.Query(sql))
            {
                var item = new AddToIndexModel { Group = "Synonym", CompanyID = companyID, Type = "Synonym", ItemUniqueID = $"{a.SynonymObjectType}|{a.SynonymObjectID}|{a.SynonymForObjectType}|{a.SynonymForObjectID}", RelativeUrl = a.Url };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Synonym);
                item.Fields.Add("SynonymFor", a.SynonymFor);
                item.Fields.Add("SynonymForObject", a.SynonymForObject);
                item.Fields.Add("SynonymForObjectType", a.SynonymForObjectType);
                item.Fields.Add("NymType", a.PredicateName);

                yield return item;
            }
        }

        private static IEnumerable<AddToIndexModel> LoadCustomSynonyms(SqlConnection context, int companyID, ElasticSearchSource source)
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
                    inner join [dbo].[predicate] p on (s.predicateid = p.id)
            ";

            foreach (var a in context.Query(sql))
            {
                var item = new AddToIndexModel { Group = "Synonym", CompanyID = companyID, Type = "Synonym", ItemUniqueID = $"custom|{a.PredicateName}|{a.ID}", RelativeUrl = a.Url };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Synonym);
                item.Fields.Add("SynonymFor", a.SynonymFor);
                item.Fields.Add("SynonymForObject", a.SynonymForObject);
                item.Fields.Add("SynonymForObjectType", a.SynonymForObjectType);
                item.Fields.Add("NymType", a.PredicateName);

                yield return item;
            }
        }

        private static void LoadFusionAttributesIncremental(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var list = new List<AddToIndexModel>();

            var sql = @"select
	                        f.ID,
	                        f.Name,
	                        f.FusionAttributeTypeID,
	                        ft.Name as FusionAttributeTypeName,
	                        fu.Name as FusionName
                        from fusionattribute f
	                        inner join fusionattributetype ft on (f.fusionattributetypeid = ft.id)
	                        inner join fusion fu on (f.fusionid = fu.id)";

            foreach (var a in context.Query(sql, new { compid = companyID }))
            {
                var item = new AddToIndexModel { Group = "FusionAttributes", CompanyID = companyID, Type = a.FusionAttributeTypeName, ID = a.ID, RelativeUrl = string.Format("#/fusion/item/{0}/{1}", a.FusionAttributeTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Type", $"{a.FusionName} {a.FusionAttributeTypeName}");

                list.Add(item);

                if (list.Count > 30000)
                {
                    source.AddToIndex(list);

                    list.Clear();
                }
            }

            source.AddToIndex(list);
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
	                        inner join fusion fu on (f.fusionid = fu.id)";

            foreach (var a in context.Query(sql, new { compid = companyID }))
            {
                var item = new AddToIndexModel { Group = "FusionAttributes", CompanyID = companyID, Type = a.FusionAttributeTypeName, ID = a.ID, RelativeUrl = string.Format("#/fusion/item/{0}/{1}", a.FusionAttributeTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Type", $"{a.FusionName} {a.FusionAttributeTypeName}");

                yield return item;
            }
        }

        private static IEnumerable<AddToIndexModel> LoadRules(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            string sql = "";

            sql = @"SELECT R.[ID]
                                ,R.[Name]
                                ,R.[Description]      
                                ,T.Name as [RuleType]
								,[dbo].GenerateNgObjectUrl('Rule',R.RuleTypeID,R.ID) as [Url]
                            FROM [dbo].[Rule] R inner join RuleType T on T.ID = R.RuleTypeID";


            foreach (var a in context.Query(sql))
            {
                var item = new AddToIndexModel { Group = "Rule", CompanyID = companyID, Type = "Rule", ID = a.ID, RelativeUrl = a.Url };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);

                item.Fields.Add("Type", $"{a.RuleType} Rule");
                item.Fields.Add("Description", a.Description);

                yield return item;
            }
        }

        private static IEnumerable<AddToIndexModel> LoadGroups(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"SELECT [ID]
                          ,[Name]
                          ,[Description]
                      FROM[dbo].[Group]";

            foreach (var a in context.Query(sql))
            {
                var item = new AddToIndexModel { Group = "Group", CompanyID = companyID, Type = "Group", ID = a.ID, RelativeUrl = string.Format("#/groups/{0}", a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Description", a.Description);
                item.Fields.Add("Type", "Group");
                yield return item;
            }
        }

        private static IEnumerable<AddToIndexModel> LoadDomains(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"select
                            ID,
	                        Name,
	                        [Description]
                        from ReferenceItemType";

            foreach (var a in context.Query(sql))
            {
                var item = new AddToIndexModel { Group = "Reference", CompanyID = companyID, ID = a.ID, Type = "Reference List", RelativeUrl = string.Format("/reference/{0}", a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Description", a.Description);
                item.Fields.Add("Type", "Reference List");
                yield return item;
            }
        }

        private static IEnumerable<AddToIndexModel> LoadPolicies(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            foreach (var a in context.Query(@"select 
                                                p.ID,
                                                p.Name,
                                                p.[Description],
                                                pt.Name as [PolicyType],
                                                p.PolicyTypeID as [PolicyTypeID]
                                            from policy p

                                                inner
                                            join policytype pt on p.PolicyTypeID = pt.id"))
            {
                var item = new AddToIndexModel { Group = "Policy", CompanyID = companyID, ID = a.ID, Type = a.PolicyType, RelativeUrl = string.Format("#/policies/{0}/{1}", a.PolicyTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Description", a.Description);
                item.Fields.Add("Type", a.PolicyType);
                yield return item;
            }
        }

        private static IEnumerable<AddToIndexModel> LoadArtifacts(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sType = SystemObjects.Artifact.ToString();

            var fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t", new { t = sType }, commandTimeout: _defaultQueryCommandTimeout).ToList();

            foreach (var a in context.Query("select A.*, T.Name as ArtifactType, V.Name as Taxonomy from Artifact A inner join ArtifactType T on T.ID = A.ArtifactTypeID inner join TaxonomyType V on V.ID = A.TaxonomyTypeID", commandTimeout: _defaultQueryCommandTimeout))
            {
                var item = new AddToIndexModel { Group = "Artifact", CompanyID = companyID, ID = a.ID, Type = a.ArtifactType, RelativeUrl = string.Format("#/artifacts/{0}/{1}", a.ArtifactTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Description", a.Description);
                item.Fields.Add("Status", a.Status);
                item.Fields.Add("Type", a.ArtifactType);
                item.Fields.Add("Taxonomy", a.Taxonomy);
                var subset = fields.Where(i => i.ObjectID == a.ID);
                foreach (var f in subset)
                {
                    if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                }
                yield return item;
            }
        }

        private static IEnumerable<AddToIndexModel> LoadAttributes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            foreach (var a in context.Query(@"select	AD.ID, AD.Name,
                                        AD.FormattedValue,
		                                        OD.Url 
                                        from	AttributeDetail AD
		                                        inner join cache.ObjectDetails OD on OD.[Object] = AD.ObjectType and  OD.ObjectID = AD.ObjectID and OD.[Object] in ('Artifact', 'Taxonomy')  "))
            {
                var item = new AddToIndexModel { Group = "Attribute", CompanyID = companyID, ID = a.ID, Type = a.Name, RelativeUrl = a.Url };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.FormattedValue);
                item.Fields.Add("Type", a.Name);
                yield return item;
            }
        }

        private static IEnumerable<AddToIndexModel> LoadModels(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sType = SystemObjects.Taxonomy.ToString();

            var fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t", new { t = sType }).ToList();

            foreach (var a in context.Query("select O.*, T.Name as TaxonomyType from Taxonomy O inner join TaxonomyType T on T.ID = O.TaxonomyTypeID"))
            {
                var item = new AddToIndexModel { Group = "Taxonomy", CompanyID = companyID, ID = a.ID, Type = a.TaxonomyType, RelativeUrl = string.Format("#/catalogs/{0}/{1}", a.TaxonomyTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Description", a.Description);
                item.Fields.Add("TextPath", a.TextPath);
                item.Fields.Add("Type", a.TaxonomyType);
                var subset = fields.Where(i => i.ObjectID == a.ID);
                foreach (var f in subset)
                {
                    if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                }
                yield return item;
            }
        }

        private static IEnumerable<AddToIndexModel> LoadFusionTypes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            foreach (var a in context.Query(@"select
			                                                f.id as ID,
			                                                f.Name as FusionName,
			                                                f.Description as FusionDescription,			                                                
			                                                ft.Name as FusionTypeName,
			                                                ft.Description as FusionTypeDescription,
                                                            ft.ID as FusionTypeID
		                                                from fusion f		                                                
		                                                inner join fusiontype ft on f.fusiontypeid = ft.id"))
            {
                var item = new AddToIndexModel { Group = "FusionType", CompanyID = companyID, ID = a.ID, Type = a.FusionTypeName, RelativeUrl = string.Format("#/fusion/{0}/{1}", a.FusionTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.FusionName);
                item.Fields.Add("Description", a.FusionDescription);
                item.Fields.Add("Type", a.FusionTypeName);
                yield return item;
            }
        }

        #endregion
    }
}
