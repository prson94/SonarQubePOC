///<reference path="../../es6-shim.d.ts"/>
import {Component, Input, Output, EventEmitter, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/index';
import { CertifyItem } from '../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-certify-details',
    template: `     
            <d3s-workflow-certify-editor *ngIf="!isLoading && showEditor" [certify]="selected" (saveClick)="handleSave();" (closeClick)="showEditor=false"></d3s-workflow-certify-editor>       
            <div class="row" *ngIf="!isLoading && !showEditor">
                <header>Open Artifact Certifications</header>
                <div class="col s12">                    
                    <p-dataTable scrollable="true" scrollWidth="100%" [rowsPerPageOptions]="[5,10,20]" [value]="items" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;handleRowDblClick();" >
                        <p-column field="TypeName" header="Type Name" [sortable]="true" [style]="{'width':'250px'}"></p-column>
                        <p-column field="Name" header="Name" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-item="rowData">
                                <d3s-tooltip [objectType]="'Artifact'" [objectId]="item.ID" [tooltipType]="'preview'">{{item.Name}}</d3s-tooltip>                                
                            </template>
                        </p-column>                                                                        
                        <p-column field="StartDate" header="Created" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-data="rowData">
                                <span>{{data.StartDate | date: 'medium'}}</span>
                            </template>
                        </p-column>
                        <p-column field="DueDate" header="Due" [sortable]="true" [style]="{'width':'250px'}">
                            <template let-col let-data="rowData">
                                <span>{{data.DueDate | date: 'medium'}}</span>
                            </template>
                        </p-column>
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
                <div class="col s12">
                    <button *ngIf="hasCloseButton" pButton type="button" (click)="close.emit();" label="Close" style="width: 150px;"></button>
                </div>  
            </div>                        
        `,
    providers: [WorkflowService]
})

export class WorkflowCertifyDetailsComponent extends BaseComponent implements OnInit {
    private items: CertifyItem[] = [];
    private selected: CertifyItem;

    private showEditor: boolean = false;

    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() hasCloseButton: boolean = true;

    @Output() close = new EventEmitter();
    
    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.loadCertifications();
    }

    private loadCertifications() {
        this.isLoading = true;
        this.workflowService.getCertifyItems(this.objectID, this.objectType)
            .then(result => {
                this.items = result;
                if (this.items.length && this.items.length > 0) this.selected = this.items[0];
                this.isLoading = false;
            });
    }

    private handleSave() {
        this.showEditor = false;
        this.loadCertifications();        
    }

    private handleRowDblClick() {
        if (this.selected.Activity > 0) this.showEditor = true;
    }

}