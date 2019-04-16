import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';
import { WorkflowService } from '../../services/workflow.service';

@Component({
    selector: 'd3s-workflow-bulk-reassign',
    templateUrl: 'workflow-bulk-reassign.component.html',
    providers: [WorkflowService]
})

export class WorkflowBulkReassignComponent extends BaseComponent implements OnInit, OnDestroy {

    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected workflowService: WorkflowService
    ) {
        super();
    }

    ngOnInit() {

    }

    ngOnDestroy() {

    }
}