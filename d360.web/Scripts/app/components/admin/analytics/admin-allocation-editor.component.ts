import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ScoreTypeAllocation, ScoreType } from '../../../models/metrics.model';
import { AssetTypeClass } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';
import { CurrentEnvironmentSettings } from '../../../static/environment-settings';

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

    savingInProgress: boolean = false;

    ddlScoreTypes: any[] = [];
    ddlAssetTypes: any[] = [];

    public scoringHelpPage: string = CurrentEnvironmentSettings.HelpBaseUri + 'Default.htm#d-admin/scoring.htm?Highlight=scoring';

    rangeValues: number[] = [];
    @ViewChild('slider', { static: true }) slider: ElementRef;

    constructor(private allocationService: AllocationService, protected messagesService: MessagesObservableService, private elementRef: ElementRef) {
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

    cancel() {
        this.onCancel.emit();
    }

    save() {
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
        if (this.selection.lowerThreshold == 100)
            this.selection.lowerThreshold = 99;
        this.selection.upperThreshold = e.values[1];
    }

    private thresholdCheckLower: any;

    onLowerThresholdChange($event, el: HTMLInputElement) {

        if (this.thresholdCheckLower)
            window.clearTimeout(this.thresholdCheckLower);

        this.thresholdCheckLower = window.setTimeout(() => {
            var tempVal = +el.value;

            if (tempVal <= 0) {
                el.value = "0";
            }

            if (tempVal > 99) {
                el.value = "99";
            }
            if (tempVal > this.selection.upperThreshold) {
                el.value = this.selection.upperThreshold.toString();
            }

            this.selection.lowerThreshold = +el.value;
            this.updateRanges();
        }, 500)

    }
    private thresholdCheckUpper: any;

    onUpperThresholdChange($event, el: HTMLInputElement) {


        if (this.thresholdCheckUpper)
            window.clearTimeout(this.thresholdCheckUpper);

        this.thresholdCheckUpper = window.setTimeout(() => {
            var tempVal = +el.value;

            if (tempVal < 0) {
                el.value = this.selection.lowerThreshold.toString();
            }

            if (tempVal > 99) {
                el.value = "99";
            }

            if (tempVal < this.selection.lowerThreshold) {
                el.value = this.selection.lowerThreshold.toString();
            }

            this.selection.upperThreshold = +el.value;
            this.updateRanges();
        }, 500);

    }

    ngAfterViewChecked() {

        //Dynamically load good, average and score css styles from computed style object so branding is possible
        var poorEl = this.elementRef.nativeElement.getElementsByClassName("score-poor")[0];
        var avgEl = this.elementRef.nativeElement.getElementsByClassName("score-average")[0];
        var goodEl = this.elementRef.nativeElement.getElementsByClassName("score-good")[0];

        var poorColor = window.getComputedStyle(poorEl).backgroundColor;
        var averageColor = window.getComputedStyle(avgEl).backgroundColor;
        var goodColor = window.getComputedStyle(goodEl).backgroundColor;

        var backgroundStyle = `linear-gradient(90deg, ${poorColor} ${this.selection.lowerThreshold}%, ${poorColor} ${this.selection.lowerThreshold}%,${averageColor} ${this.selection.lowerThreshold}%, ${averageColor} ${this.selection.upperThreshold}%, ${goodColor} ${this.selection.upperThreshold}%, ${goodColor} 100%)`;
        var sliderElement = this.slider["el"].nativeElement.getElementsByClassName('ui-slider-horizontal')[0];
        sliderElement.style.background = backgroundStyle;

        var sliders = this.slider["el"].nativeElement.getElementsByClassName('ui-slider-handle');

        this.rangeValues.forEach((value: number, index) => {
            var tooltip = sliders[index].getElementsByClassName('slider-tooltip');
            if (tooltip.length == 0) {
                var el = document.createElement("span");
                el.className = 'slider-tooltip';
                el.innerHTML = value + '%';
                sliders[index].appendChild(el);
            }
            else {
                tooltip[0].innerHTML = this.rangeValues[index] + '%';
            }
        });

    }

    reverseElipsis(str: string, length: number) {

        var startIndex = str.length - length;
        if (startIndex < 0) {
            return str;
        }

        return '...' + str.substring(startIndex);
    }

};