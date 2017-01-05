import { Component, Input, Output, EventEmitter, OnInit} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { CertifyItem } from '../../models/workflow.model';


declare var CurrentResourceID;

@Component({
    selector: 'd3s-workflow-certify-details',
    template: `     
            <d3s-workflow-certify-editor *ngIf="!isLoading && showEditor" [certify]="selected" (saveClick)="handleSave();" (closeClick)="showEditor=false"></d3s-workflow-certify-editor>       
            <div class="row" *ngIf="!isLoading && !showEditor">
                <header>Open Artifact Certifications<d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter"></d3s-tile-actions></header>
                <div class="col s12">     
                    <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." class="grid-simple-filter">                                  
                    <p-dataTable #dt [globalFilter]="gb" scrollable="true" scrollWidth="100%" [rows]="defaultInitialItemsPerPage" [rowsPerPageOptions]="defaultPagingOptions" [value]="items" selectionMode="single" paginator="true" pageLinks="3" [(selection)]="selected"  (onRowDblclick)="selected=$event.data;handleRowDblClick();" >
                        <footer *ngIf="dt.totalRecords"><d3s-grid-paging-info [totalRecords]="dt.totalRecords" [first]="dt.first" [rows]="dt.rows"></d3s-grid-paging-info></footer>
                        <p-column field="ActivityName" header="Status" sortable="true" [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                            <template let-col let-data="rowData" pTemplate type="body">
                                <span *ngIf="data.Activity <= 0 || !isMe">{{data.ActivityName}}</span>
                                <a *ngIf="data.Activity > 0 && isMe" (click)="selected=data;showEditor=true">{{data.ActivityName}}</a>
                            </template>
                        </p-column>
                        <p-column field="TypeName" header="Type Name" [sortable]="true" [style]="{'width':'250px'}" [filter]="!showSimpleFilter"></p-column>
                        <p-column field="Name" header="Name" sortable="true" [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                            <template let-col let-item="rowData" pTemplate type="body">
                                <d3s-tooltip objectType="Artifact" [objectId]="item.ID" tooltipType="preview">{{item.Name}}</d3s-tooltip>                                
                            </template>
                        </p-column>                                                                        
                        <p-column field="StartDate" header="Created" sortable="true" [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                            <template let-col let-data="rowData" pTemplate type="body">
                                <span>{{data.StartDate | date: 'medium'}}</span>
                            </template>
                        </p-column>
                        <p-column field="DueDate" header="Due" sortable="true" [style]="{'width':'250px'}" [filter]="!showSimpleFilter">
                            <template let-col let-data="rowData" pTemplate type="body">
                                <span>{{data.DueDate | date: 'medium'}}</span>
                            </template>
                        </p-column>                        
                        <p-column  *ngIf="hasCertifyButton" [style]="{width:'40px'}">
                            <template let-item="rowData" pTemplate type="body">
                                <div class="RowTools" *ngIf="item.Activity > 0 && isMe">                                
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
    private isMe: boolean = false;

    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;
    @Input() resourceID: number = null;
    @Input() hasCloseButton: boolean = true;
    @Input() hasCertifyButton: boolean = true;

    @Output() close = new EventEmitter();

    
    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.loadCertifications();
    }

    private loadCertifications() {
        this.isLoading = true;
        if (this.resourceID != null && !isNaN(this.resourceID)) {
            this.isMe = (this.resourceID == (CurrentResourceID || 0));
            
            this.workflowService.getCertifyItemsForUser(this.resourceID)
                .then(result => {
                    this.items = result;
                    if (this.items.length && this.items.length > 0) this.selected = this.items[0];
                    this.isLoading = false;
                });
        } else {
            this.isMe = true;

            this.workflowService.getCertifyItems(this.objectID, this.objectType)
                .then(result => {
                    this.items = result;
                    if (this.items.length && this.items.length > 0) this.selected = this.items[0];
                    this.isLoading = false;
                });
        }

    }

    private handleSave() {
        this.showEditor = false;
        this.loadCertifications();        
    }

    private handleRowDblClick() {
        if (this.selected.Activity > 0) this.showEditor = true;
    }
}