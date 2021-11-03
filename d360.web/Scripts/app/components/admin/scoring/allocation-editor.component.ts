import { Input, Component, EventEmitter, Output, OnInit, OnChanges, SimpleChanges, ElementRef, ViewChild, AfterViewChecked, ChangeDetectorRef } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { ScoreTypeAllocation, ScoreType } from '../../../models/metrics.model';
import { AssetTypeClass } from '../../../models/asset.model';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AllocationService } from '../../../services/allocations.service';
import { CurrentEnvironmentSettings } from '../../../static/environment-settings';
import * as _ from 'lodash';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'allocation-editor',
    templateUrl: 'allocation-editor.component.html',
    providers: [AllocationService],
    styles: [`
        .separator{
            padding:0px 4px;
        }
    `],
})

export class AllocationEditorComponent extends BaseComponent implements OnChanges, OnInit, AfterViewChecked {

    @Input() selection: ScoreTypeAllocation = new ScoreTypeAllocation();
    @Input() disabled: boolean = false;
    @Output() onCancel = new EventEmitter();
    @Output() onSave = new EventEmitter();

    savingInProgress: boolean = false;

    ddlScoreTypes: any[] = [];
    ddlAssetTypes: any[] = [];
    saveLabel: string = "Create";
    closeLabel: string = "Cancel";
    isEdit: boolean = false;
    modelChanged: boolean = false;

    originalSelection: ScoreTypeAllocation = null;

    public scoringHelpPage: string = CurrentEnvironmentSettings.HelpBaseUri + 'Default.htm#d-admin/scoring.htm?Highlight=scoring';

    rangeValues: number[] = [];
    @ViewChild('slider', { static: true }) slider: ElementRef;

    constructor(
        private allocationService: AllocationService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private elementRef: ElementRef,
        private cdRef: ChangeDetectorRef) {
        super(settingsService);
        this.selection = new ScoreTypeAllocation();
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

        if (this.selection.uid) {
            this.closeLabel = "Close";
            this.saveLabel = "Save Changes";
            this.isEdit = true;
            this.originalSelection = _.cloneDeep(this.selection);
        } else {
            this.isEdit = false;
            this.closeLabel = "Cancel";
            this.saveLabel = "Create";
        }
    }
   
    updateRanges() {
        this.rangeValues[0] = this.selection.lowerThreshold;
        this.rangeValues[1] = this.selection.upperThreshold;

        this.rangeValues = JSON.parse(JSON.stringify(this.rangeValues));

        this.hasModelChanged();        
    }

    scoreTypeChange($event) {
        if (this.selection.scoreType) {
            this.populateAssetTypesDDL();

            if (!this.selection.uid) {
                this.selection.isExternallyCalculated = false;
            }

            this.hasModelChanged();
        }
    }

    private populateAssetTypesDDL() {
        if (this.selection.scoreType) {
            this.allocationService.getunallocatedAssetTypes(this.selection.scoreType)
                .subscribe(data => {
                    this.ddlAssetTypes = [];
                    data.forEach((item) => {
                        this.ddlAssetTypes.push({
                            value: item.assetTypeUid,
                            class: this.getClassFriendlyName(item.assetTypeClass),
                            name: item.assetTypePath,
                            label: '<span>' + this.getClassFriendlyName(item.assetTypeClass) + '</span> <span class="fa fa-angle-right separator"></span> <span> ' + item.assetTypePath + '</span>'
                        });
                    })

                    if (this.selection.uid) {
                        this.ddlAssetTypes.push({ value: this.selection.assetTypeUid, class: this.selection.assetClassName, name: this.selection.assetTypePath, label: this.selection.assetClassName + ' > ' + this.selection.assetTypePath });
                    }
                    this.ddlAssetTypes = this.ddlAssetTypes.sort((a, b) => a.label.localeCompare(b.label));

                });
        }
    }



    private initialData() {
        this.ddlScoreTypes.push({ value: 'Governance', label: 'Governance Score' });
        this.ddlScoreTypes.push({ value: 'DataQuality', label: 'Data Quality Score' });
    }

    hasModelChanged() {  
        if (this.originalSelection) {
            this.modelChanged = (JSON.stringify(this.originalSelection, (k, v) => v === undefined || v === null ? "" : v) !== JSON.stringify(this.selection, (k, v) => v === undefined || v === null ? "" : v));
            if (this.isEdit) {
                if (this.modelChanged) {
                    this.closeLabel = "Discard Changes"
                } else {
                    this.closeLabel = "Close"
                }
            }
            this.cdRef.detectChanges();
        }
       
        return this.modelChanged;
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
        //Set selection back to original
        if (this.isEdit) {
            this.selection = _.cloneDeep(this.originalSelection);
            this.updateRanges();
        }
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
            .subscribe((res) => {
                let openItem = false;
                this.savingInProgress = false;
                if (!res || (res.type && res.type == "error"))
                    return;

                let msg: string = '';
                if (this.selection.uid == undefined) {
                    msg = `Your score has been added`;
                    openItem = true;
                }
                else {
                    msg = `Your score has been updated`;
                }               
                this.messagesService.showInfoMessage('Success', msg);
                this.onSave.emit({ item: res,  openItem: openItem});
            });
    }

    handleChange(e) {
        this.selection.lowerThreshold = e.values[0];
        if (this.selection.lowerThreshold == 100)
            this.selection.lowerThreshold = 99;
        this.selection.upperThreshold = e.values[1];

        this.hasModelChanged();
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
        var sliderElement = this.slider["el"].nativeElement.getElementsByClassName('p-slider-horizontal')[0];
        sliderElement.style.background = backgroundStyle;

        var sliders = this.slider["el"].nativeElement.getElementsByClassName('p-slider-handle');

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
