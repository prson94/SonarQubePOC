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

export class AdminAllocationEditorComponent implements OnChanges, OnInit {

    @Input() selection: ScoreTypeAllocation = new ScoreTypeAllocation();
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    private currentAllocationUid: string;

    private savingInProgress: boolean = false;

    private ddlScoreTypes: any[] = [];
    private ddlAssetTypes: any[] = [];

    constructor(private allocationService: AllocationService, protected messagesService: MessagesObservableService) {
        this.selection = new ScoreTypeAllocation();
        this.selection.scoreType = ScoreType.Governance;
    }

    ngOnInit() {
        this.initialData();
        this.populateAssetTypesDDL();
    }

    ngOnChanges(change: SimpleChanges) {
        if (change.currentAllocationUid && change.currentAllocationUid.currentValue != change.currentAllocationUid.previousValue) {

            this.populateAssetTypesDDL();
        }
        this.currentAllocationUid = this.selection.uid;
    }

    scoreTypeChange($event) {
        this.populateAssetTypesDDL();
    }

    private populateAssetTypesDDL() {
        this.allocationService.getunallocatedAssetTypes(this.selection.scoreType)
            .subscribe(data => {
                data.forEach(item => {
                    this.ddlAssetTypes.push({ value: item.assetTypeUid, label: this.getClassFriendlyName(item.assetTypeClass) + ' > ' + item.assetTypePath });
                })
                this.ddlAssetTypes = this.ddlAssetTypes.sort(x => x.text);
            });
    }



    private initialData() {
        this.ddlScoreTypes.push({ value: ScoreType.Governance, label: "Governance" });

        //Uncomment in 2020-sprint-3
        //this.ddlScoreTypes.push({ value: ScoreType.DataQuality, label: "Data Quality" });
        //this.ddlScoreTypes.push({ value: ScoreType.Perceptional, label: "Perceptional" });
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

    private cancel() {
        console.log("cancel");
        this.onCancel.emit();
    }

    private save() {
        console.log("save");
        console.log(this.selection);
    }

};