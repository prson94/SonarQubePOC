import { Component, EventEmitter, Output, ViewChild } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { HeaderActionsService } from '../../../services/header-actions.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { FavoritesService } from '../../../services/favorites.service';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { FavoriteApiModel, FavoriteViewModel } from '../../../models/favorite.model';
import { trimStart } from "lodash-es";
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';
import { HeaderBreadcrumbService } from "../../../services/header-breadcrumb.service";
import { HeaderActions } from "../../../models/header.model";
import { Subscription } from "rxjs";


@Component({
    selector: 'd3s-header-actions',
    templateUrl: './header-actions.component.html'
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

	public hasRaiseIssueButton: boolean = false;
    public showShoppingCart: boolean = false;

    private routerSub;
	private subObjectChange: any;
	private subShowFollow: Subscription;
    private subFavorites: any;

    private favItems: FavoriteViewModel[] = [];
    private currentObject: string;
    private currentObjectId: number;
    private headerActionsSub;
    private homePageItem: FavoriteApiModel;

    private resizeTimer: any;

    private controlWidth = 0;
    Uid: any;

    constructor(
        public headerActionsService: HeaderActionsService,
		private headerBreadcrumbService: HeaderBreadcrumbService,
        private secondaryNavService: SecondaryNavService,
        protected settingsService: CompanySettingsService,
        private favoritesService: FavoritesService,
        private router: Router) { }

    ngOnInit() {
        const hideHeader = this.settingsService.getSettingById(CompanySettingEnum.HideHeaderBarControls).BooleanSetting.Value;
        if (hideHeader) {
            this.enabled = false;
        }

        this.routerSub = this.router.events.subscribe((e) => {
            if (e instanceof NavigationEnd) {
                const showFavorite = this.settingsService.getSettingById(CompanySettingEnum.ShowFavorites).BooleanSetting.Value;
                const showFollow = this.settingsService.getSettingById(CompanySettingEnum.ShowImpactSidebar).BooleanSetting.Value;

                this.headerActionsService.setActionsToDefaultValues(showFavorite, showFollow);
                this.previousUrl = this.currentUrl;
                this.currentUrl = e.url;
                this.isAdminSidebarUrl = false;
                this.uri = trimStart(e.urlAfterRedirects, '/');
                
                let isHomeUrl: boolean = false;
                isHomeUrl = (this.uri && this.uri.toUpperCase() === SiteUrlHelpers.SITE_URL_HOME_ROOT.toUpperCase());

                //dont show raise issue button on raise issue screen or any admin screens or user profile    
                this.isAdminUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
                const isResourceUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_RESOURCE_ROOT.toUpperCase());
                const isSearchUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_SEARCH_ROOT.toUpperCase());

                let isReferenceUrl = false;
                isReferenceUrl = (this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_REFERENCE_ROOT.toUpperCase());

                if (!isReferenceUrl)
                {
                    if ((this.currentObject != null && this.currentObjectId != null) && (this.currentObject === 'ReferenceItemType'))
                    {
                        if (((this.uri || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_FIELDS_ROOT.toUpperCase()))
							||
							((this.uri || '').toUpperCase().indexOf("diagrams".toUpperCase()) > 0)
							||
							((this.uri || '').toUpperCase().endsWith("relationships".toUpperCase()))
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


                const isMonitorUrl = (this.uri || "").toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_MONITOR_ROOT.toUpperCase());
                const isCommunityUrl = (this.uri || "").toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_COMMUNITY_ROOT.toUpperCase());
				const isDashboardUrl = (this.uri || "").toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_DASHBOARD_ROOT.toUpperCase());
				const isSemanticsUrl = (this.uri || "").toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_SEMANTICTYPES_ROOT.toUpperCase());

                if (this.previousUrl) {
                    this.previousUrl = trimStart(this.previousUrl, '/');
                    this.isAdminSidebarUrl = (this.uri || '').toUpperCase().startsWith('sidebar'.toUpperCase()) && (this.previousUrl || '').toUpperCase().startsWith(SiteUrlHelpers.SITE_URL_ADMIN_ROOT.toUpperCase());
                }

                const disableIssueManagement = this.settingsService.getSettingById(CompanySettingEnum.DisableIssueManagement).BooleanSetting.Value;

                this.hasRaiseIssueButton = (
                    !e.urlAfterRedirects.toLowerCase().endsWith('workflow/raiseissue')
                    && !isHomeUrl
                    && !isSearchUrl
                    && !this.isAdminUrl
                    && !isReferenceUrl
                    && !isCommunityUrl
                    && !isMonitorUrl
                    && !isDashboardUrl
                    && !isResourceUrl
                    && !this.isAdminSidebarUrl
					&& !disableIssueManagement
					&& !isSemanticsUrl
				);

                setTimeout(() => { this.calculateControlWidth(); }, 250);
            }
        });
		
		this.subShowFollow = this.headerBreadcrumbService.currentObjectInfo$.subscribe((currentObject) => {
			const headerActions = new HeaderActions();
			headerActions.showFollow = currentObject.AssetTypeUid?.length > 0 || currentObject.AssetUid?.length > 0;
			this.headerActionsService.setCurrentHeaderActions(headerActions);
		});


        this.subFavorites = this.headerActionsService.onFavoritesChanges$.subscribe(() => {
            this.favoritesService.getHomePageAndFavorites().subscribe(
                (homefav) => {
                    this.favItems = homefav.Favorites;
                    this.homePageItem = homefav.Homepage;
                }
            );
        });
        
        this.subObjectChange = this.secondaryNavService.currentObject$.subscribe((c) => {
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
                (homefav) => {
                    this.favItems = homefav.Favorites;
                    this.homePageItem = homefav.Homepage;
                }
            );
        });

        this.showShoppingCart = this.settingsService.getSettingById(CompanySettingEnum.EnableShoppingCart).BooleanSetting.Value;

		this.headerActionsSub = this.headerActionsService.onHeaderActionsChange$.subscribe((x) => {
			if (typeof x.showFollow !== "undefined") {
				this.headerActionsService.showFollow = x.showFollow;
			}
			if (typeof x.showRaiseIssue !== "undefined") {
				this.headerActionsService.showRaiseIssue = x.showRaiseIssue;
			}
        });

    }

    private calculateControlWidth() {
        const buffer = 100;
        if (this.enabled === false) {
            this.controlWidth = buffer;
        } else {
            this.controlWidth = this.actionsUIElem.nativeElement.parentElement.offsetWidth;
            this.controlWidth += buffer; //small buffer zone + paddings to avoid wrapping           
        }
        this.controlWidthChange.emit(this.controlWidth);

    }
    onResize() {
        clearTimeout(this.resizeTimer);
        this.resizeTimer = window.setTimeout(() => this.calculateControlWidth(), 250);
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
		this.subShowFollow?.unsubscribe();
    }
}

