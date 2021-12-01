export class FavoriteApiModel{
    Id: number;
    Route: string;
    Name: string;
    Type: string;
}

export class Favorite {
    ID: number;
    ResourceID: number;
    Route: string;
    Name: string;
    SortOrder: number;
    isOverride: boolean = false;
    Category: string;
    Object: string;
    ObjectID: number;
    IsHomePage: boolean = false;
}
export class HomepageAndFavoritesModel {
    Homepage: FavoriteApiModel;
    Favorites: FavoriteApiModel[];
}
