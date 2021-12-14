import { Component, Input } from '@angular/core';
import { map } from 'rxjs/operators';
import { AssetTypeClass } from '../../../models/asset.model';
import { FavoritePageType, FavoriteViewModel } from '../../../models/favorite.model';
import { IconService } from '../../../services/icon.service';
import { FavoritesManagementService } from './FavoritesManagementService';

@Component({
    selector: 'd3s-site-menu-favorite-item',
    templateUrl: './site-menu-favorite-item.component.html',
    styleUrls: ['./site-menu-favorite-item.component.less']
})
export class SiteMenuFavoriteItemComponent {
    @Input() favorite: FavoriteViewModel;

    constructor(private iconService: IconService, private store: FavoritesManagementService) { }

    AssetTypeClass = AssetTypeClass;
    FavoritePageType = FavoritePageType;

    public get iconName() {
        if (this.favorite.AssetTypeClass != null) {
            return this.iconService.getIconIdByClass(AssetTypeClass[this.favorite.AssetTypeClass]);
        }

        const iconByPageType = new Map([
            [FavoritePageType.SearchResultsPage, 'search'],
            [FavoritePageType.DashboardPage, 'tachometer'],
            [FavoritePageType.HomePage, 'home'],
            [FavoritePageType.CommunityPage, 'group'],
            [FavoritePageType.WorkflowPage, 'usb'],
            [FavoritePageType.CartPage, 'shopping-cart']
        ])

        switch (this.favorite.PageType) {
            case FavoritePageType.Artifact: {
                console.error(
                    `Expected AssetTypeClass to be non-null, ` +
                    `but it was ${new String(this.favorite.AssetTypeClass)} ` +
                    `for ${JSON.stringify(this.favorite)}`);
                return 'question-circle';
            }
            default: {
                const icon = iconByPageType.get(this.favorite.PageType);
                if (icon != null) {
                    return icon;
                }
                
                console.error(
                    `Expected AssetTypeClass to be non-null, ` +
                    `but it was ${new String(this.favorite.AssetTypeClass)} ` +
                    `for ${JSON.stringify(this.favorite)}`);

                return 'question-circle';
            }
        }
    }

    homePageRoute$ = this.store.state$.pipe(
        map(x => x.homepageAndFavorites?.Homepage?.Route)
    );

    searchText$ = this.store.state$.pipe(
        map(x => x.searchText)
    );
};
