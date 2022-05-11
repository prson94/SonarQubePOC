import { Component, OnDestroy, OnInit } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { ReportsService } from '../../../services/reports.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Report } from '../../../models/report.model';
import { Title } from '@angular/platform-browser';
import { StateService } from '../../../services/state.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-dashboards-component',
    providers: [ReportsService],
    templateUrl: 'admin-dashboards.component.html'
})

export class AdminDashboardsComponent extends AdminBaseComponent implements OnDestroy, OnInit {
    showEditor: boolean = false;
    showDelete: boolean = false;
    showCredentials: boolean = false;
    reports: Report[] = [];
    selected: Report;
    theDeleteCallback: Function;
    powerBiUser: string;
    powerBiPassword: string;

    searchText = $localize`Search...`;
    labelSave = $localize`Save`;
    labelClose = $localize`Close`;

    constructor(
        private stateService: StateService,
        secondaryNavService: SecondaryNavService,
        protected reportsService: ReportsService,
        protected messagesService: MessagesObservableService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        titleService: Title) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Dashboards;
        this.theDeleteCallback = this.deleteReport.bind(this);
    }

    selectedItemChange() {
        if (this.selected)
            this.buildSecondaryNavigationForObject(this.selected.ID, 'Report');
    }

    ngOnInit() {
        this.loadReports();
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    private loadReports() {
        this.isLoading = true;
        this.reportsService.getReports().subscribe(result => {
            this.isLoading = false;
            for (var report of result) {
                if (report.ReportType == 'sagacity') report.DisplayType = 'Data360 DQ+';
                else report.DisplayType = report.ReportType;
            }
            this.reports = result;
            this.selected = (this.reports.length > 0 ? this.reports[0] : null);
            this.selectedItemChange();
        });
    }

    findReportIndex(id: number) {
        var index: number = -1;
        for (var report of this.reports) {
            index++;
            if (report.ID == id) return index;
        }
    }

    deleteReport(id: number) {
        this.reportsService.deleteReport(id)
            .subscribe(result => {
                this.showDelete = false;
                this.showMessageForResult(this.messagesService, result);
                if (result.type != 'error') {
                    this.selected = this.reports.length > 0 ? this.reports[0] : null;
                    this.reports.splice(this.findReportIndex(id), 1);
                    this.selectedItemChange();
                }

                this.stateService.reloadLeftNavMenu();
            });
    }

    saveReport(event) {
        this.isLoading = true;
        this.reportsService.saveReport(event.report, event.file)
            .subscribe(result => {
                this.showMessageForResult(this.messagesService, result);
                let parts = event.report.ObjectType.split('|');
                if (parts.length > 0) {
                    event.report.ObjectType = parts[0];
                    event.report.ObjectID = Number(parts[1]);
                }
                if (event.report.ID == undefined) {
                    event.report.ID = Number(result.id);
                    this.reports[this.reports.length] = event.report;
                }
                else {
                    this.reports[this.findReportIndex(event.report.ID)] = event.report;
                }

                if (event.report.ReportType == 'sagacity') event.report.DisplayType = 'Data360 DQ+';
                else event.report.DisplayType = event.report.ReportType;

                if (result.type == "error") {
                    this.showEditor = true;
                } else {
                    this.showEditor = false;
                }
                this.isLoading = false;
                this.selected = event.report;
                this.selectedItemChange();

                this.stateService.reloadLeftNavMenu();
            });
    }

    closeEditor() {
        this.showEditor = false;
        if (this.selected == null) {
            this.selected = this.reports.length > 0 ? this.reports[0] : null;
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
        this.reportsService.setPowerBICredentials(this.powerBiUser, this.powerBiPassword)
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