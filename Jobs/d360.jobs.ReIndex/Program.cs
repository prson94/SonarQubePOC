using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.extensions.search;
using d360.core.queue;
using d360.core;
using Dapper;
using d360.core.entities;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace d360.jobs.ReIndex
{
    class Program: FunctionsBase
    {
        private static int _defaultQueryCommandTimeout = 180;
        private static string _jobName = "Re-Index Search Job";

        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));
            
            var mex = new List<Exception>();

            AITrackJobStart(_jobName);

            try
            {
                var companies = GetActiveCompanyIDs();
                //var companies = GetActiveDevelopmentCompanyIDs();

#if DEBUG                       
                companies = GetActiveCompanyIDs().Where(i => i == 4).ToList();
#endif
           
              companies.ForEach(companyID =>
              {
                  try
                  {
                      var jobDuration = System.Diagnostics.Stopwatch.StartNew();

                      var source = new ElasticSearchSource();                    

                      using (var context = GetCompanyConnection(companyID))
                      {   
                                             
                          Console.WriteLine("Starting to rebuild search index [company id: {0}]", companyID);

                          context.OpenWithRetry(RetryPolicy.DefaultFixed);

                          Console.WriteLine("reseting search index [company id: {0}]", companyID);

                          source.ClearIndex(companyID);

                          Console.WriteLine("loading artifacts [company id: {0}]", companyID);

                          source.AddToIndex(LoadArtifacts(context, companyID, source));

                          Console.WriteLine("loading attributes [company id: {0}]", companyID);

                          source.AddToIndex(LoadAttributes(context, companyID, source));

                          Console.WriteLine("loading models [company id: {0}]", companyID);

                          source.AddToIndex(LoadModels(context, companyID, source));

                          Console.WriteLine("loading policies [company id: {0}]", companyID);

                          source.AddToIndex(LoadPolicies(context, companyID, source));

                          Console.WriteLine("loading fusion types [company id: {0}]", companyID);

                          source.AddToIndex(LoadFusionTypes(context, companyID, source));

                          Console.WriteLine("loading domains [company id: {0}]", companyID);

                          source.AddToIndex(LoadDomains(context, companyID, source));

                          Console.WriteLine("loading groups [company id: {0}]", companyID);

                          source.AddToIndex(LoadGroups(context, companyID, source));

                          Console.WriteLine("loading rules [company id: {0}]", companyID);

                          source.AddToIndex(LoadRules(context, companyID, source));

                          Console.WriteLine("loading fusion attributes [company id: {0}]", companyID);

                          source.AddToIndex(LoadFusionAttributes(context, companyID, source));
                          
                          Console.WriteLine("loading artifact synonyms [company id: {0}]", companyID);

                          source.AddToIndex(LoadArtifactSynonyms(context, companyID, source));

                      }

                      using (var community = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION))
                      {
                          Console.WriteLine("loading users [company id: {0}]", companyID);

                          source.AddToIndex(LoadUsers(community, companyID, source));
                      }

                      AITrackRequest($"{_jobName} - for company {companyID}", jobDuration.Elapsed);
                  }
                  catch (Exception ex)
                  {
                      AITrackException(_jobName, ex, companyID.ToString());

                      mex.Add(ex);
                  }

              });
            }
            catch (Exception ex)
            {
                AITrackException(_jobName, ex);

                mex.Add(ex);
            }

            if (mex.Count > 0)
            {
                throw new AggregateException("One or more exceptions occurred", mex);
            }
            else
            {
                AITrackJobCompletedNoErrors(_jobName);
            }            
        }

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
	                ArtType.Name as 'SynonymForObjectType'	
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
	                ArtType.Name as 'SynonymForObjectType'	
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

                if(list.Count > 30000)
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

        private static IEnumerable<AddToIndexModel> LoadUsers(SqlConnection context, int companyID, ElasticSearchSource source)
        {            
            var sql = @"select 
	                        r.ID,
	                        r.Username,
	                        r.LastName,
	                        r.FirstName,
	                        r.Email	                        
                        from
	                        [dbo].[resource] r
	                        inner join [dbo].[companyresource] cr on (r.id = cr.resourceid)
                        where cr.companyid = @compid";

            foreach (var a in context.Query(sql, new { compid = companyID }))
            {
                var item = new AddToIndexModel { Group = "Users", CompanyID = companyID, Type = "User", ID = a.ID, RelativeUrl = string.Format("#/resources/{0}", a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", $"{a.FirstName} {a.LastName}");
                
                item.Fields.Add("Type", "User");
                item.Fields.Add("Email", a.Email);
                item.Fields.Add("Username", a.Username);

                yield return item;
            }            
        }

        private static IEnumerable<AddToIndexModel> LoadRules(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var sql = @"SELECT [ID]
                                  ,[Name]
                                  ,[Description]      
                                  ,[RuleType]
                              FROM [dbo].[Rule]";

            foreach (var a in context.Query(sql))
            {
                var item = new AddToIndexModel { Group = "Rule", CompanyID = companyID, Type = "Rule", ID = a.ID, RelativeUrl = string.Format("#/rules/{0}", a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                var ruleType = (core.enums.RuleType)a.RuleType;

                item.Fields.Add("Type", $"{ruleType.ToString()} Rule");
                item.Fields.Add("Description", a.Description);

                yield return item;                
            }
        }

        private static void LoadLookupTypes(SqlConnection context, int companyID, ElasticSearchSource source)
        {
            var list = new List<AddToIndexModel>();

            var sql = @"select
                            lt.ID,
	                        lt.Name
                        from[dbo].[lookuptype] lt";

            foreach (var a in context.Query(sql))
            {
                var item = new AddToIndexModel { Group = "LookupType", CompanyID = companyID, Type = "Lookup Type", ID = a.ID, RelativeUrl = string.Format("#/lookups/administration/{0}", a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);                
                item.Fields.Add("Type", "Lookup Type");
                list.Add(item);
            }

            source.AddToIndex(list);
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
    }
}
