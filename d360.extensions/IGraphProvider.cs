using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions
{
    interface IGraphProvider
    {
        void AddVertices<T>(int companyId, IEnumerable<T> items);

        void ClearData(int companyId);

        Task AddVertex(int companyId, string id, string objectType, IDictionary<string,string> properties);
        Task AddEdge(int companyId, string startId, string endId, string relationshipName, IDictionary<string, string> properties);

        Task DeleteEdge(int companyId, string field, string value);
    }
}
