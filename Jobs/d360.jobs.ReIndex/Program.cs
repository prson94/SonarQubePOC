using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.WebJobs;
using d360.extensions.search;
using d360.core.queue;
using d360.core;
using Dapper;
using d360.core.entities;

namespace d360.jobs.ReIndex
{
    class Program: FunctionsBase
    {
        static void Main()
        {
            var host = new JobHost(new JobHostConfiguration(d360.core.constants.WEBJOBS_STORAGE_CONNECTION));

            var mex = new List<Exception>();

            try
            {
                var companies = GetActiveCompanyIDs();

                companies.AsParallel().WithDegreeOfParallelism(4).ForAll(companyID =>
                {
                    var context = GetCompanyConnection(companyID);
                    var search = new AzureSearchSource();

                    var sType = "";
                    var source = new AzureSearchSource();
                    var list = new List<AddToIndexModel>();

                    #region Artifacts

                    sType = SystemObjects.Artifact.ToString();

                    var fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t", new { t = sType }).ToList();

                    foreach (var a in context.Query("select A.*, T.Name as ArtifactType, V.Name as Vocabulary from Artifact A inner join ArtifactType T on T.ID = A.ArtifactTypeID inner join Vocabulary V on V.ID = A.VocabularyID"))
                    {
                        var item = new AddToIndexModel { Group = "Artifact", CompanyID = companyID, ID = a.ID, Type = a.ArtifactType, RelativeUrl = string.Format("#/artifacts/{0}/{1}", a.ArtifactTypeID, a.ID) };
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

                    #endregion

                    #region Models

                    sType = SystemObjects.Taxonomy.ToString();

                    fields = context.Query<FieldWithRelation>("select * from FieldWithRelation where ObjectType = @t", new { t = sType }).ToList();

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

                    #endregion

                    #region Attributes

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
                        list.Add(item);
                    }

                    #endregion

                    source.ClearIndex(companyID);
                    source.AddToIndex(list);
                });
            }
            catch (Exception ex)
            {
                mex.Add(ex);
            }

            if (mex.Count > 0) throw new AggregateException("One or more exceptions occurred", mex);
        }
    }
}
