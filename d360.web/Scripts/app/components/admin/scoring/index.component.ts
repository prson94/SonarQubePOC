import { Input, Component, OnInit, OnDestroy, Output, ViewChild } from '@angular/core';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AdminBaseComponent } from '../admin-base.component';
import { Title } from '@angular/platform-browser';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';
import { Table } from 'primeng/table';
import { ScoreType, ScoreTypeAllocation, ScoreTypeAllocationFormatted } from '../../../models/metrics.model';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';
import { AssetTypeClass } from '../../../models/asset.model';
import { StringConstants } from '../../../static/string-constants';
import { CompanySettingsService } from '../../../services/settings.service';
import '@angular/localize/init';

@Component({
    selector: 'scoring-index-component',
    templateUrl: './index.component.html',
    providers: [AllocationService]
})

export class ScoringIndexComponent extends AdminBaseComponent implements OnInit, OnDestroy {

    private _loadedAllocations: ScoreTypeAllocation[];
    selection: ScoreTypeAllocation = new ScoreTypeAllocation();

    //This one will hold data but with formatted enums to friendly string (for readability and search)
    private allocations: ScoreTypeAllocationFormatted[] = [];

    showDelete = false;
    public theDeleteCallback: Function;

    showEdit = false;
    editTitle = $localize`Create Scoring Definition`;
    submitLabel = $localize`Create`;
    searchText = $localize`Search...`;

    @ViewChild('dt', { static: false }) dt: Table;

    constructor(
        private allocationService: AllocationService,
        secondaryNavService: SecondaryNavService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        titleService: Title,
        private router: Router) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.areaName = StringConstants.Section_Scoring;
        this.tabTitle = StringConstants.Section_Scoring;
        this.setCommonItems();
        this.selection = new ScoreTypeAllocation();
        this.selection.scoreType = ScoreType.Governance;
        this.buildSecondaryNavigationForObject(0, 'MetricAllocation');
    }

    ngOnInit() {
        this.load();
        this.theDeleteCallback = this.deleteAllocation.bind(this);
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load(event: any = null) {
        if (event && event.openItem) {
            this.openMeasures(null, event.item);
            return;
        }

        if (event) {
            this.selection = event.item ? event.item : event;
        }

        this.isLoading = true;
        this.allocationService.getAllocations()
            .subscribe(r => {
                this.allocations = [];
                this._loadedAllocations = r;
                r.forEach(x => {
                    this.allocations.push(this.getFormattedItem(x));
                })
                this.isLoading = false;
            });
    }

    getFormattedItem(x: ScoreTypeAllocation): ScoreTypeAllocationFormatted {
        const formatted = new ScoreTypeAllocationFormatted();
        formatted.assetClassName = this.getClassFriendlyName(x.assetClassName);
        formatted.assetTypePath = x.assetTypePath;
        formatted.assetTypeUid = x.assetTypeUid;
        formatted.scoreType = this.getScoreTypeFriendlyName(x.scoreType);
        formatted.state = x.state;
        formatted.uid = x.uid;
        formatted.hasMeasure = x.hasMeasure;
        formatted.hasDisabledMeasure = x.hasDisabledMeasure;
        formatted.hasField = x.hasField;
        formatted.isExternallyCalculated = x.isExternallyCalculated ? 'External' : 'Internal';
        formatted.lowerThreshold = x.lowerThreshold;
        formatted.upperThreshold = x.upperThreshold;
        formatted.formattedThreshold = +(x.lowerThreshold + "" + x.upperThreshold);
        return formatted;
    }

    onRowSelect(e: any) {

    }

    getClassFriendlyName(atc: AssetTypeClass): string {
        switch (atc.toString()) {
            case 'BusinessAsset':
                return StringConstants.AssetTypeClass_Business;
            case 'TechnicalAsset':
                return StringConstants.AssetTypeClass_Technical;
            default:
                return atc.toString();
        }
    }

    getScoreTypeFriendlyName(sct: ScoreType): string {
        switch (sct.toString()) {
            case 'DataQuality':
                return $localize`Data Quality`;
            default:
                return sct.toString();
        }
    }

    export() {
        this.allocationService.export(this.dt.filters);
    }

    add() {
        this.editTitle = $localize`Create Scoring Definition`;
        this.selection = new ScoreTypeAllocation();
        this.selection.scoreType = null;
        this.selection.isExternallyCalculated = false;
        this.selection.lowerThreshold = 50;
        this.selection.upperThreshold = 90;
        this.showEdit = true;
    }

    doDelete(item: ScoreTypeAllocationFormatted) {
        this.showDelete = true;
        this.selection = this.getAllocationByUid(item.uid);
    }

    doEdit(item: ScoreTypeAllocationFormatted) {
        this.editTitle = $localize`Edit Scoring Definition`;
        this.showEdit = true;
        this.selection = this.getAllocationByUid(item.uid);
    }

    private getAllocationByUid(uid: string): ScoreTypeAllocation {
        return this._loadedAllocations.find(x => x.uid === uid);
    }

    private deleteAllocation($event) {
        this.allocationService.deleteAllocationByUid($event.uid).
            subscribe(result => {
                result.message = $localize`Score successfully deleted`;
                this.showMessageForResult(this.messagesService, result);
                this.load();
                this.showDelete = false;

            }, err => { this.showMessageForResult(this.messagesService, err); this.showDelete = false; });
    }

    onSaveCancel() {
        this.showEdit = false;
    }

    private getClass(item): string {
        const listItem = this.getAllocationByUid(item.uid);
        if (this.selection.assetTypeUid === listItem.assetTypeUid && this.selection.scoreType === listItem.scoreType) {
            return 'p-highlight';
        }
        return '';
    }

    private openMeasures(event: ScoreTypeAllocationFormatted, allocation: ScoreTypeAllocation = null) {
        let alloc = allocation;
        if (!alloc)
            alloc = this.getAllocationByUid(event.uid)
        const url = `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SCORING}/${alloc.assetTypeUid}/${alloc.uid}`;
        this.router.navigateByUrl(url);
    }

    get deletePopupTitle(): string {
        return (this.selection.hasField || this.selection.hasMeasure) ? $localize`Cannot Delete Scoring Definition` : $localize`Delete Scoring Definition`;
    }
}