using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using d360.core.queue;
using System.Collections.Generic;
using d360.core;
using Dapper;
using System.Linq;
using d360.core.entities;
using d360.extensions.search;

namespace d360.test.jobs
{
    [TestClass]
    public class IndexerTests : BaseTest
    {
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
    }
}
