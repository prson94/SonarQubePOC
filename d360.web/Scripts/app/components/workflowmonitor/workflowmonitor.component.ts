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
    Workflow Monitor Is Awsome!
</div>
              `,
    providers: []
})

export class WorkflowMonitorComponent extends BaseComponent implements OnInit, OnDestroy {
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
}