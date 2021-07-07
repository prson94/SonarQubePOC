import { Input, Component, OnInit} from '@angular/core';
import { BaseComponent } from '../base.component';

@Component({
    selector: 'data-profile',
    templateUrl: './dataprofile.component.html',
    styleUrls: ['dataprofile.less']
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
    private baseType: string;
    private hasValidCounts: boolean = true;

    ngOnInit() { 
        
        this.validPercentage = ((this.dataProfile.matchCount / this.dataProfile.totalCount) * 100);

        if (this.dataProfile.sampleCount != null && this.dataProfile.totalCount) {
            this.sampleCountPercentage = (this.dataProfile.sampleCount / this.dataProfile.totalCount) * 100;
        }
        this.nullBlankCountTotal = ((this.dataProfile.nullCount ?? 0) + (this.dataProfile.blankCount ?? 0));
        this.sortSamples();

        this.sampleBarChart = this.getSampleBarChart();

        this.nullBlankTooltipText = this.getNullBlankToolTip();

        this.setBaseTypeText();

        this.checkVisibility();
    }

    private showSidePanel() {
        this.showDataProfile = !this.showDataProfile;
    }

    private sortSamples() {
        if (this.dataProfile.outlierDetail) {
            this.dataProfile.outlierDetail = this.dataProfile.outlierDetail.sort((a, b) => (b.count - a.count));
        }

        if (this.dataProfile.sampleDetail) {
            this.dataProfile.sampleDetail = this.dataProfile.sampleDetail.sort((a, b) => (b.count - a.count));
        }

        if (this.dataProfile.cardinalityDetail) {
            this.dataProfile.cardinalityDetail = this.dataProfile.cardinalityDetail.sort((a, b) => (b.count - a.count));
        }

        if (this.dataProfile.cardinalityDetail) {
            if (this.dataProfile.topK) {
                this.topSamples = this.dataProfile.cardinalityDetail.filter((i) => this.dataProfile.topK.some((x) => x === i.key));
            }
            if (this.dataProfile.bottomK) {
                this.bottomSamples = this.dataProfile.cardinalityDetail.filter((i) => this.dataProfile.bottomK.some((x) => x === i.key));
            }
        }

        if (this.dataProfile.shapesDetail) {
            this.dataProfile.shapesDetail = this.dataProfile.shapesDetail.sort((a, b) => (b.count - a.count));
        }
    }

    private getBackgroundSize(size: number, total: number) {
        return "" + (size / total) * 100 + "% 100%";
    }

    private getNullBlankToolTip() {
        var retval: string = "";
        if (this.dataProfile.nullCount != null) {
            retval = `Null: ${this.dataProfile.nullCount.toLocaleString()} <span style="color: gray;">${((this.dataProfile.nullCount / this.dataProfile.sampleCount) * 100).toPrecision(2)}%</span>`;
        }

        if (this.dataProfile.blankCount != null) {
            if (retval.length > 0) {
                retval = retval + "<br/>";
            }
            retval = retval + `Blank: ${this.dataProfile.blankCount.toLocaleString()} <span style="color: gray;">${((this.dataProfile.blankCount / this.dataProfile.sampleCount) * 100).toPrecision(2) }%</span>`;
        }

        return retval;
    }

    private getSampleBarChart() {

        var validBar: string = "#7690a9 0%, ";
        var outlierBar: string = "";

        var matchPercentage = (this.dataProfile.matchCount / this.dataProfile.sampleCount) * 100;
        var outlierPercentage = (this.dataProfile.outlierCount / this.dataProfile.sampleCount) * 100;

        if (this.dataProfile.matchCount != null) {
            validBar = `var(--validbar) 0%, var(--validbar) ${matchPercentage}%, `;
        }

        if (this.dataProfile.outlierCount != null) {
            if (this.dataProfile.matchCount != null) {
                validBar += `var(--invalidbar) ${matchPercentage}%,`;
            }
            outlierBar = `var(--invalidbar) ${matchPercentage + outlierPercentage}%, var(--otherbar) ${matchPercentage + outlierPercentage}%,`;
        } else {
            if (this.dataProfile.matchCount != null) {
                outlierBar += `var(--otherbar) ${matchPercentage}%,`;
            }            
        }
        
        return { "background-image": `linear-gradient(to right, ${validBar} ${outlierBar} #7690a9 100%)` };      
    }

    private checkVisibility() {         
        if (!this.dataProfile.totalCount && !this.dataProfile.sampleCount) {
            this.hasValidCounts = false;
        }

        if (this.dataProfile.totalCount != null || this.dataProfile.sampleCount != null || this.dataProfile.type || this.dataProfile.typeQualifier || this.dataProfile.confidence != null)
        {
            this.showSampleSummary = true;
        }

        if (this.hasValidCounts && this.dataProfile.sampleCount != null && (this.dataProfile.cardinality != null || this.dataProfile.matchCount != null || this.dataProfile.outlierCount != null || this.dataProfile.nullCount != null || this.dataProfile.blankCount != null)) {
            this.showSampleQuality = true;
        }

        if (this.hasValidCounts && (this.dataProfile.regExp || this.dataProfile.blankCount != null || this.dataProfile.nullCount != null ||
            this.dataProfile.min != null || this.dataProfile.max != null || this.dataProfile.mean != null || this.dataProfile.standardDeviation != null ||
            this.dataProfile.leadingZeroCount != null || this.dataProfile.minLength != null || this.dataProfile.maxLength != null ||
            this.dataProfile.multiline != null || this.dataProfile.leadingWhiteSpace != null || this.dataProfile.trailingWhiteSpace != null)) {
            this.showStatistics = true;
        }

        if (this.dataProfile.type) {
            switch (this.dataProfile.type.toLowerCase()) {
                case "boolean": 
                    this.ShowBoolean = true;
                    if (!(this.dataProfile.regExp || this.dataProfile.blankCount != null || this.dataProfile.nullCount != null)) {
                        this.showStatistics = false;
                    }
                    break;                
                case "number":
                case "long":
                case "double":
                    this.ShowNumber = true;
                    if (!(this.dataProfile.regExp || this.dataProfile.blankCount != null || this.dataProfile.nullCount != null
                        || this.dataProfile.min != null || this.dataProfile.max != null || this.dataProfile.mean != null
                        || this.dataProfile.leadingZeroCount != null || this.dataProfile.standardDeviation != null)) {
                        this.showStatistics = false;
                    }
                    break;
                case "string": 
                    this.ShowString = true;
                    if (!(this.dataProfile.regExp || this.dataProfile.blankCount != null || this.dataProfile.nullCount != null
                        || this.dataProfile.min != null || this.dataProfile.max != null || this.dataProfile.multiline != null
                        || this.dataProfile.minLength != null || this.dataProfile.maxLength != null || this.dataProfile.leadingWhiteSpace != null
                        || this.dataProfile.trailingWhiteSpace != null)) {
                        this.showStatistics = false;
                    }
                    break;       
                case "date":
                case "localdate":                   
                case "localtime":
                case "localdatetime":
                case "offsetdatetime":
                case "zoneddatetime":
                    this.ShowDateTime = true;                    
                    if (!(this.dataProfile.regExp || this.dataProfile.blankCount != null || this.dataProfile.nullCount != null
                        || this.dataProfile.min != null || this.dataProfile.max != null)) {
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

    private setBaseTypeText() {
        if (this.dataProfile.type) {
            switch (this.dataProfile.type.toLowerCase()) {
                case "long":
                    this.baseType = "Number (Long)";
                    break;
                case "double":
                    this.baseType = "Number (Double)";
                    break;
                case "boolean":
                    this.baseType = "True/False (Boolean)";
                    break;
                default:
                    this.baseType = this.dataProfile.type;
                    break;
            }
        }
    }
}