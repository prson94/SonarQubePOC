using d360.core;
using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace d360.extensions.indexers.glossary
{
    public class ArtifactIndexer: IIndexer
    {
        #region DI

        D360Context Context;
        ISearchSource SearchSource;

        public ArtifactIndexer(D360Context context, ISearchSource searchSource)
        {
            Context = context;
            SearchSource = searchSource;
        }

        #endregion

        public void Build()
        {
            Trace.TraceInformation("Processing ArtifactIndexer.");

            var sType = SystemObjects.Artifact.ToString();
            var list = new List<AddToIndexItem>();

            Context.ExecuteCompanyFederationCommand();

            var fields = Context.FieldWithRelations.Where(i => i.ObjectType == sType).ToList();

            foreach (var a in Context.Artifacts.Include(i => i.ArtifactType).AsQueryable())
            {
                try
                {
                    var item = new AddToIndexItem { CompanyID = Context.CurrentCompanyID, ID = a.ID, Type = a.ArtifactType.Name, Url = string.Format("#/artifacts/{0}/{1}", a.ArtifactTypeID, a.ID) };
                    item.Fields = new Dictionary<string, object>();
                    item.Fields.Add("Name", a.Name);
                    item.Fields.Add("Description", a.Description);
                    item.Fields.Add("Status", a.Status);
                    item.Fields.Add("Type", a.ArtifactType.Name);
                    var subset = fields.Where(i => i.ObjectID == a.ID);
                    foreach (var f in subset)
                    {
                        if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                    }
                    list.Add(item);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("ArtifactIndexer: {0}, {1}", ex.Message, ex.StackTrace);
                }

            }

            Trace.TraceInformation("Processing ArtifactIndexer: ReIndexing {0} items in list.", list.Count);
            SearchSource.ReIndex(Context.CurrentCompanyID, sType, list);
        }
    }
}
