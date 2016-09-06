///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { WorkflowType } from '../../models/workflow.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-detail',
    template: ` 
                <d3s-workflow-issue-details *ngIf="workflowType == tempWorkflowtype.WorkIssue"></d3s-workflow-issue-details>                    
                <d3s-workflow-suggest-details *ngIf="workflowType == tempWorkflowtype.SuggestNewArtifact"></d3s-workflow-suggest-details>                    
                <div style="padding:10px">
                    <button *ngIf="hasCloseButton" pButton type="button" (click)="close.emit();" label="Close" style="width: 150px;"></button>
                </div>                    
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