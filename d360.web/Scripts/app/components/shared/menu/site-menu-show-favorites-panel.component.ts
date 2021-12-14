import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, SimpleChanges, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { Favorite, FavoriteViewModel } from '../../../models/favorite.model';
import { SearchFieldComponent } from '../controls/search-field/search-field.component';
import { FavoritesManagementService } from './FavoritesManagementService';

@Component({
    selector: 'd3s-site-menu-show-favorites-panel',
    templateUrl: './site-menu-show-favorites-panel.component.html',
    styleUrls: ['./site-menu-show-favorites-panel.component.less'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SiteMenuShowFavoritesPanelComponent {
    @Input() isActive: boolean = false;

    @ViewChild('searchinput', { static: false }) searchInput: SearchFieldComponent;

    constructor(public store: FavoritesManagementService, private router: Router) {
    }

    ngOnChanges(changes: SimpleChanges) {
        if (this.isActive) {
            if (this.searchInput) {
                this.searchInput.focus();
            }

            this.store.setSearchTextAction({ searchText: '' });
        }
    }

    isLoading$ = this.store.state$.pipe(
        map(state => state.loadingCounter > 0)
    );

    favorites$ = this.store.state$.pipe(
        map(state => state.homepageAndFavorites?.Favorites ?? [])
    );

    searchText$ = this.store.state$.pipe(
        map(state => state.searchText)
    );

    filterBySearch = (favorites: FavoriteViewModel[], searchText: string): FavoriteViewModel[] => {
        return favorites.filter(f => includes(f.Name, searchText));

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
