import { Component } from '@angular/core';
import * as _ from 'lodash';
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
            const removalStatus = allFavoriteIds.map(id => state.removeFavoritesByIds.get(id) ?? false);
            const removeEverything = _.every(removalStatus, x => x === true);
            if (removeEverything) {
                return true;
            }

            const removeNothing = _.every(removalStatus, x => x === false);
            if (removeNothing) {
                return false;
            }

            return null;
        })
    )

    canRemove$ = this.allFavoritesRemovalStatus$.pipe(
        map(removalStatus => removalStatus != false)
    );

    getFavoriteRemovalStatus(favoriteId: number){
        return this.store.state$.pipe(
            map(state => state.removeFavoritesByIds.get(favoriteId) ?? false)
        );
    }

    get maxHeight() {
        return (window.innerHeight - 80) + 'px';
    }
};
