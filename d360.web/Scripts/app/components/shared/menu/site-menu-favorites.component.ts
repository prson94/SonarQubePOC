import { Component, OnInit, OnDestroy, ChangeDetectorRef, Input, EventEmitter, Output } from '@angular/core';
import { BaseComponent } from '../base.component';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { SiteMenu } from '../../../models/site-menu.model';
import * as _ from 'lodash';
import { StringConstants } from "../../../static/string-constants";
import { CompanySettingsService } from '../../../services/settings.service';
import { FavoritesManagementService } from './FavoritesManagementService';
import { distinctUntilChanged, map } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { isEqual } from 'lodash';
import { SiteMenuComponent } from './site-menu.component';

@Component({
    selector: 'd3s-site-menu-favorites',
    templateUrl: './site-menu-favorites.component.html',
    providers: [FavoritesManagementService]
})
export class SiteMenuFavoritesComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() expanded: boolean;
    @Output() activeItemChanged = new EventEmitter();
    title = 'Favorites';


    public menu: SiteMenu;
    private subs: Subscription[] = [];

    constructor(
        private headerActionsService: HeaderActionsService,
        protected settingsService: CompanySettingsService,
        private ref: ChangeDetectorRef,
        public store: FavoritesManagementService,
        private siteMenu: SiteMenuComponent) {
        super(settingsService);
    }

    get state$() {
        return this.store.state$;
    }

    // Please, note that this mapping is almost not required (at least, we don't need to put actual favorites inside)
    // But our site-menu-category.component depends on it's existence
    // TODO: once upon a time, cleanup this
    menu$ = this.store.state$.pipe(
        map(x => x.homepageAndFavorites),
        distinctUntilChanged(isEqual),
        map(homefav => {
            if (homefav == null) {
                return null;
            }

            const menu = new SiteMenu();
            menu.MenuID = StringConstants.MenuId_Favorites;
            menu.NavigationItems = [];

            for (let favorite of homefav.Favorites) {
                menu.NavigationItems.push({
                    Name: favorite.Name,
                    Url: favorite.Route,
                    IsLink: false,
                    Items: null,
                    count: null
                });
            }

            return menu;
        })
    );

    ngOnInit() {
        this.store.tryLoadFavoritesSaga();

        this.subs.push(this.headerActionsService.onFavoritesChanges$.subscribe(() => {
            this.store.tryLoadFavoritesSaga();
        }));

        this.subs.push(this.menu$.subscribe(menu => {
            const wasActive = this.menu?.isActiveItem;
            this.menu = menu;
            if (wasActive) {
                this.menu.isActiveItem = true;
            }

            this.ref.markForCheck();
        }))

        this.subs.push(this.siteMenu.activeMenu$.subscribe((activeMenu: SiteMenu | undefined) => {
            if (this.menu == null) {
                return;
            }
            
            if (activeMenu?.MenuID != this.menu.MenuID) {
                this.store.toggleManageFavoritesOffAction();
            }
        }));
    }

    ngOnDestroy() {
        this.subs.forEach(sub => sub.unsubscribe());
    }

    handleActiveItem(event) {
        this.activeItemChanged.emit(event);
        this.ref.detectChanges();
    }
};


