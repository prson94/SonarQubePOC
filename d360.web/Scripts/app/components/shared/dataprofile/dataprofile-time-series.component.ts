import { Input, Component, OnInit, SimpleChanges, OnChanges, AfterViewInit, LOCALE_ID, Output, EventEmitter, HostListener, ViewChild, ElementRef } from '@angular/core';
import { BaseComponent } from '../base.component';

//import * as Highcharts from 'highcharts';
import * as Stockcharts from 'highcharts/highstock';
import { AssetTypeService } from '../../../services/asset-type.service';
import { AssetService } from '../../../services/asset.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { SelectItem } from 'primeng/api';

@Component({
    selector: 'data-profile-time-series',
    templateUrl: './dataprofile-time-series.component.html',
    styleUrls: ['dataprofile.less']
})

export class DataProfileTimeSeriesComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() dataProfileList: any[] = [];
    @Input() chartType: string;
    @Output() close = new EventEmitter();

    public displayChart: boolean = false;
    public graphTitle: string = "Default Title";
    public selectedGraphRangeOption: any;

    public graphRangeOptions: SelectItem[] = [
        {
            label: 'View 1 Month', value: 1
        },
        {
            label: 'View 3 Month', value: 3
        },
        {
            label: 'View 6 Month', value: 6
        },
        {
            label: 'View Year to Date', value: "YTD"
        },
        {
            label: 'View All', value: "All"
        }
    ];

    private graphType: any =
        {
            'totalCount': { title: "Total Row Count" },
            "confidence": { title: "Type Confidence", decimals: 2, dataType: "percentage" },
            "matchCount": { title: "Valid", color: "#2e9b61" },
            "cardinality": { title: "Distinct", color: "#2e9b61" },
            "outlierCount": { title: "Invalid/Outliers", color: "#d73961" },
            "nullCount": { title: "Null Count", },
            "blankCount": { title: "Blank Count" },
            "mean": { title: "Mean" },
            "standardDeviation": { title: "Standard Deviation" },
            "leadingZeroCount": { title: "Leading Zero Count" },
            "minLength": { title: "Minimum Length" },
            "maxLength": { title: "Maximum Length" },
            "min": { title: "Minimum Value", dataType: "string" },
            "max": { title: "Maximum Value", dataType: "string" },

            "nullBlankCount": { title: "Null/Blank", color: "#b2c1cf", seriesType: "sum", fields: ["nullCount", "blankCount"] },
            "allSampleQuality": { title: "Sample Quality", seriesType: "combined", series: ["matchCount", "outlierCount", "nullBlankCount"] },
        }

    matchAssetUid: string = "";
    private timeSeriesChart: Stockcharts.Chart;

    @HostListener('document:click', ['$event'])
    documentClick(event: any) {
        if (!(event.path.filter((f) => f?.classList?.contains("time-series-tile") || f?.classList?.contains("trendIcon")).length>0)) {
            this.close.emit();
        }
    }

    constructor(
        protected settingsService: CompanySettingsService
    ) {
        super(settingsService);
    }

    ngOnInit() {
        if (this.chartType) {
            this.disableChartOptions();
            this.renderTimeSeriesChart(this.chartType);
        }        
    }

    ngOnChanges() {
        if (this.chartType) {
            this.selectedGraphRangeOption = null;
            this.disableChartOptions();
            this.renderTimeSeriesChart(this.chartType);
        }        
    }

    private disableChartOptions() {
        let lastDate = new Date(this.dataProfileList[this.dataProfileList.length - 1].profileSetDate);

        this.graphRangeOptions.filter((f) => !isNaN(f.value)).forEach((g) => {
            let d = new Date();
            d.setMonth(d.getMonth() - g.value);
            if (lastDate > d || d > (new Date(this.dataProfileList[0].profileSetDate))) {
                g.disabled = true;
            }
            if (!g.disabled && !this.selectedGraphRangeOption) {
                this.selectedGraphRangeOption = g.value;
            }
        }
        );
    }

    public graphOptionChanged() {
        let d = new Date();
        d.setMinutes(0);
        d.setSeconds(0);
        d.setMilliseconds(0);

        if (this.selectedGraphRangeOption !== "C") {
            if (isNaN(this.selectedGraphRangeOption)) {
                if (this.selectedGraphRangeOption === "YTD") {
                    d.setMonth(0);
                    d.setDate(0);
                }
                if (this.selectedGraphRangeOption === "All") {
                    d = null;
                }
            } else {
                d.setMonth(d.getMonth() - this.selectedGraphRangeOption);
            }

            this.timeSeriesChart.xAxis[0].setExtremes(d?.getTime(), null);
        }
    }

    private renderTimeSeriesChart(chartType: string) {
        this.graphTitle = this.graphType[chartType].title;
        let series = []

        if (this.graphType[chartType].seriesType === "combined" && this.graphType[chartType]?.series?.length > 0) {
            this.graphType[chartType].series.forEach((s) => series.push(this.generateSeries(s)))
        } else {
            series.push(this.generateSeries(chartType));
        }

        let chartOptions: Stockcharts.Options = {
            title: {
                text: '',
            },
            legend: {
                enabled: false
            },
            credits: {
                enabled: false
            },
            rangeSelector: {
                verticalAlign: 'top',
                buttons: [],
                enabled: true,
                dropdown: "always"
            },
            series: series,
            navigator: {
                enabled: true,
                height: 50,
                maskFill: 'rgba(180, 198, 220, 0.75)'
            },
            xAxis: {
                type: 'datetime',
                dateTimeLabelFormats: {
                    day: '%d %b %Y'
                },
                events: {
                    afterSetExtremes: (e) => {
                        let d = new Date();
                        d.setMinutes(0);
                        d.setSeconds(0);
                        d.setMilliseconds(0);
                        let baseMonth = d.getMonth();
                        let isCustom = true;
                        let maxdate = new Date(e.max);
                        let mindate = new Date(e.min);
                        mindate.setMinutes(0);
                        mindate.setSeconds(0);
                        mindate.setMilliseconds(0);

                        let currentProfileDate = new Date(this.dataProfileList[0].profileSetDate);                        
                        if (currentProfileDate.getTime() <= maxdate.getTime()) {
                            d.setMonth(baseMonth - 1);
                            if (isCustom && (mindate.getTime() - d.getTime() === 0)) { // 1 Months
                                isCustom = false;
                            }
                            d.setMonth(baseMonth - 3);
                            if (isCustom && (mindate.getTime() - d.getTime() === 0)) { // 3 Months
                                isCustom = false;
                            }
                            d.setMonth(baseMonth - 6);
                            if (isCustom && (mindate.getTime() - d.getTime() === 0)) { // 6 Months
                                isCustom = false;
                            }
                            d.setMonth(0);
                            d.setDate(0);
                            if (isCustom && (mindate.getTime() - d.getTime() === 0)) { // YTD
                                isCustom = false;
                            }
                            if (isCustom) {
                                let lastProfileDate = new Date(this.dataProfileList[this.dataProfileList.length-1].profileSetDate);
                                if (lastProfileDate.getTime() >= mindate.getTime()) { //All
                                    isCustom = false;
                                }
                            }
                        }

                        if (isCustom) {
                            this.selectedGraphRangeOption = "";
                        }
                    }
                }
            },
            yAxis: {
                title: {
                    text: '',
                    reserveSpace: false
                },
                labels: {
                    format: this.graphType[chartType]?.dataType === "percentage" ? '{text}%' : '{text}'

                },
                min: this.graphType[chartType]?.dataType === "percentage" ? 0 : null,
                max: this.graphType[chartType]?.dataType === "percentage" ? 100 : null,
            }
        }

        this.timeSeriesChart = Stockcharts.chart('timeSeries', chartOptions);

        this.graphOptionChanged();
    }

    private generateSeries(chartType: string) {
        let data = [];

        if (this.graphType[chartType]?.seriesType === "sum" && this.graphType[chartType]?.fields && Array.isArray(this.graphType[chartType].fields)) {
            data = this.dataProfileList.map((x) => {
                let v = 0;
                this.graphType[chartType].fields.forEach((f) => v += x[f]);
                return [new Date(x.profileSetDate).getTime(), v];
            });
        } else {
            data = this.dataProfileList.map((x) => {
                let value = x[chartType];
                if (this.graphType[chartType]?.dataType) {
                    if (this.graphType[chartType]?.dataType === "string") {
                        value = Number(x[chartType])
                    }
                    if (this.graphType[chartType]?.dataType === "percentage") {
                        value = x[chartType] * 100
                    }
                }
                return [new Date(x.profileSetDate).getTime(), value];                
            });
        }

        return {
            chartType: "Highstock",
            name: this.graphType[chartType].title,
            color: this.graphType[chartType]?.color ?? '#597897',
            data: data,
            marker: {
                enabled: true,
                radius: 4
            },
            shadow: false,
            tooltip: {
                valueDecimals: this.graphType[chartType]?.decimals ?? 0,
                valueSuffix: this.graphType[chartType]?.dataType === "percentage" ? '%' : '',
            },
            showInNavigator: true

        }
    }
}