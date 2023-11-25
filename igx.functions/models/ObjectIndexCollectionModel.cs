using d360.core.queue;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace igx.functions.consumption.models
{
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
}
