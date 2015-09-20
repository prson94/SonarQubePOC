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
    public class DomainListIndexer: IIndexer
    {
        #region DI

        D360Context Context;
        ISearchSource SearchSource;

        public DomainListIndexer(D360Context context, ISearchSource searchSource)
        {
            Context = context;
            SearchSource = searchSource;
        }

        #endregion

        public void Build()
        {
            Trace.TraceInformation("Processing DomainListIndexer.");
            var sType = SystemObjects.DomainList.ToString();
            var list = new List<AddToIndexItem>();

            Context.ExecuteCompanyFederationCommand();

            foreach (var a in Context.DomainLists.Include(i => i.DomainListGroup).Include(i => i.DomainListType).AsQueryable())
            {
                try
                {
                    var item = new AddToIndexItem { CompanyID = Context.CurrentCompanyID, ID = a.ID, Type = a.DomainListType.Name, Url = string.Format("#/domains/{0}/{1}", a.DomainListTypeID, a.ID) };
                    item.Fields.Add("Name", a.Name);
                    item.Fields.Add("Description", a.Description);
                    item.Fields.Add("DomainListGroup", a.DomainListGroup.Name);
                    list.Add(item);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("DomainListIndexer: {0}, {1}", ex.Message, ex.StackTrace);
                }
            }
            Trace.TraceInformation("Processing DomainListIndexer: ReIndexing {0} items in list.", list.Count);
            SearchSource.ReIndex(Context.CurrentCompanyID, sType, list);
        }
    }
}
