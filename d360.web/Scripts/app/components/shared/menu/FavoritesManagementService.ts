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

    public toggleManageFavoritesAction() {
        this.mutate(state => {
            state.isManageFavoritesModeEnabled = !state.isManageFavoritesModeEnabled;
        });
    }

    public setFavoriteRemovalAction(payload: { favoriteUid: string, remove: boolean }) {
        this.mutate(state => {
            state.removeFavoritesByUid.set(payload.favoriteUid, payload.remove);
        });
    }

    public setAllFavoritesRemovalAction(payload: { remove: boolean }) {
        this.mutate(state => {
            for (const favorite of state.homepageAndFavorites.Favorites) {
                state.removeFavoritesByUid.set(favorite.Uid, payload.remove);
            }
        });
    }

    public setFavoritesAction(payload: { homefav: HomepageAndFavoritesModel }) {
        this.mutate(state => {
            state.homepageAndFavorites = payload.homefav;
            state.removeFavoritesByUid = new Map();
        })
    }

    public removeFavoritesSaga() {
        // TODO: this should remove only specified favorites;
        this.favoritesService.deleteCurrentUsersFavoritesV2().subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.headerActionsService.emitFavoritesChange()
            }
        );
    }

    public tryLoadFavoritesSaga() {
        if (!this.settingsService.getSettingById(CompanySettingEnum.ShowFavorites).BooleanSetting.Value) {
            return;
        }

        this.favoritesService.getHomePageAndFavorites().subscribe(
            homefav => this.setFavoritesAction({ homefav })
        );
    }
}

interface FavoritesManagementState {
    isManageFavoritesModeEnabled: boolean;
    homepageAndFavorites: HomepageAndFavoritesModel | null;
    removeFavoritesByUid: Map<string, boolean>;
}

const initialState: FavoritesManagementState = {
    isManageFavoritesModeEnabled: false,
    homepageAndFavorites: null,
    removeFavoritesByUid: new Map()
}