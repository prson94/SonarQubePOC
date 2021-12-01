import { Component, Input, OnInit, OnDestroy, OnChanges, ChangeDetectionStrategy, ChangeDetectorRef, SimpleChanges } from '@angular/core';
import { FavoritesService } from '../../../services/favorites.service';
import { FavoriteApiModel } from '../../../models/favorite.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import * as _ from 'lodash';


@Component({
    selector: 'd3s-header-homepage',
    template:
    `
        <div *ngIf="visible" class="show-on-medium-and-down hide-on-med-and-up" (click)="handleClick()">
            <div class="mini-menu-line">
                <div class="check-gutter">
                    <i *ngIf="isHomePageItem && !isLoading" class="fa fa-check"></i>
                    <i *ngIf="isLoading" style="color: #000;" class="fa fa-spinner fa-spin"></i>
                </div>
                <div class="text">Set as Home Page</div>
                <div class="expand-gutter"></div>            
            </div>
        </div>
        <div *ngIf="visible" (click)="handleClick()" class="header-button hide-on-med-and-down" [ngClass]="{'active' : isHomePageItem }"  [title]="isHomePageItem ? 'Remove home page' : 'Make this my home page'" >
            <i *ngIf="!isLoading" class="fa fa-home"></i><i *ngIf="isLoading" class="fa fa-spinner fa-spin"></i>    
        </div>
    `,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderHomePageComponent implements OnInit, OnDestroy, OnChanges {
    @Input() uri: string;
    @Input() homePageItem: FavoriteApiModel = null;
    @Input() currentObject: string; // TODO: get rid of mixed clientside-serverside work here. 
    @Input() currentObjectId: number; // TODO: get rid of mixed clientside-serverside work here. 

    private subBreadcrumb: any;
    public isLoading = false;

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

        this.isLoading = true;
        let f = new FavoriteApiModel();
        // TODO: get rid of mixed clientside-serverside work here. 
        // TODO: Either Type&Name is determined by server, or Route isn't passed to server
        //check these to determine fav type
        if (this.IsPageType()) {
            f.Type = "Page";
        } else if (this.currentObject.endsWith("Type")) {
            f.Type = "AssetType";
        } else {
            f.Type = "Asset";
        }
        f.Name = this.name;
        f.Route = this.currentUri;   
        this.favoritesService.toggleHomePageV2(f).subscribe(
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

