using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.extensions
{ 
    public class EdgeModel
    {
        public string StartLabel { get; set; }
        public string StartID { get; set; }
        public string EndLabel { get; set; }
        public string EndID { get; set; }

        public string RelationshipType { get; set; }

        public override bool Equals(object obj)
        {
            var b = obj as EdgeModel;
            if (b == null) return false;

            return b.StartID == this.StartID && b.EndID == this.EndID;
        }

        public override int GetHashCode()
        {
            return (StartID + EndID).GetHashCode();
        }

    }

    public class VertexModel
    {        
        public string ID { get; set; }     
        public string Label { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public override bool Equals(object obj) {
            var b = obj as VertexModel;
            if (b == null) return false;

            return b.ID == this.ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
    }

    interface IGraphProvider
    {
        Task AddObjects<T>(int companyId, IEnumerable<T> items);
        
        Task ClearData(int companyId);

        Task AddVertex(int companyId, string id, string objectType, IDictionary<string,string> properties);
        Task AddEdge(int companyId, string startId, string endId, string relationshipName, IDictionary<string, string> properties);

        Task DeleteEdge(int companyId, string field, string value);
    }
}
