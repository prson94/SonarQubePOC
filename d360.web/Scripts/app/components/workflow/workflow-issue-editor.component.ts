import { Input, Component, EventEmitter, Output } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { ResourcesService } from '../../services/resources.service';
import { MessagesService } from '../../services/messages.service';
import { WorkflowService } from '../../services/workflow.service';
import { Issue, IssueInfo } from '../../models/workflow.model';
import { Resource } from '../../models/resource.model';


@Component({
    selector: 'd3s-workflow-issue-editor',
    template: ` 
                <form (ngSubmit)="onSubmit()" #issueEditorForm="ngForm">
                <header>Work Action</header>
                <div id="FormDescription" class="form-instructions">The workflow that is triggered when an action is reported.  The owner is assigned as a potential resource to work on the action item. They must still choose to work the action item.</div>                
                <div class="row">                    
                    <div class="col s12 l6">                        
                        <div class="FieldName">Action Type</div>
                        <div>{{issue?.IssueTypeName}}</div>                                             
                        <div class="FieldName">Item Name</div>
                        <div><d3s-preview-tooltip [objectType]="issue.Object" [objectId]="issue.ObjectID">{{issue.ObjectName}}</d3s-preview-tooltip></div>
                        <div class="FieldName">Requestor</div>
                        <div>{{issue?.ResourceName}}</div>                        
                        <div class="FieldName">Date</div>
                        <div>{{issue?.DateStarted | date: 'fullDate'}}</div>
                        <ng-template ngFor let-field [ngForOf]="issueDetails?.Fields">
                            <div class="FieldName">{{field.FieldName}}</div>
                            <div [innerHtml]="field.Value"></div>
                        </ng-template>
                    </div>
                    <div class="col s12 l6">      
                        <div id="PoolMessage">
                            By clicking Save below, you are assigning yourself to this issue.  Please provide a comment below.  As you are working on this issue,
                            you may also comment on the issue where it is listed on your Board.
                        </div>
                        <div class="row">                                          
                            <div class="col s12 m4 l4" *ngIf="issue?.Activity == 3">
                                <label><input required type="radio" name="Action" [(ngModel)]="action" value="assign" checked="checked" />Accept Assignment</label>
                            </div>
                            <div class="col s12 m4 l4" *ngIf="issue?.Activity != 3">
                                <label><input required type="radio" [(ngModel)]="action" name="Action" value="reassign" />Re-assign</label>                                
                                <select name="reassignTo" style="width:100%;" [(ngModel)]="assignToId" [disabled]="action != 'reassign'">
                                      <option></option>
                                      <option *ngFor="let p of resources" [value]="p.ID">{{p.FirstName}} {{p.LastName}}</option>
                                </select>
                            </div>
                            <div class="col s12 m4 l4" *ngIf="issue?.Activity != 3">
                                <label><input required type="radio" [(ngModel)]="action" name="Action" value="close" checked="checked"/>Close</label>
                            </div>     
                        </div>
                        <div id="CommentArea">
                            <div class="FieldName">Comments</div>
                            <textarea name="Comment" [(ngModel)]="comments"></textarea>
                        </div>                                                                                           
                    </div>                    
                </div>
                <div class="row">
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="submit" [disabled]="!issueEditorForm.form.valid" label="Save"></button>                            
                        <button pButton type="button" (click)="closeClick.emit();" label="Cancel"></button>
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
    private action: string;
    private issueDetails: IssueInfo;

    constructor(private resourcesService: ResourcesService, private workflowService: WorkflowService, private messagesService: MessagesService) { super(); }

    ngOnInit() {
        if (this.resources.length <= 0) {
            this.loadResources();
        }
        if (this.issue.IssueID > 0) {            
            this.loadFields();
        }
    }

    loadResources() {
        this.isLoading = true;
        this.resourcesService.getResources()
            .subscribe(res => {
                this.isLoading = false;
                this.resources = res;
            });
    }

    loadFields() {
        //get the fields / values for this issue
        this.isLoading = true;
        this.workflowService.getIssueDetails(this.issue.IssueID)
            .subscribe(result => {
                this.issueDetails = result;
                this.isLoading = false;                
            });
    }

    onSubmit() {
        this.isLoading = true;        
        this.workflowService.updateIssue(this.issue, this.action, this.comments, this.assignToId).subscribe(
            res => {
                this.showMessageForResult(this.messagesService, res);
                this.isLoading = false;
                this.saveClick.emit();
            });
    }
};