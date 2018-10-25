using System;
using System.Collections.Generic;
using StackExchange.Redis;
using System.Runtime.Serialization;
using System.IO;
using Newtonsoft.Json;

namespace d360.extensions.caching
{
    public static class SerialisationExtensions
    {
        public static byte[] Serialize(this object o)
        {
            if (o == null)
            {
                return null;
            }

            var srlzr = new NetDataContractSerializer();
            using (var memoryStream = new MemoryStream())
            {
                srlzr.Serialize(memoryStream, o);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        public static T Deserialize<T>(this byte[] stream)
        {
            if (stream == null)
            {
                return default(T);
            }

            var srlzr = new NetDataContractSerializer();
            using (MemoryStream memoryStream = new MemoryStream(stream))
            {
                var result = (T)srlzr.Deserialize(memoryStream);
                return result;
            }
        }
    }

    public class RedisCachingProvider: ICachingProvider
    {
        public RedisCachingProvider()
        {            
            
        }

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() => {
            return ConnectionMultiplexer.Connect("d3sdev.redis.cache.windows.net:6380,password=yOxnljCQFbYjVtd0QJR/Vmujy4++VzVQ/J9yjk+JnyI=,ssl=True,abortConnect=False");
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        public bool ListItemExists<T, TIdentifier>(string name, TIdentifier id)
        {
            IDatabase cache = Connection.GetDatabase();
            return cache.SetContains(name, id.ToString());
        }

        public bool ItemExists<T>(string name)
        {
            IDatabase cache = Connection.GetDatabase();

            return cache.KeyExists(name);
        }

        public T GetItem<T>(string name)
        {
            IDatabase cache = Connection.GetDatabase();

            var data = cache.StringGet(name);

            if (!data.IsNull && data.HasValue)
            {                
                return JsonConvert.DeserializeObject<T>(data);
            }
            else
            {
                return default(T);
            }
        }

        public T GetItemInListByID<T, TIdentifier>(string name, TIdentifier id)
        {
            IDatabase cache = Connection.GetDatabase();

            var data = cache.StringGetSet(name, id.ToString());

            if (!data.IsNull && data.HasValue)
            {                
                return JsonConvert.DeserializeObject<T>(data);
            }
            else
            {
                return default(T);
            }
        }

        public void RemoveItem(string name)
        {
            IDatabase cache = Connection.GetDatabase();

            cache.KeyDelete(name);
        }

        public void SetItem<T>(string name, T item, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            TimeSpan expiry;

            if (isAbsoluteExpiration)
            {
                expiry = DateTime.Now.AddMinutes(expirationMinutes) - DateTime.Now;
            }
            else
            {
                expiry = DateTime.Now.AddMinutes(expirationMinutes).TimeOfDay;
            }
            IDatabase cache = Connection.GetDatabase();

            //_Cache.StringSet(name, item.Serialize(), expiry);
            cache.StringSet(name, JsonConvert.SerializeObject(item), expiry);
        }

        public void SetList<T, TIdentifier>(string name, SortedDictionary<TIdentifier, T> list, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            IDatabase cache = Connection.GetDatabase();
            cache.SetAdd(name, JsonConvert.SerializeObject(list));//entries);
        }

        public void SetItemInListByID<T, TIdentifier>(string name, TIdentifier id, T item, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            var dictionary = getOrCreateDictionary<T, TIdentifier>(name);

            if (dictionary.ContainsKey(id))
                dictionary[id] = item;
            else
                dictionary.Add(id, item);

            SetItem(name, dictionary, isAbsoluteExpiration, expirationMinutes);
        }

        private SortedDictionary<TIdentifier, T> getOrCreateDictionary<T, TIdentifier>(string name)
        {
            var list = GetItem< SortedDictionary<TIdentifier, T>>(name);
            if (list == null)
            {
                list = new SortedDictionary<TIdentifier, T>();
            }

            return list;
        }
    }
}
