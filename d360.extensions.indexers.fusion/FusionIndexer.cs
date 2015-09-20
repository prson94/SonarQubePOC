using d360.core;
using d360.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using d360.core.entities;

namespace d360.extensions.indexers.fusion
{
    public class FusionIndexer : IIndexer
    {
        #region DI

        D360Context Context;
        ISearchSource SearchSource;

        public FusionIndexer(D360Context context, ISearchSource searchSource)
        {
            Context = context;
            SearchSource = searchSource;
        }

        #endregion

        public void Build()
        {
            Trace.TraceInformation("Processing FusionIndexer.");

            var sType = SystemObjects.Fusion.ToString();
            var list = new List<AddToIndexItem>();

            Context.ExecuteCompanyFederationCommand();

            List<Fusion> fusions = null;
            List<FieldWithRelation> fields = null;
            try
            {
                fusions = Context.FusionTypeConfigurations.Include(i => i.FusionType).Where(i => i.CompanyID == Context.CurrentCompanyID).ToList();
                fields = Context.FieldWithRelations.Where(i => i.CompanyID == Context.CurrentCompanyID && i.ObjectType == sType).ToList();
                fusions.AsParallel().ForAll(a =>
                {
                    try
                    {
                        var item = new AddToIndexItem { CompanyID = Context.CurrentCompanyID, ID = a.ID, Type = a.FusionType.Name, Url = string.Format("#/fusion/{0}/{1}", a.FusionTypeID, a.ID) };
                        item.Fields.Add("Name", a.Name);
                        item.Fields.Add("Description", a.Description);
                        var subset = fields.Where(i => i.ObjectID == a.ID);
                        foreach (var f in subset)
                        {
                            if (!item.Fields.ContainsKey(f.Name)) item.Fields.Add(f.Name, f.FormattedValue);
                        }
                        lock (list) list.Add(item);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("FusionIndexer: {0}, {1}", ex.Message, ex.StackTrace);
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occurred: {0}", ex.Message);
            }
            finally 
            {
                fields = null;
                fusions = null;
            }

            List<LeafFusionAttribute> leaves = null;
            try
            {
                leaves = Context.LeafFusionAttributes.Where(i => i.CompanyID == Context.CurrentCompanyID).ToList();
                leaves.AsParallel().ForAll(a =>
                {
                    try
                    {
                        var item = new AddToIndexItem { CompanyID = Context.CurrentCompanyID, ID = a.ID, Type = a.TypeName, Url = a.Url };
                        item.Fields.Add("Name", a.AttributePath);
                        item.Fields.Add("Description", string.Format("<div>Configuration:{0}<br/>Tab:{1}<br/></div>", a.FusionName, a.Tab));
                        item.Fields.Add("Path", a.TypePath);
                        lock (list) list.Add(item);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("FusionIndexer: {0}, {1}", ex.Message, ex.StackTrace);
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error occurred: {0}", ex.Message);
            }
            finally 
            {
                leaves = null;
            }

            Trace.TraceInformation("Processing FusionIndexer: ReIndexing {0} items in list.", list.Count);
            SearchSource.ReIndex(Context.CurrentCompanyID, sType, list);
        }
    }
}
