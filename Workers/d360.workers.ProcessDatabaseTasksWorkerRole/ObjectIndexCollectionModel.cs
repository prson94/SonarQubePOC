using d360.core.queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace d360.workers.ProcessDatabaseTasksWorkerRole
{
    public class ObjectIndexCollectionModel
    {
        public ObjectIndexCollectionModel()
        {
            Adds = new List<AddToIndexModel>();
            Deletes = new List<RemoveFromIndexModel>();
            Updates = new List<UpdateInIndexModel>();
        }

        public List<AddToIndexModel> Adds { get; set; }
        public List<RemoveFromIndexModel> Deletes { get; set; }
        public List<UpdateInIndexModel> Updates { get; set; }
    }
}
