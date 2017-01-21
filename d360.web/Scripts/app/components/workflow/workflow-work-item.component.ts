import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { WorkflowService  } from '../../services/workflow.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../services/right-sidebar.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowType } from '../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-work-item',    
    template: ` 
                <template [ngIf]="!isLoading">
                    <div class="tile tile-detail" [ngSwitch]="workflowType">
                        <d3s-workflow-issue-editor *ngSwitchCase="WorkflowType.WorkIssue" [issue]="issue" (closeClick)="close()" (saveClick)="save()"></d3s-workflow-issue-editor>
                        <d3s-workflow-certify-editor *ngSwitchCase="WorkflowType.CertifyArtifact" [certify]="issue" (closeClick)="close()" (saveClick)="save()"></d3s-workflow-certify-editor>
                        <d3s-workflow-suggest-editor *ngSwitchCase="WorkflowType.SuggestNewArtifact" [suggest]="issue" (closeClick)="close()" (saveClick)="save()"></d3s-workflow-suggest-editor>
                    </div>
                </template>
              `,
    providers: [WorkflowService],
})

export class WorkflowWorkItemComponent extends BaseComponent implements OnInit, OnDestroy {
    private sub: any;
    private issue: any;

    WorkflowType = WorkflowType;
    private workflowType: WorkflowType;
    

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
        
        this.sub = this.route.params.subscribe(params => {
            this.isLoading = true;
            let workflowId = params['workflowId']; // (+) converts string 'id' to a number  
            this.workflowType = +params['workflowType'];

            switch (this.workflowType) {
                case WorkflowType.WorkIssue:
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Work Action'));
                    this.setBrowserTitle(this.titleService, 'Work Action');
                    break;
                case WorkflowType.CertifyArtifact:
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Certify Artifact'));
                    this.setBrowserTitle(this.titleService, 'Certify Artifact');
                    break;
                case WorkflowType.SuggestNewArtifact:
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Suggest New Artifact'));
                    this.setBrowserTitle(this.titleService, 'Suggest New Artifact');
                    break;
            }

            
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