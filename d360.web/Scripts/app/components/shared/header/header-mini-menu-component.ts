import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit, OnDestroy, Output, EventEmitter } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { FavoritesService } from '../../../services/favorites.service';
import { FavoriteApiModel, FavoriteViewModel } from '../../../models/favorite.model';
import * as _ from 'lodash'; 
import { CompanySettingEnum } from '../../../models/settings.model';
import { CompanySettingsService } from '../../../services/settings.service';

declare var CurrentResourceID;
declare var SingleSignOn;
declare var ResourceName;
declare var ResourceEmail;

@Component({
    selector: 'd3s-header-mini-menu',
    template: ` <span #item class="header-search header-table" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)" >
                    <div class="header-button"><i class="fa fa-bars"></i></div>
                    <div class="search-child header-profile-panel">                        
                        <div class="row">          
                            <ul>
                                <li class="header-item"><d3s-header-profile></d3s-header-profile></li>
                                <li class="header-item">
                                    <div class="mini-menu-line">
                                        <div class="separator"></div>       
                                    </div>
                                </li>
                                <li *ngIf="showShoppingCart" routerLink="/cart" class="header-item">
                                    <div class="mini-menu-line">
                                        <div class="check-gutter">
                                        </div>
                                        <div class="text">Shopping Cart</div>
                                        <div class="expand-gutter right"></div>            
                                    </div>
                                </li>
                                <li class="header-item" *ngIf="headerActionsService.showFavorite && !isAdminUrl" ><d3s-header-favorites [uri]="uri" [favItems]="favItems" [homePageItem]="homePageItem"></d3s-header-favorites></li>
                                <li class="header-item" *ngIf="headerActionsService.showFavorite && !isAdminUrl" ><d3s-header-homepage [uri]="uri" [homePageItem]="homePageItem"></d3s-header-homepage></li>
                                <li class="header-item" *ngIf="headerActionsService.showFollow  && !isAdminUrl" ><d3s-header-follow></d3s-header-follow></li>                    
                                <li class="header-item" *ngIf="headerActionsService.showNotifications"><a href="#" title="Go to notification settings"><i class="fa fa-bell-o"></i>Notifications</a></li>
                            </ul>                                                    
                        </div>
                    </div>
                <span>`,
    changeDetection: ChangeDetectionStrategy.OnPush
})

export class HeaderMiniMenuComponent implements OnInit, OnDestroy {

    @Output() controlWidthChange = new EventEmitter();
    public isAdminUrl = false;
    private uri = "";
    public notTopArtifact: boolean = true;
    public testuri: string[] = [];
    public hasRaiseIssueButton: boolean = false;
    public showShoppingCart: boolean = false;

    public active: boolean = false;
    private hideHandle: number = 0;

    private routerSub;
    private subObjectChange: any;
    private subFavorites: any;

    private homePageItem: FavoriteApiModel;
    private favItems: FavoriteViewModel[] = [];
    private currentObject: string;
    private currentObjectId: number;
    private headerActionsSub;

    private controlWidth = 0;
    Uid: any;

    constructor(
        private favoritesService: FavoritesService,
        public headerActionsService: HeaderActionsService,
        private secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private router: Router,
        private ref: ChangeDetectorRef) {
    }

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
                this.hasRaiseIssueButton = 
                        !e.urlAfterRedirects.toLowerCase().endsWith('workflow/raiseissue')
                        && !this.isAdminUrl
                        && !isResourceUrl
                        && !this.settingsService.getSettingById(CompanySettingEnum.DisableIssueManagement).BooleanSetting.Value;

                this.calculateControlWidth();
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

        this.showShoppingCart = this.settingsService.getSettingById(CompanySettingEnum.EnableShoppingCart).BooleanSetting.Value;

        this.headerActionsSub = this.headerActionsService.onHeaderActionsChange$.subscribe(x => {
            this.headerActionsService.showFollow = x.showFollow;
        });

    }

    public raiseIssue() {
        this.router.navigateByUrl(`${SiteUrlHelpers.SITE_URL_WORKFLOW_ROOT}/${SiteUrlHelpers.SITE_URL_WORKFLOW_RAISE_ISSUE}`);
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

    show(item) {
        // check for any pending hides and cancel them
        if (this.hideHandle > 0) {
            window.clearTimeout(this.hideHandle);
            this.hideHandle = 0;
        }
        let panel = item.children[0].nextElementSibling;
        if (panel) {
            this.active = true;

            panel.style.zIndex = 1000;

            panel.style.top = (item.offsetHeight - 1) + 'px'; // -1 for the border so it blends
            panel.style.right = '0px';

        }
    }

    hide(item) {
        if (this.hideHandle > 0) return; //pending hide ignore new request
        //queue up a request to hide the window.
        this.hideHandle = window.setTimeout(() => {
            this.active = false;
            this.ref.markForCheck();
        },
            500);
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

