import { Component, ChangeDetectionStrategy, ChangeDetectorRef, OnInit, OnDestroy, Output, EventEmitter } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { FavoritesService } from '../../../services/favorites.service';
import { Favorite } from '../../../models/favorite.model';
import * as _ from 'lodash'; 

declare var CurrentResourceID;
declare var SingleSignOn;
declare var ResourceName;
declare var ResourceEmail;
declare var CompanySettings;

@Component({
    selector: 'd3s-header-mini-menu',
    template: ` <span #item style="display:table;" class="header-search" [ngClass]="{'header-search-active':active}" (mouseenter)="show(item)" (mouseleave)="hide(item)" >
                    <div><i class="fa fa-bars"></i></div>
                    <div class="search-child header-profile-panel">                        
                        <div class="row">          
                            <ul>
                                <li class="header-item"><d3s-header-profile></d3s-header-profile></li>
                                <li *ngIf="showShoppingCart" routerLink="/cart" class="header-item">Shopping Cart</li>
                                <li class="header-item" *ngIf="headerActionsService.showFavorite && !isAdminUrl" ><d3s-header-favorites [uri]="uri" [favItems]="favItems" [currentObject]="currentObject" [currentObjectId]="currentObjectId"></d3s-header-favorites></li>
                                <li class="header-item" *ngIf="headerActionsService.showFavorite && !isAdminUrl" ><d3s-header-homepage [uri]="uri" [favItems]="favItems" [currentObject]="currentObject" [currentObjectId]="currentObjectId"></d3s-header-homepage></li>
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

    private favItems: Favorite[] = [];
    private currentObject: string;
    private currentObjectId: number;
    private headerActionsSub;

    private controlWidth = 0;

    constructor(
        public headerActionsService: HeaderActionsService,
        private breadcrumbService: HeaderBreadcrumbService,
        private favoritesService: FavoritesService,
        private router: Router,
        private ref: ChangeDetectorRef,) { }

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
                this.hasRaiseIssueButton = ((!e.urlAfterRedirects.toLowerCase().endsWith('workflow/raiseissue') && !this.isAdminUrl && !isResourceUrl && (CompanySettings.DisableIssueManagement === 'false')) == true);


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
            if (this.favItems == null) {
                this.favoritesService.getFavorites().subscribe(
                    fav => {
                        this.favItems = fav;
                    }
                );
            }
        });


        if (CompanySettings != null && CompanySettings.EnableShoppingCart.toString() === 'true') {
            this.showShoppingCart = true;
        }

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
        this.routerSub.unsubscribe();
        this.subFavorites.unsubscribe();
        this.subObjectChange.unsubscribe();
        this.headerActionsSub.unsubscribe();
    }

}

