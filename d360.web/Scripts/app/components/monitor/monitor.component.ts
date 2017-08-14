import { Component, OnInit, OnChanges, Input, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ObjectDetailService } from '../../services/object-detail.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-monitor',
    template: ` 
<div class="row">
    <div class="col s12 m6">
        <d3s-monitor-filter [hidden]="isFiltered" (selectionChange)="selectedWorkflowTypes = $event" [selectAll]="selectAll"></d3s-monitor-filter>
        
        <d3s-monitor-workflow-actions *ngIf="isFiltered" [workflowTypes]="filteredTypes" [objectId]="objectId" [objectType]="objectType"></d3s-monitor-workflow-actions>
        
        <d3s-monitor-workflow 
                [workflowTypes]="selectedWorkflowTypes" 
                (selectionChange)="selectedWorkflowType = $event" 
                [objectType]="objectType" 
                [objectId]="objectId" 
                (filteredTypes)="filteredTypes = $event">
        </d3s-monitor-workflow>

        <d3s-monitor-workflow-actions *ngIf="!isFiltered" [workflowTypes]="filteredTypes" [objectId]="objectId" [objectType]="objectType"></d3s-monitor-workflow-actions>
    </div>
    <div class="col s12 m6">
        <d3s-workflow-diagram 
            [id]="selectedWorkflowType?.TypeID" 
            [version]="selectedWorkflowType?.Version" 
            [readonly]="true" 
            [hasHeader]="false"
            [selectedStepId]="selectedWorkflowItem?.VersionStepID"
            [monitorView]="true">
        </d3s-workflow-diagram>
    </div>
</div>
              `,
    providers: [ ObjectDetailService ]
})

export class MonitorComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() objectType: string;
    @Input() objectId: number;
    selectedWorkflowTypes: any[];
    selectedWorkflowType: any;
    selectedWorkflowItem: any;
    selectedWorkflowItemDetail: any;
    selectAll: boolean = true;
    isFiltered: boolean = false;
    filteredTypes: any[];

    sub: any;
    type: number;


    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected router: Router,
        protected route: ActivatedRoute,
        private objectDetailService: ObjectDetailService) {
        super();
    }

    ngOnInit() {
        this.isLoading = true;
        this.sub = this.route.params.subscribe(params => {
            if (params['id'] != null) {
                this.selectedWorkflowTypes = [];
                this.selectedWorkflowTypes.push(+params['id']);
                this.selectAll = false;
            }
        });

        if (this.objectType != null && this.objectId != null && this.selectedWorkflowTypes == null) {
            this.selectAll = true;
            this.isFiltered = true;
        }

        this.setBrowserTitle(this.titleService, 'Workflow Monitor');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Workflow Monitor', SiteUrlHelpers.SITE_URL_MONITOR_ROOT));
        if (this.isFiltered) {
            this.objectDetailService.getObject(this.objectId, this.objectType)
                .then(detail => {
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(detail.Name || '', null, true));
                });
        }

        this.isLoading = false;
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
}