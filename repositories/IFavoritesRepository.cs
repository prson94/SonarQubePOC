using d360.core.entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace repositories
{
	public interface IFavoritesRepository
    {
        Task<IReadOnlyList<FavoriteShortModel>> GetFavorites(int resourceID, bool homePageOnly);

        Task<IReadOnlyList<FavoritesObjectDetailsResponse>> GetFavoriteDetails(IEnumerable<FavoritesObjectDetailsRequest> items);

        Task<int?> GetFavoriteIdByRoute(int resourceID, string route);
    }
}
