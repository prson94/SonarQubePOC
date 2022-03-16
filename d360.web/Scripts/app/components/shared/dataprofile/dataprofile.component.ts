import { Input, Component, OnInit, SimpleChanges, OnChanges, AfterViewInit, LOCALE_ID, Output, EventEmitter, ChangeDetectorRef} from '@angular/core';
import { BaseComponent } from '../base.component';

import * as Highcharts from 'highcharts';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetService } from '../../../services/asset.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { DataProfileService } from '../../../services/dataprofile.service';
import { SemanticType } from '../../../models/semantic-type.model';
import { FeatureFlags, FeatureFlagsService } from '../../../services/featureflags.service';

@Component({
    selector: 'data-profile',
    templateUrl: './dataprofile.component.html',
    styleUrls: ['dataprofile.less']
})

export class DataProfileComponent extends BaseComponent implements OnInit {
    @Input() dataProfile: any;    
    @Input() isModal: boolean = false;  
    @Input() assetData: any;
    @Output() linkClicked = new EventEmitter();
    @Output() showTimeSeries = new EventEmitter();
    @Output() secondaryPanelLinkClicked = new EventEmitter();

    showMaxValueGraphIcon: boolean;
    showMinValueGraphIcon: boolean;

    constructor(
        private assetService: AssetService,
        private assetTypeService: AssetTypeService,
        protected settingsService: CompanySettingsService,
        private dataProfileService: DataProfileService,
        private cdRef: ChangeDetectorRef,
        private featureFlagService: FeatureFlagsService
    ) {
        super(settingsService);        
    }

    private sampleCountPercentage: number;
    private nullBlankCountTotal: number;

    private showDataProfile: boolean = true;

    private validPercentage: number;

    private outliersToShow: number = 5;
    private shapesToShow: number = 5;
    private topSamplesToShow: number = 5;
    private bottomSamplesToShow: number = 5;
    private topSamples: any[] = [];
    private bottomSamples: any[] = [];
    
    private sampleBarChart: any;

    private ShowBoolean: boolean = false;
    private ShowNumber: boolean = false;
    private ShowString: boolean = false;
    private ShowDate: boolean = false;
    private ShowDateTime: boolean = false;
    private ShowOther: boolean = false;
    private showSampleSummary: boolean = false;
    private showSampleQuality: boolean = false;
    private showSampleDistribution: boolean = false;
    private showStatistics: boolean;
    private nullBlankTooltipText: string;
    private baseType: string;
    private hasValidCounts: boolean = true;
    private maxValue: any;
    private minValue: any;
    private maxValueStr: any;
    private minValueStr: any;
    private lastMaxValue: any;
    private lastMinValue: any;
    private validCount: number;
    private distinctCount: number;
    private invalidCount: number;
    private showDuplicates: boolean = true;
    private showSimilar: boolean = true;
    private assetName: string;
    private assetTypeName: string;
    private hideInfoMessage: boolean = false;
    public displayChart: boolean = false;
    private hideDataProfileInstructionMessageKey = "hideDataProfileInstructionMessage";
    public chartType: string;
    public timeSeriesProfileList: any[] = [];
    public dataProfileList: any[] = [];

    private numberTypes: string[] = ["long", "double"];

    isMatchDetectionPopupVisible: boolean = false;
    matchType: string = "";

    sampleDistributionChart: Highcharts.Chart;
    sampleChartXLabel: string = '';

    matchAssetUid: string = "";
    hasDefinedType: boolean = false;
    semanticType: SemanticType;

    ngOnInit() { 
        this.initialize();
        if (this.assetData) {
            this.assetName = this.assetData.AssetName;
            this.assetTypeName = this.assetData.AssetTypeName;
        } else {
            let uriParams: any = {};
            this.isLoading = true;
            this.assetService.getAsset(this.dataProfile.assetUid)
                .subscribe((res) => {
                    this.assetName = res.Name;
                    uriParams.assetTypeUid = res.AssetTypeUid;
                    this.assetTypeService.getAssetTypes(uriParams).subscribe((result) => {
                        this.assetTypeName = result[0].Name;
                        this.isLoading = false;
                    });
                });
        }
    }    
    
    ngAfterViewInit() {
        if (!this.sampleDistributionChart) {
            setTimeout(() => this.renderSampleDistributionChart(), 10);
        }               
    }

    initialize() {
        let startDate = new Date();
        startDate.setFullYear(startDate.getUTCFullYear() - 100);
        if (this.featureFlagService.flags[FeatureFlags.SemanticTypesUiFlag]) {
            debugger;
            this.dataProfileService.getDataProfiles(this.dataProfile.assetUid, startDate, null, false, false, false).subscribe(
                (r) => {
                    if (r && r.items && r.items.length > 1) {
                        this.dataProfileList = r.items;
                        let maxProfileDate = new Date();
                        maxProfileDate.setFullYear(maxProfileDate.getFullYear() - 1);
                        this.timeSeriesProfileList = this.dataProfileList.filter((p) => new Date(p.profileSetDate) >= maxProfileDate);
                        this.cdRef.markForCheck();
                    }
                });

            if (this.dataProfile.typeQualifier && this.featureFlagService.flags[FeatureFlags.SemanticTypesUiFlag]) {
                debugger;
                this.dataProfileService.getSemanticTypes(1, 1, "", `qualifier eq '${this.dataProfile.typeQualifier}'`).subscribe((s) => {
                    this.semanticType = s.items[0];
                    if (this.semanticType) {
                        this.hasDefinedType = true;
                    }
                });
            }
        }
        
        
        if (localStorage.getItem(this.hideDataProfileInstructionMessageKey)) {
            this.hideInfoMessage = localStorage.getItem(this.hideDataProfileInstructionMessageKey).toLowerCase() === "true";            
        }
        if (!this.dataProfile) {
            this.dataProfile = this.dataProfileList[0];
        }
        
        this.validPercentage = ((this.dataProfile.matchCount / this.dataProfile.totalCount) * 100);

        this.nullBlankCountTotal = ((this.dataProfile.nullCount ?? 0) + (this.dataProfile.blankCount ?? 0));
        this.validCount = this.dataProfile.matchCount ?? 0;
        this.distinctCount = this.dataProfile.cardinality ?? 0;
        this.invalidCount = this.dataProfile.outlierCount ?? 0;

        this.matchAssetUid = this.dataProfile.assetUid;
        this.sortSamples();

        this.sampleBarChart = this.getSampleBarChart();

        this.nullBlankTooltipText = this.getNullBlankToolTip();

        this.setBaseTypeText();

        this.checkVisibility();

        this.setMinAndMaxText();             
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
            if (this.dataProfile.topK) {
                this.dataProfile.topK.forEach((t) => {
                    let value = this.dataProfile.cardinalityDetail.find((cd) => cd.key === t);
                    if (value) {
                        this.topSamples.push(value);
                    }
                });
                if (this.numberTypes.indexOf(this.dataProfile.type?.toLowerCase()) >= 0) {
                    this.topSamples = this.topSamples.sort((a, b) => (b.key - a.key));
                }
            }
            if (this.dataProfile.bottomK) {
                this.dataProfile.bottomK.forEach((b) => {
                    let value = this.dataProfile.cardinalityDetail.find((cd) => cd.key === b);
                    if (value) {
                        this.bottomSamples.push(value);
                    }
                });
                if (this.numberTypes.indexOf(this.dataProfile.type?.toLowerCase()) >= 0) {
                    this.bottomSamples = this.bottomSamples.sort((a, b) => (a.key - b.key));
                }
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
        if (this.dataProfile.nullCount) {
            retval = `Null: ${this.dataProfile.nullCount.toLocaleString()} <span style="color: gray;">${((this.dataProfile.nullCount / this.dataProfile.sampleCount) * 100).toPrecision(2)}%</span>`;
        }

        if (this.dataProfile.blankCount) {
            if (retval.length > 0) {
                retval = retval + "<br/>";
            }
            retval = retval + `Blank: ${this.dataProfile.blankCount.toLocaleString()} <span style="color: gray;">${((this.dataProfile.blankCount / this.dataProfile.sampleCount) * 100).toPrecision(2)}%</span>`;
        }

        return retval;
    }

    private getSampleBarChart() {

        var validBar: string = "var(--otherbar) 0%, ";
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

        return { "background-image": `linear-gradient(to right, ${validBar} ${outlierBar} var(--otherbar) 100%)` };
    }

    private checkVisibility() {
        if (!this.dataProfile.totalCount && !this.dataProfile.sampleCount) {
            this.hasValidCounts = false;
        }

        if (this.dataProfile.totalCount != null || this.dataProfile.sampleCount != null || this.dataProfile.type || this.dataProfile.typeQualifier || this.dataProfile.confidence != null) {
            this.showSampleSummary = true;
        }

        if (this.hasValidCounts && this.dataProfile.sampleCount && (this.dataProfile.cardinality || this.dataProfile.matchCount || this.dataProfile.outlierCount || this.dataProfile.nullCount || this.dataProfile.blankCount)) {
            this.showSampleQuality = true;
        }

        if (this.hasValidCounts && (this.dataProfile.regExp || this.dataProfile.blankCount != null || this.dataProfile.nullCount != null ||
            this.dataProfile.min != null || this.dataProfile.max != null || this.dataProfile.mean != null || this.dataProfile.standardDeviation != null ||
            this.dataProfile.leadingZeroCount != null || this.dataProfile.minLength != null || this.dataProfile.maxLength != null ||
            this.dataProfile.multiline != null || this.dataProfile.leadingWhiteSpace != null || this.dataProfile.trailingWhiteSpace != null)) {
            this.showStatistics = true;
        }

        if (this.hasValidCounts && this.dataProfile.cardinalityDetail && this.dataProfile.cardinalityDetail.length > 0) {
            this.showSampleDistribution = true;
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

    private setMinAndMaxText() {
        if (this.dataProfile.type && (this.dataProfile.type.toLowerCase() === 'double' || this.dataProfile.type.toLowerCase() === 'long')) {
            if (isNaN(Number(this.dataProfile?.max))) {
                this.maxValueStr = this.dataProfile?.max;
            } else {
                this.maxValue = Number(this.dataProfile?.max);
                this.maxValueStr = Number(this.dataProfile?.max).toLocaleString();
                this.lastMaxValue = Number(this.timeSeriesProfileList[1]?.max);
                this.showMaxValueGraphIcon = true;
            }

            if (isNaN(Number(this.dataProfile?.min))) {
                this.minValueStr = this.dataProfile?.min;
            } else {
                this.minValue = Number(this.dataProfile?.min);
                this.minValueStr = Number(this.dataProfile?.min).toLocaleString();
                this.lastMinValue = Number(this.timeSeriesProfileList[1]?.min);
                this.showMinValueGraphIcon = true;
            }
        } else {
            this.maxValueStr = this.dataProfile?.max;
            this.minValueStr = this.dataProfile?.min;
        }
    }

    private capitaliseBoolean(str: any) {
        return str.toString()[0].toUpperCase() + str.toString().slice(1);
    }

    getMatchTooltip(type: string, count: number): string {
        let assetCountStr: string = count > 1 ? `${count} assets` : '1 asset';
        let descStr: string = type === 'duplicates' ? 'same type and matching data' : 'same type but different data';
        return `${assetCountStr} detected which have the ${descStr}.\nClick to investigate and add tags.`;
    }

    matchDetectionLinkClicked(type: string) {        
        if (this.dataProfile.matches != null) {
            this.showDuplicates = this.dataProfile.matches.data > 0;
            this.showSimilar = this.dataProfile.matches.structure > 0;
        }
        if (!this.isModal) {
            this.isMatchDetectionPopupVisible = true;
            this.matchAssetUid = this.dataProfile.assetUid;
            this.matchType = type;
        } else {           
            this.linkClicked.emit({ assetUid: this.dataProfile.assetUid, matchType: type, showDuplicates: this.showDuplicates, showSimilar: this.showSimilar });
        }       
    }    

    public renderSampleDistributionChart() {

        if (this.showSampleDistribution === false) {
            return;
        }
        //use of var here is to allow access to the dataProfile object inside Highcharts specific functions
        var dataProfile: any = this.dataProfile;

        var nullColor: string = '#b2c1cf';
        var validColor: string = '#2e9b61';
        var invalidColor: string = '#d73961';

        let dataProfileType: string = this.dataProfile.type?.toLowerCase();
        let categories: string[] = [];
        let data: any[] = [];
        let colors: string[] = [];

        var blankNullLabel: string = 'Blank/Null';
        var invalidOutlierLabel: string = 'Invalid/Outliers';

        let maxSampleCount: number = 24;
        let maxNumberCount: number = 6;
        let showXAxisLabel: boolean = true;
        let pointPadding: number = 0.1;
        let maxPointWidth: number = null;
        let xAxisStep: number = 0;
        let spacingTop: number = 10;
        let includeStatsWidget: boolean = false;
        let maxYValue = 0;
        let index: number = 0;
        let leftPadding: number = 10;

        var meanIndex = 0;
        var meanPercentInterval = 0;
        var interval = 0;

        let testCardinality: string = this.dataProfile.cardinalityDetail[0].key;

        this.sampleChartXLabel = '';


        //add common null/invalid columns 
        if (this.dataProfile.outlierCount && this.dataProfile.outlierCount > 0) {
            categories.push(invalidOutlierLabel);
            data.push(this.invalidCount);
            colors.push(invalidColor);
            if (this.invalidCount > maxYValue) {
                maxYValue = this.invalidCount;
            }
            index++;
        }

        if ((this.dataProfile.nullCount && this.dataProfile.nullCount > 0) || (this.dataProfile.blankCount && this.dataProfile.blankCount > 0)) {
            categories.push(blankNullLabel);
            data.push(this.nullBlankCountTotal);
            colors.push(nullColor);
            if (this.nullBlankCountTotal > maxYValue) {
                maxYValue = this.nullBlankCountTotal;
            }
            index++;
        }



        if (dataProfileType === 'boolean') {           
            pointPadding = 0.05;
            showXAxisLabel = true;
            leftPadding = 0;

                this.dataProfile.cardinalityDetail.forEach((c) => {
                    categories.push(c.key);
                    data.push(c.count);
                    colors.push(validColor);
                    if (c.count > maxYValue) {
                        maxYValue = c.count;
                    }
                });

        } else if ((dataProfileType === 'long' || dataProfileType === 'double') && !isNaN(+testCardinality)) {
            showXAxisLabel = true;
            pointPadding = 0.15;
            maxPointWidth = 15;
            meanIndex = 0;
            //draw the stats widget as long as we have at least a mean value
            includeStatsWidget = this.dataProfile.mean != null && !isNaN(+this.dataProfile.mean);
            spacingTop = includeStatsWidget ? 50 : 10; 

            //calculate buckets based on data
            //range of numbers available based on min/max
            let range = +this.dataProfile.max - +this.dataProfile.min;
            let fixedLen = range < 10 ? 1 : 0;
            let current = +this.dataProfile.min;
            let lower = current;
            let sampleCount = Math.min(maxNumberCount, this.dataProfile.cardinalityDetail.length);

            //number interval for each bar
            interval = range / sampleCount;
            
            //fill interval buckets and keep track of index where mean widget will be drawn
            while (current < this.dataProfile.max) {               
                current += interval;
                let upper = current;

                if (this.dataProfile.mean != null) {
                    if (+this.dataProfile.mean >= lower && +this.dataProfile.mean < upper) {
                        meanIndex = index;
                        meanPercentInterval = ((interval - (upper - +this.dataProfile.mean)) / interval);
                    }
                }

                categories.push(`${lower.toFixed(fixedLen)} - ${upper.toFixed(fixedLen)}`);
                let count = this.dataProfile.cardinalityDetail
                    .filter((c) => +c.key >= lower && +c.key < upper)
                    .reduce((count, r) => count += r.count, 0);

                data.push(count);
                colors.push(validColor);

                if (count > maxYValue) {
                    maxYValue = count;
                }

                index++;
                lower = current;
            }

        } else if ((dataProfileType === 'date' || dataProfileType === 'datetime' || dataProfileType === 'localdate' || dataProfileType === 'localdatetime') && !isNaN(Date.parse(testCardinality))) {
            pointPadding = 0.1;          
            leftPadding = 22; 

            let minDate = new Date(this.dataProfile.min);
            let maxDate = new Date(this.dataProfile.max);
            let difference = maxDate.getTime() - minDate.getTime();
            let sampleCount = Math.min(maxSampleCount, this.dataProfile.cardinalityDetail.length);

            interval = difference / sampleCount;

            if (sampleCount > 18) {
                xAxisStep = 4;
            } else if (sampleCount > 9) {
                xAxisStep = 2;
            }

            let current = new Date(this.dataProfile.min);
            let lower = new Date(current);

            while (lower < maxDate) {
                current = new Date(current.getTime() + interval);
                let upper = current;
                let count = this.dataProfile.cardinalityDetail
                    .filter((c) => new Date(c.key) >= lower && new Date(c.key) < upper)
                    .reduce((count, r) => count += r.count, 0);

                let opts: Intl.DateTimeFormatOptions = { month: 'short', year: '2-digit' };
                let dateString = new Intl.DateTimeFormat(navigator.language, opts).format(lower);

                categories.push(dateString);
                data.push(count);

                if (count > maxYValue) {
                    maxYValue = count;
                }

                colors.push(validColor);
                lower = new Date(current);
            }

        } else {
            showXAxisLabel = false;
            pointPadding = 0.1;
            leftPadding = 0;
            this.sampleChartXLabel = this.distinctCount.toLocaleString() + ' distinct values';

            let i = 0;
            let max = Math.min(maxSampleCount, this.dataProfile.cardinalityDetail.length);
            let c = this.dataProfile.cardinalityDetail;
            while (i < max) {
                categories.push(c[i].key);
                data.push(c[i].count);
                colors.push(validColor);

                if (c[i].count > maxYValue) {
                    maxYValue = c[i].count;
                }

                i++;
            }
        }

        
        let tooltipFormatter = function () {
            let formatString = '';

            if (this.x === blankNullLabel) {
                formatString += `<div>Null: ${dataProfile.nullCount} <span style="color: #818385">${Math.round((dataProfile.nullCount / dataProfile.totalCount) * 100)}%</span></div>`;
                formatString += `<div>Blank: ${dataProfile.blankCount} <span style="color: #818385">${Math.round((dataProfile.blankCount / dataProfile.totalCount) * 100)}%</span></div>`;
            } else {
                formatString = `<div>${this.x}: ${this.y} <span style="color: #818385">${Math.round((this.y / dataProfile.totalCount) * 100)}%</span></div>`;
            }

            return formatString;
        };

        let renderStatsWidget = function (chart) {
            let strokeColor = '#bdbfc6';
            //include Std Dev bar if available
            let drawStd = dataProfile.standardDeviation != null && !isNaN(+dataProfile.standardDeviation);

            //get X position of bars surrounding the mean value and interval between them in pixels
            let lowerX = chart.series[0].data[meanIndex].plotX;
            let upperX = (meanIndex === chart.series[0].data.length - 1) ? 0 : chart.series[0].data[meanIndex + 1].plotX;
            let pixelInterval = upperX - lowerX;

            //calculate position of mean line
            let xPos = chart.plotLeft + lowerX + (pixelInterval * meanPercentInterval);

            chart.renderer.path([
                'M', xPos, chart.plotTop + chart.plotHeight,
                'L', xPos, chart.plotTop + 5
            ]).attr({
                stroke: strokeColor,
                'stroke-width': 1
            }).add();

            chart.renderer.text(
                'Mean',
                xPos,
                chart.plotTop - 30
            ).attr({
                align: 'center',
                zIndex: 5
            }).add();

            chart.renderer.text(
                dataProfile.mean.toFixed(2),
                xPos,
                chart.plotTop - 16
            ).attr({
                align: 'center',
                zIndex: 5
            }).css({
                color: '#898d99',
            }).add();

            if (drawStd) {
                //calculate length of horizonal std dev line
                let stdLen = (+dataProfile.standardDeviation / interval) * pixelInterval;
                let drawLeft = false;
                //figure out if we need to draw on the left
                if (Math.abs((xPos + stdLen) - (chart.plotLeft + chart.plotWidth)) < 50) {
                    drawLeft = true;
                }

                chart.renderer.path([
                    'M', xPos - stdLen, chart.plotTop + 5,
                    'L', xPos + stdLen, chart.plotTop + 5
                ]).attr({
                    stroke: strokeColor,
                    'stroke-dasharray': '2,2',
                    'stroke-width': 1
                }).add();

                chart.renderer.path([
                    'M', xPos - stdLen, chart.plotTop - 5,
                    'L', xPos - stdLen, chart.plotTop + 15
                ]).attr({
                    stroke: strokeColor,
                    'stroke-width': 1
                }).add();

                chart.renderer.path([
                    'M', xPos + stdLen, chart.plotTop - 5,
                    'L', xPos + stdLen, chart.plotTop + 15
                ]).attr({
                    stroke: strokeColor,
                    'stroke-width': 1
                }).add();

                chart.renderer.text(
                    'Std Dev',
                    xPos + ((stdLen + 5) * (drawLeft ? -1 : 1)),
                    chart.plotTop - 5,
                ).attr({
                    zIndex: 5,
                    align: drawLeft ? 'right' : 'left'
                }).add();

                chart.renderer.text(
                    +dataProfile.standardDeviation.toFixed(2),
                    xPos + ((stdLen + 5) * (drawLeft ? -1 : 1)),
                    chart.plotTop + 9,
                ).attr({
                    zIndex: 5,
                    align: drawLeft ? 'right' : 'left'
                }).css({
                    color: '#898d99',
                }).add();
            }
        };

        let chartOptions: any = {
            chart: {
                type: 'column',
                height: 200,
                spacingTop,
                marginLeft: leftPadding,
                spacingLeft: leftPadding,
                marginRight: 15,
                spacingRight: 15
            },
            credits: {
                enabled: false
            },
            title: {
                text: '',
                reserveSpace: false
            },
            xAxis: {
                categories,
                minPadding: 0,
                maxPadding: 0,
                labels: {
                    enabled: showXAxisLabel,
                    reserveSpace: showXAxisLabel,
                    step: xAxisStep
                },
                crosshair: false,   
            },
            yAxis: {
                min: 0,
                max: maxYValue,
                title: {
                    text: '',
                    reserveSpace: false
                },
                gridLineWidth: 0,
                labels: {
                    enabled: false,
                    reserveSpace: false
                }
            },
            legend: {
                enabled: false
            },
            tooltip: {
                formatter: tooltipFormatter,
                shared: true,
                useHTML: true,
                style: {
                    background: 'white',
                    borderRadius: 5,
                    opacity: 1,
                    border: '1px solid transparent'
                }
            },
            plotOptions: {
                column: {
                    pointPadding,
                    borderWidth: 0,
                    groupPadding: 0,
                    pointWidth: undefined,
                    colorByPoint: true,
                    colors
                }
            },
            series: [{
                name: '',
                data,
                maxPointWidth,
                minPointLength: 3
            }]
        };

        if (this.sampleDistributionChart) {
            this.sampleDistributionChart.destroy();
        }

        this.sampleDistributionChart = Highcharts.chart('sampleChart'+ (this.isModal ? 'Modal': ''), chartOptions, includeStatsWidget ? renderStatsWidget : null);
    }

    handleLinkClicked(event: any) {
        this.matchAssetUid = event.assetUid;
        this.matchType = event.matchType;
        this.showDuplicates = event.showDuplicates;
        this.showSimilar = event.showSimilar;
    }

    matchDetectionClosed() {
        this.isMatchDetectionPopupVisible = false;
        this.matchAssetUid = this.dataProfile.assetUid;
    }

    private hideChartInfoMessage() {
        localStorage.setItem("hideDataProfileInstructionMessage", "true");
        this.hideInfoMessage = true;
    }

    private openChart(chartType: string) {
        this.displayChart = true;
        this.chartType = chartType;       
    }

    openSemanticTab() {
        this.secondaryPanelLinkClicked.emit({ semanticType: this.semanticType });
    }
}
