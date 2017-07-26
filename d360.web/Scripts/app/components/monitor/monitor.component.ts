import { Component, OnInit, OnChanges } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-monitor',
    template: ` 
<div class="row">
    <div class="col s12 m5">
        <d3s-monitor-filter (selectionChange)="selectedWorkflowTypes = $event"></d3s-monitor-filter>
        <d3s-monitor-workflow [workflowTypes]="selectedWorkflowTypes" (selectionChange)="selectedWorkflowType = $event"></d3s-monitor-workflow>
        <!--<d3s-monitor-workflow-item [workflowVersionID]="selectedWorkflowType?.Version" (selectionChange)="selectedWorkflowItem = $event"></d3s-monitor-workflow-item>-->
    </div>
    <div class="col s12 m7">
        <d3s-workflow-diagram 
            [id]="selectedWorkflowType?.TypeID" 
            [version]="selectedWorkflowType?.Version" 
            [readonly]="true" 
            [hasHeader]="false"
            [selectedStepId]="selectedWorkflowItem?.VersionStepID">
        </d3s-workflow-diagram>
    </div>
</div>
              `
})

export class MonitorComponent extends BaseComponent implements OnInit {
    selectedWorkflowTypes: any[];
    selectedWorkflowType: any;
    selectedWorkflowItem: any;
    selectedWorkflowItemDetail: any;

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected router: Router) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Workflow Monitor');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Workflow Monitor', SiteUrlHelpers.SITE_URL_MONITOR_ROOT));
    }
}