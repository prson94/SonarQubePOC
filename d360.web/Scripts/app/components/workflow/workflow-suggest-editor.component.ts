import { Input, Component, EventEmitter, Output } from '@angular/core';
import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { WorkflowService } from '../../services/workflow.service';
import { SuggestedItem } from '../../models/workflow.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-suggest-editor',
    template: ` 
                <form (ngSubmit)="onSubmit()" #suggestEditorForm="ngForm">
                <header>Suggest Artifact</header>
                <div id="FormDescription" class="form-instructions"></div>                
                <div class="row">                    
                    <div class="col s12 l6">
                        <div class="FieldName">Type</div>
                        <div><d3s-tooltip [objectType]="'ArtifactType'" [objectId]="suggest.ID" [tooltipType]="'preview'">{{suggest.Name}}</d3s-tooltip></div>
                        <div class="FieldName">Proposed Name</div>
                        <div [innerHtml]="suggest.ProposedName"></div>
                        <div *ngIf="suggest.ProposedDescription" class="FieldName">Proposed Description</div>
                        <div *ngIf="suggest.ProposedDescription" [innerHtml]="suggest.ProposedDescription"></div>
                        <div class="FieldName">Requestor</div>
                        <div><d3s-tooltip [objectType]="'Resource'" [objectId]="suggest.RequestingResourceID" [tooltipType]="'preview'">{{suggest.RequestingResourceName}}</d3s-tooltip></div>
                        <div class="FieldName">Subject Area</div>
                        <div>{{suggest.TaxonomyTypeName}}</div>
                        <div class="FieldName">Date</div>
                        <div id="DateValue">{{suggest?.StartDate | date: 'medium'}}</div>
                    </div>
                    <div class="col s12 l6">                                                                              
                        <div class="FieldName">Comments</div>
                        <textarea name="Comment" [(ngModel)]="comments"></textarea>
                    </div>                    
                </div>
                <div class="row">
                    <div class="col s12">&nbsp;</div>
                    <div class="col s12">
                        <button pButton type="submit" style="width: 150px;" label="Approve"></button>                            
                        <button pButton type="button" style="width: 150px;" label="Reject" (click)="reject();"></button>
                        <button pButton type="button" (click)="closeClick.emit();" label="Close" style="width: 150px;"></button>
                    </div>
                </div>
                </form>
                `,
    providers: [WorkflowService],
})

export class WorkflowSuggestEditorComponent extends BaseComponent {
    @Input() suggest: SuggestedItem;

    @Output() closeClick = new EventEmitter();
    @Output() saveClick = new EventEmitter();
        
    private comments: string = "";
    
    private action: string = "assign";

    constructor(private workflowService: WorkflowService) { super(); }

    private handleApproval(approved: boolean) {
        this.isLoading = true;
        this.workflowService.updateSuggestion(this.suggest, approved, this.comments).then(
            res => {
                this.isLoading = false;
                this.saveClick.emit();
            });
    }

    onSubmit() {
        this.handleApproval(true);
    }

    reject() {
        this.handleApproval(false);
    }
};