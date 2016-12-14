import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService  } from '../../services/index';
import { WorkflowType } from '../../models/workflow.model';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-detail',
    template: ` 
                <div class="row">
                    <div class="col s12">
                        <div class="tile tile-detail">
                                <d3s-workflow-issue-details *ngIf="workflowType == tempWorkflowtype.WorkIssue" [hasCloseButton]="hasCloseButton" [hasCertifyButton]="hasCertifyButton" (close)="close();"></d3s-workflow-issue-details>                    
                                <d3s-workflow-suggest-details *ngIf="workflowType == tempWorkflowtype.SuggestNewArtifact" [hasCloseButton]="hasCloseButton" [hasCertifyButton]="hasCertifyButton" (close)="close();"></d3s-workflow-suggest-details>                    
                                <d3s-workflow-certify-details *ngIf="workflowType == tempWorkflowtype.CertifyArtifact" [hasCloseButton]="hasCloseButton" [hasCertifyButton]="hasCertifyButton" (close)="close();"></d3s-workflow-certify-details>                                  
                        </div>
                    </div>
                </div>                
                `,    
})

export class WorkflowDetailComponent extends BaseComponent implements OnInit, OnDestroy {
    @Input() workflowType: WorkflowType;
    @Input() hasCloseButton: boolean = true;
    @Input() hasCertifyButton: boolean = true;
    

    private sub: any;
    private tempWorkflowtype = WorkflowType;    

    constructor(private route: ActivatedRoute,
        private location: Location,
        private router: Router,        
        protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();

        this.sub = this.route.params.subscribe(params => {
            this.isLoading = true;            
            this.workflowType = +params['workflowType'];
            this.headerBreadcrumbService.clearBreadcrumbs();
            switch (this.workflowType) {
                case WorkflowType.WorkIssue:
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Issue List'));
                    this.setBrowserTitle(this.titleService, 'Issue List');
                    break;
                case WorkflowType.CertifyArtifact:
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Certify Artifact List'));
                    this.setBrowserTitle(this.titleService, 'Certify Artifact List');
                    break;
                case WorkflowType.SuggestNewArtifact:
                    this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Suggest New Artifact List'));
                    this.setBrowserTitle(this.titleService, 'Suggest New Artifact List');
                    break;
            }
        });
    }

    private close() {
        this.location.back();
    }

    ngOnDestroy() {
        this.sub.unsubscribe();
    }
    
};