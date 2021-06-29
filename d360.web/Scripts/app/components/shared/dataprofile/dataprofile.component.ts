import { Input, Component, OnChanges, SimpleChange, ChangeDetectorRef, Output, EventEmitter, OnInit, ViewEncapsulation } from '@angular/core';
import { DetailRow, DetailField, DetailFieldType, ComplexLookupType, NymType, Category } from '../../../models/object-detail.model';
import { ObjectDetailService } from '../../../services/object-detail.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetService } from '../../../services/asset.service';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'data-profile',
    templateUrl: './dataprofile.component.html',
    styleUrls: ['dataprofile.less'],
    providers: [ObjectDetailService, AssetService]
})

export class DataProfileComponent extends BaseComponent implements OnInit {
    @Input() dataProfile: any;

    private sampleCountTooltip: number;
    private sampleCountPercentage: number;
    private nullBlankCountTotal: number;

    private showDataProfile: boolean = true;

    private validPercentage: number;

    private outliersToShow: number = 5;
    private shapesToShow: number = 5;
    private topSamplesToShow: number = 5;
    private bottomSamplesToShow: number = 5;
    private topSamples: any;
    private bottomSamples: any;

    private sampleBarChart: any;

    private ShowBoolean: boolean = false;
    private ShowNumber: boolean = false;
    private ShowString: boolean = false;
    private ShowDate: boolean = false;
    private ShowDateTime: boolean = false;
    private ShowOther: boolean = false;
    private showSampleSummary: boolean = false;
    private showSampleQuality: boolean = false;
    private showStatistics: boolean;
    private nullBlankTooltipText: string;

    constructor(
        private objectDetailService: ObjectDetailService,
        private assetService: AssetService,
        private messagesService: MessagesObservableService,
        protected cdRef: ChangeDetectorRef,
    ) {
        super();
    }

    ngOnInit() { 
        this.cdRef.markForCheck();
        this.validPercentage = ((this.dataProfile.matchCount / this.dataProfile.totalCount) * 100);

        if (this.dataProfile.sampleCount && this.dataProfile.totalCount) {
            this.sampleCountPercentage = (this.dataProfile.sampleCount / this.dataProfile.totalCount) * 100;
        }
        this.nullBlankCountTotal = ((this.dataProfile.nullCount ?? 0) + (this.dataProfile.blankCount ?? 0));
        this.sortSamples();

        this.sampleBarChart = this.getSampleBarChart();

        this.nullBlankTooltipText = this.getNullBlankToolTip();

        this.checkVisibility();
    }

    private showSidePanel() {
        this.showDataProfile = !this.showDataProfile;
    }

    private sortSamples() {
        if (this.dataProfile.outlierDetail) {
            this.dataProfile.outlierDetail = this.dataProfile.outlierDetail.sort((a, b) => (b.count - a.count))
        }

        if (this.dataProfile.sampleDetail) {
            this.dataProfile.sampleDetail = this.dataProfile.sampleDetail.sort((a, b) => (b.count - a.count))
        }

        if (this.dataProfile.cardinalityDetail) {
            this.dataProfile.cardinalityDetail = this.dataProfile.cardinalityDetail.sort((a, b) => (b.count - a.count))
        }
        if (this.dataProfile.cardinalityDetail) {
            if (this.dataProfile.topK) {
                this.topSamples = this.dataProfile.cardinalityDetail.filter(i => this.dataProfile.topK.some(x => x === i.key));
            }
            if (this.dataProfile.topK) {
                this.bottomSamples = this.dataProfile.cardinalityDetail.filter(i => this.dataProfile.bottomK.some(x => x === i.key));
            }
        }
        
    }

    private getBackgroundSize(size: number, total: number) {
        return "" + (size / total) * 100 +"% 100%"
    }

    private getNullBlankToolTip() {
        var retval: string = "";
        if (this.dataProfile.nullCount) {
            retval = `Null: ${this.dataProfile.nullCount} <span style="color: gray;">${((this.dataProfile.nullCount / this.dataProfile.sampleCount) * 100).toPrecision(2)}%</span>`;
        }

        if (this.dataProfile.blankCount) {
            if (retval.length > 0) {
                retval = retval + "<br/>"
            }
            retval = retval + `Blank: ${this.dataProfile.blankCount} <span style="color: gray;">${((this.dataProfile.blankCount / this.dataProfile.sampleCount) * 100).toPrecision(2) }%</span>`;;
        }

        return retval;
    }

    private getSampleBarChart() {

        var validBar: string = "#7690a9 0%, "
        var outlierBar: string = ""

        var matchPercentage = (this.dataProfile.matchCount / this.dataProfile.sampleCount) * 100;
        var outlierPercentage = (this.dataProfile.outlierCount / this.dataProfile.sampleCount) * 100;

        if (this.dataProfile.matchCount) {
            validBar = `#2e9b61 0%, #2e9b61 ${matchPercentage}%, `;
        }

        if (this.dataProfile.outlierCount) {
            if (this.dataProfile.matchCount) {
                validBar += `#d83961 ${matchPercentage}%,`
            }
            outlierBar = `#d83961 ${matchPercentage + outlierPercentage}%, #7690a9 ${matchPercentage + outlierPercentage}%,`;
        } else {
            if (this.dataProfile.matchCount) {
                outlierBar += `#7690a9 ${matchPercentage}%,`
            }            
        }
        
        return { "background-image": `linear-gradient(to right, ${validBar} ${outlierBar} #7690a9 100%)` };      
        //return { "background-image": `linear-gradient(to right, #2e9b61 0%,#2e9b61 50%,#d83961 50%,#d83961 88%,#7690a9 88%,#7690a9 100%)` };      
    }

    private checkVisibility() {   
        if(this.dataProfile.totalCount || this.dataProfile.sampleCount || this.dataProfile.type || this.dataProfile.typeQualifier || this.dataProfile.confidence)
        {
            this.showSampleSummary = true;
        }

        if (this.dataProfile.sampleCount && (this.dataProfile.cardinality || this.dataProfile.matchCount || this.dataProfile.outlierCount || this.dataProfile.nullCount || this.dataProfile.blankCount)) {
            this.showSampleQuality = true;
        }

        if (this.dataProfile.regExp || this.dataProfile.blankCount || this.dataProfile.nullCount ||
            this.dataProfile.min || this.dataProfile.max || this.dataProfile.mean || this.dataProfile.standardDeviation ||
            this.dataProfile.leadingZeroCount || this.dataProfile.minLength || this.dataProfile.maxLength ||
            this.dataProfile.multiline || this.dataProfile.leadingWhiteSpace || this.dataProfile.trailingWhiteSpace) {
            this.showStatistics = true;
        }

        if (this.dataProfile.type) {
            switch (this.dataProfile.type.toLowerCase()) {
                case "boolean": 
                    this.ShowBoolean = true;
                    if (!(this.dataProfile.regExp || this.dataProfile.blankCount || this.dataProfile.nullCount)) {
                        this.showStatistics = false;
                    }
                    break;                
                case "number":
                case "long":
                case "double":
                    this.ShowNumber = true;
                    if (!(this.dataProfile.regExp || this.dataProfile.blankCount || this.dataProfile.nullCount
                        || this.dataProfile.min || this.dataProfile.max || this.dataProfile.mean
                        || this.dataProfile.leadingZeroCount || this.dataProfile.standardDeviation)) {
                        this.showStatistics = false;
                    }
                    break;
                case "string": 
                    this.ShowString = true;
                    if (!(this.dataProfile.regExp || this.dataProfile.blankCount || this.dataProfile.nullCount
                        || this.dataProfile.min || this.dataProfile.max || this.dataProfile.multiline
                        || this.dataProfile.minLength || this.dataProfile.maxLength || this.dataProfile.leadingWhiteSpace
                        || this.dataProfile.trailingWhiteSpace)) {
                        this.showStatistics = false;
                    }
                    break;       
                case "date":
                case "localDate":
                    this.ShowDate = true;
                    if (!(this.dataProfile.regExp || this.dataProfile.blankCount || this.dataProfile.nullCount
                        || this.dataProfile.min || this.dataProfile.max)) {
                        this.showStatistics = false;
                    }
                    break;
                case "localTime":
                case "localDateTime":
                case "offsetDateTime":
                case "zonedDateTime":
                    this.ShowDateTime = true;                    
                    if (!(this.dataProfile.regExp || this.dataProfile.blankCount || this.dataProfile.nullCount
                        || this.dataProfile.min || this.dataProfile.max)) {
                        this.showStatistics = false;
                    }
                    break;   
                default: 
                    this.ShowOther = true;
                    break;                
            }             
        } else {
            this.ShowOther = true;
        }         
    }
}