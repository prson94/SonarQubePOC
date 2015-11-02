using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using d360.extensions;
using StackExchange.Redis;
using System.Linq;
using System.Runtime.Serialization;
using System.IO;

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
        ConnectionMultiplexer Connection;
        IDatabase _Cache;

        public RedisCachingProvider()
        {
            Connection = ConnectionMultiplexer.Connect("d3ssession.redis.cache.windows.net,ssl=true,password=V8oa+l3HhHOxzLJtltnHBXPKVxzvH3vjbrI4NxLeXX4=");
            _Cache = Connection.GetDatabase();
        }

        public bool ListItemExists<T, TIdentifier>(string name, TIdentifier id)
        {
            return _Cache.SetContains(name, id.ToString());
        }

        public bool ItemExists<T>(string name)
        {
            return _Cache.KeyExists(name);
        }

        public T GetItem<T>(string name)
        {
            var data = _Cache.StringGet(name);

            if (!data.IsNull && data.HasValue)
            {
                var blobBytes = (byte[])data;
                var deserializedObject = blobBytes.Deserialize<T>();
                return deserializedObject;
            }
            else
            {
                return default(T);
            }
        }

        public T GetItemInListByID<T, TIdentifier>(string name, TIdentifier id)
        {
            var data = _Cache.StringGetSet(name, id.ToString());

            if (!data.IsNull && data.HasValue)
            {
                var blobBytes = (byte[])data;
                var deserializedObject = blobBytes.Deserialize<T>();
                return deserializedObject;
            }
            else
            {
                return default(T);
            }
        }

        public void RemoveItem(string name)
        {
            _Cache.KeyDelete(name);
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

            _Cache.StringSet(name, item.Serialize(), expiry);
        }

        public void SetList<T, TIdentifier>(string name, SortedDictionary<TIdentifier, T> list, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            RedisValue[] entries = new RedisValue[list.Keys.Count];
            var i = 0;
            foreach(var k in list.Keys)
            {
                entries[i] = list[k].Serialize();
                i++;
            }
            _Cache.SetAdd(name, entries);
        }

        public void SetItemInListByID<T, TIdentifier>(string name, TIdentifier id, T item, bool isAbsoluteExpiration = true, int expirationMinutes = 10)
        {
            var dictionary = getOrCreateDictionary<T, TIdentifier>(name);

//           _Cache.get

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
