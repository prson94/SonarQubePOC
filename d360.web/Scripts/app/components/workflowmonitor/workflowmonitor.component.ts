import { Component, OnInit, OnChanges, Input, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { RightSidebarService } from '../../services/right-sidebar.service';

@Component({
    selector: 'd3s-workflow-monitor',
    template: ` 
<div class="row">
    <d3s-loading [isLoading]="isLoading"></d3s-loading>
    <div class="col s6" [hidden]="detailVisible">
        <div class="tile tile-detail" *ngIf="!isLoading">
            <d3s-workflowmonitor-list (selectionChange)="listChange($event)"></d3s-workflowmonitor-list>  
        </div>
    </div>
    <div class="col s6" [hidden]="!detailVisible">
        <d3s-workflow-monitor-step-details [versionStepId]="versionStepId" [(visible)]="detailVisible"></d3s-workflow-monitor-step-details>
    </div>
    <div class="col s6">
        <d3s-workflow-monitor-step-list [itemId]="itemId" (selectionChange)="stepChange($event)"></d3s-workflow-monitor-step-list>
    </div>
</div>
              `,
    providers: []
})

export class WorkflowMonitorComponent extends BaseComponent implements OnInit, OnDestroy {
    versionStepId: number = null;
    itemId: number = null;
    detailVisible = false;

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected router: Router,
        protected route: ActivatedRoute,        
        rightSidebarService: RightSidebarService) {
        super();
        this.rightSidebarService = rightSidebarService;
    }
    ngOnInit() {
        this.clearSidebar();        
    }

    ngOnDestroy() {
        this.clearSidebar();
        
    }    

    listChange($event) {
        this.itemId = $event.Id;
    }

    stepChange($event) {
        this.versionStepId = $event.data.StepID;
        this.detailVisible = true;
    }
}