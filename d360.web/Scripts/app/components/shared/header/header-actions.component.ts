import { Component, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
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
                    <li><a [routerLink]="resourceUrl()" class="photo" title="Go to your profile"><img [src]="'/resources/image/' + resourceId + '?size=25'" height="25" width="25" /></a></li>                    
                </ul> 
                `,  
    providers: [FavoritesService]
})

export class HeaderActionsComponent {        
    private resourceId: number = CurrentResourceID;
    private isAdminUrl = false;
    private uri = "";
    private hasRaiseIssueButton: boolean = true;
    private showShoppingCart: boolean = false;

    private routerSub;
    private subObjectChange: any;
    private subFavorites: any;

    private favItems: Favorite[] = [];
    private currentObject: string;
    private currentObjectId: number;

    constructor(
        private headerActionsService: HeaderActionsService,
        private breadcrumbService: HeaderBreadcrumbService,
        private favoritesService: FavoritesService,
        private router: Router) { }

    ngOnInit() {

       // this.routerSub = this.router.
        this.routerSub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.uri = _.trimStart(e.urlAfterRedirects,'/');
                this.isAdminUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());

                //dont show raise issue button on raise issue screen or any admin screens            
                this.hasRaiseIssueButton = (!e.urlAfterRedirects.toLowerCase().endsWith('workflow/raiseissue') && (e.urlAfterRedirects.toLowerCase().indexOf('/admin/') == -1) && CompanySettings.DisableIssueManagement != 'true');            

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

    private resourceUrl() {
        return SiteUrlHelpers.getObjectUrl('Resource', this.resourceId);
    }

    ngOnDestroy() {
        this.routerSub.unsubscribe();
        this.subFavorites.unsubscribe();
        this.subObjectChange.unsubscribe();
    }
}

