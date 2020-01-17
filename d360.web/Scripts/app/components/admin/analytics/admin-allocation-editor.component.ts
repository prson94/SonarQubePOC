import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ViewChild, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { Item, ScoreTypeAllocation, ScoreType, ScoreTypeAllocationFormatted } from '../../../models/metrics.model';
import { FormMode } from '../../../models/form.model';
import { AssetTypeMetricModel, AssetTypeClass } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';
import { Table } from 'primeng/table';

@Component({
    selector: 'd3s-admin-allocation-editor',
    templateUrl: 'admin-allocation-editor.component.html',
    providers: [AllocationService]
})

export class AdminAllocationEditorComponent implements OnChanges {

    @Input() selection: ScoreTypeAllocationFormatted;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    private savingInProgress: boolean = false;

    constructor(private allocationService: AllocationService, protected messagesService: MessagesObservableService) {

    }

    ngOnChanges(change: SimpleChanges) {
        if (change.selection && change.selection.currentValue != change.selection.previousValue) {
            this.prepareData();
        }
    }

    private prepareData() {
        console.log("preparing data");
    }

    private cancel() {
        console.log("cancel");
        this.onCancel.emit();
    }

    private save() {
        console.log("save");
        this.onSave.emit();
    }

};