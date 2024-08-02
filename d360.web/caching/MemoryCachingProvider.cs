using d360.extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace d360.web.caching
{
	public class MemoryCachingProvider : ICachingProvider
	{
		public MemoryCache Cache { get { return MemoryCache.Default; } }

		public bool ListItemExists<T, TIdentifier>(string name, TIdentifier id)
		{
			var d = getOrCreateDictionary<T, TIdentifier>(name);

			return d.Keys.Any(i => i.Equals(id));
		}

		public bool ItemExists<T>(string name)
		{
			return Cache.Get(name) is T;
		}

		public T GetItem<T>(string name)
		{
			var obj = Cache.Get(name);

			if (obj is T)
			{
				return (T)obj;
			}
			else
			{
				return default;
			}
		}

		public bool CachePresent<T>(string name)
		{
			var obj = Cache.Get(name);
			return (obj is T);
		}

		public T GetItemInListByID<T, TIdentifier>(string name, TIdentifier id)
		{
			var dictionary = getOrCreateDictionary<T, TIdentifier>(name);

			if (dictionary.TryGetValue(id, out T obj))
			{
				return obj;
			}
			else
			{
				return default(T);
			}
		}

		CacheItemPolicy createPolicy(bool isAbsoluteExpiration = true, int expirationMinutes = 10)
		{
			return (isAbsoluteExpiration) ?
				new CacheItemPolicy { AbsoluteExpiration = DateTime.UtcNow.AddMinutes(expirationMinutes) } :
				new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(expirationMinutes) };
		}

		public void SetItem<T>(string name, T item, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
		{
			if (CachePresent<T>(name))
			{
				RemoveItem(name);
			}

			Cache.Add(name, item, createPolicy(isAbsoluteExpiration, expirationMinutes));
		}

		public void SetList<T, TIdentifier>(string name, SortedDictionary<TIdentifier, T> list, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
		{
			if (CachePresent<T>(name))
			{
				RemoveItem(name);
			}

			Cache.Add(name, list, createPolicy(isAbsoluteExpiration, expirationMinutes));
		}

		public void SetItemInListByID<T, TIdentifier>(string name, TIdentifier id, T item, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
		{
			var dictionary = getOrCreateDictionary<T, TIdentifier>(name);

			if (dictionary.ContainsKey(id))
			{
				dictionary[id] = item;
			}
			else
			{
				dictionary.TryAdd(id, item);
			}

			Cache.Add(name, dictionary, createPolicy(isAbsoluteExpiration, expirationMinutes));
		}

		private ConcurrentDictionary<TIdentifier, T> getOrCreateDictionary<T, TIdentifier>(string name)
		{
			var list = Cache.Get(name);
			ConcurrentDictionary<TIdentifier, T> dictionary;

			if (list != null)
			{
				dictionary = (ConcurrentDictionary<TIdentifier, T>)list;
			}
			else
			{
				dictionary = new ConcurrentDictionary<TIdentifier, T>();
			}

			return dictionary;
		}

		public void RemoveItem(string name)
		{
			Cache.Remove(name);
		}
	}
}