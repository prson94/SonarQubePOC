using d360.core;
using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace d360.extensions.search.azure.Indexers
{
    public class DomainListIndexer: IIndexer
    {
        #region DI

        CompanyContext Context;
        ISearchSource SearchSource;

        public DomainListIndexer(CompanyContext context, ISearchSource searchSource)
        {
            Context = context;
            SearchSource = searchSource;
        }

        #endregion

        public void Build()
        {
            Trace.TraceInformation("Processing DomainListIndexer.");
            var sType = SystemObjects.Domain.ToString();
            var list = new List<AddToIndexItem>();

            foreach (var a in Context.Domains.Include(i => i.DomainGroup).Include(i => i.DomainType).AsQueryable())
            {
                try
                {
                    var item = new AddToIndexItem { ID = a.ID, Type = a.DomainType.Name, Url = string.Format("#/domains/{0}/{1}", a.DomainTypeID, a.ID) };
                    item.Fields.Add("Name", a.Name);
                    item.Fields.Add("Description", a.Description);
                    item.Fields.Add("DomainListGroup", a.DomainGroup.Name);
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
