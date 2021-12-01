using System;
using System.Runtime.Serialization;

namespace d360.core.entities.Membership
{
    public enum FavoriteType { Asset, AssetType, Page };

    public interface IFavoriteUpsert
    {
        string Route { get; set; }
        FavoriteType Type { get; set; }
    }

    public class FavoriteApiModel : IFavoriteUpsert
    {

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Route { get; set; }

        [DataMember]
        public FavoriteType Type { get; set; }
    }

    public class FavoriteApiViewModel : IFavoriteUpsert
    {
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Route { get; set; }

        [DataMember]
        public FavoriteType Type { get; set; }
    }
}
