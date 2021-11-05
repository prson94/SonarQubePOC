import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef, AfterContentInit, Input, EventEmitter, Output } from '@angular/core';
import { BaseComponent } from '../base.component';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { FavoritesService } from '../../../services/favorites.service';
import { SiteMenu } from '../../../models/site-menu.model';
import * as _ from 'lodash';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from "../../../static/string-constants";
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';

@Component({
    selector: 'd3s-site-menu-favorites',
    templateUrl: './site-menu-favorites.component.html'
})

export class SiteMenuFavoritesComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() expanded: boolean;
    @Output() activeItemChanged = new EventEmitter();
    title = 'Favorites';
    
    public menu: SiteMenu;
    private subFavorites: any;
    public manageFavoritesMode = false;

    constructor(
        private favoritesService: FavoritesService,
        private headerActionsService: HeaderActionsService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef) {
        super(settingsService);
    }    

    ngOnInit() {
        this.loadFavorites();

        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(() => {
            this.loadFavorites();
        });
    }

    ngOnDestroy() {
        if (this.subFavorites) {
            this.subFavorites.unsubscribe();
        }
    }
    
    handleActiveItem(event) {
        this.activeItemChanged.emit(event);
        this.ref.detectChanges();
    }

    loadFavorites() {
        if (this.getBooleanSetting(CompanySettingEnum.ShowFavorites)) {
            return;
        }

        this.favoritesService.getHomePageAndFavorites().subscribe(
            homefav => {
                this.menu = new SiteMenu();
                this.menu.MenuID = StringConstants.MenuId_Favorites;
                this.menu.NavigationItems = [];

                for (let favorite of homefav.Favorites) {
                    let isHomePage = _.isEqual(favorite, homefav.Homepage);
                    this.menu.NavigationItems.push({
                        Name: favorite.Name,
                        Url: favorite.Route,
                        IsLink: false,
                        Items: null,
                        IsHomePage: isHomePage,
                        count: null
                    });
                }

                this.ref.markForCheck();
            }
        );
    }

    toggleManageFavorites() {
        this.manageFavoritesMode = !this.manageFavoritesMode;
        this.ref.markForCheck();
    }

    protected clearFavorites() {
        this.favoritesService.deleteCurrentUsersFavoritesV2().subscribe(
            result => {
                this.showMessageForResult(this.messagesService, result);
                this.loadFavorites(); // reload favorites because the user could still have global favorites.
                this.headerActionsService.emitFavoritesChange()
            }
        );
    }
};
