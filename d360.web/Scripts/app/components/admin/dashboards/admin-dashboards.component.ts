import { Component, OnDestroy, OnInit } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Report } from '../../../models/report.model';
import { Title } from '@angular/platform-browser';
import { StateService } from '../../../services/state.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';
import { DashboardService } from '../../../services/dashboard.service';
import { DashboardModel } from '../../../models/dashboard.model';

@Component({
	selector: 'd3s-admin-dashboards-component',
	providers: [DashboardService],
	templateUrl: 'admin-dashboards.component.html'
})

export class AdminDashboardsComponent extends AdminBaseComponent implements OnDestroy, OnInit {
	showEditor: boolean = false;
	showDelete: boolean = false;
	showCredentials: boolean = false;
	dashboards: DashboardModel[] = [];
	selected: DashboardModel;
	theDeleteCallback: Function;
	powerBiUser: string;
	powerBiPassword: string;

	searchText = $localize`Search...`;
	labelSave = $localize`Save`;
	labelClose = $localize`Close`;

	constructor(
		private stateService: StateService,
		secondaryNavService: SecondaryNavService,
		protected dashboardService: DashboardService,
		protected messagesService: MessagesObservableService,
		headerBreadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService,
		titleService: Title) {
		super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
		this.areaName = StringConstants.Section_Dashboards;
		this.theDeleteCallback = this.deleteReport.bind(this);
	}

	selectedItemChange() {
		//if (this.selected)
		//    this.buildSecondaryNavigationForObject(this.selected.ID, 'Report');
	}

	ngOnInit() {
		this.loadDashboards();
	}

	ngOnDestroy() {
		this.clearSidebar();
	}

	private loadDashboards() {
		this.isLoading = true;
		this.dashboardService.getDashboardsV2().subscribe(result => {
			this.isLoading = false;
			this.dashboards = result;
			this.dashboards.forEach((dashboard) => {
				dashboard.TypeDisplayValue = dashboard.DashboardType === 'DqPlus' ? 'Data360 DQ+' : dashboard.DashboardType;
			});
			this.selected = (this.dashboards.length > 0 ? this.dashboards[0] : null);
			this.selectedItemChange();
		});
	}

	findReportIndex(uid: string) {
		var index: number = -1;
		for (var dashboard of this.dashboards) {
			index++;
			if (dashboard.uid == uid) return index;
		}
	}

	deleteReport(uid: string) {
		this.dashboardService.deleteDashboard(uid)
			.subscribe(result => {
				this.messagesService.showInfoMessage($localize`Success`, $localize`Dashboard successfully deleted`);
				this.showDelete = false;
				this.selected = null;
				this.loadDashboards();
				this.stateService.reloadLeftNavMenu();
			});
	}

	saveReport(event) {
		this.isLoading = true;
		this.dashboardService.saveDashboard(event.report, event.file)
			.subscribe(result => {
				if (result) {
					this.messagesService.showInfoMessage($localize`Success`, event.report.uid ? $localize`Dashboard successfully updated` : $localize`Dashboard successfully added`);

					this.loadDashboards();
					this.selectedItemChange();
					this.selected = null;
					this.showEditor = false;

					this.stateService.reloadLeftNavMenu();
				}
				this.isLoading = false;
			});
	}

	closeEditor() {
		this.showEditor = false;
		if (this.selected == null) {
			this.selected = this.dashboards.length > 0 ? this.dashboards[0] : null;
			this.selectedItemChange();
		}
	}

	add() {
		this.showEditor = true;
		this.selected = null;
		this.selectedItemChange();
	}

	private onSubmitPowerCreds() {
		this.isLoading = true;
		this.dashboardService.setPowerBICredentials(this.powerBiUser, this.powerBiPassword)
			.subscribe(result => {
				this.isLoading = false;
				this.showMessageForResult(this.messagesService, result);
				if (result.type != 'error') {
					this.showCredentials = false;
				}
			});
	}

	get deleteDashboardModalTitle(): string {
		return $localize`Are you sure you want to delete the dashboard [${this.selected?.Name}]?`;
	}
}