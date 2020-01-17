import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChange, ViewChild, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { MetricsService } from '../../../services/metrics.service';
import { Item, ScoreTypeAllocation, ScoreType, ScoreTypeAllocationFormatted, ScoreTypeLabel } from '../../../models/metrics.model';
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

export class AdminAllocationEditorComponent implements OnChanges, OnInit {

    @Input() selection: ScoreTypeAllocation = new ScoreTypeAllocation();
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    private savingInProgress: boolean = false;

    private ddlScoreTypes: any[] = [];

    constructor(private allocationService: AllocationService, protected messagesService: MessagesObservableService) {

    }

    ngOnChanges(change: SimpleChanges) {
        if (change.selection && change.selection.currentValue != change.selection.previousValue) {
            console.log("test");
        }
    }

    scoreTypeChange($event) {
        console.log($event);
        console.log(this.selection);
    }

    ngOnInit() {
        this.prepareData();
    }

    private prepareData() {
        this.ddlScoreTypes.push({ value: ScoreType.DataQuality, text: ScoreTypeLabel.get(ScoreType.DataQuality) });
        this.ddlScoreTypes.push({ value: ScoreType.Governance, text: ScoreTypeLabel.get(ScoreType.Governance) });
        this.ddlScoreTypes.push({ value: ScoreType.Perceptional, text: ScoreTypeLabel.get(ScoreType.Perceptional)});

        console.log(this.ddlScoreTypes);
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