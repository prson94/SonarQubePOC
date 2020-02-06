import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ScoreTypeAllocation, ScoreType } from '../../../models/metrics.model';
import { AssetTypeClass } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';

@Component({
    selector: 'd3s-admin-allocation-editor',
    templateUrl: 'admin-allocation-editor.component.html',
    providers: [AllocationService]
})

export class AdminAllocationEditorComponent extends BaseComponent implements OnChanges, OnInit {

    @Input() selection: ScoreTypeAllocation = new ScoreTypeAllocation();
    @Input() disabled: boolean = false;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    private savingInProgress: boolean = false;

    private ddlScoreTypes: any[] = [];
    private ddlAssetTypes: any[] = [];

    constructor(private allocationService: AllocationService, protected messagesService: MessagesObservableService) {
        super();
        this.selection = new ScoreTypeAllocation();
        this.selection.scoreType = ScoreType.Governance;
    }

    ngOnInit() {
        this.initialData();
    }

    ngOnChanges(change: SimpleChanges) {
        this.populateAssetTypesDDL();
    }

    scoreTypeChange($event) {
        this.populateAssetTypesDDL();

        if (this.selection.scoreType.toString() == 'DataQuality')
            this.selection.isExternallyCalculated = true;

    }

    isExtCalcDisabled(): boolean {
        return this.selection.scoreType.toString() == 'DataQuality';
    }

    private populateAssetTypesDDL() {

        this.allocationService.getunallocatedAssetTypes(this.selection.scoreType)
            .subscribe(data => {
                this.ddlAssetTypes = [];
                data.forEach(item => {
                    this.ddlAssetTypes.push({ value: item.assetTypeUid, class: this.getClassFriendlyName(item.assetTypeClass), name: item.assetTypePath, label: this.getClassFriendlyName(item.assetTypeClass) + ' | ' + item.assetTypePath });
                })

                if (this.selection.uid) {
                    this.ddlAssetTypes.push({ value: this.selection.assetTypeUid, class: this.selection.assetClassName, name: this.selection.assetTypePath, label: this.selection.assetClassName + ' | ' + this.selection.assetTypePath });
                }
                this.ddlAssetTypes = this.ddlAssetTypes.sort((a, b) => a.label.localeCompare(b.label));

                this.ddlAssetTypes = [{ value: null,  label: 'Select Asset Type' }, ...this.ddlAssetTypes];

            });
    }



    private initialData() {
        this.ddlScoreTypes.push({ value: 'Governance', label: 'Governance' });
        this.ddlScoreTypes.push({ value: 'DataQuality', label: 'Data Quality' });
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
        this.onCancel.emit();
    }

    private save() {
        var item = new ScoreTypeAllocation();
        if (this.selection.uid)
            item.uid = this.selection.uid;

        item.assetTypeUid = this.selection.assetTypeUid;
        item.scoreType = this.selection.scoreType;
        item.isExternallyCalculated = this.selection.isExternallyCalculated;
        this.savingInProgress = true;
        this.allocationService.save(item)
            .subscribe(res => {

                this.savingInProgress = false;
                if (!res || (res.type && res.type == "error"))
                    return;

                let msg: string = '';
                if (this.selection.uid == undefined) {
                    msg = `Your score has been added`;
                }
                else {
                    msg = `Your score has been updated`;
                }
                this.selection = new ScoreTypeAllocation();
                this.messagesService.showInfoMessage('Success', msg);
                this.onSave.emit(res);
            });
    }

};