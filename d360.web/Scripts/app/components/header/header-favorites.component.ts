import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { FavoritesService } from '../../services/favorites.service';
import { Favorite } from '../../models/favorite.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { HeaderActionsService } from '../../services/header-actions.service';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import * as _ from 'lodash';


@Component({
    selector: 'd3s-header-favorites',
    styles: [
        `
            .favorite {
                font-size: 1.2em;
                color: #666;
                padding: 0 15px;
            }

            .favorite.active {
                color: #FFB230;
            }
        `
    ],
    template:
    `
        <span (click)="handleClick()" [class.active]="active" class="favorite">
            <i *ngIf="!isLoading" class="fa fa-star"></i>
            <i *ngIf="isLoading" class="fa fa-spinner fa-spin" style="color:black;"></i>
        </span>
    `,
    providers: [FavoritesService]
})

export class HeaderFavoritesComponent implements OnInit, OnDestroy {
    @Input() uri: string;
    @Input() active: boolean = false;
    @Output() onClick = new EventEmitter();

    private sub: any;
    private subBread: any;
    private subFavorites: any;
    private name: string;
    private isLoading = false;

    private favItems: Favorite[];


    constructor(private router: Router, private favoritesService: FavoritesService, private breadcrumbService: HeaderBreadcrumbService, protected headerActionsService: HeaderActionsService) { }

    //TODO: refactor/cleanup initial load logic
    ngOnInit() {
        this.sub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                //this.uri = _.trimStart(e.url, '/');
                if (this.favItems == null) {
                    this.favoritesService.getFavorites()
                        .then(fav => {
                            this.favItems = fav;
                            this.activateFavorites();
                        });
                } else {
                    this.activateFavorites();
                }
            }
        });

        this.subBread = this.breadcrumbService.breadcrumbs$.subscribe(b => {
            this.name = b.text;
        });

        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(fav => {
            this.favItems = fav;
            this.activateFavorites();
        });

        this.favoritesService.getFavorites()
            .then(fav => {
                this.favItems = fav;
                this.activateFavorites();
            });
    }


    handleClick() {
        if (this.isLoading)
            return;

        if (this.isAdminUri())
            return;
        this.isLoading = true;
        this.favoritesService.toggleFavorite(this.name, this.uri)
            .then(() => this.favoritesService.getFavorites())
            .then(fav => {
                this.headerActionsService.emitFavoritesChange(fav);
                this.favItems = fav;
                this.activateFavorites();
                this.isLoading = false;
            });
    }


    activateFavorites(favorites: Favorite[] = null) {
        let favs = favorites
        if (favs == null)
            favs = this.favItems;
        if (favs == null)
            return;
        this.active = false;
        for (let f of favs) {
            if (f.Route == this.uri)
                this.active = true;
        }
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
        this.subBread.unsubscribe();
        this.subFavorites.unsubscribe();
    }

    isAdminUri() {        
        //TODO: need a better way to do this
        return (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
    }
}

