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
            var companyID = 13;
            var fusionTypeID = 5;
            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), new StaticSecurityContextProvider());

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

			INSERT INTO IntersectType (AllowSourcing) values (0)
			set @intersectTypeID = SCOPE_IDENTITY()

			INSERT INTO IntersectTypeNode (IntersectTypeID, ObjectType, ObjectID, [Order], IsHierarchical, IsSourcingItem) values (@intersectTypeID, @type, @si, 1, 0, 0)
			INSERT INTO IntersectTypeNode (IntersectTypeID, ObjectType, ObjectID, [Order], IsHierarchical, IsSourcingItem) values (@intersectTypeID, @type, @ti, 2, 0, 0)
END", 
                new { type = "FusionAttributeType", si = t.StartFusionAttributeTypeID, ti = t.EndFusionAttributeTypeID, ro = t.ReadOnly });
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

        string getConnectionString(int id, string server, string username, string password)
        {
            return string.Format("server={0};Database=D3S_{1};User ID={2};Password={3}", server, id, username, password);
        }


        SqlConnection GetCompanyConnection(int companyID)
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
                cnn = new SqlConnection(getConnectionString(companyID, db.Server, db.Username, db.Password));
                db = null;
            }
            return cnn;
        }

        [TestMethod]
        public void SaveCertificate_Success()
        {
            var companyID = 6;
            var sec = new StaticSecurityContextProvider() { RawCompanyID = companyID.ToString(), RawUserID = "mike@data3sixty.com" };
            var community = new CommunityContext(new DummyCachingProvider(), new AzureQueueSource(), sec);
            
            var bytes = File.ReadAllBytes("SecAuth3Pubcert.cer");
            var dc = new DomainCertificate { Name = "American Century Public Certificate", File = bytes };
            community.Add<DomainCertificate>(dc);
        }

        [TestMethod]
        public void ReIndex_Execute_Artifacts()
        {
            var companyID = 1;
            var context = GetCompanyConnection(companyID);

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
            var context = GetCompanyConnection(companyID);

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
        public void ProcessBulkLoadFile_Success()
        {
            var cnn11 = getCompanyConnection(1);
            cnn11.Open();
            var load = cnn11.Query<Load>("select * from Load where ID = @id", new { id = 3 }).SingleOrDefault();

            var fields = cnn11.Query<LoadTypeField>(
                "select * from LoadTypeField where LoadTypeID = @id order by SortOrder",
                new { id = load.LoadTypeID }
            ).ToList();
            //cnn11.Close();

            var memoryStream = new MemoryStream(load.File);
            var xls = new SLDocument(memoryStream);

            var stats = xls.GetWorksheetStatistics();
            var rowIndex = stats.StartRowIndex+1;
            while (rowIndex <= stats.EndRowIndex)
            {
                var loadItemID = cnn11.ExecuteScalar<int>("insert into LoadItem (LoadID, RowIndex) values (@l, @r); select SCOPE_IDENTITY()", new { l = load.ID, r = rowIndex });

                var columnIndex = stats.StartColumnIndex;

                while (columnIndex <= stats.EndColumnIndex)
                {
                    var field = fields[columnIndex-1];

                    if (field != null)
                    {
                        cnn11.Execute("insert into LoadItemField (LoadItemID, LoadTypeFieldID, Value) values (@l, @f, @v)", new { l = loadItemID, f = field.ID, v = xls.GetCellValueAsString(rowIndex, columnIndex) });
                    }

                    columnIndex++;
                }

                rowIndex++;
            }

            cnn11.Close();
            cnn11.Dispose();
        }

        //[TestMethod]
        //public void IndexerJob_Execute_Success()
        //{
        //    var job = new IndexerJob();
        //    job.Execute(null);
        //}

        //[TestMethod]
        //public void ProcessQueueJob_Execute_Success()
        //{
        //    var job = new ProcessQueueJob();
        //    job.Execute(null);
        //}
    }
}
