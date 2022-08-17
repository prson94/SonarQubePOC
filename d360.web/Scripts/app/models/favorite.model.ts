import { AssetTypeClass } from "./asset.model";

export class FavoriteApiModel {
    Id: number;
    Route: string;
    Name: string;
    Type: string;
}

export class FavoriteViewModel {
    Id: number;
    PageType: FavoritePageType;
    AssetTypeClass?: keyof AssetTypeClass;
    Name: string;
    Route: string;
    Breadcrumbs: string[];
}

export enum FavoritePageType {
    Artifact = 'Artifact',
    SearchResultsPage = 'SearchResultsPage',
    DashboardPage = 'DashboardPage',
    CommunityPage = 'CommunityPage',
    WorkflowPage = 'WorkflowPage',
    HomePage = 'HomePage',
    ResourceListPage = 'ResourceListPage',
    CartPage = 'CartPage',
    SemanticTypePage = 'SemanticTypePage'
}

export class HomepageAndFavoritesModel {
    Homepage: FavoriteApiModel;
    Favorites: FavoriteViewModel[];
}
