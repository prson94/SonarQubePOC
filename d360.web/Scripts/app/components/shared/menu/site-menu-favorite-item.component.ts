import { Component, Input } from '@angular/core';
import { AssetTypeClass } from '../../../models/asset.model';
import { FavoritePageType, FavoriteViewModel } from '../../../models/favorite.model';
import { IconService } from '../../../services/icon.service';

@Component({
    selector: 'd3s-site-menu-favorite-item',
    templateUrl: './site-menu-favorite-item.component.html',
    styleUrls: ['./site-menu-favorite-item.component.less']
})
export class SiteMenuFavoriteItemComponent {
    @Input() favorite: FavoriteViewModel;

    constructor(private iconService: IconService) { }

    AssetTypeClass = AssetTypeClass;
    FavoritePageType = FavoritePageType;

    public get iconName() {
        if (this.favorite.AssetTypeClass != null) {
            return this.iconService.getIconIdByClass(AssetTypeClass[this.favorite.AssetTypeClass]);
        }

        switch (this.favorite.PageType) {
            case FavoritePageType.Artifact:
                console.error(
                    `Expected AssetTypeClass to be non-null, ` +
                    `but it was ${new String(this.favorite.AssetTypeClass)} ` + 
                    `for ${JSON.stringify(this.favorite)}`);
                return 'question-circle';
            case FavoritePageType.SearchResultsPage:
                return 'search';
        }
    }
};
