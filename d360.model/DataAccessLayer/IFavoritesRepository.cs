using d360.core;
using d360.core.enums;
using d360.model.DataAccessLayer.repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace d360.model.DataAccessLayer
{
    public interface IFavoritesRepository
    {
        Task<IReadOnlyList<FavoriteShortModel>> GetFavorites(int resourceID, bool homePageOnly);

        Task<IReadOnlyList<FavoritesObjectDetailsResponse>> GetFavoriteDetails(IEnumerable<FavoritesObjectDetailsRequest> items);
    }

    public class FavoriteShortModel
    {
        public int Id { get; set; }

        public string Route { get; set; }

        public bool IsHomePage { get; set; }

        public int SortOrder { get; set; }
    }

    [UDTName("dbo.FavoritesObjectDetailsRequest")]
    public class FavoritesObjectDetailsRequest : IEquatable<FavoritesObjectDetailsRequest>
    {
        [UDTOrder(0)]
        public int FavoriteId { get; set; }

        [UDTOrder(1)]
        public SystemObjects? ObjectType { get; set; }

        [UDTOrder(2)]
        public int? ObjectId { get; set; }

        [UDTOrder(3)]
        public int? AssetId { get; set; }

        [UDTOrder(4)]
        public int? AssetTypeId { get; set; }

        [UDTOrder(5)]
        public Guid? Uid { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as FavoritesObjectDetailsRequest);
        }

        public bool Equals(FavoritesObjectDetailsRequest other)
        {
            return other != null &&
                   FavoriteId == other.FavoriteId &&
                   ObjectType == other.ObjectType &&
                   ObjectId == other.ObjectId &&
                   AssetId == other.AssetId &&
                   EqualityComparer<Guid?>.Default.Equals(Uid, other.Uid);
        }

        public override int GetHashCode()
        {
            int hashCode = -669969164;
            hashCode = hashCode * -1521134295 + FavoriteId.GetHashCode();
            hashCode = hashCode * -1521134295 + ObjectType.GetHashCode();
            hashCode = hashCode * -1521134295 + ObjectId.GetHashCode();
            hashCode = hashCode * -1521134295 + AssetId.GetHashCode();
            hashCode = hashCode * -1521134295 + Uid.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(FavoritesObjectDetailsRequest left, FavoritesObjectDetailsRequest right)
        {
            return EqualityComparer<FavoritesObjectDetailsRequest>.Default.Equals(left, right);
        }

        public static bool operator !=(FavoritesObjectDetailsRequest left, FavoritesObjectDetailsRequest right)
        {
            return !(left == right);
        }
    }

    public class FavoritesObjectDetailsResponse
    {
        public int FavoriteId { get; set; }

        public string Name { get; set; }

        public List<BreadcrumbsInfo> Breadcrumbs { get; set; }

        public SystemObjects ObjectType { get; set; }

        public int ObjectId { get; set; }

        public Guid Uid { get; set; }

        public int? TypeObjectId { get; set; }

        public AssetTypeClass AssetTypeClass { get; set; }
    }

    public class BreadcrumbsInfo
    {
        public int Level { get; set; }

        public string TypeName { get; set; }

        public string Name { get; set; }

        public string TypeUrl { get; set; }

        public string Url { get; set; }
    }
}