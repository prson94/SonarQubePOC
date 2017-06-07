using System.Web.Mvc;
using System.Linq;
using d360.core;
using System.Collections.Generic;
using d360.web.Models;
using d360.model;
using d360.core.entities;
using System.Xml.Linq;
using d360.core.enums;
using System.Text;
using System.Security.Cryptography;
using System;
using d360.web.Models.Attributes;
using d360.extensions;

namespace d360.web.Repositories
{
    public class SiteMenuRepository
    {
        internal CompanyContext Company;
        internal CommunityContext Community;
        ICachingProvider CachingProvider;
        private string MenuCacheKey;

        public SiteMenuRepository(CommunityContext community, CompanyContext company)
        {
            this.Community = community;
            this.Company = company;
            this.CachingProvider = new extensions.caching.MemoryCachingProvider();
            this.MenuCacheKey = $"Company{Company.CurrentCompanyID}_SiteMenu";
        }

        public void ClearCachedMenu()
        {
            CachingProvider.RemoveItem(MenuCacheKey);
        }

        public List<TopNavigationItem> SiteMenu
        {
            get
            {                
                List<TopNavigationItem> menuItems = CachingProvider.GetItem<List<TopNavigationItem>>(MenuCacheKey);

                if (menuItems == null)
                {
                    menuItems = GetSiteNavigation();

                    CachingProvider.SetItem(MenuCacheKey, menuItems, true, 10);
                }

                return menuItems;
            }        
        }

        private List<TopNavigationItem> GetSiteNavigation()
        {
            List<TopNavigationItem> nodes = null;

            nodes = Company.Query<TopNavigationItem>(string.Format(@"GetSiteNavigation @ResourceID", (Company.CurrentResourceIsAdmin ? "1" : "0")), new { ResourceID = Company.CurrentResourceID }).ToList();

            if (nodes == null) return null;

            var features = Community.Filter<CompanyFeature>(i => i.CompanyID == Company.CurrentCompanyID).ToList();

            nodes.ForEach(n => {
                n.ShouldDisplay = features.Any(f => f.Feature == n.Feature);
                n.NavigationItems = (string.IsNullOrEmpty(n.Items)) ?
                    new List<NavigationItem>() :
                    parseXmlNavigationDocument(XElement.Parse(string.Format("<nav>{0}</nav>", n.Items)), features);
            });

            return nodes;
        }

        List<NavigationItem> parseXmlNavigationDocument(XElement xml, List<CompanyFeature> features)
        {
            var items = new List<NavigationItem>();

            foreach (var el in xml.Elements("nav"))
            {
                bool shouldParse = (el.Element("feature").Value == "0");
                if (!shouldParse)   //further check is required.
                {
                    var feature = (Feature)System.Enum.Parse(typeof(Feature), el.Element("feature").Value);
                    shouldParse = features.Any(i => i.Feature == feature);
                }
                if (shouldParse)
                {
                    var item = new NavigationItem { Name = el.Element("name").Value, Url = el.Element("url").Value };
                    if (el.Element("items") != null)
                    {
                        item.Items = parseXmlNavigationDocument(el.Element("items"), features);
                    }
                    items.Add(item);
                }
            }

            return items;
        }
    }
}