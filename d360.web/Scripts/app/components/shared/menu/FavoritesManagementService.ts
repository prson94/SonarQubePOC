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
        private messagesService: MessagesObservableService) {
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

    public toggleManageFavoritesAction() {
        this.mutate(state => {
            state.isManageFavoritesModeEnabled = !state.isManageFavoritesModeEnabled;
        });
    }

    public setFavoriteRemovalAction(payload: { favoriteId: number, remove: boolean }) {
        this.mutate(state => {
            state.removeFavoritesByIds.set(payload.favoriteId, payload.remove);
        });
    }

    public setAllFavoritesRemovalAction(payload: { remove: boolean }) {
        this.mutate(state => {
            for (const favorite of state.homepageAndFavorites.Favorites) {
                state.removeFavoritesByIds.set(favorite.Id, payload.remove);
            }
        });
    }

    public setFavoritesAction(payload: { homefav: HomepageAndFavoritesModel }) {
        this.mutate(state => {
            state.homepageAndFavorites = payload.homefav;
            state.removeFavoritesByIds = new Map();
        })
    }

    public removeFavoritesSaga() {
        const favoriteIds = Array
            .from(this.currentState.removeFavoritesByIds.entries())
            .filter(([id, remove]) => remove === true)
            .map(([id]) => id);

        this.increaseLoadingCounterAction();
        this.favoritesService.deleteCurrentUsersFavoritesV2(favoriteIds).subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.headerActionsService.emitFavoritesChange();
                this.toggleManageFavoritesAction();
                this.decreaseLoadingCounterAction();
                // TODO: close menu if there are no items left
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
    // TODO: consider using Set
    removeFavoritesByIds: Map<number, boolean>;
    loadingCounter: number;
}

const initialState: FavoritesManagementState = {
    isManageFavoritesModeEnabled: false,
    homepageAndFavorites: null,
    removeFavoritesByIds: new Map(),
    loadingCounter: 0
}