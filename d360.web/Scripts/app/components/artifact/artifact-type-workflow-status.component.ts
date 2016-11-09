import { Input, Component, OnChanges, SimpleChange } from '@angular/core';
import { ArtifactTypeService, WorkflowService } from '../../services/index';
import { BaseComponent} from '../shared/base.component';
import { ArtifactType } from '../../models/artifact-type.model';
import { ArtifactTypeWorkflowBreakdown, WorkflowStepStatistic, WorkflowType } from '../../models/workflow.model';
import { DynamicGridResultsInData } from '../../models/grid-definition.model';
import { Highcharts } from 'angular2-highcharts';
import * as _ from 'lodash';

@Component({
    selector: 'd3s-artifact-type-workflow-status',
    template: `     
                <d3s-loading [isLoading]="isLoading"></d3s-loading>            
                <div class="row" *ngIf="!isLoading">                  
                    <div class="col s12 m12 l8">
                        <div class="tile tile-detail">                            
                            <header>{{artifactType?.Name}} Workflow Type Breakdown</header>
                            <chart [options]="workflowColumn">
                                <series (click)="onSeriesClick($event)">
                                </series>
                            </chart>
                        </div>  
                    </div>
                    <div class="col s12 m12 l4">
                        <div class="tile tile-detail">                            
                            <header>{{selected?.Name}} Workflow</header>
                            <div class="form-instructions">{{selectedWorkflow?.Description}}</div>
                            <p-dataTable [value]="selectedWorkflow?.Steps" selectionMode="single" [selection]="selectedWorkflowStep" (selectionChange)="selectedWorkflowStep=$event;workflowTypeStepChanged()">                    
                                <p-column field="ID" header="ID" [sortable]="true" [style]="{'width':'50px'}"></p-column>
                                <p-column field="Name" header="Name" [sortable]="true"></p-column>                                                                                               
                                <p-column field="Count" header="Count" [sortable]="true"></p-column>
                            </p-dataTable> 
                        </div>  
                    </div>
                    <div class="col s12 m12 l6">                                      
                        <div class="tile tile-detail">                            
                            <header>{{selected?.Name}} - {{selectedWorkflowStep?.Name}} Details</header>
                            <d3s-loading [isLoading]="isDetailsLoading"></d3s-loading>
                            <span *ngIf="!isDetailsLoading">
                                <div class="form-instructions">Items in the selected workflow step.</div>                                    
                                <p-dataTable #dt [rows]="defaultInitialItemsPerPage" paginator="true" pageLinks="3" [rowsPerPageOptions]="defaultPagingOptions" [(selection)]="selectedWorkflowStepItem" [value]="workflowStepDetails?.Data" selectionMode="single" scrollable="true" scrollWidth="100%" >                    
                                    <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                                    <p-column *ngFor="let column of workflowStepDetails?.Columns" [field]="column.datafield" [header]="column.text" [sortable]="column.sortable"  [style]="{'width':'200px'}">                                
                                    <template let-item="rowData" pTemplate type="body">                                    
                                        <span *ngIf="column.datafield != 'Artifact'">
                                            <span [ngSwitch]="columnDataType(column)">
                                                <span *ngSwitchCase="'date'">{{item[column.datafield] | date:'short'}}</span>
                                                <span *ngSwitchCase="'bool'">
                                                    <i *ngIf="item[column.datafield]" class="fa fa-check enabled" title="True"></i>
                                                    <i *ngIf="!item[column.datafield]" class="fa fa-times disabled" title="False"></i>
                                                </span>
                                                <span *ngSwitchDefault [innerHtml]="item[column.datafield]"></span>
                                            </span>
                                        </span>
                                        <span *ngIf="column.datafield == 'Artifact'">
                                            <span><d3s-tooltip [objectType]="'Artifact'" [objectId]="item.ArtifactID" [tooltipType]="'preview'">{{item.Artifact}}</d3s-tooltip></span>
                                        </span>
                                    </template>
                                </p-column>
                                </p-dataTable> 
                            </span>                            
                        </div> 
                    </div>
                    <div class="col s12 m12 l6">                                       
                        <div class="tile tile-detail" *ngIf="selectedWorkflowStepItem">                            
                            <header>Selected Workflow Details</header>
                            <d3s-workflow-detailed-view [workflowId]="selectedWorkflowStepItem.ID"></d3s-workflow-detailed-view>
                        </div>        
                    </div>
                </div>                
                `,
    providers: [ArtifactTypeService,WorkflowService],
})

export class ArtifactTypeWorkflowStatusComponent extends BaseComponent implements OnChanges {
    @Input() artifactType: ArtifactType;


    private workflowTypes: any[] = [{ Name: 'Certify Artifact', ID: WorkflowType.CertifyArtifact }, { Name: 'Propose New Artifact', ID: WorkflowType.SuggestNewArtifact }, { Name: 'Propose New Artifact(multi-approval)', ID: WorkflowType.SuggestNewArtifactMulti }];
    private selected: any;
        
    private workflowStats: ArtifactTypeWorkflowBreakdown[] = [];
    private selectedWorkflow: ArtifactTypeWorkflowBreakdown;

    private selectedWorkflowStep: WorkflowStepStatistic;

    private workflowStepDetails: DynamicGridResultsInData;
    private isDetailsLoading: boolean = false;

    private selectedWorkflowStepItem: any; //has to be any type dynamic fields

    private workflowColumn: Object;

    constructor(private workflowService: WorkflowService) {
        super();
    }
        
    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        if (this.artifactType != null && changes['artifactType']) {            
            this.load();
        }
    }
    

    private load() {
        this.isLoading = true;
        this.workflowService.getWorkflowStepBreakdownByArtifactType(this.artifactType.ID)
            .then(result => {
                this.workflowStats = _.sortBy(result, 'Name');
                this.selected = this.workflowTypes[0];
                this.workflowTypeChanged();
                this.generateChart();
                this.isLoading = false;
            });
    }

    private workflowTypeChanged() {
        let res = this.workflowStats.filter(x => x.ID == this.selected.ID);

        if (res.length > 0) {
            this.selectedWorkflow = res[0];

            if (this.selectedWorkflow.Steps && this.selectedWorkflow.Steps.length) {
                this.selectedWorkflowStep = this.selectedWorkflow.Steps[0];
                this.workflowTypeStepChanged();
            }
        }
    }

    private workflowTypeStepChanged() {
        this.isDetailsLoading = true;
        this.workflowService.getWorkflowsByArtifactTypeAndStep(this.artifactType.ID, this.selectedWorkflow.ID, this.selectedWorkflowStep.ID)
            .then(result => {                
                this.workflowStepDetails = result;
                if (this.workflowStepDetails.Data && this.workflowStepDetails.Data.length > 0)
                    this.selectedWorkflowStepItem = this.workflowStepDetails.Data[0];
                else
                    this.selectedWorkflowStepItem = null;
                this.isDetailsLoading = false;
            });
    }

    private columnDataType(column) : string {
        var fields = this.workflowStepDetails.Fields.filter(x => x.name == column.datafield);

        if (fields.length > 0)
            return fields[0].type;
        return 'string';
    }
    
    private generateChart() {
        
        this.workflowColumn =
            {
                chart: {
                    type: 'column'
                },
                title: {
                    text: null
                },
                subtitle: {
                    text: 'Click the column to see details for that workflow type'
                },
                xAxis: {
                    type: 'category'
                },
                yAxis: {
                    title: {
                        text: 'Number of Items'
                    }
                },
                legend: {
                    enabled: false
                },
                credits: {
                    enabled: false
                },
                plotOptions: {
                    series: {
                        borderWidth: 0,
                        dataLabels: {
                            enabled: true,                         
                        }
                    }
                },
                tooltip: {
                    headerFormat: '<span style="font-size:11px">Workflow Type</span><br>',
                    pointFormat: '<span style="color:{point.color}">{point.name}</span>: <b>{point.y}</b> items<br/>'
                },

                series: [{
                    name: 'Workflows',
                    colorByPoint: true,
                    data: this.workflowStats.map(x => ({
                        name: x.Name,                        
                        y: x.Steps.reduce(function (a, b) { return a + b.Count; }, 0),
                        id: x.ID
                        }))
                }]
            };
    }


    onSeriesClick(e) {        
        var selected = this.workflowTypes.filter(x => x.ID == e.originalEvent.point.id);
        
        if (selected.length > 0) {
            this.selected = selected[0];
            this.workflowTypeChanged();
        }        
    }
        
}