using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.model;
using d360.extensions.info;
using d360.core;
using d360.core.queue;
using System.Collections.Generic;
using d360.extensions.caching;
using d360.extensions.search;
using d360.extensions.queue;
using d360.core.entities;
using System.Data.SqlClient;
using System.Configuration;
using Dapper;
using System.IO;
using SpreadsheetLight;
using Microsoft.WindowsAzure.Storage;
using d360.extensions.storage;

namespace d360.test.jobs
{
    [TestClass]
    public class RunJobsTest
    {
        List<int> getCompanies(bool developmentOnly = false)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.Open();
            var sql = "select ID from Company where ";
            if (developmentOnly)
            {
                sql += "DatabaseServerID = 6 and ";
            }
            sql += "Status = 'Active'";
            var list = cnn.Query<int>(sql).ToList();
            cnn.Close();
            cnn.Dispose();

            return list;
        }

        SqlConnection getCompanyConnection(int companyID)
        {
            var cnn = new SqlConnection(constants.COMMUNITY_DATABASE_CONNECTION);
            cnn.Open();
            var db = cnn.Query<DatabaseServer>(
                @"select D.* from Company C inner join DatabaseServer D on D.ID = C.DatabaseServerID where C.ID = @id",
                new { id = companyID }
            ).SingleOrDefault();
            cnn.Close();
            cnn.Dispose();

            if (db != null)
            {
                cnn = new SqlConnection(
                    string.Format("server={0};Database=D3S_{1};User ID={2};Password={3}", db.Server, companyID, db.Username, db.Password)
                );
                db = null;
            }
            return cnn;
        }

        [TestMethod]
        public void DeployFusionConnector()
        {
            var companyID = 22; //10
            var fusionTypeID = 16;
            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), new UriSecurityContextProvider());

            var fusionType = community.GetById<d360.core.entities.Plugins.FusionType>(fusionTypeID, i => i.FieldTypes);
            var fusionAttributeTypes = community.Filter<d360.core.entities.Plugins.FusionAttributeType>(i => i.FusionTypeID == fusionTypeID, i => i.FieldTypes).ToList();
            var fusionIntersectTypes = community.Filter<d360.core.entities.Plugins.FusionIntersectType>(i => i.FusionTypeID == fusionTypeID).ToList();

            var company = getCompanyConnection(companyID);

            company.Execute(@"SET IDENTITY_INSERT FusionType ON
if not exists(select 1 from FusionType where ID = @i) BEGIN INSERT INTO FusionType (ID, Name, Description) VALUES (@i, @n, @d) END
SET IDENTITY_INSERT FusionType OFF", new { i = fusionType.ID, n = fusionType.Name, d = fusionType.Description });

            loadFields(company, "FusionType", fusionType.ID, fusionType.FieldTypes);

            loadFusionAttributeTypes(company, fusionType.ID, "FusionAttributeType", null, fusionAttributeTypes);

            fusionIntersectTypes.ForEach(t => {
                company.Execute(@"
if not exists(select 1 from IntersectTypeNode S inner join IntersectTypeNode T on S.IntersectTypeID = T.IntersectTypeID and S.ID <> T.ID and S.ObjectType = @type and S.ObjectID = @si and T.ObjectType = @type and T.ObjectID = @ti) 
BEGIN 
            declare @intersectTypeID int

			INSERT INTO IntersectType (UpdatedOn, UpdatedBy) values (getutcdate(), 0)
			set @intersectTypeID = SCOPE_IDENTITY()

			INSERT INTO IntersectTypeNode (IntersectTypeID, ObjectType, ObjectID, [Order]) values (@intersectTypeID, @type, @si, 1)
			INSERT INTO IntersectTypeNode (IntersectTypeID, ObjectType, ObjectID, [Order]) values (@intersectTypeID, @type, @ti, 2)
END", 
                new { type = "FusionAttributeType", si = t.StartFusionAttributeTypeID, ti = t.EndFusionAttributeTypeID, ro = t.ReadOnly });
            });
        }

        [TestMethod]
        public void DeployDatabaseChanges()
        {
            #region SQL
            var sql = @"ALTER TRIGGER [dbo].[Artifact_AfterUpdate]
   ON  [dbo].[Artifact] 
   AFTER UPDATE
AS 
	SET NOCOUNT ON;
	declare @ot varchar(50) = 'Artifact'
	INSERT INTO [queue].[Task] ([Action], [Custom], [Object], [ObjectID])
        select 'Update', [queue].WriteIndexXml('', @ot, ID, coalesce(UpdatedBy, 0)), @ot, ID from inserted;

	with S as	(
				select	ID,
						ParentID
				from	inserted
				union all
				select	A.ID,
						A.ParentID
				from	Artifact A
						inner join S on S.ID = A.ParentID
				)
	update	T
	set		T.TextPath = utility.GetBreadcrumbString('Artifact', S.ID, '/')
	from	Artifact T
			inner join S on S.ID = T.ID


	merge	[cache].[Object] as T
	using	(
			select	'Artifact' as [Object],
					ID as ObjectID,
					--Name as Name,
					--TextPath as TextPath,
					'ArtifactType' as ObjectType,
					ArtifactTypeID as ObjectTypeID--,
					--[dbo].[GenerateObjectUrl]('Artifact', ArtifactTypeID, ID) as Url
			from	inserted
			) as S
	on		T.[Object] = S.[Object] and T.[ObjectID] = S.[ObjectID]
	when	matched then
			update set	T.[ObjectType] = S.[ObjectType],
						T.[ObjectTypeID] = S.[ObjectTypeID]--,
						--T.[Name] = S.[Name],
						--T.[TextPath] = S.[TextPath]
	when	not matched then
			insert	(
					[Object],[ObjectID], --[Name], [TextPath], 
					[ObjectType], [ObjectTypeID]--, [Url]
					)
			values	(
					S.[Object], S.[ObjectID], --S.[Name], S.[TextPath], 
					S.[ObjectType], S.[ObjectTypeID]--, S.[Url]
					);";

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
                loadFields(company, type, t.ID, t.FieldTypes);
                loadFusionAttributeTypes(company, fusionTypeID, type, t.ID, types);
            });
        }

        void loadFields(SqlConnection company, string type, int id, ICollection<d360.core.entities.Plugins.FieldType> fieldTypes)
        {
            int sort = 1;
            foreach (var o in fieldTypes)
            {
                var oID = company.Execute(@"
declare @ft int
if not exists(select 1 from FieldType where Name = @n and [Object] = @ot and ObjectID = @oid) 
BEGIN 
INSERT INTO FieldType (Name, FriendlyName, [Type], [Object], ObjectID, SortOrder, IsRequired, IsListable) VALUES (@n, @f, @t, @ot, @oid, @s, 0, 1) 
END 
", 
                new { n = o.Name, f = o.FriendlyName, t = o.Type, ot = type, oid = id, s = sort });
                sort++;
            }        
        }

        [TestMethod]
        public void SaveCertificate_Success()
        {
            var companyID = 15;
            var sec = new UriSecurityContextProvider() { CompanyID = companyID, ResourceID = 1 };
            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), sec);

            var bytes = File.ReadAllBytes("GMO.cer");//("SecAuth3Pubcert.cer");
            var dc = new DomainCertificate { Name = "GMO Certificate", File = bytes };
            community.Add<DomainCertificate>(dc);
        }

        [TestMethod]
        public void ReIndex_Execute_Artifacts()
        {
            var companyID = 1;
            var context = getCompanyConnection(companyID);

            var sType = SystemObjects.Artifact.ToString();
            var list = new List<UpdateInIndexModel>();//<AddToIndexModel>();

            var fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t", new { t = sType}).ToList();

            foreach (var a in context.Query("select A.*, T.Name as ArtifactType, V.Name as Vocabulary from Artifact A inner join ArtifactType T on T.ID = A.ArtifactTypeID inner join Vocabulary V on V.ID = A.VocabularyID"))
            {
                var item = new UpdateInIndexModel { Group = "Artifact", CompanyID = companyID, ID = a.ID, Type = a.ArtifactType, RelativeUrl = string.Format("#/artifacts/{0}/{1}", a.ArtifactTypeID, a.ID) };
                item.Fields = new Dictionary<string, string>();
                item.Fields.Add("Name", a.Name);
                item.Fields.Add("Description", a.Description);
                item.Fields.Add("Status", a.Status);
                item.Fields.Add("Type", a.ArtifactType);
                item.Fields.Add("Vocabulary", a.Vocabulary);
                var subset = fields.Where(i => i.ObjectID == a.ID);
                foreach (var f in subset)
                {
                    if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                }
                list.Add(item);
            }

            var source = new AzureSearchSource();
            source.ClearIndex(companyID, "Artifact");
            source.UpdateInIndex(list);
            //source.AddToIndex(list);
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

            var source = new AzureSearchSource();
            source.ClearIndex(companyID, "Taxonomy");
            source.AddToIndex(list);
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
