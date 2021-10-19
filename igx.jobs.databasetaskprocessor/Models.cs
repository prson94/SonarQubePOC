using d360.core.queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace igx.jobs.databasetaskprocessor
{
    public static class ThreadSafeRandom
    {
        [ThreadStatic]
        private static Random Local;

        public static Random ThisThreadsRandom
        {
            get { return Local ?? (Local = new Random(unchecked(System.Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
        }
    }

    static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class ObjectIndexCollectionModel
    {
        public ObjectIndexCollectionModel()
        {
            Adds = new ConcurrentBag<IndexObjectModel>();
            Deletes = new ConcurrentBag<IndexObjectModel>();
            Updates = new ConcurrentBag<IndexObjectModel>();
            UpsertByUid = new ConcurrentBag<Guid>();
            UpsertByObject = new ConcurrentBag<Tuple<string, long>>();
            UpsertPathByAssetId = new ConcurrentBag<long>();
        }

        public ConcurrentBag<IndexObjectModel> Adds { get; set; }
        public ConcurrentBag<IndexObjectModel> Deletes { get; set; }
        public ConcurrentBag<IndexObjectModel> Updates { get; set; }
        public ConcurrentBag<Guid> UpsertByUid { get; set; }
        public ConcurrentBag<Tuple<string, long>> UpsertByObject { get; set; }
        public ConcurrentBag<long> UpsertPathByAssetId { get; set; }

        public bool ContainsIndexerCollections()
        {
            return UpsertByObject.Any() || UpsertByUid.Any() || UpsertPathByAssetId.Any();
        }
    }

    public class QueueTask
    {
        public Guid ID { get; set; }
        public string Action { get; set; }
        public string Custom { get; set; }
        public string Object { get; set; }
        public int ObjectID { get; set; }
        public DateTime Date { get; set; }
        public string MachineAssigned { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
        public int NumberOfRetries { get; set; }
        public short Priority { get; set; }
        public long AssetID { get; set; }
    }
}
