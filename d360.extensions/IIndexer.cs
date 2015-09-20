using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions
{
    /// <summary>
    /// Used as the interface for defining custom indexers that the indexing service will use to gather data for the search index.  Wather internal data or external.
    /// </summary>
    public interface IIndexer
    {
        void Build();
    }
}
