import { Input, Component, OnInit, ViewChild } from '@angular/core';
import { ArtifactTypeService } from '../../services/artifact-type.service';
import { StateService } from '../../services/state.service';
import { BaseComponent} from '../shared/base.component';
import { ArtifactTypeStatusCount, ArtifactTypeUsedVsUnusedResponsibility } from '../../models/artifact-type.model';
import { ArtifactType } from '../../models/artifact-type.model';
import { Highcharts } from 'angular2-highcharts';
import { GridDefinition, GridColumn, GridField, GridFilterColumn, GridFilterExpression, GridRelationshipFilterExpression, GridAttributeFilterExpression, GridFilterFieldType } from '../../models/grid-definition.model';
import { ArtifactGridComponent} from './artifact-grid.component';

@Component({
    selector: 'd3s-artifact-type-metrics',    
    template: `     
                <d3s-loading [isLoading]="isLoading"></d3s-loading>            
                <div class="row" *ngIf="!isLoading">                    
                    <div class="col s12 m12 l6">                        
                        <div class="tile tile-detail">                            
                            <header>Responsibilities Assigned</header>
                            <chart [options]="responsibilitiesBar"></chart>                                                 
                        </div>
                    </div>
                    <div class="col s12 m12 l6">                        
                        <div class="tile tile-detail">                            
                            <header>Status Breakdown</header>                                                      
                            <chart [options]="statusPie">
                                <series (click)="onStatusSeriesClick($event)">
                                </series>
                            </chart> 
                        </div>
                    </div>
                    <div class="col s12" [hidden]="!showArtifactStatusGrid">       
                        <div class="tile tile-detail">                                                                       
                            <d3s-artifact-grid [artifactType]="artifactType" [titlePostfix]="statusHeader" rowsPerPage="10"></d3s-artifact-grid>
                        </div>
                    </div>
                </div>
                `,
    providers: [ArtifactTypeService],
})

export class ArtifactTypeMetricsComponent extends BaseComponent implements OnInit {    
    @Input() artifactType: ArtifactType;

    @ViewChild(ArtifactGridComponent) artifactTypeGrid: ArtifactGridComponent;

    private responsibilitiesBar: Object;
    private statusPie: Object;

    private status: ArtifactTypeStatusCount[] = [];
    private responsibilities: ArtifactTypeUsedVsUnusedResponsibility[] = [];

    private showArtifactStatusGrid: boolean = false;
    private statusHeader: string;
    
    constructor(private stateService: StateService, private artifactTypeService: ArtifactTypeService) {
        super();
    }

    ngOnInit() {        
        this.loadStatusPie();
        this.loadResponsibilityBar();
    }
    

    private loadResponsibilityBar() {
        this.isLoading = true;
        this.artifactTypeService.getArtifactTypeUsedVsUnusedResponsibilities(this.artifactType.ID)
            .then(result => {
                this.responsibilities = result;
                this.isLoading = false;

                this.responsibilitiesBar = {
                    chart: {
                        type: 'column',
                        backgroundColor: 'transparent',                        
                    },
                    title: {
                        text: null
                    },
                    xAxis: {
                        categories: this.responsibilities.map(x => x.Responsibility)
                    },
                    yAxis: {
                        min: 0,
                        title: {
                            text: '# Items'
                        },
                        stackLabels: {
                            enabled: true,
                            style: {
                                fontWeight: 'bold',
                                color: (Highcharts.theme && Highcharts.theme.textColor) || 'gray'
                            }
                        }
                    },
                    credits: {
                        enabled: false
                    },
                    legend: {
                        align: 'right',
                        x: -30,
                        verticalAlign: 'top',
                        y: 25,
                        floating: true,
                        backgroundColor: (Highcharts.theme && Highcharts.theme.background2) || 'white',
                        borderColor: '#CCC',
                        borderWidth: 1,
                        shadow: false
                    },
                    tooltip: {
                        headerFormat: 'Responsibility - <b>{point.x}</b><br/>',
                        pointFormat: '{series.name}: {point.y}<br/>Total: {point.stackTotal}'
                    },
                    plotOptions: {
                        column: {
                            stacking: 'normal',
                            dataLabels: {
                                enabled: true,
                                color: (Highcharts.theme && Highcharts.theme.dataLabelsColor) || 'white'
                            }
                        }
                    },
                    series: [{
                        name: 'Unassigned',
                        data: this.responsibilities.map(x => x.UnassignedCount)
                    }, {
                            name: 'Assigned',
                            data: this.responsibilities.map(x => x.AssignedCount)
                        }]
                };
            });
    }

    private loadStatusPie() {
        this.isLoading = true;
        this.artifactTypeService.getArtifactTypeStatus(this.artifactType.ID)
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
                    subtitle: {
                        text: 'Click on a pie piece for more details.'
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

    private onStatusSeriesClick(e) {        
        //reset any filters on the grid and add a new one for status
        this.stateService.artifactTypeFilters.filters = [];
        let filter = new GridFilterExpression();
        
        filter.field = 'Status';
        filter.value = e.originalEvent.point.name;
        filter.condition = 'EQUALS';
        filter.fieldtype = GridFilterFieldType.Normal;

        this.statusHeader = ` - With Status of ${e.originalEvent.point.name}`;

        this.stateService.artifactTypeFilters.attributes = undefined;
        this.stateService.artifactTypeFilters.relationships = undefined;
        this.stateService.artifactTypeFilters.simpleTextFilter = '';

        this.stateService.artifactTypeFilters.filters.push(filter);

        this.artifactTypeGrid.filterGridData();

        this.showArtifactStatusGrid = !e.originalEvent.point.sliced; // appears to be 1 behind        
    }
};