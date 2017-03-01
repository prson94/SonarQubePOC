using d360.core;
using d360.core.entities;
using d360.core.queue;
using d360.extensions.caching;
using d360.extensions.info;
using d360.extensions.powerbi;
using d360.extensions.queue;
using d360.extensions.search;
using d360.extensions.storage;
using d360.model;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace d360.test.jobs
{
    [TestClass]
    public class RunJobsTest: BaseTest
    {
        [TestMethod]
        public void DeployFusionConnector()
        {
            var companyID = 29; //10
            var fusionTypeID = 13;//13;
            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), new UriSecurityContextProvider());

            var fusionType = community.GetById<d360.core.entities.Plugins.FusionType>(fusionTypeID, i => i.FusionTypeFields);
            var fusionAttributeTypes = community.Filter<d360.core.entities.Plugins.FusionAttributeType>(i => i.FusionTypeID == fusionTypeID, i => i.FusionAttributeTypeFields).ToList();
            var fusionIntersectTypes = community.Filter<d360.core.entities.Plugins.FusionIntersectType>(i => i.FusionTypeID == fusionTypeID).ToList();

            var company = getCompanyConnection(companyID);

            company.Execute(@"SET IDENTITY_INSERT FusionType ON
if not exists(select 1 from FusionType where ID = @i) BEGIN INSERT INTO FusionType (ID, Name, Description) VALUES (@i, @n, @d) END
SET IDENTITY_INSERT FusionType OFF", new { i = fusionType.ID, n = fusionType.Name, d = fusionType.Description });

            foreach (var o in fusionType.FusionTypeFields)
            {
                var oID = company.Execute(@"
                    declare @ft int
                    if not exists(select 1 from FieldType where Name = @n and [Object] = 'FusionType' and ObjectID = @oid) 
                    BEGIN 
                    INSERT INTO FieldType (Name, FriendlyName, [Type], [Object], ObjectID, SortOrder, IsRequired, IsListable) VALUES (@n, @f, @t, 'FusionType', @oid, @s, 0, @l) 
                    END",
                new { n = o.Name, f = o.FriendlyName, t = o.Type, oid = o.FusionTypeID, s = o.SortOrder, l = o.IsListable });
            }

            loadFusionAttributeTypes(company, fusionType.ID, "FusionAttributeType", null, fusionAttributeTypes);

            fusionIntersectTypes.ForEach(t => {
                company.Execute(@"
if not exists(select 1 from IntersectType where Subject = @type and SubjectID = @si and Object = @type and ObjectID = @ti) 
BEGIN 
			INSERT INTO IntersectType (Subject, SubjectID, Object, ObjectID, UpdatedOn, UpdatedBy, IsSystem) values (@type, @si, @type, @ti, getutcdate(), 0, 1)
END", 
                new { type = "FusionAttributeType", si = t.StartFusionAttributeTypeID, ti = t.EndFusionAttributeTypeID, ro = t.ReadOnly });
            });
        }

        [TestMethod]
        public void DeployDatabaseChanges()
        {
            #region SQL
            var sql = @"";

            #endregion
            var list = getCompanies().ToList();
            list.ForEach(id =>
            {
                var cnn = getCompanyConnection(id);
                cnn.Open();
                cnn.Execute(sql);
                cnn.Close();
                cnn.Dispose();
            });
        }

        void loadFusionAttributeTypes(SqlConnection company, int fusionTypeID, string type, int? parentID, List<d360.core.entities.Plugins.FusionAttributeType> types)
        {
            types.Where(i => i.ParentID == parentID).ToList().ForEach(t =>
            {
                company.Execute(@"
if not exists(select 1 from FusionAttributeType where ID = @i) 
BEGIN 
SET IDENTITY_INSERT FusionAttributeType ON
INSERT INTO FusionAttributeType (ID, ParentID, FusionTypeID, Name, Assignable) VALUES (@i, @p, @t, @n, 0) 
SET IDENTITY_INSERT FusionAttributeType OFF
END",
                new { i = t.ID, p = parentID, t = fusionTypeID, n = t.Name });

                foreach (var o in t.FusionAttributeTypeFields)
                {
                    var oID = company.Execute(@"
                    declare @ft int
                    if not exists(select 1 from FieldType where Name = @n and [Object] = 'FusionAttributeType' and ObjectID = @oid) 
                    BEGIN 
                    INSERT INTO FieldType (Name, FriendlyName, [Type], [Object], ObjectID, SortOrder, IsRequired, IsListable) VALUES (@n, @f, @t, 'FusionAttributeType', @oid, @s, 0, @l) 
                    END 
                    ",
                    new { n = o.Name, f = o.FriendlyName, t = o.Type, oid = o.FusionAttributeTypeID, s = o.SortOrder, l = o.IsListable });
                }

                loadFusionAttributeTypes(company, fusionTypeID, type, t.ID, types);
            });
        }

        [TestMethod]
        public void SaveCertificate_Success()
        {
            var companyID = 4;
            var sec = new UriSecurityContextProvider() { CompanyID = companyID, ResourceID = 1 };
            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), sec);

            var bytes = File.ReadAllBytes("adfs365.txt.cer");//("SecAuth3Pubcert.cer");
            var dc = new DomainCertificate { Name = "Infogix - 2017 - Office 365", File = bytes };
            community.Add<DomainCertificate>(dc);
        }

        [TestMethod]
        public void Index_Single_Item()
        {
            var companyID = 4;
            var context = getCompanyConnection(companyID);

            var sType = SystemObjects.Artifact.ToString();
            var id = 733;
            AddToIndexModel item = null;

            var fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t and ObjectID = @id", new { t = sType, id }).ToList();
            var a = context.Query(@"
select  A.*, 
        T.Name as ArtifactType, 
        V.Name as SubjectArea 
from    Artifact A 
        inner join ArtifactType T on T.ID = A.ArtifactTypeID and A.ID = @id
        inner join TaxonomyType V on V.ID = A.TaxonomyTypeID
", new { id }).Single();

            item = new AddToIndexModel { Group = "Artifact", CompanyID = companyID, ID = a.ID, Type = a.ArtifactType, RelativeUrl = string.Format("#/artifacts/{0}/{1}", a.ArtifactTypeID, a.ID) };
            item.Fields = new Dictionary<string, string>();
            item.Fields.Add("Name", a.Name);
            item.Fields.Add("Description", a.Description);
            item.Fields.Add("Status", a.Status);
            item.Fields.Add("Type", a.ArtifactType);
            item.Fields.Add("SubjectArea", a.SubjectArea);
            foreach (var f in fields)
            {
                if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
            }

            var source = new ElasticSearchSource();
            source.AddToIndex(item);
        }

        [TestMethod]
        public void ReIndex_Execute_Artifacts()
        {
            var companyID = 4;
            var context = getCompanyConnection(companyID);

            var sType = SystemObjects.Artifact.ToString();
            
            var list = new List<AddToIndexModel>();

            var fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t and ObjectID in (733,732,4651)", new { t = sType}).ToList();

            foreach (var a in context.Query("select A.*, T.Name as ArtifactType, V.Name as SubjectArea from Artifact A inner join ArtifactType T on T.ID = A.ArtifactTypeID inner join TaxonomyType V on V.ID = A.TaxonomyTypeID where A.ID in (733,732,4651)"))
            {
                var item = new AddToIndexModel { Group = "Artifact", CompanyID = companyID, ID = a.ID, Type = a.ArtifactType, RelativeUrl = string.Format("#/artifacts/{0}/{1}", a.ArtifactTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Description", a.Description);
                item.Fields.Add("Status", a.Status);
                item.Fields.Add("Type", a.ArtifactType);
                item.Fields.Add("SubjectArea", a.SubjectArea);
                var subset = fields.Where(i => i.ObjectID == a.ID);
                foreach (var f in subset)
                {
                    if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                }
                list.Add(item);
            }

            var source = new ElasticSearchSource();
            source.AddToIndex(list);
        }

        [TestMethod]
        public void Search_FindArtifact()
        {
            var source = new ElasticSearchSource();
            var results = source.GetSearchResults(4, 1, "Data Warehouse",10,0);
        }
        
        [TestMethod]
        public void Search_ClearIndexGroup()
        {
            var source = new ElasticSearchSource();
            source.ClearIndex(4, "Artifact");            
        }

        [TestMethod]
        public void Search_ClearIndex()
        {
            var source = new ElasticSearchSource();
            source.ClearIndex(4);
        }

        [TestMethod]
        public void Search_RemoveItem()
        {         
            var companyID = 4;
            var context = getCompanyConnection(companyID);

            var sType = SystemObjects.Artifact.ToString();
            var id = 733;
            AddToIndexModel item = null;

            var fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t and ObjectID = @id", new { t = sType, id }).ToList();
            var a = context.Query(@"
select  A.*, 
        T.Name as ArtifactType, 
        V.Name as SubjectArea 
from    Artifact A 
        inner join ArtifactType T on T.ID = A.ArtifactTypeID and A.ID = @id
        inner join TaxonomyType V on V.ID = A.TaxonomyTypeID
", new { id }).Single();

            item = new AddToIndexModel { Group = "Artifact", CompanyID = companyID, ID = a.ID, Type = a.ArtifactType, RelativeUrl = string.Format("#/artifacts/{0}/{1}", a.ArtifactTypeID, a.ID) };
            item.Fields = new Dictionary<string, string>();
            item.Fields.Add("Name", a.Name);
            item.Fields.Add("Description", a.Description);
            item.Fields.Add("Status", a.Status);
            item.Fields.Add("Type", a.ArtifactType);
            item.Fields.Add("SubjectArea", a.SubjectArea);
            foreach (var f in fields)
            {
                if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
            }

            var source = new ElasticSearchSource();
            source.AddToIndex(item);

            var delItem = new RemoveFromIndexModel
            {
                CompanyID = 4,
                ID = 732,
                Group = "Artifact"
            };

            source.RemoveFromIndex(delItem);
        }

        [TestMethod]
        public void Search_RemoveItems()
        {
            var companyID = 4;
            var context = getCompanyConnection(companyID);

            var sType = SystemObjects.Artifact.ToString();
            //var list = new List<UpdateInIndexModel>();
            var list = new List<AddToIndexModel>();

            var fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t and ObjectID in (733,732,4651)", new { t = sType }).ToList();

            foreach (var a in context.Query("select A.*, T.Name as ArtifactType, V.Name as SubjectArea from Artifact A inner join ArtifactType T on T.ID = A.ArtifactTypeID inner join TaxonomyType V on V.ID = A.TaxonomyTypeID where A.ID in (733,732,4651)"))
            {
                var item = new AddToIndexModel { Group = "Artifact", CompanyID = companyID, ID = a.ID, Type = a.ArtifactType, RelativeUrl = string.Format("#/artifacts/{0}/{1}", a.ArtifactTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Description", a.Description);
                item.Fields.Add("Status", a.Status);
                item.Fields.Add("Type", a.ArtifactType);
                item.Fields.Add("SubjectArea", a.SubjectArea);
                var subset = fields.Where(i => i.ObjectID == a.ID);
                foreach (var f in subset)
                {
                    if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                }
                list.Add(item);
            }

            var source = new ElasticSearchSource();
            source.AddToIndex(list);

            // now delete them

            var items = new List<RemoveFromIndexModel>();
            items.Add(new RemoveFromIndexModel
            {
                CompanyID = 4,
                ID = 733,
                Group = "Artifact"
            });

            items.Add(new RemoveFromIndexModel
            {
                CompanyID = 4,
                ID = 4651,
                Group = "Artifact"
            });

            source.RemoveFromIndex(items);
        }

        [TestMethod]
        public void Search_UpdateItems()
        {
            var companyID = 4;
            var context = getCompanyConnection(companyID);

            var sType = SystemObjects.Artifact.ToString();
            //var list = new List<UpdateInIndexModel>();
            var list = new List<AddToIndexModel>();

            var fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t and ObjectID in (733,732,4651)", new { t = sType }).ToList();

            foreach (var a in context.Query("select A.*, T.Name as ArtifactType, V.Name as SubjectArea from Artifact A inner join ArtifactType T on T.ID = A.ArtifactTypeID inner join TaxonomyType V on V.ID = A.TaxonomyTypeID where A.ID in (733,732,4651)"))
            {
                var item = new AddToIndexModel { Group = "Artifact", CompanyID = companyID, ID = a.ID, Type = a.ArtifactType, RelativeUrl = string.Format("#/artifacts/{0}/{1}", a.ArtifactTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Description", a.Description);
                item.Fields.Add("Status", a.Status);
                item.Fields.Add("Type", a.ArtifactType);
                item.Fields.Add("SubjectArea", a.SubjectArea);
                var subset = fields.Where(i => i.ObjectID == a.ID);
                foreach (var f in subset)
                {
                    if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                }
                list.Add(item);
            }

            var source = new ElasticSearchSource();
            source.AddToIndex(list);

            // now delete them
            var updateList = new List<UpdateInIndexModel>();

            foreach (var item in list)
            {
                var updItem = new UpdateInIndexModel
                {
                    CompanyID = 4,
                    Group = item.Group,      
                    ID = item.ID,
                    RelativeUrl = item.RelativeUrl                        
                };

                updItem.Fields = new Dictionary<string, string>();
                foreach (var field in item.Fields)
                {
                    updItem.Fields.Add(field.Key, "hi mom");
                }

                updateList.Add(updItem);
            }
            
            source.UpdateInIndex(updateList);
        }

        [TestMethod]
        public void ReIndex_Execute_InformationModels()
        {
            var companyID = 1;
            var context = getCompanyConnection(companyID);

            var sType = SystemObjects.Taxonomy.ToString();
            var list = new List<AddToIndexModel>();

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
                list.Add(item);
            }

            //var source = new AzureSearchSource();
            //source.ClearIndex(companyID, "Taxonomy");
            //source.AddToIndex(list);
        }

        [TestMethod]
        public void ReleaseBlobFileLock_Success()
        {
            var folder = "d3s-searchindex-10";
            var file = "write.lock";

            var storage = new AzureStorageProvider();
            Assert.IsTrue(storage.ReleaseLockOnBlobFile(folder, file));
        }

        [TestMethod]
        public void DeployFusionLookupEagleStarTagData()
        {
            var companyID = 22;

            var company = getCompanyConnection(companyID);

            //var starTagFieldTypeID = 51622;
            var starTagFieldTypeID = company.Query<int>("select id from fieldtype where name = 'startag' and[object] = 'FusionAttributeType' and objectid = 205").FirstOrDefault();

            if (starTagFieldTypeID <= 0) throw new Exception("Unable to find StarTag field associated with fusionattribute 205.");

            //delete fields with null star tags or star tags that are not numbers
            //delete from field where (value = '' or IsNumeric(value) = 0) and objecttype = 'FusionAttribute' and fieldtypeid = (select id from fieldtype where name = 'startag' and[object] = 'FusionAttributeType' and objectid = 205)
            company.Execute("delete from field where (value = '' or IsNumeric(value) = 0) and objecttype = 'FusionAttribute' and fieldtypeid = (select id from fieldtype where name = 'startag' and[object] = 'FusionAttributeType' and objectid = 205)");

            //load id's of fields that dont have star tags         
            var fusionAttributes = company.Query<dynamic>(@"select
                                                fa.id,fa.name from fusionattribute fa where fa.fusionattributetypeid = 205 and fa.sourceid like 'security%'
                                                            and
                                        not exists(select 1 from field f where fa.id = f.objectid and f.objecttype = 'fusionattribute' and f.fieldtypeid = (select id from fieldtype where name = 'startag' and[object] = 'FusionAttributeType' and objectid = 205))").ToList();

            //if there are none we are done
            if (fusionAttributes.Count == 0) return;

            //load all the star tags from the community
            var eagleStarTagMap = LoadEagleStarTagsFromCommunity();

            //need to find tag for given column
            foreach (var item in fusionAttributes)
            {
                // try to get star tag for that name from the map
                var fieldName = (string)item.name;
                var fusionAttributeId = (int)item.id;
                var tag = string.Empty;

                //if found add to the fields 
                if (!eagleStarTagMap.TryGetValue(fieldName, out tag)) continue;

                //add an entry to field table for this fusion attribute for the star tag field type
                company.Execute(@"                            
			                      INSERT INTO [Field] ([ObjectType], [ObjectID], [FieldTypeID], [Value]) values ('FusionAttribute', @id, @fieldId, @val)
                            ",
                    new { type = "FusionAttributeType", id = fusionAttributeId, val = tag, fieldId = starTagFieldTypeID });
            }       
            
        }

        private Dictionary<string,string> LoadEagleStarTagsFromCommunity()
        {
            Dictionary<string, string> hash = new Dictionary<string, string>();

            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), new UriSecurityContextProvider());

            var mapping = community.Query<dynamic>(@"
                    select 
	                    flv.[key],
	                    flv.[value]
                    FROM 
	                    [plugin].[FusionLookupType] flt
	                    inner join [plugin].[fusionlookupvalue] flv on(flt.id = flv.fusionlookuptypeid)
                    where
	                    flt.name = 'EagleStarTags'
                 ").ToList();

            foreach (var item in mapping)
            {
                hash.Add(item.key, item.value);
            }
            return hash;
        }
    }
}
