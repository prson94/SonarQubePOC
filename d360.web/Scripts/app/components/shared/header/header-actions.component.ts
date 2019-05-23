import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, Output, EventEmitter, ViewChild, AfterViewInit } from '@angular/core';
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
                <div #actions class="header-action-container">
                    <ul class="header-actions-list">
                        <li class="header-action-li" *ngIf="headerActionsService.showSearch"><d3s-header-typeahead-search></d3s-header-typeahead-search></li>
                        <li class="header-action-li" *ngIf="hasRaiseIssueButton"><d3s-raise-issue-button></d3s-raise-issue-button></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="showShoppingCart" ><d3s-header-shopping-cart ></d3s-header-shopping-cart></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="headerActionsService.showFavorite && !isAdminUrl" ><d3s-header-favorites [uri]="uri" [favItems]="favItems" [currentObject]="currentObject" [currentObjectId]="currentObjectId"></d3s-header-favorites></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="headerActionsService.showFavorite && !isAdminUrl" ><d3s-header-homepage [uri]="uri" [favItems]="favItems" [currentObject]="currentObject" [currentObjectId]="currentObjectId"></d3s-header-homepage></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="headerActionsService.showFollow  && !isAdminUrl" ><d3s-header-follow></d3s-header-follow></li>                    
                        <li class="header-action-li" *ngIf="headerActionsService.showHelp"><d3s-header-help></d3s-header-help></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="headerActionsService.showNotifications"><a href="#" title="Go to notification settings"><i class="fa fa-bell-o"></i></a></li>
                        <li class="header-action-li hide-on-med-and-down" ><d3s-header-profile></d3s-header-profile></li>                    
                    </ul> 
                    <ul class="show-on-medium-and-down hide-on-large-only header-actions-list">             
                        <li class="header-action-li"><d3s-header-mini-menu></d3s-header-mini-menu></li>
                    </ul>
                </div>
                `,
    providers: [FavoritesService]
})

export class HeaderActionsComponent {
    @Output() controlWidthChange = new EventEmitter();
    @ViewChild('actions') actionsUIElem : any;

    public isAdminUrl = false;
    private uri = "";
    public notTopArtifact: boolean = true;
    public testuri: string[] = [];
    public hasRaiseIssueButton: boolean = false;
    public showShoppingCart: boolean = false;

    private routerSub;
    private subObjectChange: any;
    private subFavorites: any;

    private favItems: Favorite[] = [];
    private currentObject: string;
    private currentObjectId: number;
    private headerActionsSub;

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
                if ((this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ARTIFACT_ROOT.toUpperCase())) {
                    this.testuri = this.uri.split("/");
                    if (this.testuri.length < 3) {
                        this.notTopArtifact = false;
                    } else {
                        this.notTopArtifact = true;
                    }
                }
                else {
                    this.notTopArtifact = true;
                }

                //dont show raise issue button on raise issue screen or any admin screens or user profile           
                this.isAdminUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
                let isResourceUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_RESOURCE_ROOT.toUpperCase());
                this.hasRaiseIssueButton = ((!e.urlAfterRedirects.toLowerCase().endsWith('workflow/raiseissue') && !this.isAdminUrl && !isResourceUrl && (CompanySettings.DisableIssueManagement==='false') ) == true);                
                this.calculateControlWidth();
            }
        });


        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(() => {
            this.favoritesService.getFavorites().subscribe(
                res => {
                    this.favItems = res;
                }
            );
        });

        this.subObjectChange = this.breadcrumbService.currentObjectInfo$.subscribe(c => {
            this.currentObject = c.type;
            this.currentObjectId = c.id;
            this.favoritesService.getFavorites().subscribe(
                fav => {
                    this.favItems = fav;
                }
            );
        });


        if (CompanySettings != null && CompanySettings.EnableShoppingCart.toString() === 'true') {
            this.showShoppingCart = true;
        }

        this.headerActionsSub = this.headerActionsService.onHeaderActionsChange$.subscribe(x => {
            this.headerActionsService.showFollow = x.showFollow;
        });

    }

    private calculateControlWidth() {
        this.controlWidth = this.actionsUIElem.nativeElement.parentElement.offsetWidth;
        this.controlWidth += 10; //small buffer zone to avoid wrapping

        this.controlWidthChange.emit(this.controlWidth);
    }

    ngOnDestroy() {
        this.routerSub.unsubscribe();
        this.subFavorites.unsubscribe();
        this.subObjectChange.unsubscribe();
        this.headerActionsSub.unsubscribe();
    }
}

