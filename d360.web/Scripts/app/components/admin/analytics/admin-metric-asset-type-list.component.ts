import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ViewChild } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { Item, ScoreTypeAllocation, ScoreType, ScoreTypeAllocationFormatted } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { AssetTypeMetricModel, AssetTypeClass } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';
import { Table } from 'primeng/table';

@Component({
    selector: 'd3s-admin-metric-asset-type-list',
    templateUrl: 'admin-metric-asset-type-list.component.html',
    providers: [AllocationService]
})

export class AdminMetricAssetTypeListComponent extends BaseComponent implements OnInit {

    private allocations: ScoreTypeAllocationFormatted[] = [];
    private selection: ScoreTypeAllocationFormatted;


    @ViewChild('dt', { static: false }) dt: Table;

    constructor(private allocationService: AllocationService, protected messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.allocationService.getAllocations()
            .subscribe(r => {
                this.allocations = [];
                r.forEach(x => {
                    let formatted: ScoreTypeAllocationFormatted = new ScoreTypeAllocationFormatted();
                    formatted.assetClassName = this.getClassFriendlyName(x.assetClassName);
                    formatted.assetTypePath = x.assetTypePath;
                    formatted.assetTypeUid = x.assetTypeUid;
                    formatted.scoreType = this.getScoreTypeFriendlyName(x.scoreType);
                    formatted.state = x.state;
                    formatted.uid = x.uid;
                    this.allocations.push(formatted);
                })
                this.isLoading = false;
            });
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

};