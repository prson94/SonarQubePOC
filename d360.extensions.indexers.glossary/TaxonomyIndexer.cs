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
    public class TaxonomyIndexer: IIndexer
    {
        #region DI

        D360Context Context;
        ISearchSource SearchSource;

        public TaxonomyIndexer(D360Context context, ISearchSource searchSource)
        {
            Context = context;
            SearchSource = searchSource;
        }

        #endregion

        public void Build()
        {
            Trace.TraceInformation("Processing TaxonomyIndexer.");

            var sType = SystemObjects.Taxonomy.ToString();
            var list = new List<AddToIndexItem>();
            
            Context.ExecuteCompanyFederationCommand();

            var fields = Context.FieldWithRelations.Where(i => i.ObjectType == sType).ToList();
            foreach (var a in Context.Taxonomies.Include(i => i.TaxonomyType).AsQueryable())
            {
                try
                {
                    var item = new AddToIndexItem { CompanyID = Context.CurrentCompanyID, ID = a.ID, Type = a.TaxonomyType.Name, Url = string.Format("#/catalogs/{0}/{1}", a.TaxonomyType, a.ID) };
                    item.Fields = new Dictionary<string, object>();
                    item.Fields.Add("Name", a.Name);
                    item.Fields.Add("Path", a.TextPath);
                    var subset = fields.Where(i => i.ObjectID == a.ID);
                    foreach (var f in subset)
                    {
                        if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                    }
                    list.Add(item);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("TaxonomyIndexer: {0}, {1}", ex.Message, ex.StackTrace);
                }
            }
            Trace.TraceInformation("Processing TaxonomyIndexer: ReIndexing {0} items in list.", list.Count);
            SearchSource.ReIndex(Context.CurrentCompanyID, sType, list);
        }
    }
}
