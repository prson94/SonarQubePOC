///<reference path="../../es6-shim.d.ts"/>
import {Component, Input} from '@angular/core';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService, AuditService  } from '../../services/index';
import { Audit } from '../../models/audit.model';
import { DataTable, Column} from 'primeng/primeng';

@Component({
    selector: 'd3s-audit',
    directives: [DataTable, Column],
    providers: [AuditService],
    template: `
             <div *ngIf="isLoading" style="width:100%; text-align:center;">
                    <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                </div>
                <span *ngIf="!isLoading">Audit History for {{objectName}}</span>
               <p-dataTable *ngIf="!isLoading" [value]="audits" selectionMode="single" [rows]="10" [paginator]="true" [pageLinks]="3" expandableRows="true" [(selection)]="selected" >                                                                        
                    <p-column field="ResourceName" header="User" [sortable]="true" [filter]="true"></p-column>                                                            
                    <p-column field="Date" header="Date" [sortable]="true" [filter]="true"></p-column>
                    <p-column field="Action" header="Action" [sortable]="true" [filter]="true"></p-column>                                                            
                    <p-column field="ActionObjectTypeName" header="Type" [sortable]="true" [filter]="true"></p-column>
                    <p-column field="ActionObjectName" header="Item" [sortable]="true" [filter]="true"></p-column>
                    <p-column field="AuditDescription" header="Audit Description" [sortable]="true" [filter]="true"></p-column>                                                        
                </p-dataTable> 
        `    
})

export class AuditComponent {
    @Input() objectID: number = 0;
    @Input() objectType: string;
    @Input() objectName: string;

    audits: Audit[] = [];
    isLoading: boolean = false;
    selected: Audit;


    constructor(private auditService: AuditService, private headerBreadcrumbService: HeaderBreadcrumbService) {
      
    }

    ngOnInit() {        
        this.getData();
    }

    private getData() {
        this.isLoading = true;
        this.auditService.getAuditData(this.objectID, this.objectType)
            .then(result => {
                this.isLoading = false;
                this.audits = result;
            });
    }    
}