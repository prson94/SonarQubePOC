import { Input, Component, OnInit } from '@angular/core';
import { ArtifactTypeService } from '../../services/index';
import { BaseComponent} from '../shared/base.component';
import { ArtifactTypeStatusCount } from '../../models/artifact-type.model';

@Component({
    selector: 'd3s-artifact-type-metrics',
    template: `     
                <d3s-loading [isLoading]="isLoading"></d3s-loading>            
                <div class="row" *ngIf="!isLoading">                    
                    <div class="col s12 m12 l6">                        
                        <div class="tile tile-detail">                            
                            <header>Responsibilities</header>     
                            <chart [options]="responsibilitiesBar"></chart>                                                 
                        </div>
                    </div>
                    <div class="col s12 m12 l6">                        
                        <div class="tile tile-detail">                            
                            <header>Status Breakdown</header>                                                      
                            <chart [options]="statusPie"></chart> 
                        </div>
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService],
})

export class ArtifactTypeMetricsComponent extends BaseComponent implements OnInit {
    @Input() objectID: number;
    @Input() objectName: string;
    @Input() objectType: string;

    private responsibilitiesBar: Object;
    private statusPie: Object;

    private status: ArtifactTypeStatusCount[] = [];

    constructor(private artifactTypeService: ArtifactTypeService) {
        super();
    }

    ngOnInit() {        
        this.loadStatusPie();
    }


    private loadStatusPie() {
        this.isLoading = true;
        this.artifactTypeService.getArtifactTypeStatus(this.objectID)
            .then(result => {
                this.status = result;

                this.statusPie = {
                    chart: {
                        plotBackgroundColor: null,
                        plotBorderWidth: null,
                        plotShadow: false,
                        type: 'pie',
                        backgroundColor: 'transparent',                        
                    },
                    title: {
                        text: null
                    },
                    credits: {
                        enabled: false
                    },
                    tooltip: {
                        pointFormat: '{series.name}: <b>{point.y} - {point.percentage:.1f}%</b><br>'
                    },
                    plotOptions: {
                        pie: {
                            allowPointSelect: true,
                            cursor: 'pointer',
                            dataLabels: {
                                enabled: false
                            },
                            showInLegend: true
                        }
                    },
                    series: [{
                        name: 'Status',
                        colorByPoint: true,
                        data: this.status.map(x => ({
                            name: x.Status,
                                y: x.Count
                        })),                     
                    }]
                };


                this.isLoading = false;
            });           
    }    
};