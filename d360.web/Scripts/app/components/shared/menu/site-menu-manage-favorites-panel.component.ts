import { Component } from '@angular/core';
import { map } from 'rxjs/operators';
import { FavoritesManagementService } from './FavoritesManagementService';

@Component({
    selector: 'd3s-site-menu-manage-favorites-panel',
    templateUrl: './site-menu-manage-favorites-panel.component.html',
    styleUrls: ['./site-menu-manage-favorites-panel.component.css']
})
export class SiteMenuManageFavoritesPanelComponent {
    constructor(public store: FavoritesManagementService) {
    }

    isLoading$ = this.store.state$.pipe(
        map(state => state.loadingCounter > 0)
    );

    favorites$ = this.store.state$.pipe(
        map(state => state.homepageAndFavorites?.Favorites ?? [])
    );

    allFavoritesRemovalStatus$ = this.store.state$.pipe(
        map(state => {
            const allFavoriteIds = state.homepageAndFavorites.Favorites.map(f => f.Id);
            const removeEverything = state.removeFavoriteIds.size === allFavoriteIds.length;
            if (removeEverything) {
                return true;
            }

            const removeNothing = state.removeFavoriteIds.size === 0;
            if (removeNothing) {
                return false;
            }

            return null;
        })
    )

    canRemove$ = this.store.state$.pipe(
        map(state => state.removeFavoriteIds.size > 0)
    );

    getFavoriteRemovalStatus(favoriteId: number){
        return this.store.state$.pipe(
            map(state => state.removeFavoriteIds.has(favoriteId) ?? false)
        );
    }

    get maxHeight() {
        return (window.innerHeight - 80) + 'px';
    }

    toggleAll(currentStatus, _newStatus) {
        // we ignore _newStatus, because p-triStateCheckbox thinks that it's nice idea to switch into 'intermediate' status
        // but we can switch only to on & off statuses
        this.store.setAllFavoritesRemovalSaga({ removeOn: !currentStatus })
    }
};
