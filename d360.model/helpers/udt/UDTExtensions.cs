using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

using Dapper;

using static Dapper.SqlMapper;

namespace d360.model.DataAccessLayer.repositories
{
    internal static class UDTExtensions
    {
        public static ICustomQueryParameter AsUDTParameter<T>(this IEnumerable<T> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            UDTNameAttribute udtName = typeof(T).GetCustomAttribute<UDTNameAttribute>();
            if (udtName == null)
            {
                throw new ArgumentException($"Type {typeof(T).FullName} doesn't have attribute UDTNameAttribute");
            }

            PropertyInfo[] properties = typeof(T)
                .GetProperties(
                    BindingFlags.Public
                    | BindingFlags.Instance
                    | BindingFlags.FlattenHierarchy
                    | BindingFlags.GetProperty
                );

            List<PropertyInfo> columns = properties
                .Where(p => p.GetCustomAttribute<UDTOrderAttribute>() != null)
                .OrderBy(p => p.GetCustomAttribute<UDTOrderAttribute>().Order)
                .ToList();

            if (!columns.Any())
            {
                throw new ArgumentException($"Type {typeof(T).FullName} doesn't have public properties with UDTOrder attribute");
            }

            DataTable dataTable = new DataTable();
            foreach (PropertyInfo column in columns)
            {
                dataTable.Columns.Add(column.Name);
            }

            foreach (T item in items)
            {
                dataTable.Rows.Add(columns.Select(column => column.GetValue(item)).ToArray());
            }

            return dataTable.AsTableValuedParameter(udtName.Name);
        }
    }
}
