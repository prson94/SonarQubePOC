import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { FusionService } from '../../services/index';
import { FusionSummaryStats } from '../../models/fusion.model';

@Component({
    selector: 'd3s-fusion-statistics',
    styles: [`
      chart {
        display: block;
      }
    `],
    template: ` 
                <div class="tile tile-detail" *ngIf="!showAgentHistory && !showFusionHistory">
                    <header>Statistics <span style="color:#999;font-size:60%;vertical-align:middle;">{{timeFrameMessage()}}</span>
                        <d3s-tile-actions [hasAdd]="false" [hasDate]="true" (dateClick)="changeDates($event);"></d3s-tile-actions>                            
                    </header>
                    <div class="row">                        
                        <div class="col s6">
                            <div class="row" (click)="showAgentHistory=true;">
                                <div class="col s12" style="font-weight:bold">Agent % Success</div>
                                <div class="col s12">
                                    <chart [options]="agentPie"></chart>
                                </div>
                            </div>
                        </div>
                        <div class="col s6">
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
                            <h6>&nbsp;* Percentage is based off {{timeFrameMessage()}}.  Click on charts for more information.</h6>
                        </div>
                    </div>
                </div> 
                <div class="tile tile-detail" *ngIf="showAgentHistory">
                    <div class="row">
                        <d3s-fusion-agent-errors [days]="daysToLookBack"></d3s-fusion-agent-errors>
                        <button pButton type="button" (click)="showAgentHistory=false;" label="Close" style="width: 150px;"></button>
                    </div>                 
                </div>
                <div class="tile tile-detail" *ngIf="showFusionHistory">
                    <div class="row" *ngIf="showFusionHistory">                        
                        <d3s-fusion-process-errors [days]="daysToLookBack"></d3s-fusion-process-errors>
                        <button pButton type="button" (click)="showFusionHistory=false;" label="Close" style="width: 150px;"></button>
                    </div>   
                </div>
                `,
    providers: [FusionService],
})

export class FusionStatisticsComponent extends BaseComponent implements OnInit {
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
        this.fusionService.getFusionStatsSummary(this.daysToLookBack)
            .then(res => {
                this.fusionSummaryStats = res;
                let agentSuccess = this.calculateSuccess(res.AgentExecutions, res.AgentErrors);
                let workerSuccess = this.calculateSuccess(res.FusionExecutions, res.FusionErrors);

                agentSuccess = +agentSuccess.toFixed(2);
                workerSuccess = +workerSuccess.toFixed(2);
                
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

    private changeDates(event) {
        this.daysToLookBack = event.days;        
        this.load();
    }

    private timeFrameMessage() {
        switch (this.daysToLookBack) {
            case 7:
                return ' (Past week)';
            case 30:
                return ' (Past month)';
            case 365:
                return ' (Past year)';
        }
        return ' (All Activity)'
    }
};