using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Caching;
using d360.extensions;

namespace d360.extensions.caching
{
    public class MemoryCachingProvider: ICachingProvider
    {
        Cache _Cache { get { return System.Web.HttpContext.Current.Cache;  } }

        public bool ListItemExists<T, TIdentifier>(string name, TIdentifier id)
        {
            var d = getOrCreateDictionary<T, TIdentifier>(name);
            return d.Keys.Any(i => i.Equals(id));
        }

        public bool ItemExists<T>(string name)
        {
            return (_Cache[name] is T);
        }

        public T GetItem<T>(string name)
        {
            var obj = _Cache.Get(name);
            if (obj is T)
                return (T)obj;
            else
                return default(T);
        }

        public T GetItemInListByID<T, TIdentifier>(string name, TIdentifier id)
        {
            var dictionary = getOrCreateDictionary<T, TIdentifier>(name);
            T obj;
            if (dictionary.TryGetValue(id, out obj))
                return obj;
            else
                return default(T);
        }

        public void SetItem<T>(string name, T item, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            try
            {
                T obj = (T)_Cache.Get(name);
                _Cache[name] = item;
            }
            catch
            {
                if (isAbsoluteExpiration)
                {
                    _Cache.Add(name, item,
                        null, DateTime.Now.AddMinutes(expirationMinutes),
                        Cache.NoSlidingExpiration,
                        CacheItemPriority.Normal,
                        null);
                }
                else
                {
                    _Cache.Add(name, item,
                        null,
                        Cache.NoAbsoluteExpiration,
                        TimeSpan.FromMinutes(expirationMinutes),
                        CacheItemPriority.Normal,
                        null);
                }
            }
        }

        public void SetList<T, TIdentifier>(string name, SortedDictionary<TIdentifier, T> list, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            var obj = _Cache.Get(name);
            if (obj != null)
            {
                _Cache[name] = list;
            }
            else
            {
                if (isAbsoluteExpiration)
                {
                    _Cache.Add(name, list,
                        null, DateTime.Now.AddMinutes(expirationMinutes),
                        Cache.NoSlidingExpiration,
                        CacheItemPriority.Normal,
                        null);
                }
                else
                {
                    _Cache.Add(name, list,
                        null,
                        Cache.NoAbsoluteExpiration,
                        TimeSpan.FromMinutes(expirationMinutes),
                        CacheItemPriority.Normal,
                        null);
                }
            }
        }

        public void SetItemInListByID<T, TIdentifier>(string name, TIdentifier id, T item, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            var dictionary = getOrCreateDictionary<T, TIdentifier>(name);
            if (dictionary.ContainsKey(id))
                dictionary[id] = item;
            else
                dictionary.Add(id, item);

            try
            {
                T obj = (T)_Cache.Get(name);
                _Cache[name] = dictionary;
            }
            catch
            {
                if (isAbsoluteExpiration)
                {
                    _Cache.Add(name, dictionary,
                        null, DateTime.Now.AddMinutes(expirationMinutes),
                        Cache.NoSlidingExpiration,
                        CacheItemPriority.Normal,
                        null);
                }
                else
                {
                    _Cache.Add(name, dictionary,
                        null,
                        Cache.NoAbsoluteExpiration,
                        TimeSpan.FromMinutes(expirationMinutes),
                        CacheItemPriority.Normal,
                        null);
                }
            }
        }

        private SortedDictionary<TIdentifier, T> getOrCreateDictionary<T, TIdentifier>(string name)
        {
            var list = _Cache.Get(name);
            SortedDictionary<TIdentifier, T> dictionary = null;
            if (list != null)
            {
                dictionary = (SortedDictionary<TIdentifier, T>)list;
            }
            else
            {
                dictionary = new SortedDictionary<TIdentifier, T>();
            }

            return dictionary;
        }


        public void RemoveItem(string name)
        {
            _Cache.Remove(name);
        }
    }
}
