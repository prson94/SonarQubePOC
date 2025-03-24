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

	private iconByPageType = new Map<FavoritePageType, string>([
		[ FavoritePageType.SearchResultsPage, 'fa-search' ],
		[ FavoritePageType.DashboardPage, 'fa-tachometer' ],
		[ FavoritePageType.HomePage, 'fa-home' ],
		[ FavoritePageType.CommunityPage, 'fa-group' ],
		[ FavoritePageType.WorkflowPage, 'fa-usb' ],
		[ FavoritePageType.SemanticTypePage, 'fa-tags' ],
		[ FavoritePageType.DataCatalogPage, 'gov-data-catalog-icon'],
		[ FavoritePageType.AssignmentsPage, 'fa-list'],
		[ FavoritePageType.RequestsPage, 'fa-plus-square-o']
	]);

	public get iconName() {
		if (this.favorite.AssetTypeClass !== null && this.favorite.AssetTypeClass !== undefined) {
			const icon = this.iconService.getIconIdByClass(AssetTypeClass[this.favorite.AssetTypeClass]);
			if (icon !== '') {
				return 'fa-' + icon;
			}
        }

		switch (this.favorite.PageType) {
            case FavoritePageType.Artifact: {
                return 'fa-question-circle';
            }
			default: {
				const icon = this.iconByPageType.get(this.favorite.PageType);
                if (icon != null) {
                    return icon;
                }
                return 'fa-question-circle';
            }
		}
    }

    homePageRoute$ = this.store.state$.pipe(
        map((x) => x.homepageAndFavorites?.Homepage?.Route)
    );

    searchText$ = this.store.state$.pipe(
        map((x) => x.searchText)
	);
}
