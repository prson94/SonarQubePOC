import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { Item, Allocation, ScoreType } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { AssetTypeMetricModel, AssetTypeClass } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';

@Component({
    selector: 'd3s-admin-metric-asset-type-list',
    templateUrl: 'admin-metric-asset-type-list.component.html',
    providers: [AllocationService]
})

export class AdminMetricAssetTypeListComponent extends BaseComponent implements OnInit {

    private allocations: Allocation[] = [];
    private selection: Allocation;
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
                this.allocations = r;
                console.log(this.allocations);
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

};