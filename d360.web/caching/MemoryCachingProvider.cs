using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;

using d360.extensions;

namespace d360.web.caching
{
    public class MemoryCachingProvider : ICachingProvider
    {
        public bool ListItemExists<T, TIdentifier>(string name, TIdentifier id)
        {
            var d = getOrCreateDictionary<T, TIdentifier>(name);

            return d.Keys.Any(i => i.Equals(id));
        }

        public bool ItemExists<T>(string name)
        {
            return HttpContext.Current.Cache[name] is T;
        }

        public T GetItem<T>(string name)
        {
            var obj = HttpContext.Current.Cache.Get(name);

            if (obj is T)
            {
                return (T)obj;
            }
            else
            {
                return default(T);
            }
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

        public void SetItem<T>(string name, T item, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            bool cachePresent = false;

            try
            {
                T obj = (T)HttpContext.Current.Cache.Get(name);
                cachePresent = obj != null;
            }
            catch
            {
            }

            if (cachePresent)
            {
                RemoveItem(name);
            }


            if (isAbsoluteExpiration)
            {
                HttpContext.Current.Cache.Insert(name, item,
                    null, DateTime.UtcNow.AddMinutes(expirationMinutes),
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.Default,
                    null);
            }
            else
            {
                HttpContext.Current.Cache.Insert(name, item,
                    null,
                    Cache.NoAbsoluteExpiration,
                    TimeSpan.FromMinutes(expirationMinutes),
                    CacheItemPriority.Default,
                    null);
            }
        }

        public void SetList<T, TIdentifier>(string name, SortedDictionary<TIdentifier, T> list, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            var obj = HttpContext.Current.Cache.Get(name);
            if (obj != null)
            {
                RemoveItem(name);
            }

            if (isAbsoluteExpiration)
            {
                HttpContext.Current.Cache.Insert(name, list,
                    null, DateTime.UtcNow.AddMinutes(expirationMinutes),
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.Default,
                    null);
            }
            else
            {
                HttpContext.Current.Cache.Insert(name, list,
                    null,
                    Cache.NoAbsoluteExpiration,
                    TimeSpan.FromMinutes(expirationMinutes),
                    CacheItemPriority.Default,
                    null);
            }
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

            if (isAbsoluteExpiration)
            {
                HttpContext.Current.Cache.Insert(name, dictionary,
                    null, DateTime.UtcNow.AddMinutes(expirationMinutes),
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.Default,
                    null);
            }
            else
            {
                HttpContext.Current.Cache.Insert(name, dictionary,
                    null,
                    Cache.NoAbsoluteExpiration,
                    TimeSpan.FromMinutes(expirationMinutes),
                    CacheItemPriority.Default,
                    null);
            }
        }

        private ConcurrentDictionary<TIdentifier, T> getOrCreateDictionary<T, TIdentifier>(string name)
        {
            var list = HttpContext.Current.Cache.Get(name);
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
            HttpContext.Current.Cache.Remove(name);
        }
    }
}
