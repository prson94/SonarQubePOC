import { Component, OnInit, OnDestroy, ChangeDetectionStrategy, Output, EventEmitter, ViewChild, AfterViewInit } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { FavoritesService } from '../../../services/favorites.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { FavoriteApiModel } from '../../../models/favorite.model';
import * as _ from 'lodash';

declare var CurrentResourceID;
declare var CompanySettings;

@Component({
    selector: 'd3s-header-actions',
    template: `
                <div *ngIf="enabled" #actions class="header-action-container" (window:resize)="onResize($event)">
                    <ul class="header-actions-list">
                        <li class="header-action-li spacer" *ngIf="headerActionsService.showSearch"><d3s-header-typeahead-search></d3s-header-typeahead-search></li>
                        <li class="header-action-li spacer" *ngIf="hasRaiseIssueButton && headerActionsService.showRaiseIssue"><d3s-raise-issue-button></d3s-raise-issue-button></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="showShoppingCart && headerActionsService.showShoppingCart" ><d3s-header-shopping-cart ></d3s-header-shopping-cart></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="headerActionsService.showFavorite && !isAdminUrl && !isAdminSidebarUrl" ><d3s-header-favorites [uri]="uri" [favItems]="favItems" [currentObject]="currentObject" [currentObjectId]="currentObjectId" [Uid]="Uid" [homePageItem]="homePageItem"></d3s-header-favorites></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="headerActionsService.showFavorite && !isAdminUrl && !isAdminSidebarUrl" ><d3s-header-homepage [uri]="uri" [favItems]="favItems" [currentObject]="currentObject" [currentObjectId]="currentObjectId" [Uid]="Uid" [homePageItem]="homePageItem"></d3s-header-homepage></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="headerActionsService.showFollow  && !isAdminUrl && !isAdminSidebarUrl" ><d3s-header-follow></d3s-header-follow></li>                    
                        <li class="header-action-li" *ngIf="headerActionsService.showHelp"><d3s-header-help></d3s-header-help></li>
                        <li class="header-action-li hide-on-med-and-down" *ngIf="headerActionsService.showNotifications"><a href="#" title="Go to notification settings"><i class="fa fa-bell-o"></i></a></li>
                        <li class="header-action-li hide-on-med-and-down" ><d3s-header-profile></d3s-header-profile></li>                    
                    </ul> 
                    <ul class="show-on-medium-and-down hide-on-large-only header-actions-list">             
                        <li class="header-action-li"><d3s-header-mini-menu></d3s-header-mini-menu></li>
                    </ul>
                </div>
                `
})

export class HeaderActionsComponent {
    @Output() controlWidthChange = new EventEmitter();
    @ViewChild('actions', { static: false }) actionsUIElem : any;

    public enabled: boolean = true;
    public isAdminUrl = false;
    public isAdminSidebarUrl = false;
    public previousUrl: string;
    public currentUrl: string;

    private uri = "";
    public notTopArtifact: boolean = true;
    public testuri: string[] = [];
    public hasRaiseIssueButton: boolean = false;
    public showShoppingCart: boolean = false;

    private routerSub;
    private subObjectChange: any;
    private subFavorites: any;

    private favItems: FavoriteApiModel[] = [];
    private currentObject: string;
    private currentObjectId: number;
    private headerActionsSub;
    private homePageItem: FavoriteApiModel;

    private resizeTimer: any;

    private controlWidth = 0;
    Uid: any;

    constructor(
        public headerActionsService: HeaderActionsService,
        private secondaryNavService: SecondaryNavService,
        private favoritesService: FavoritesService,
        private router: Router) { }

    ngOnInit() {
        if (CompanySettings && CompanySettings.HideHeaderBarControls.toString().toLowerCase() === 'true') {
            this.enabled = false;
        }

        this.routerSub = this.router.events.subscribe(e => {
            if (e instanceof NavigationEnd) {
                this.headerActionsService.setActionsToDefaultValues();
                this.previousUrl = this.currentUrl;
                this.currentUrl = e.url;
                this.isAdminSidebarUrl = false;
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
                
                let isHomeUrl: boolean = false;
                isHomeUrl = (this.uri && this.uri.toUpperCase() == SiteUrlHelpers.SITE_URL_HOME_ROOT.toUpperCase());

                //dont show raise issue button on raise issue screen or any admin screens or user profile    
                this.isAdminUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
                let isResourceUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_RESOURCE_ROOT.toUpperCase());
                let isSearchUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_SEARCH_ROOT.toUpperCase());

                let isReferenceUrl = false;
                isReferenceUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_REFERENCE_ROOT.toUpperCase());

                if (!isReferenceUrl)
                {
                    if ((this.currentObject != null && this.currentObjectId != null) && (this.currentObject == 'ReferenceItemType'))
                    {
                        if (((this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_FIELDS_ROOT.toUpperCase()))
                            ||
                            ((this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_VISUALIZATION_ROOT.toUpperCase()))
                            ||
                            ((this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_RELATIONSHIP_ROOT.toUpperCase()))
                            ||
                            ((this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_RESPONSIBILITIES_ROOT.toUpperCase()))
                            ||
                            ((this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_WORKFLOW_MONITOR_ROOT.toUpperCase()))
                            ||
                            ((this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_AUDIT_ROOT.toUpperCase()))
                        )
                        {
                            isReferenceUrl = true;
                        }
                        
                    }
                    else if (this.currentObject == null) {
                        if ((this.uri || '').toUpperCase().startsWith('SIDEBAR/') && (this.previousUrl || '').toUpperCase().startsWith('/REFERENCE;REFERENCELISTID')) {
                            isReferenceUrl = true;
                        }
                    }
                }


                let isMonitorUrl = (this.uri || "").toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_MONITOR_ROOT.toUpperCase());
                let isCommunityUrl = (this.uri || "").toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT.toUpperCase());
                let isDashboardUrl = (this.uri || "").toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_DASHBOARD_ROOT.toUpperCase());

                if (this.previousUrl) {
                    this.previousUrl = _.trimStart(this.previousUrl, '/');
                    this.isAdminSidebarUrl = (this.uri || '').toUpperCase().startsWith('sidebar'.toUpperCase()) && (this.previousUrl || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
                }

                this.hasRaiseIssueButton = ((!e.urlAfterRedirects.toLowerCase().endsWith('workflow/raiseissue') && !isHomeUrl && !isSearchUrl &&
                    !this.isAdminUrl && !isReferenceUrl && !isCommunityUrl && !isMonitorUrl && !isDashboardUrl && !isResourceUrl && !this.isAdminSidebarUrl &&
                    (CompanySettings.DisableIssueManagement === "false")) === true);                

                setTimeout(() => { this.calculateControlWidth(); }, 250);
            }
        });


        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(() => {
            this.favoritesService.getHomePageAndFavorites().subscribe(
                homefav => {
                    this.favItems = homefav.Favorites;
                    this.homePageItem = homefav.Homepage;
                }
            );
        });
        
        this.subObjectChange = this.secondaryNavService.currentObject$.subscribe(c => {
            this.currentObject = null;
            this.currentObjectId = null;
            this.Uid = null;
            if (c) {
                if (c.isType) {
                    this.currentObject = c.objectType;
                    this.currentObjectId = c.objectTypeID;
                } else {
                    this.currentObject = c.objectName;
                    this.currentObjectId = c.objectID;
                }
                this.Uid = c.Uid;
            }
            this.favoritesService.getHomePageAndFavorites().subscribe(
                homefav => {
                    this.favItems = homefav.Favorites;
                    this.homePageItem = homefav.Homepage;
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
        let buffer = 100;
        if (this.enabled === false) {
            this.controlWidth = buffer;
        } else {
            this.controlWidth = this.actionsUIElem.nativeElement.parentElement.offsetWidth;
            this.controlWidth += buffer; //small buffer zone + paddings to avoid wrapping           
        }
        this.controlWidthChange.emit(this.controlWidth);

    }
    onResize(event) {
        clearTimeout(this.resizeTimer);
        this.resizeTimer = window.setTimeout(() => this.calculateControlWidth(), 250)
    }
    ngOnDestroy() {
        if (this.routerSub) {
            this.routerSub.unsubscribe();
        }
        if (this.subFavorites) {
            this.subFavorites.unsubscribe();
        }
        if (this.subObjectChange) {
            this.subObjectChange.unsubscribe();
        }
        if (this.headerActionsSub) {
            this.headerActionsSub.unsubscribe();
        }
    }
}

