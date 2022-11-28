import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BaseComponent } from '../../shared/base.component';
import { DashboardService } from '../../../services/dashboard.service';
import { DashboardModel } from '../../../models/dashboard.model';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { TitleAndTabsService } from '../../../services/title-and-tabs.service';
import { Param } from '../../../enums/param.enum';

@Component({
	selector: 'd3s-dashboard',
	templateUrl: './dashboard.component.html',
	providers: [DashboardService],
})

export class DashboardComponent extends BaseComponent implements OnInit, OnDestroy {
	private sub: any;
	dashboards: DashboardModel[] = [];
	dashboard: DashboardModel;
	selected: DashboardModel;
	dashboardName: string;
	reportID: number | string;
	assetTypeUid: string;
	assetUid: string;
	showSingle: boolean = false;
	showError: boolean;
	private folderTitle: string;

	constructor(
		protected dashboardService: DashboardService,
		private route: ActivatedRoute,
		private headerBreadcrumbService: HeaderBreadcrumbService,
		private titleAndTabsService: TitleAndTabsService,
		secondaryNavService: SecondaryNavService,
		private router: Router,
		breadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService
	) {
		super(settingsService);
		this.secondaryNavService = secondaryNavService;
		this.breadcrumbsService = breadcrumbService;
	}

	ngOnInit() {
		this.showSingle = false;
		this.sub = this.route.params.subscribe((params) => {
			this.titleAndTabsService.initializeTitleAndTabsCheck(this.route.params, params, $localize`Dashboards`);

			this.dashboardName = params['name'];
			this.assetTypeUid = null;
			if (params['uid'] && (params['uid'] as string).length === 36) {
				this.assetTypeUid = params['uid'];
			}

			this.assetUid = params['assetUid'];
			if (this.assetUid === 'preview') {
				//because of routing without type specifying we cannot easiliy destinguish all 3 rooting scenarious
				//preview string in route gets matches as assetuid value
				this.reportID = this.assetTypeUid;
				this.assetUid = null;
				this.assetTypeUid = null;
			}
			this.loadAvailableDashboards();
			if (this.assetUid) {
				this.buildSecondaryNavigationByAssetUid(this.assetUid,this.buildBreadcrumb.bind(this));
			}
		});
	}


	private buildBreadcrumb(clearInfo: boolean) {

		this.headerBreadcrumbService.clearBreadcrumbs();

		this.headerBreadcrumbService.getFolderTitle('#Dashboards').then((res) => {
			this.folderTitle = res;
			const areaBreadcrumb = new Breadcrumb(
				this.folderTitle ? this.folderTitle : 'Dashboards',
				'/dashboard',
				false
			);
			this.headerBreadcrumbService.showBreadcrumb(areaBreadcrumb);

			if (this.selected) {
				const dashboardCrumb = new Breadcrumb(
					this.selected.Name,
					SiteUrlHelpers.getObjectUrl("Dashboard", this.selected.uid),
					false
				);
				this.headerBreadcrumbService.showBreadcrumb(dashboardCrumb);
			}
			if (clearInfo) {
				this.headerBreadcrumbService.getFolderIcon(res).subscribe((icon) => {
					this.clearSidebar();
					this.secondaryNavService.setCurrentArea(res, icon, $localize`Dashboards`);
					this.secondaryNavService.clearCurrentObject();
					this.secondaryNavService.clearButtons();
					this.secondaryNavService.showHeader(false);
				});
			}

		});
	}

	ngOnDestroy() {
		this.secondaryNavService.resetSecondaryNavActiveItem();

		if (this.sub) {
			this.sub.unsubscribe();
		}
	}

	private loadAvailableDashboards() {
		this.isLoading = true;
		if (this.reportID) {
			this.dashboardService.getDashboardById(this.reportID).subscribe(
				(result) => {
					if (result) {
						this.selected = result[0];
						this.showSingle = true;
					}
					else {
						this.showError = true;
					}

					if (this.showSingle || this.objectType == null) {
						this.buildBreadcrumb(true);
					} else {
						this.buildBreadcrumb(false);
					}

					this.isLoading = false;
				}
			);
		} else {
			var location = null;
			if (this.assetTypeUid) {
				location = 1;
			}
			if (this.assetUid) {
				location = 2;
			}
			this.dashboardService.getDashboardsV2(null, location, null, this.assetTypeUid, this.assetUid)
				.subscribe((res) => {
					this.dashboards = res;
					if (this.objectType && this.objectID && this.dashboardName) {
						this.selected = this.dashboards[0];
						this.showSingle = true;
					}

					if (this.showSingle || this.objectType == null) {
						this.buildBreadcrumb(true);
					} else {
						this.buildBreadcrumb(false);
					}

					this.isLoading = false;
				});
		}
	}

	setSelected(dashboard) {
		this.selected = dashboard;
		this.buildBreadcrumb(false);
	}
}
