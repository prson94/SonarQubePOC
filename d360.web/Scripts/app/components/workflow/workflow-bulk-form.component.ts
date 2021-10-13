import { Input, Output, Component, OnInit, OnDestroy, EventEmitter } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { NgForm, FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowFormField, WorkflowFormFieldType, BulkWorkflowFormModel } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Tag } from '../../models/tag.model';
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';
import { TagService } from '../../services/tag.service';
import { ResourcesService } from '../../services/resources.service';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-workflow-bulk-form',
    template: `                 
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div class="row" *ngIf="!isLoading">
                        <div class="col s12">
                            <div class="tile tile-detail" *ngIf="!isCompleted && hasItems && !isSubmitting">                        
                                <header>{{title}}</header>
                                <div class="row" style="margin-bottom: 5px">
                                    <div class="col l4 m6 s12">
                                           <span class="FieldName FieldDisplayName">Workflow:&nbsp;</span>
                                            <span class="FieldDisplayContent">{{workflowName}}</span>
                                    </div>
                                </div>
                                <div class="row" style="margin-bottom: 5px">
                                <div class="col l2 m4 s12">
                                           <span class="FieldName FieldDisplayName">Version:&nbsp;</span>
                                            <span class="FieldDisplayContent">{{version}}</span>
                                    </div>
                                </div>
                                <div class="row" style="margin-bottom: 5px">
                                    <div class="col l4 m6 s12">
                                           <span class="FieldName FieldDisplayName">Object:&nbsp;</span>
                                            <span class="FieldDisplayContent">{{objName}}</span>
                                    </div>
                                </div>
                                <div class="row" style="margin-bottom: 5px">
                                    <div class="col l4 m6 s12">
                                            <span class="FieldName FieldDisplayName">Type:&nbsp;</span>
                                            <span class="FieldDisplayContent">{{typeName}}</span>
                                    </div>
                                </div>
                                <div class="form-instructions">Please complete this form for the {{itemSteps?.length}} selected items</div>
                                <div *ngIf="omittedCount > 0" class="form-instructions">{{omittedCount}} items were excluded because they were deleted, already completed, or you do not have permission to complete them.</div>
                                <div [innerHtml]="description"></div>                                
                                <form (ngSubmit)="onSubmit()" #workflowForm="ngForm">                           
                                    <div class="row">   
                                        <div class="col l6 s12">
                                            <div *ngFor="let field of fields;let indx=index" class="row">                                            
                                                <div [ngSwitch]="field.FieldType" class="col s12">
                                                    <div class="FieldName" [innerHtml]="field.Label"></div>
                                                    <input *ngSwitchCase="fieldType.Text" [name]="'input_'+indx" style="width: 100%;" type="string" [(ngModel)]="field.Value" >  
                                                    <span *ngSwitchCase="fieldType.Boolean">&nbsp;&nbsp;
                                                        <label><input [name]="'input_'+indx" type="radio" [value]="true" required [(ngModel)]="field.Value" #f="ngModel" >&nbsp;Yes</label>&nbsp;
                                                        <label><input [name]="'input_'+indx" type="radio" [value]="false" required [(ngModel)]="field.Value" #f="ngModel" >&nbsp;No</label>
                                                        <span *ngIf="f.errors">
                                                          <span *ngIf="f.errors.required" class="error">&nbsp;&nbsp;&nbsp;* This field is required </span>                                                          
                                                        </span>
                                                    </span>                                                
                                                    <input *ngSwitchCase="fieldType.Integer" [name]="'input_'+indx" style="width: 100%;" type="number" [(ngModel)]="field.Value" >  
                                                    <textarea *ngSwitchCase="fieldType.TextArea" [name]="'input_'+indx" style="width: 100%;" [(ngModel)]="field.Value" ></textarea>
                                                    <p-calendar *ngSwitchCase="fieldType.Date" [(ngModel)]="field.Value" [name]="'input_'+indx"></p-calendar>
                                                    <div *ngSwitchCase="fieldType.List">
                                                        <select *ngIf="!field.AllowMultipleValues" [name]="'input_'+indx" style="height:auto;width:100%;" [(ngModel)]="field.Value">
                                                            <option></option>
                                                            <option *ngFor="let opt of field.Values" [value]="opt.Value">{{opt.Text}}</option>
                                                        </select>    
                                                        <p-multiSelect *ngIf="field.AllowMultipleValues" [name]="'input_'+indx" [(ngModel)]="field.Value" [options]="field.Values | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" selectedItemsLabel="{0} items selected" ngDefaultControl></p-multiSelect>
                                                    </div>
                                                    <div *ngSwitchCase="fieldType.RelationshipType">
                                                        <select *ngIf="!field.AllowMultipleValues" [name]="'input_'+indx" style="height:auto;width:100%;" [(ngModel)]="field.Value">
                                                            <option></option>
                                                            <option *ngFor="let opt of field.Values" [value]="opt.Value">{{opt.Text}}</option>
                                                        </select>    
                                                        <p-multiSelect *ngIf="field.AllowMultipleValues" [name]="'input_'+indx" [(ngModel)]="field.Value" [options]="field.Values | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" selectedItemsLabel="{0} items selected" ngDefaultControl></p-multiSelect>
                                                    </div>                                                
                                                </div>
                                                <div class="col s12">&nbsp;</div>                                                                                        
                                            </div>
                                        </div>
                                        <div class="col s12">&nbsp;</div>                                                                                        
                                        <div class="col s12">
                                                <button pButton type="submit" [disabled]="!workflowForm.valid" style="width: 150px;" label="Submit"></button>                                    
                                                <button pButton type="button" (click)="close()" label="Cancel" style="width: 150px;"></button>
                                        </div>
                                    </div>                                       
                                </form>                                                                                     
                            </div>
                            <div *ngIf="isCompleted && !isSubmitting" class="tile tile-detail">
                                <header>{{title}}</header>
                                <div class="row">
                                    <div class="col s12">Thank you, your responses have been submitted and are being processed.</div>
                                    <div *ngIf="omittedCount > 0" class="col s12">{{omittedCount}} items were omitted because they were deleted, already completed, or you do not have permission to complete them.</div>
                                    <div class="col s12">&nbsp;</div>
                                    <div class="col s12">
                                        <button pButton type="button" (click)="complete();" label="Close" style="width: 150px;"></button>
                                    </div>
                                </div>
                            </div>   
                            <div *ngIf="!isCompleted && !hasItems" class="tile tile-detail">
                                <header>{{title}}</header>
                                <div class="row">
                                    <div class="col s12">All the selected items have been omitted because they were deleted, already completed, or you do not have permission to complete them.</div>
                                    <div class="col s12">&nbsp;</div>
                                    <div class="col s12">
                                        <button pButton type="button" (click)="close();" label="Close" style="width: 150px;"></button>
                                    </div>
                                </div>
                            </div>  
                            <div *ngIf="isSubmitting" class="tile tile-detail">
                                <header>{{title}}</header>
                                <div class="row">
                                    <div class="col s12">Processing, please wait...</div>
                                    <d3s-loading isLoading="true"></d3s-loading>
                                </div>
                            </div>  
                        </div>
                    </div>                                               
                `,
    providers: [WorkflowService]
})

export class WorkflowBulkFormComponent extends BaseComponent implements OnInit, OnDestroy { 
    @Input() model: BulkWorkflowFormModel = null;
    @Output() onClose = new EventEmitter();
    @Output() onComplete = new EventEmitter();

    private workflowId: number;
    private workflowItemStepId: number;
    private workflowItemId: number;
    private fields: WorkflowFormField[] = [];
    private description: string;
    private title: string;
    private issueObject: string;
    private issueObjectName: string;
    private issueObjectID: number;
    private issueTypeName: string;
    private itemSteps: any[] = [];
    private omittedCount: number = 0;
    private workflowName: string;
    private version: number = 0;
    private objName: string;
    private typeName: string;
    
    fieldType = WorkflowFormFieldType;
    private isCompleted: boolean = false;
    private hasItems: boolean = false;
    private isUserAllowedToComplete: boolean = true;
    private isSubmitting = false;

    constructor(
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        protected workflowService: WorkflowService,
        private route: ActivatedRoute
    ) {
        super(settingsService);
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.load();
    }

    
    ngOnDestroy() {
    }

    private onSubmit() {
        for (var i = 0; i < this.fields.length;i++) {
            if (Array.isArray(this.fields[i].Value)) {
                this.fields[i].Value = this.fields[i].Value.join();
            }            
        }

        this.model.Fields = this.fields;
        this.isSubmitting = true;
        this.workflowService.submitBulkWorkflowForm(this.model)
            .pipe(
                map(r => {
                this.isCompleted = true;
                if (r && r.omittedCount)
                    this.omittedCount = r.omittedCount;
                }),
                map(() => setTimeout(() => this.isSubmitting = false, 5000)))
            .subscribe(); //pause for 5 seconds to ensure user sees processing message
        
    }

    private load() {

        this.isLoading = true;
        this.workflowService.getWorkflowBulkForm(this.model)
            .subscribe(res => {
                //console.log(res);

                this.title = res.Title;
                this.description = res.Description;
                this.fields = res.Fields;
                this.objName = res.ObjectName;
                this.workflowName = res.WorkflowName;
                this.typeName = res.TypeName;
                this.version = res.Version;

                this.issueObject = res.IssueObject;
                this.issueObjectID = res.IssueObjectID;
                this.issueObjectName = res.IssueObjectName;
                this.issueTypeName = res.IssueTypeName;

                this.itemSteps = res.ItemStepIDs;
                this.omittedCount = res.OmittedCount;
                this.hasItems = this.itemSteps == null ? false : this.itemSteps.length > 0;
                if (this.hasItems)
                    this.model.ItemStepIDs = this.itemSteps;
                else
                    this.model.ItemStepIDs = null;

                this.isLoading = false;

            });
    }

    private close() {
        this.onClose.emit();
    }

    private complete() {
        this.onComplete.emit();
    }

    private userFriendlyObjectName(objectType: string) {
        return D3SObjectHelpers.getObjectTypeFriendlyName(objectType);
    }
}