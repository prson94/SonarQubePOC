using System.Collections.Generic;

namespace d360.extensions
{
    public interface ICachingProvider
    {
        bool ItemExists<T>(string name);
        bool ListItemExists<T, TIdentifier>(string name, TIdentifier id);
        T GetItem<T>(string name);
        T GetItemInListByID<T, TIdentifier>(string name, TIdentifier id);
        void SetItem<T>(string name, T item, bool isAbsoluteExpiration = false, int expirationMinutes = 10);
        void SetItemInListByID<T, TIdentifier>(string name, TIdentifier id, T item, bool isAbsoluteExpiration = true, int expirationMinutes = 10);
        void SetList<T, TIdentifier>(string name, SortedDictionary<TIdentifier, T> list, bool isAbsoluteExpiration = false, int expirationMinutes = 10);
        void RemoveItem(string name);
    }
}
