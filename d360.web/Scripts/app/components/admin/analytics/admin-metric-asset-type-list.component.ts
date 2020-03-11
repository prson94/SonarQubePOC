import { Component, OnInit, ViewChild } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ScoreTypeAllocation, ScoreType, ScoreTypeAllocationFormatted } from '../../../models/metrics.model';
import { AssetTypeClass } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';
import { Table } from 'primeng/table';
import { Router } from '@angular/router';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
    selector: 'd3s-admin-metric-asset-type-list',
    templateUrl: 'admin-metric-asset-type-list.component.html',
    providers: [AllocationService]
})

export class AdminMetricAssetTypeListComponent extends BaseComponent implements OnInit {

    private _loadedAllocations: ScoreTypeAllocation[];
    private selection: ScoreTypeAllocation = new ScoreTypeAllocation();

    //This one will hold data but with formatted enums to friendly string (for readability and search)
    private allocations: ScoreTypeAllocationFormatted[] = [];

    private showDelete = false;
    public theDeleteCallback: Function;

    private showEdit = false;
    private editTitle = 'Add Score';

    @ViewChild('dt', { static: false }) dt: Table;

    constructor(private allocationService: AllocationService,
        protected messagesService: MessagesObservableService,
        private router: Router
    ) {
        super();
        this.selection = new ScoreTypeAllocation();
        this.selection.scoreType = ScoreType.Governance;
    }

    ngOnInit() {
        this.load();
        this.theDeleteCallback = this.deleteAllocation.bind(this);
    }

    load(selectedItem: any = null) {

        if (selectedItem)
            this.selection = selectedItem;

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
        let formatted: ScoreTypeAllocationFormatted = new ScoreTypeAllocationFormatted();
        formatted.assetClassName = this.getClassFriendlyName(x.assetClassName);
        formatted.assetTypePath = x.assetTypePath;
        formatted.assetTypeUid = x.assetTypeUid;
        formatted.scoreType = this.getScoreTypeFriendlyName(x.scoreType);
        formatted.state = x.state;
        formatted.uid = x.uid;
        formatted.hasMeasure = x.hasMeasure;
        formatted.isExternallyCalculated = x.isExternallyCalculated ? 'External' : 'Internal';
        formatted.lowerThreshold = x.lowerThreshold;
        formatted.upperThreshold = x.upperThreshold;
        formatted.formattedThreshold = x.lowerThreshold + "," + x.upperThreshold;
        return formatted;
    }

    onRowSelect(e: any) {

    }

    getClassFriendlyName(atc: AssetTypeClass): string {
        switch (atc.toString()) {
            case 'BusinessAsset':
                return 'Business Asset';
            case 'TechnicalAsset':
                return 'Technical Asset';
            default:
                return atc.toString();
        }
    }

    getScoreTypeFriendlyName(sct: ScoreType): string {
        switch (sct.toString()) {
            case 'DataQuality':
                return 'Data Quality';
            default:
                return sct.toString();
        }
    }

    private export() {
        this.allocationService.export(this.dt.filters);
    }

    private add() {
        this.editTitle = 'Add Score';
        this.selection = new ScoreTypeAllocation();
        this.selection.scoreType = ScoreType.Governance;
        this.selection.isExternallyCalculated = false;
        this.selection.lowerThreshold = 50;
        this.selection.upperThreshold = 90;
        this.showEdit = true;
    }

    private doDelete(item: ScoreTypeAllocationFormatted) {
        this.showDelete = true;
        this.selection = this.getAllocationByUid(item.uid);
    }
    private doEdit(item: ScoreTypeAllocationFormatted) {
        this.editTitle = 'Edit Score';
        this.showEdit = true;
        this.selection = this.getAllocationByUid(item.uid);
    }

    private getAllocationByUid(uid: string): ScoreTypeAllocation {
        return this._loadedAllocations.find(x => x.uid == uid);
    }

    private deleteAllocation($event) {
        this.allocationService.deleteAllocationByUid($event.uid).
            subscribe(result => {
                result.message = "Score successfully deleted";
                this.showMessageForResult(this.messagesService, result);
                this.load();
                this.showDelete = false;

            }, err => { this.showMessageForResult(this.messagesService, err); this.showDelete = false; });
    }

    private onSaveCancel() {
        this.showEdit = false;
    }

    private getClass(item): string {
        var listItem = this.getAllocationByUid(item.uid);
        if (this.selection.assetTypeUid == listItem.assetTypeUid && this.selection.scoreType == listItem.scoreType) {
            return 'ui-state-highlight';
        }
        return '';
    }

    private openMeasures(event: ScoreTypeAllocationFormatted) {
        var alloc = this.getAllocationByUid(event.uid);
        var url = `${SiteUrlHelpers.SITE_URL_ADMIN_ROOT}/${SiteUrlHelpers.SITE_URL_ADMIN_SCORING}/${alloc.assetTypeUid}/${alloc.scoreType}`;
        this.router.navigateByUrl(url);
    }
};