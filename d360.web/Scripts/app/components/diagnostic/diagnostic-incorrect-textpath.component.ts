///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { HeaderBreadcrumbService, PageHeader, DiagnosticService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { BaseComponent } from '../shared/base.component';
import { DiagnosticInvalidTextPath } from '../../models/diagnostic.model';

@Component({
    selector: 'd3s-diagnostic-incorrect-textpath',
    template: `                 
                <div class="tile tile-detail">                                              
                    <header *ngIf="!isLoading">Objects with Invalid Textpaths values</header>           
                    <div *ngIf="isLoading" style="width:100%; text-align:center;">
                        <div style="padding:10px;"><i class="fa fa-spinner fa-spin fa-2x"></i></div>
                    </div>    
                    <p-dataTable *ngIf="!isLoading" [value]="items" selectionMode="single" [rows]="20" [paginator]="true" [pageLinks]="4" [(selection)]="selected">                                                                       
                        <p-column field="object" header="Object Type" [sortable]="true" [filter]="true"></p-column>
                        <p-column field="objectid" header="Object ID" [sortable]="true" [filter]="true"></p-column>
                        <p-column field="name" header="Name" [sortable]="true" [filter]="true"></p-column>                    
                        <p-column field="textpath" header="Current textpath" [sortable]="true" [filter]="true"></p-column>
                        <p-column field="correctTextpath" header="Correct textpath" [sortable]="true" [filter]="true"></p-column>
                    </p-dataTable>                           
                </div>
                `,
    providers: [DiagnosticService]
})

export class DiagnosticIncorrectTextpathComponent extends BaseComponent implements OnInit, OnDestroy {
    items: DiagnosticInvalidTextPath[];
    constructor(private pageHeader: PageHeader,
        private titleService: Title,
        private diagnosticService: DiagnosticService,
        private headerBreadcrumbService: HeaderBreadcrumbService) {
        super();        
        pageHeader.description = "";
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Diagnostics", null, false));        
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Invalid Textpaths", null, true));        
        this.setBrowserTitle(this.titleService, "Diagnostic Invalid Textpaths");
        this.isLoading = true;
        this.diagnosticService.getObjectsWithInvalidTextpath()
            .then(result => {
                this.items = result;
                this.pageHeader.description = `Found ${this.items.length} items with invalid / incorrect textpaths.  There should be 0 items with incorrect textpaths.`;
                this.isLoading = false;
            });
    }

    ngOnDestroy() {
       
    }


};