///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/index';

@Component({
    selector: 'd3s-workflow-issue-details',
    template: `
            <d3s-workflow-issue-editor *ngIf="!isLoading && showEditor" [issue]="selected" (saveClick)="handleSave();" (closeClick)="showEditor=false"></d3s-workflow-issue-editor>
            <div class="row" *ngIf="!isLoading && issues.length > 0 && !showEditor">
                <header>Open Issues</header>
                <div class="col s12">                    
                    <p-dataTable scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="[5,10,20]" [value]="issues" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected" (onRowDblclick)="selected=$event.data;handleRowDblClick();" >
                        <p-column field="Issue" header="Issue" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-issue="rowData">
                                <span [innerHtml]="issue?.Issue"></span>
                            </template>
                        </p-column>
                        <p-column field="ResourceName" header="Reported By" [sortable]="true" [style]="{'width':'250px'}"></p-column>
                        <p-column field="DateStarted" header="Created" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-data="rowData">
                                <span>{{data.DateStarted | date: 'medium'}}</span>
                            </template>
                        </p-column>
                        <p-column field="ActivityName" header="Status" [sortable]="true" [style]="{'width':'250px'}"></p-column>
                        <p-column  [style]="{width:'40px'}">
                            <template let-issue="rowData">
                                <div class="RowTools" *ngIf="issue.Activity > 0">                                
                                    <a style="cursor:pointer;" (click)="showEditor=true"><i class="fa fa-check-circle-o"></i></a>                                    
                                </div>
                            </template>
                        </p-column>                            
                    </p-dataTable>   
                </div>
            </div>            
            <div style="min-height:100px" *ngIf="!isLoading && issues.length == 0">
                <h4>No issues currently exist for <b>{{objectName}}</b>.</h4>
            </div>
            
        `,
    providers: [WorkflowService]
})

export class WorkflowIssueDetailsComponent extends BaseComponent implements OnInit {
    private issues: any[] = [];
    private selected: any;
    private loaded: boolean = false;
    private showEditor: boolean = false;
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    @Output() countsChanged = new EventEmitter();

    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        if (!this.loaded)
            this.loadIssues();
    }

    private loadIssues() {
        this.isLoading = true;
        this.workflowService.getIssues(this.objectID, this.objectType)
            .then(result => {
                this.issues = result;
                if (this.issues.length && this.issues.length > 0) this.selected = this.issues[0];
                this.isLoading = false;
                this.loaded = true;
            });
    }

    private handleSave() {
        this.showEditor = false;
        this.loadIssues();
        this.countsChanged.emit({});
    }

    private handleRowDblClick() {
        if (this.selected.Activity > 0) this.showEditor = true;
    }
}