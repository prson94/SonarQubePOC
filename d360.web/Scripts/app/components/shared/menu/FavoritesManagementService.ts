import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import * as _ from 'lodash';
import { cloneDeep, isEqual } from 'lodash';
import { FavoritesService } from '../../../services/favorites.service';
import { HomepageAndFavoritesModel } from '../../../models/favorite.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { BaseComponent } from '../base.component';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { SiteMenuComponent } from './site-menu.component';


// readability: this can & should be replaced with reduxjs-toolkit
abstract class BaseStore<TState> extends BaseComponent {
    private mutableState$ = new BehaviorSubject<TState>(null!);

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    public get state$() {
        return this.mutableState$.asObservable();
    }

    protected get currentState() {
        return this.mutableState$.value;
    }

    protected init(state: TState) {
        this.mutableState$.next(state);
    }

    protected mutate(mutator: (state: TState) => void) {
        // perfomance: in case if this is too slow, use immerjs library (or reduxjs-toolkit)
        const original = this.mutableState$.value;
        const cloned = cloneDeep(original);
        mutator(cloned);
        if (!isEqual(cloned, original)) {
            this.mutableState$.next(cloned);
        }
    }
}

// readability: this can & should be replaced with reduxjs-toolkit
@Injectable({
    providedIn: 'root'
})
export class FavoritesManagementService extends BaseStore<FavoritesManagementState> {

    constructor(
        private favoritesService: FavoritesService,
        private headerActionsService: HeaderActionsService,
        protected settingsService: CompanySettingsService,
        private messagesService: MessagesObservableService,
        private siteMenuComponent: SiteMenuComponent) {
        super(settingsService);
        this.init(initialState);
    }

    public increaseLoadingCounterAction() {
        this.mutate(state => {
            state.loadingCounter = state.loadingCounter + 1;
        });
    }

    public decreaseLoadingCounterAction() {
        this.mutate(state => {
            state.loadingCounter = state.loadingCounter - 1;
        });
    }

    public toggleManageFavoritesOnAction() {
        this.mutate(state => {
            state.isManageFavoritesModeEnabled = !state.isManageFavoritesModeEnabled;
            state.removeFavoriteIds = new Set();
        });
    }

    public toggleManageFavoritesOffAction() {
        this.mutate(state => {
            state.isManageFavoritesModeEnabled = false;
            state.removeFavoriteIds = new Set();
        });
    }

    public setFavoriteRemovalAction(payload: { favoriteId: number, removeOn: boolean }) {
        this.mutate(state => {
            if (payload.removeOn) {
                state.removeFavoriteIds.add(payload.favoriteId)
            } else {
                state.removeFavoriteIds.delete(payload.favoriteId)
            }
        });
    }

    public setAllFavoritesRemovalSaga(payload: { removeOn: boolean }) {
        for (const favorite of this.currentState.homepageAndFavorites.Favorites) {
            this.setFavoriteRemovalAction({favoriteId: favorite.Id, removeOn: payload.removeOn});
        }
    }

    public setFavoritesAction(payload: { homefav: HomepageAndFavoritesModel }) {
        this.mutate(state => {
            state.homepageAndFavorites = payload.homefav;
            state.removeFavoriteIds = new Set();
        })
    }

    public removeFavoritesSaga() {
        const favoriteIds = Array.from(this.currentState.removeFavoriteIds);
        const removingEverything = this.currentState.removeFavoriteIds.size === this.currentState.homepageAndFavorites.Favorites.length;

        this.increaseLoadingCounterAction();
        this.favoritesService.deleteCurrentUsersFavoritesV2(favoriteIds).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.headerActionsService.emitFavoritesChange();
                this.toggleManageFavoritesOffAction();
                this.decreaseLoadingCounterAction();
                if (removingEverything) {
                    this.siteMenuComponent.changeActiveMenu(null);
                }
            },
            error => {
                this.decreaseLoadingCounterAction();
            }
        );
    }

    public tryLoadFavoritesSaga() {
        if (!this.settingsService.getSettingById(CompanySettingEnum.ShowFavorites).BooleanSetting.Value) {
            return;
        }

        this.increaseLoadingCounterAction();
        this.favoritesService.getHomePageAndFavorites().subscribe(
            homefav => {
                this.setFavoritesAction({ homefav });
                this.decreaseLoadingCounterAction();
            },
            error => {
                this.decreaseLoadingCounterAction();
            }
        );
    }
}

interface FavoritesManagementState {
    isManageFavoritesModeEnabled: boolean;
    homepageAndFavorites: HomepageAndFavoritesModel | null;
    removeFavoriteIds: Set<number>;
    loadingCounter: number;
}

const initialState: FavoritesManagementState = {
    isManageFavoritesModeEnabled: false,
    homepageAndFavorites: null,
    removeFavoriteIds: new Set(),
    loadingCounter: 0
}