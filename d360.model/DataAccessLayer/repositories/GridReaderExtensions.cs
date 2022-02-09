using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;

namespace d360.model.DataAccessLayer.repositories
{
    internal static class GridReaderExtensions
    {
        public static async Task<IReadOnlyList<T>> ReadListAsync<T>(this SqlMapper.GridReader gridReader)
        {
            var enumerable = await gridReader.ReadAsync<T>().ConfigureAwait(false);
            return enumerable.ToArray();
        }
    }
}