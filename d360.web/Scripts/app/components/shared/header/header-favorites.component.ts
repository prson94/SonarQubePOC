import {
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    Component,
    Input,
    OnChanges,
    OnDestroy,
    OnInit,
    SimpleChange
} from '@angular/core';
import {Router} from '@angular/router';
import {FavoritesService} from '../../../services/favorites.service';
import {Favorite} from '../../../models/favorite.model';
import {HeaderBreadcrumbService} from '../../../services/header-breadcrumb.service';
import {HeaderActionsService} from '../../../services/header-actions.service';
import {SiteUrlHelpers} from '../../../static/site-url-helpers';


@Component({
    selector: 'd3s-header-favorites',
    template:
    `
        <div *ngIf="visible" class="show-on-medium-and-down hide-on-med-and-up" (click)="handleClick()">
            <div class="mini-menu-line">
                <div class="check-gutter">
                    <i *ngIf="isFavoriteItem && !isLoading" class="fa fa-check"></i>
                    <i *ngIf="isLoading" style="color: #000;" class="fa fa-spinner fa-spin"></i>
                </div>
                <div class="text">Favourite</div>
                <div class="expand-gutter"></div>            
            </div>
        </div>
        <div *ngIf="visible" (click)="handleClick()" class="header-button hide-on-med-and-down" [ngClass]="{'active' : isFavoriteItem }" [title]="isFavoriteItem ? 'Remove from favorites' : 'Add to favorites'">
            <i *ngIf="!isLoading" class="fa fa-star"></i><i *ngIf="isLoading" style="color: #000;" class="fa fa-spinner fa-spin"></i>    
        </div>
    `,
    providers: [FavoritesService],
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderFavoritesComponent implements OnInit, OnDestroy, OnChanges {
    @Input() uri: string;
    @Input() isFavoriteItem: boolean = false;
    @Input() favItems: Favorite[] = [];
    @Input() currentObject: string;
    @Input() currentObjectId: number;

    private isHomePageItem: boolean = false;
    private subBreadcrumb: any;
    private isLoading = false;

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
        this.checkIsFavorite();
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

        if (this.isHomePageItem)
            return;

        this.isLoading = true;
        let f = new Favorite();
        f.ObjectID = this.currentObjectId;
        f.Object = this.currentObject;
        f.Name = this.name;
        f.Route = this.uri ? this.uri : 'home';//null route is home        
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
        let index = this.favItems.findIndex(x => x.Route == this.uri);
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

