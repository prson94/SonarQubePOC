///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/index';
import { SuggestedItem } from '../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-suggest-details',
    template: `            
            <div class="row" *ngIf="!isLoading">
                <header>Open Proposed New Artifacts</header>
                <div class="col s12">                    
                    <p-dataTable scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="[5,10,20]" [value]="items" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selected" >
                        <p-column field="Name" header="Type" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-item="rowData">
                                <d3s-tooltip [objectType]="'ArtifactType'" [objectId]="item.ID" [tooltipType]="'preview'">{{item.Name}}</d3s-tooltip>                                
                            </template>
                        </p-column>                        
                        <p-column field="RequestingResourceName" header="Requested By" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-item="rowData">
                                <d3s-tooltip [objectType]="'Resource'" [objectId]="item.RequestingResourceID" [tooltipType]="'preview'">{{item.RequestingResourceName}}</d3s-tooltip>                                
                            </template>
                        </p-column>
                        <p-column field="StartDate" header="Created" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-data="rowData">
                                <span>{{data.StartDate | date: 'medium'}}</span>
                            </template>
                        </p-column>
                        <p-column field="ProposedName" header="Proposed Name" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-item="rowData">
                                <span [innerHtml]="item?.ProposedName"></span>
                            </template>
                        </p-column>
                        <p-column field="TaxonomyTypeName" header="Subject Area" [sortable]="true" [style]="{'width':'250px'}"></p-column>
                        <p-column field="ActivityName" header="Status" [sortable]="true" [style]="{'width':'250px'}"></p-column>
                        <p-column  [style]="{width:'40px'}">
                            <template let-item="rowData">
                                <div class="RowTools" *ngIf="item.Activity > 0">                                
                                    <a style="cursor:pointer;" (click)="showEditor=true"><i class="fa fa-check-circle-o"></i></a>                                    
                                </div>
                            </template>
                        </p-column>                            
                    </p-dataTable>   
                </div>
            </div>                        
        `,
    providers: [WorkflowService]
})

export class WorkflowSuggestDetailsComponent extends BaseComponent implements OnInit {
    private items: SuggestedItem[] = [];
    private selected: SuggestedItem;

    private showEditor: boolean = false;
    
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    @Output() countsChanged = new EventEmitter();

    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.loadSuggestions();
    }

    private loadSuggestions() {
        this.isLoading = true;
        this.workflowService.getSuggestedItems(this.objectID, this.objectType)
            .then(result => {
                this.items = result;
                if (this.items.length && this.items.length > 0) this.selected = this.items[0];
                this.isLoading = false;                
            });
    }
    
}