///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionSummaryStats } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-statistics',
    template: ` 
                <div class="tile tile-detail">
                    <header>Statistics</header>
                    <div class="row">                        
                        <div class="col l6 s12">
                            <div class="row">
                                <div class="col s12" style="font-weight:bold">Agent % Success</div>
                                <div class="col s12">
                                    <chart [options]="agentPie"></chart>
                                </div>
                            </div>
                        </div>
                        <div class="col l6 s12">
                            <div class="row">
                                <div class="col s12" style="font-weight:bold">Processing % Success</div>
                                <div class="col s12">
                                    <chart [options]="workerPie"></chart>
                                </div>
                        </div>
                    </div>                    
                </div>
                `,
    providers: [FusionService],
})

export class FusionStatisticsComponent extends BaseComponent implements OnInit {
    private fusionSummaryStats: FusionSummaryStats;
    private agentPie: Object;
    private workerPie: Object;

    constructor(private fusionService: FusionService) {
        super();
    }

    ngOnInit() {
        this.load();
    }

    private load() {
        this.isLoading = true;
        this.fusionService.getFusionStatsSummary()
            .then(res => {
                this.fusionSummaryStats = res;
                let agentSuccess = this.calculateSuccess(res.AgentExecutions, res.AgentErrors);
                let workerSuccess = this.calculateSuccess(res.FusionExecutions, res.FusionErrors);
                
                this.agentPie = this.getKpi(agentSuccess, 100 - (agentSuccess), agentSuccess+' %',"Agent % Success");               
                this.workerPie = this.getKpi(workerSuccess, 100 - (workerSuccess), workerSuccess + ' %', "Processing % Success");               
                this.isLoading = false;
            });
    }

    private calculateSuccess(total, errors): number {        
        if (total == 0) return 100;
        if (errors == undefined) return 0;        
        return ((total - errors) / total) * 100;
    }

    private getKpi(score: number, remaining: number, title?: string, label?:string) {
        return {
            chart: {
                type: 'pie',
                backgroundColor: 'transparent',
                height: 187,
                width: 187
            },
            title: {
                text: title,
                align: 'center',
                verticalAlign: 'middle',
                y: 5
            },
            credits: {
                enabled: false
            },
            yAxis: {
                max: 1.0
            },
            plotOptions: {
                pie: {
                    shadow: false
                }
            },
            tooltip: {
                formatter: function () {
                    if (!this.point.name) return null;
                    return '<b>' + this.point.name + '</b>: ' + this.y + ' %';
                }
            },
            series: [{
                name: 'Score',
                data: [{ name: label, y: score, color: '#398D3A' }, { name: "", y: remaining, color: "white" }],
                showInLegend: false,
                innerSize: '70%',
                size: '80%',
                dataLabels: {
                    enabled: false,
                }
            }
            ]
        };
    }
};