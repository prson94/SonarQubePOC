import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, Output, EventEmitter } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { FavoritesService } from '../../../services/favorites.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { Favorite } from '../../../models/favorite.model';
import * as _ from 'lodash';

declare var CurrentResourceID;
declare var CompanySettings;

@Component({
    selector: 'd3s-header-actions',
    template: `
                <ul class="right hide-on-med-and-down">
                    <li *ngIf="hasRaiseIssueButton"><d3s-raise-issue-button></d3s-raise-issue-button></li>
                    <li *ngIf="showShoppingCart" style="cursor: pointer"><d3s-header-shopping-cart ></d3s-header-shopping-cart></li>
                    <li *ngIf="headerActionsService.showFavorite && !isAdminUrl" style="cursor: pointer"><d3s-header-favorites [uri]="uri" [favItems]="favItems" [currentObject]="currentObject" [currentObjectId]="currentObjectId"></d3s-header-favorites></li>
                    <li *ngIf="headerActionsService.showFavorite && !isAdminUrl" style="cursor: pointer"><d3s-header-homepage [uri]="uri" [favItems]="favItems" [currentObject]="currentObject" [currentObjectId]="currentObjectId"></d3s-header-homepage></li>
                    <li *ngIf="headerActionsService.showFollow  && !isAdminUrl" style="cursor: pointer"><d3s-header-follow></d3s-header-follow></li>                    
                    <li *ngIf="headerActionsService.showHelp"><a routerLink="help" class="help" title="Get help!"><i class="fa fa-question-circle"></i></a></li>
                    <li *ngIf="headerActionsService.showSearch"><d3s-header-typeahead-search></d3s-header-typeahead-search></li>
                    <li *ngIf="headerActionsService.showNotifications"><a href="#" title="Go to notification settings"><i class="fa fa-bell-o"></i></a></li>
                    <li><a href="/slo" title="Sign out"><i class="fa fa-sign-out"></i></a></li>
                    <li><d3s-header-profile></d3s-header-profile></li>                    
                </ul> 
                `,
    providers: [FavoritesService]
})

export class HeaderActionsComponent {
    @Output() controlWidthChange = new EventEmitter();    
    public isAdminUrl = false;
    private uri = "";
    public hasRaiseIssueButton: boolean = true;
    public showShoppingCart: boolean = false;

    private routerSub;
    private subObjectChange: any;
    private subFavorites: any;

    private favItems: Favorite[] = [];
    private currentObject: string;
    private currentObjectId: number;

    private controlWidth = 0;

    constructor(
        public headerActionsService: HeaderActionsService,
        private breadcrumbService: HeaderBreadcrumbService,
        private favoritesService: FavoritesService,
        private router: Router) { }

    ngOnInit() {
        this.routerSub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.uri = _.trimStart(e.urlAfterRedirects, '/');
                this.isAdminUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());

                //dont show raise issue button on raise issue screen or any admin screens            
                this.hasRaiseIssueButton = (!e.urlAfterRedirects.toLowerCase().endsWith('workflow/raiseissue') && (e.urlAfterRedirects.toLowerCase().indexOf('/admin/') == -1) && CompanySettings.DisableIssueManagement != 'true');

                this.calculateControlWidth();
            }
        });


        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(() => {
            this.favoritesService.getFavorites().then(res => {
                this.favItems = res;
            });
        });

        this.subObjectChange = this.breadcrumbService.currentObjectInfo$.subscribe(c => {
            this.currentObject = c.type;
            this.currentObjectId = c.id;
            if (this.favItems == null) {
                this.favoritesService.getFavorites()
                    .then(fav => {
                        this.favItems = fav;
                    });
            }
        });


        if (CompanySettings != null && CompanySettings.EnableShoppingCart.toString() === 'true') {
            this.showShoppingCart = true;
        }

    }

    private calculateControlWidth() {
        this.controlWidth = 55 + 45; //user image and logout
        this.controlWidth += this.headerActionsService.showNotifications ? 45 : 0;
        this.controlWidth += this.headerActionsService.showSearch ? 45 : 0;
        this.controlWidth += this.headerActionsService.showHelp ? 45 : 0;
        this.controlWidth += this.headerActionsService.showFollow ? 45 : 0;
        this.controlWidth += this.headerActionsService.showFavorite ? 45 * 2 : 0; //x2 for fav and home buttons
        this.controlWidth += this.hasRaiseIssueButton ? 115 : 0;

        this.controlWidth += 10; //small buffer zone to avoid wrapping

        this.controlWidthChange.emit(this.controlWidth);
    }

    ngOnDestroy() {
        this.routerSub.unsubscribe();
        this.subFavorites.unsubscribe();
        this.subObjectChange.unsubscribe();
    }
}

