using System;
using System.Collections.Generic;
using System.Configuration;
using d360.core;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace d360.extensions.caching
{
    public class RedisCachingProvider : ICachingProvider
    {
        private static readonly Lazy<ConnectionMultiplexer> LazyConnection;
        public static ConnectionMultiplexer Connection => LazyConnection.Value;
        public static IDatabase db => Connection.GetDatabase();

        static RedisCachingProvider()
        {
            try
            {
                LazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(Config.GetValue<string>(constants.REDIS_CONNECTION)));
            }
            catch
            {
                // Do nothing
            }
        }

        public bool ListItemExists<T, TIdentifier>(string name, TIdentifier id)
        {
            try
            {
                if (db.KeyExists($"{name}_{id}"))
                {
                    var item = GetItem<T>($"{name}_{id}");
                    return (item != null && item is T);
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                // Do nothing
                return false;
            }
        }

        public bool ItemExists<T>(string name)
        {
            try
            {
                if (db.KeyExists(name))
                {
                    var item = GetItem<T>(name);
                    return (item != null && item is T);
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                // Do nothing
                return false;
            }
        }

        public T GetItem<T>(string name)
        {
            try
            {
                var json = db.StringGet(name);
                if (string.IsNullOrEmpty(json))
                {
                    return default(T);
                }
                else
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
            }
            catch
            {
                // Do nothing
                return default(T);
            }
        }

        public T GetItemInListByID<T, TIdentifier>(string name, TIdentifier id)
        {
            try
            {
                T item = GetItem<T>($"{name}_{id}");
                return (item == null) ? default : item;
            }
            catch
            {
                // Do nothing
                return default(T);
            }
        }

        public void SetItem<T>(string name, T item, bool isAbsoluteExpiration = false, int expirationMinutes = 10)
        {
            try
            {
                db.StringSet(
                    new RedisKey(name),
                    new RedisValue(JsonConvert.SerializeObject(item)),
                    TimeSpan.FromMinutes(expirationMinutes),
                    When.Always,
                    CommandFlags.None);
            }
            catch
            {
                // Do nothing
            }
        }

        public void SetList<T, TIdentifier>(string name, SortedDictionary<TIdentifier, T> list, bool isAbsoluteExpiration = false, int expirationMinutes = 10)
        {
            try
            {
                foreach (var key in list.Keys)
                {
                    SetItem($"{name}_{key}", list[key], isAbsoluteExpiration, expirationMinutes);
                }
            }
            catch
            {
                // Do nothing
            }
        }

        public void SetItemInListByID<T, TIdentifier>(string name, TIdentifier id, T item, bool isAbsoluteExpiration = false, int expirationMinutes = 10)
        {
            try
            {
                SetItem($"{name}_{id}", item, isAbsoluteExpiration, expirationMinutes);
            }
            catch
            {
                // Do nothing
            }
        }

        public void RemoveItem(string name)
        {
            try
            {
                db.KeyDelete(name, CommandFlags.FireAndForget);
            }
            catch
            {
                // Do nothing
            }
        }
    }
}
