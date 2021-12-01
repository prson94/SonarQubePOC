import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    Input,
    OnChanges,
    OnDestroy,
    OnInit,
    SimpleChanges
} from '@angular/core';
import { FavoritesService } from '../../../services/favorites.service';
import { FavoriteApiModel } from '../../../models/favorite.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import * as _ from 'lodash';


@Component({
    selector: 'd3s-header-favorites',
    template:
        `
        <div *ngIf="visible" class="show-on-medium-and-down hide-on-med-and-up" (click)="handleClick()">
            <div class="mini-menu-line">
                <div class="check-gutter">
                    <i *ngIf="isFavoriteItem && !isLoading" class="fa fa-check"></i>
                    <i *ngIf="isLoading" style="color: #000;" class="fa fa-spinner fa-spin "></i>
                </div>
                <div class="text">Favorite</div>
                <div class="expand-gutter"></div>            
            </div>
        </div>
        <div *ngIf="visible" (click)="handleClick()" class="header-button hide-on-med-and-down" [ngClass]="{'active' : isFavoriteItem }" [title]="isFavoriteItem ? 'Remove from favorites' : 'Add to favorites'">
            <i *ngIf="!isLoading" class="fa fa-star"></i><i *ngIf="isLoading" class="fa fa-spinner fa-spin"></i>    
        </div>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderFavoritesComponent implements OnInit, OnDestroy, OnChanges {
    @Input() uri: string;
    @Input() favItems: FavoriteApiModel[] = [];
    @Input() currentObject: string; // TODO: get rid of mixed clientside-serverside work here. 
    @Input() currentObjectId: number; // TODO: get rid of mixed clientside-serverside work here. 
    @Input() homePageItem: FavoriteApiModel = null;

    private subBreadcrumb: any;
    private isLoading = false;
    private favoriteRoutesSet = new Set<string>();

    private name: string;

    constructor(
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

    ngOnChanges(changes: SimpleChanges) {
        if ('favItems' in changes) {
            this.favoriteRoutesSet = new Set(this.favItems.map(f => f.Route));
        }

        this.ref.markForCheck();
    }

    ngOnDestroy() {
        if (this.subBreadcrumb) {
            this.subBreadcrumb.unsubscribe();
        }
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
        if (this.isHomePageItem) {
            console.log('ERROR: CANNOT CHANGE THIS FAVORITE IS HOMEPAGE');
            return;
        }

        this.isLoading = true;
        let f = new FavoriteApiModel();
        // TODO: get rid of mixed clientside-serverside work here. 
        // TODO: Either Type&Name is determined by server, or Route isn't passed to server
        if (this.IsPageType()) {
            f.Type = "Page";
        } else if (this.currentObject.endsWith("Type")) {
            f.Type = "AssetType";
        } else {
            f.Type = "Asset";
        }
        f.Name = this.name;
        f.Route = this.currentUri;
        this.favoritesService.toggleFavoriteV2(f).subscribe(
            fav => {
                this.headerActionsService.emitFavoritesChange();
                this.isLoading = false;
                this.ref.markForCheck();
            }
        );
    }

    IsPageType(): any {
        let res = false;
        if ((!this.currentObject && !this.currentObjectId) || (this.currentObject == 'ReferenceItemType')) {
            res = true;
        }
        if (this.uri.toLowerCase().indexOf("sidebar") !== -1) {
            res = true;
        }
        return res;
    }

    get isFavoriteItem() {
        return this.favoriteRoutesSet.has(this.currentUri);
    }

    get isHomePageItem() {
        return this.homePageItem?.Route === this.currentUri;
    }

    get currentUri() {
        return this.uri ?? 'home';
    }

    get visible() {
        return !this.isAdminUri() && !this.isIssueUri();
    }

    isAdminUri() {
        return (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
    }

    isIssueUri() {
        return (this.uri || '').toUpperCase() == `${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE}`.toUpperCase();
    }
}

