///<reference path="../../../../node_modules/typings/index.d.ts"/>  

import { Input, Component, EventEmitter, Output } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { ResourcesService, WorkflowService } from '../../services/index';
import { Issue } from '../../models/workflow.model';
import { Resource } from '../../models/resource.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-issue-editor',
    template: ` 
                <form (ngSubmit)="onSubmit()" #issueEditorForm="ngForm">
                <header>Work Issue</header>
                <div id="FormDescription" class="form-instructions">The workflow that is triggered when an issue is reported.  The owner is assigned as a potential resource to work on the issue. They must still choose to work the issue.</div>                
                <div class="row">                    
                    <div class="col s12 l6">
                        <div class="FieldName">Issue</div>
                        <div id="IssueValue" [innerHtml]="issue?.Issue"></div>
                        <div class="FieldName">Requestor</div>
                        <div id="RequestorValue">{{issue?.ResourceName}}</div>
                        <div class="FieldName">Date</div>
                        <div id="DateValue">{{issue?.DateStarted | date: 'medium'}}</div>
                    </div>
                    <div class="col s12 l6">      
                        <div id="PoolMessage">
                            By clicking Save above, you are assigning yourself to this issue.  Please provide a comment below.  As you are working on this issue,
                            you may also comment on the issue where it is listed on your Board.
                        </div>
                        <div class="row">                                          
                            <div class="col s12 m4 l4" *ngIf="issue?.Activity == 3">
                                <input type="radio" id="assign" name="Action" [(ngModel)]="action" value="assign" checked="checked" />
                                <label for="assign">Accept Assignment</label>
                            </div>
                            <div class="col s12 m4 l4" *ngIf="issue?.Activity != 3">
                                <input type="radio" id="reassign" [(ngModel)]="action" name="Action" value="reassign" />
                                <label for="reassign">Re-assign</label>
                                <div class="FieldName">Re-assign To:</div>
                                <select name="reassignTo" style="width:100%;" [(ngModel)]="assignToId" [disabled]="action != 'reassign'">
                                      <option></option>
                                      <option *ngFor="let p of resources" [value]="p.ID">{{p.FirstName}} {{p.LastName}}</option>
                                </select>
                            </div>
                            <div class="col s12 m4 l4" *ngIf="issue?.Activity != 3">
                                <input type="radio" id="close" [(ngModel)]="action" name="Action" value="close" />
                                <label for="close">Close</label>
                            </div>     
                        </div>
                        <div id="CommentArea">
                            <div class="FieldName">Comment</div>
                            <textarea name="Comment" [(ngModel)]="comments"></textarea>
                        </div>                                                                                           
                    </div>                    
                </div>
                <div class="row">
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="submit" [disabled]="!issueEditorForm.form.valid" style="width: '150px';" label="Save"></button>                            
                        <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: '150px';"></button>
                    </div>
                </div>
                </form>
                `,
    providers: [ResourcesService, WorkflowService],   
})

export class WorkflowIssueEditorComponent extends BaseComponent {    
    @Input() issue: Issue;
    
    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();

    private resources: Resource[] = [];
    private comments: string = "";
    private assignToId: string;
    private action: string = "assign";

    constructor(private resourcesService: ResourcesService, private workflowService: WorkflowService) { super(); }

    ngOnInit() {
        if (this.resources.length <= 0) {
            this.loadResources();
        }
    }

    loadResources() {
        this.isLoading = true;
        this.resourcesService.getResources()
            .then(res => {
                this.isLoading = false;
                this.resources = res;
            });
    }

    onSubmit() {
        this.isLoading = true;
        console.log(this.assignToId);
        this.workflowService.updateIssue(this.issue, this.action, this.comments, this.assignToId).then(
            res => {
                this.isLoading = false;
                this.saveClick.emit();
            });
    }
};