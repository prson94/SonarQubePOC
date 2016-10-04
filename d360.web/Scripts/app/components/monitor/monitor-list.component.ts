
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-monitor-list',
    template: ` 
                <div class="tile tile-detail">
                    <header>Monitor
                        <d3s-tile-actions [hasAdd]="false" [hasFilterMode]="true" [(filterMode)]="showSimpleFilter" [hasExport]="true"></d3s-tile-actions>                            
                    </header>
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <span *ngIf="!isLoading">
                        <input #gb [hidden]="!showSimpleFilter" type="text" pInputText size="100" placeholder="Search..." style="margin-bottom:10px;width:100%;">
                        <p-dataTable [globalFilter]="gb" [value]="issues" selectionMode="single" [(selection)]="selected" scrollable="true" scrollWidth="100%" [rows]="20" [paginator]="true" [pageLinks]="4" [rowsPerPageOptions]="[5,10,20]" [responsive]="true" [stacked]="stacked">                    
                            <p-column field="Issue" header="Issue" [sortable]="true" [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <span [innerHtml]="item.Issue"></span>
                                </template>
                            </p-column>
                            <p-column field="Name" header="Name" [sortable]="true" [filter]="!showSimpleFilter">
                                <template let-col let-item="rowData" pTemplate type="body">
                                    <d3s-tooltip [objectType]="'Artifact'" [objectId]="item.ObjectID" [tooltipType]="'Preview'">{{item.Name}}</d3s-tooltip>
                                </template>
                            </p-column>
                        </p-dataTable>        
                    </span>
                </div>
              `
})

export class MonitorListComponent extends BaseComponent implements OnInit {

    private issues: any[] = [];
    private selected: any;

    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Monitor');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Monitor'));
    }
};