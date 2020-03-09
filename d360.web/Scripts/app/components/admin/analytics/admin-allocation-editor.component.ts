import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ScoreTypeAllocation, ScoreType } from '../../../models/metrics.model';
import { AssetTypeClass } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';
import { ScoreTypeConstants } from '../../../static/score-type-helpers';

@Component({
    selector: 'd3s-admin-allocation-editor',
    templateUrl: 'admin-allocation-editor.component.html',
    providers: [AllocationService]
})

export class AdminAllocationEditorComponent extends BaseComponent implements OnChanges, OnInit, AfterViewChecked {

    @Input() selection: ScoreTypeAllocation = new ScoreTypeAllocation();
    @Input() disabled: boolean = false;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    private savingInProgress: boolean = false;

    private ddlScoreTypes: any[] = [];
    private ddlAssetTypes: any[] = [];

    private poorColour: string = ScoreTypeConstants.poorColour;
    private averageColour: string = ScoreTypeConstants.averageolour;
    private goodColour: string = ScoreTypeConstants.goodColour;

    rangeValues: number[] = [];
    @ViewChild('slider', { static: true }) slider: ElementRef;

    constructor(private allocationService: AllocationService, protected messagesService: MessagesObservableService) {
        super();
        this.selection = new ScoreTypeAllocation();
        this.selection.scoreType = ScoreType.Governance;
        this.selection.isExternallyCalculated = false;
        this.selection.lowerThreshold = 50;
        this.selection.upperThreshold = 90;

        this.updateRanges();
    }

    ngOnInit() {
        this.initialData();
    }

    ngOnChanges(change: SimpleChanges) {
        this.populateAssetTypesDDL();
        this.updateRanges();
    }

    updateRanges() {
        this.rangeValues[0] = this.selection.lowerThreshold;
        this.rangeValues[1] = this.selection.upperThreshold;

        this.rangeValues = JSON.parse(JSON.stringify(this.rangeValues));
    }

    scoreTypeChange($event) {
        this.populateAssetTypesDDL();

        if (this.selection.scoreType.toString() == 'DataQuality')
            this.selection.isExternallyCalculated = true;

        if (!this.selection.uid && this.selection.scoreType.toString() == 'Governance') {
            this.selection.isExternallyCalculated = false;

        }

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

                this.ddlAssetTypes = [{ value: null, label: 'Select Asset Type' }, ...this.ddlAssetTypes];

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
        item.lowerThreshold = this.selection.lowerThreshold;
        item.upperThreshold = this.selection.upperThreshold;
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

    handleChange(e) {
        this.selection.lowerThreshold = e.values[0];
        this.selection.upperThreshold = e.values[1];
    }

    onLowerThresholdChange($event, el: HTMLInputElement) {
        var tempVal = +el.value;
        
        if (tempVal <= 0) {
            el.value = "0";
        }
        if (tempVal > this.selection.upperThreshold) {
            el.value = this.selection.upperThreshold.toString();
        }

        this.selection.lowerThreshold = +el.value;
        this.updateRanges();
    }

    onUpperThresholdChange($event, el: HTMLInputElement, checkFull: boolean = false) {
        var tempVal = +el.value;

        if (tempVal < 0) {
            el.value = this.selection.lowerThreshold.toString();
        }

        if (tempVal > 100) {
            el.value = "100";
        }
        if (tempVal < 10 && checkFull) {
            return;
        }
        if (tempVal < this.selection.lowerThreshold) {
            el.value = this.selection.lowerThreshold.toString();
        }

        this.selection.upperThreshold = +el.value;
        this.updateRanges();
    }

    ngAfterViewChecked() {
        var backgroundStyle = `linear-gradient(90deg, ${ScoreTypeConstants.poorColour} ${this.selection.lowerThreshold}%, ${ScoreTypeConstants.poorColour} ${this.selection.lowerThreshold}%,${ScoreTypeConstants.averageolour} ${this.selection.lowerThreshold}%, ${ScoreTypeConstants.averageolour} ${this.selection.upperThreshold}%, ${ScoreTypeConstants.goodColour} ${this.selection.upperThreshold}%, ${ScoreTypeConstants.goodColour} 100%)`;
        var sliderElement = this.slider["el"].nativeElement.getElementsByClassName('ui-slider-horizontal')[0];
        sliderElement.style.background = backgroundStyle;

        var sliders = this.slider["el"].nativeElement.getElementsByClassName('ui-slider-handle');

        this.rangeValues.forEach((value: number, index) => {
            var tooltip = sliders[index].getElementsByClassName('slider-tooltip');
            if (tooltip.length == 0) {
                var el = document.createElement("span");
                el.className = 'slider-tooltip';
                el.innerHTML = value + '%';
                sliders[index].append(el);
            }
            else {
                tooltip[0].innerHTML = this.rangeValues[index] + '%';
            }
        });

    }

};