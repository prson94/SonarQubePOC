import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RightSidebarService, WorkflowService  } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';


@Component({
    selector: 'd3s-workflow-work-issue',    
    template: ` 
                <div class="tile tile-detail">
                    <d3s-workflow-issue-editor [issue]="issue" (closeClick)="close()" (saveClick)="save()"></d3s-workflow-issue-editor>
                </div>
              `,
    providers: [WorkflowService],
})

export class WorkflowWorkIssueComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private issue: any;

    constructor(
        private route: ActivatedRoute,
        private location: Location,
        private router: Router,
        private workflowService: WorkflowService,
        rightSidebarService: RightSidebarService, protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Work Issue'));

        this.setBrowserTitle(this.titleService, 'Work Issue');       

        this.sub = this.route.params.subscribe(params => {
            let workflowId = params['workflowId']; // (+) converts string 'id' to a number   

            this.isLoading = true;
            this.workflowService.getWorkflowDetails(workflowId)
                .then(result => {                    
                    this.issue = result;
                                        
                    this.isLoading = false;
                });            
        });
    }


    ngOnDestroy() {
        this.sub.unsubscribe();
    }

    private save() {
        this.location.back();
    }

    private close() {        
        this.location.back();
    }
    
};