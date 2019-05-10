import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChange, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { FavoritesService } from '../../../services/favorites.service';
import { Favorite } from '../../../models/favorite.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';


@Component({
    selector: 'd3s-header-homepage',
    template:
    `
        <div *ngIf="visible" class="show-on-medium-and-down hide-on-med-and-up" (click)="handleClick()">
            <i *ngIf="isHomePageItem && !isLoading" class="fa fa-check"></i>
            <i *ngIf="isLoading" style="color: #000;" class="fa fa-spinner fa-spin"></i>
            Home Page
        </div>
        <span *ngIf="visible" (click)="handleClick()" class="favorite hide-on-med-and-down" [style.color]="isHomePageItem ? '#54fffb' : null" [title]="isHomePageItem ? 'Remove home page' : 'Make this my home page'" >
            <i *ngIf="!isLoading" class="fa fa-home"></i><i *ngIf="isLoading" style="color: #000;" class="fa fa-spinner fa-spin"></i>    
        </span>
    `,
    providers: [FavoritesService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderHomePageComponent implements OnInit, OnDestroy, OnChanges {
    @Input() uri: string;
    @Input() isFavoriteItem: boolean = false;
    @Input() isHomePageItem: boolean = false;
    @Input() favItems: Favorite[] = [];
    @Input() currentObject: string;
    @Input() currentObjectId: number;

    private subBreadcrumb: any;
    public isLoading = false;


    private name: string;
    public visible: boolean = true;

    constructor(private router: Router,
        private favoritesService: FavoritesService,
        private breadcrumbService: HeaderBreadcrumbService,
        protected headerActionsService: HeaderActionsService,
        private ref: ChangeDetectorRef
    ) {
    }

    ngOnInit() {
        this.subBreadcrumb = this.breadcrumbService.breadcrumbs$.subscribe(b => {
            this.name = b.text;
        });
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.uri && changes["uri"]) {
            this.visible = this.checkVisible();
        }
        this.checkIsHomePage();
    }

    ngOnDestroy() {
        this.subBreadcrumb.unsubscribe();
    }

    handleClick() {
        if (this.isLoading) {
            console.log('ERROR: CANNOT SAVE FAVORITE LOADING');
            return;
        }

        if (this.isAdminUri()) {
            console.log('ERROR: CANNOT SAVE FAVORITE FOR ADMIN PAGES');
            return;
        }

        if (this.isIssueUri()) {
            console.log('ERROR: CANNOT SAVE FAVORITE FOR RAISE ISSUE');
            return;
        }

        this.isLoading = true;
        let f = new Favorite();
        f.ObjectID = this.currentObjectId;
        f.Object = this.currentObject;
        f.Name = this.name;
        f.Route = this.uri ? this.uri : 'home';//null route is home    
        f.IsHomePage = !this.isHomePageItem;
        this.isHomePageItem = !this.isHomePageItem;    
        this.isFavoriteItem = !this.isFavoriteItem;
        this.favoritesService.toggleFavorite(f).subscribe(
            fav => {
                this.headerActionsService.emitFavoritesChange();
                this.isLoading = false;
                this.ref.markForCheck();
            }
        );
    }

    checkIsFavorite() {
        if (this.favItems == null) return;

        this.isFavoriteItem = false;
        if (!this.uri) this.uri = 'home';
        let index = this.favItems.findIndex(x => x.Route == this.uri && x.IsHomePage == false);

        this.isFavoriteItem = index >= 0;
    }

    checkIsHomePage() {
        if (this.favItems == null) return;

        this.isHomePageItem = false;
        if (!this.uri) this.uri = 'home';
        let index = this.favItems.findIndex(x => x.Route == this.uri && x.IsHomePage == true);

        this.isHomePageItem = index >= 0;
    }

    checkVisible() {
        return !this.isAdminUri() && !this.isIssueUri();
    }

    isAdminUri() {
        return (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
    }

    isIssueUri() {
        return (this.uri || '').toUpperCase() == `${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE}`.toUpperCase();
    }
}

