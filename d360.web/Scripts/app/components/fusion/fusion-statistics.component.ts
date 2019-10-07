import {Component, OnInit} from '@angular/core';

import {FusionSummaryStats} from '../../models/fusion.model';

import {FusionService} from '../../services/fusion.service';

import {BaseComponent} from '../shared/base.component';
import {takeUntil} from "rxjs/operators";
import {Subject} from "rxjs";

declare var require: any;
const Highcharts = require('highcharts/highstock.src');

@Component({
    selector: 'd3s-fusion-statistics',
    template: `
        <div class="tile tile-detail" *ngIf="!showAgentHistory && !showFusionHistory">
            <header>Statistics <span
                    style="color:#999;font-size:60%;vertical-align:middle;">{{timeFrameMessage()}}</span>
                <d3s-tile-actions [hasAdd]="false" [hasDate]="true"
                                  (dateClick)="changeDates($event);"></d3s-tile-actions>
            </header>
            <div class="row">
                <div class="col m6 s12">
                    <div class="row" (click)="showAgentHistory=true;">
                        <div class="col s12" style="font-weight:bold">Agent % Success</div>
                        <div class="col s12">
                            <chart [options]="agentPie"></chart>
                        </div>
                    </div>
                </div>
                <div class="col m6 s12">
                    <div class="row" (click)="showFusionHistory=true;">
                        <div class="col s12" style="font-weight:bold">Processing % Success</div>
                        <div class="col s12">
                            <chart [options]="workerPie"></chart>
                        </div>
                    </div>
                </div>
            </div>
            <div class="row">
                <div class="col s12">
                    <h6>&nbsp;* Percentage is based off {{timeFrameMessage()}}. Click on charts for more
                        information.</h6>
                </div>
            </div>
        </div>
        <div class="tile tile-detail" *ngIf="showAgentHistory">
            <div class="row">
                <d3s-fusion-agent-errors [days]="daysToLookBack"></d3s-fusion-agent-errors>
                <button pButton type="button" (click)="showAgentHistory=false;" label="Close"
                        style="width: 150px;"></button>
            </div>
        </div>
        <div class="tile tile-detail" *ngIf="showFusionHistory">
            <div class="row" *ngIf="showFusionHistory">
                <d3s-fusion-process-errors [days]="daysToLookBack"></d3s-fusion-process-errors>
                <button pButton type="button" (click)="showFusionHistory=false;" label="Close"
                        style="width: 150px;"></button>
            </div>
        </div>
    `,
    providers: [FusionService],
})

export class FusionStatisticsComponent extends BaseComponent implements OnInit {
    destroySubject$: Subject<void> = new Subject();

    private fusionSummaryStats: FusionSummaryStats;
    private agentPie: Object;
    private workerPie: Object;

    private showAgentHistory: boolean;
    private showFusionHistory: boolean;

    private daysToLookBack: number = 7;

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;

        this.fusionService
            .getFusionStatsSummary(this.daysToLookBack)
            .pipe(takeUntil(this.destroySubject$))
            .subscribe(
                res => {
                    let agentSuccess = this.calculateSuccess(res.AgentExecutions, res.AgentErrors);
                    let workerSuccess = this.calculateSuccess(res.FusionExecutions, res.FusionErrors);

                    this.fusionSummaryStats = res;

                    agentSuccess = +agentSuccess.toFixed(2);
                    workerSuccess = +workerSuccess.toFixed(2);

                    this.agentPie = this.getKpi(agentSuccess, "Agent % Success");
                    this.workerPie = this.getKpi(workerSuccess, "Processing % Success");

                    this.isLoading = false;
                }
            );
    }

    private calculateSuccess(
        total,
        errors
    ): number {
        let num = ((total - errors) / total) * 100;

        if (total == 0) {
            num = 100;
        }

        if (errors == undefined
            || (errors >= total)
        ) {
            num = 0;
        }

        return num;
    }

    private getKpi(
        score: number,
        title?: string
    ) {
        console.log(Highcharts.theme && Highcharts.theme.background2);
        return {
            chart: {
                type: 'solidgauge',
                backgroundColor: 'transparent',
                height: 87,
                width: 187
            },
            title: '',
            pane: {
                center: ['50%', '90%'],
                size: '160%',
                startAngle: -90,
                endAngle: 90,
                background: {
                    backgroundColor: (Highcharts.theme && Highcharts.theme.background2) || '#EEE',
                    innerRadius: '80%',
                    outerRadius: '100%',
                    shape: 'arc',
                    borderColor: 'transparent'
                }
            },
            tooltip: {
                enabled: false
            },
            // the value axis
            yAxis: {
                min: 0,
                max: 100,
                stops: [
                    [0.1, '#BC1B01'], // red
                    [0.5, '#FFB230'], // yellow
                    [0.9, '#02981B'] // green
                ],
                lineWidth: 0,
                minorTickLength: 0,
                tickLength: 100,
                tickWidth: 4,
                tickColor: 'transparent',
                gridLineWidth: 0,
                gridLineColor: 'transparent',
                tickAmount: 2,
                title: {
                    enabled: false,
                },
                labels: {
                    enabled: false,
                }
            },
            plotOptions: {
                solidgauge: {
                    innerRadius: '80%',
                    outerRadius: '100%',
                    dataLabels: {
                        y: 5,
                        borderWidth: 0,
                        useHTML: true,
                        style: {
                            fontFamily: '',
                            fontSize: '20px',
                            color: '#646464'
                        }
                    }
                }
            },
            credits: {
                enabled: false
            },
            series: [{
                name: title,
                data: [Math.round(score)],
                dataLabels: {
                    format: '<div style="text-align:center">{y}%</div>',
                }
            }],
        };
    }

    private changeDates(event) {
        this.daysToLookBack = event.days;
        this.load();
    }

    private timeFrameMessage() {
        let str = " (All Activity)";

        switch (this.daysToLookBack) {
            case 7:
                str = " (Past week)";
                break;
            case 30:
                str = " (Past month)";
                break;
            case 365:
                str = " (Past year)";
                break;
        }

        return str;
    }
}
