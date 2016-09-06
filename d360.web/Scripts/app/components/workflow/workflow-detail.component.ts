///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Input, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { WorkflowType } from '../../models/workflow.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-detail',
    template: ` 
                <d3s-object-issue-details *ngIf="workflowType == tempWorkflowtype.WorkIssue"></d3s-object-issue-details>
                `,    
})

export class WorkflowDetailComponent extends BaseComponent implements OnInit {
    @Input() workflowType: WorkflowType;

    private tempWorkflowtype = WorkflowType;


    ngOnInit() {
        
    }
    
};