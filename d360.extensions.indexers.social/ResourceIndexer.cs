using d360.core;
using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace d360.extensions.indexers.social
{
    public class ResourceIndexer: IIndexer
    {
        #region DI

        D360Context Context;
        ISearchSource SearchSource;

        public ResourceIndexer(D360Context context, ISearchSource searchSource)
        {
            Context = context;
            SearchSource = searchSource;
        }

        #endregion

        public void Build()
        {
            Trace.TraceInformation("Processing ResourceIndexer.");
            var sType = SystemObjects.Resource.ToString();
            var list = new List<AddToIndexItem>();

            Context.ExecuteCompanyFederationCommand();

            var fields = Context.FieldWithRelations.Where(i => i.CompanyID == Context.CurrentCompanyID && i.ObjectType == sType).ToList();

            Trace.TraceInformation("ResourceIndexer: Pulled {0} fields.", fields.Count);

            foreach (var a in Context.CompanyResources.AsQueryable())
            {
                try
                {
                    var item = new AddToIndexItem { CompanyID = Context.CurrentCompanyID, ID = a.ResourceID, Type = SystemObjects.Resource.ToString(), Url = string.Format("#/resources/{0}",  a.ResourceID) };//a.ResourceTypeID,
                    item.Fields.Add("Name", a.FormatDisplayName());
                    item.Fields.Add("Description", "");
                    var subset = fields.Where(i => i.ObjectID == a.ResourceID);
                    foreach (var f in subset)
                    {
                        if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                    }
                    list.Add(item);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("ResourceIndexer: {0}, {1}", ex.Message, ex.StackTrace);
                }
            }
            Trace.TraceInformation("Processing ResourceIndexer: ReIndexing {0} items in list.", list.Count);
            SearchSource.ReIndex(Context.CurrentCompanyID, sType, list);
        }
    }
}
