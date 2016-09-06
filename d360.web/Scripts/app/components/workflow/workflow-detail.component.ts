///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { WorkflowType } from '../../models/workflow.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-detail',
    template: ` 
                <d3s-workflow-issue-details *ngIf="workflowType == tempWorkflowtype.WorkIssue" [hasCloseButton]="hasCloseButton" (close)="close.emit({});"></d3s-workflow-issue-details>                    
                <d3s-workflow-suggest-details *ngIf="workflowType == tempWorkflowtype.SuggestNewArtifact" [hasCloseButton]="hasCloseButton" (close)="close.emit({});"></d3s-workflow-suggest-details>                    
                <d3s-workflow-certify-details *ngIf="workflowType == tempWorkflowtype.CertifyArtifact" [hasCloseButton]="hasCloseButton" (close)="close.emit({});"></d3s-workflow-certify-details>                                  
                `,    
})

export class WorkflowDetailComponent extends BaseComponent implements OnInit {
    @Input() workflowType: WorkflowType;
    @Input() hasCloseButton: boolean = true;

    @Output() close = new EventEmitter();

    private tempWorkflowtype = WorkflowType;


    ngOnInit() {
        console.log(this.workflowType);
    }
    
};