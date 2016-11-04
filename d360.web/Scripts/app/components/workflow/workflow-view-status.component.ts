import { Input, Component, OnInit, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RightSidebarService  } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';


@Component({
    selector: 'd3s-workflow-view-status',
    template: ` 
                <div class="tile tile-detail">
                    <header>Workflow Item Details</header>
                    <d3s-workflow-detailed-view [workflowId]="workflowId"></d3s-workflow-detailed-view>
                </div>
              `
})

export class WorkflowViewStatusComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private workflowId: number;

    constructor(
        private route: ActivatedRoute,        
        private router: Router,        
        rightSidebarService: RightSidebarService,
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService
    ) {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Workflow Item Status'));

        this.setBrowserTitle(this.titleService, 'Workflow Item Status');

        this.sub = this.route.params.subscribe(params => {
            this.workflowId = params['workflowId']; // (+) converts string 'id' to a number               
        });
    }
    
    ngOnDestroy() {
        this.sub.unsubscribe();
    }
};