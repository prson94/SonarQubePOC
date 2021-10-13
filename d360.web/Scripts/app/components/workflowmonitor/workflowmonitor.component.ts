import { Component, OnInit, Input, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { GridFilterExpression } from '../../models/grid-definition.model';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-workflow-monitor',
    template: ` 
<div class="row">
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div class="col s6">
        <div class="tile tile-detail" *ngIf="!isLoading">
            <d3s-workflowmonitor-list (selectionChange)="listChange($event)" [predefinedFilters]="predefinedFilters"></d3s-workflowmonitor-list>  
        </div>
    </div>
    <div class="col s6">
        <div class="tile tile-detail">
            <d3s-workflow-monitor-step-list [itemId]="itemId" (selectionChange)="stepChange($event)"></d3s-workflow-monitor-step-list>
        </div>
        <div class="tile tile-detail" [hidden]="!detailVisible">
            <d3s-workflow-monitor-step-details [itemStepId]="itemStepId" [(visible)]="detailVisible"></d3s-workflow-monitor-step-details>
        </div>
    </div>
</div>
              `,
    providers: []
})

export class WorkflowMonitorComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() predefinedFilters: GridFilterExpression[] = [];

    itemStepId: number = null;
    itemId: number = null;
    detailVisible = false;

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected settingsService: CompanySettingsService,
        protected router: Router,
        protected route: ActivatedRoute,        
        secondaryNavService: SecondaryNavService) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
    }
    ngOnInit() {
        if (!this.predefinedFilters || this.predefinedFilters.length < 1)
            this.clearSidebar();        
    }

    ngOnDestroy() {
        if (!this.predefinedFilters || this.predefinedFilters.length < 1)
            this.clearSidebar();     
    }    

    listChange($event) {
        if ($event) {
            this.itemId = $event.Id
        } else {
            this.itemId = null;
            this.detailVisible = false;
        }
    }

    stepChange($event) {
        if ($event) {
            this.itemStepId = $event.ID;
            this.detailVisible = true;
        } else {
            this.itemStepId = null;
            this.detailVisible = false;
        }

    }
}