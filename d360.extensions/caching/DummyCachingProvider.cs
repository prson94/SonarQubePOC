using System.Collections.Generic;

namespace d360.extensions.caching
{
    public class DummyCachingProvider : ICachingProvider
    {
        public bool ListItemExists<T, TIdentifier>(string name, TIdentifier id)
        {
            return true;
        }

        public bool ItemExists<T>(string name)
        {
            return true;
        }

        public T GetItem<T>(string name)
        {
            return default(T);
        }

        public T GetItemInListByID<T, TIdentifier>(string name, TIdentifier id)
        {
            return default(T);
        }

        public void SetItem<T>(string name, T item, bool isAbsoluteExpiration = false, int expirationMinutes = 10)
        {
        }

        public void SetList<T, TIdentifier>(string name, SortedDictionary<TIdentifier, T> list, bool isAbsoluteExpiration = false, int expirationMinutes = 10)
        {
        }

        public void SetItemInListByID<T, TIdentifier>(string name, TIdentifier id, T item, bool isAbsoluteExpiration = false, int expirationMinutes = 10)
        {
        }

        public void RemoveItem(string name)
        {
        }
    }
}
