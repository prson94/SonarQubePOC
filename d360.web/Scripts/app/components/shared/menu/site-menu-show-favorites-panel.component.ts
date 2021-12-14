import { ChangeDetectionStrategy, Component, EventEmitter, Output } from '@angular/core';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { Favorite, FavoriteViewModel } from '../../../models/favorite.model';
import { FavoritesManagementService } from './FavoritesManagementService';

@Component({
    selector: 'd3s-site-menu-show-favorites-panel',
    templateUrl: './site-menu-show-favorites-panel.component.html',
    styleUrls: ['./site-menu-show-favorites-panel.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SiteMenuShowFavoritesPanelComponent {
    // TODO: highlight search text
    searchText = '';

    constructor(public store: FavoritesManagementService, private router: Router) {
    }

    isLoading$ = this.store.state$.pipe(
        map(state => state.loadingCounter > 0)
    );

    favorites$ = this.store.state$.pipe(
        map(state => state.homepageAndFavorites?.Favorites ?? [])
    );

    filterBySearch = (favorites: FavoriteViewModel[], searchText: string): FavoriteViewModel[] => {
        return favorites.filter(f => matches(f));

        function matches(f: FavoriteViewModel) {
            return includes(f.Name, searchText)
                // TODO: include also area
                || includes(f.Breadcrumbs.join(" > "), searchText);
        }

        function includes(where: string, what: string) {
            return (where ?? '').toLowerCase().includes((what ?? '').toLowerCase());
        }
    }

    @Output() contentSizeChanged = new EventEmitter();
    @Output() activeItemChanged = new EventEmitter();

    get maxHeight() {
        return (window.innerHeight - 80) + 'px';
    }

    openFavorite(f: FavoriteViewModel) {
        this.router.navigateByUrl(f.Route, { state: { "invalidateKey": true } });
        this.activeItemChanged.emit(undefined);
    }
};
