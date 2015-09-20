using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.model;
using System.Xml.Linq;
using System.Diagnostics;
using d360.core.entities;
using System.Data;

namespace d360.model.interfaces
{
    public interface IDomainListRepository : IRepository<DomainList, int>
    {
        IEnumerable<HierarchyItem> GetDomainListInHierarchy(int id);
        int GetRelatedItemCount(int id);
    }

    public interface IDomainListGroupRepository : IRepository<DomainListGroup, int>
    {
    }

    public interface IDomainListItemRepository : IRepository<DomainListItem, int>
    {
        List<DomainListItem> GetMatchingDomainListItems(long DomainListItemID);
    }

    public interface IDomainListTypeRepository : IRepository<DomainListType, int>
    {
    }
}
