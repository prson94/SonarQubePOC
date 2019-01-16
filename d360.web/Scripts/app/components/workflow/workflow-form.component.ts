
import {debounceTime} from 'rxjs/operators';
import { Input, Component, OnInit, OnDestroy } from '@angular/core';
import { Location } from '@angular/common';
import { Router, ActivatedRoute }       from '@angular/router';
import { NgForm, FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowFormField, WorkflowFormFieldType } from '../../models/workflow.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { Tag } from '../../models/tag.model';
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';
import { TagService } from '../../services/tag.service';
import { ResourcesService } from '../../services/resources.service';
import { Resource } from '../../models/resource.model';
import { MessagesService } from '../../services/messages.service';
import { SubscriptionLike as ISubscription } from 'rxjs';
import { close } from 'fs';

@Component({
    selector: 'd3s-workflow-form',
    template: `                 
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div class="row" *ngIf="!isLoading">
                        <div class="col s12">
                            <div class="tile tile-detail" *ngIf="!isCompleted && isUserAllowedToComplete && !isItemDeleted">                        
                                <header>{{title}}</header>
                                <div class="col s12" *ngIf="objectType != 'Issue'">The following form is for the [<b>{{typeName}}</b>] named [<b>
                                    <d3s-preview-tooltip [objectType]="objectType" [objectId]="objectID">
                                        <a *ngIf="objectUrl != null" [routerLink]="objectUrl">{{objectName}}</a>
                                        <span *ngIf="objectUrl == null">{{objectName}}</span>
                                    </d3s-preview-tooltip></b>].  <span [innerHtml]="description"></span></div>
                                <div class="form-instructions" *ngIf="objectType == 'Issue'">The following form is for the [<b><d3s-preview-tooltip [objectType]="objectType" [objectId]="objectID">{{issueTypeName}}</d3s-preview-tooltip></b>] action raised on [<b><d3s-preview-tooltip [objectType]="issueObject" [objectId]="issueObjectID">{{issueObjectName}}</d3s-preview-tooltip></b>].  <span [innerHtml]="description"></span></div>
                                <form (ngSubmit)="onSubmit()" #workflowForm="ngForm">                           
                                    <div class="row">   
                                        <div class="l6 s12">
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
                                                        <p-multiSelect *ngIf="field.AllowMultipleValues" [name]="'input_'+indx" [(ngModel)]="field.Value" [options]="field.Values | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" ngDefaultControl></p-multiSelect>
                                                    </div>
                                                    <div *ngSwitchCase="fieldType.RelationshipType">
                                                        <select *ngIf="!field.AllowMultipleValues" [name]="'input_'+indx" style="height:auto;width:100%;" [(ngModel)]="field.Value">
                                                            <option></option>
                                                            <option *ngFor="let opt of field.Values" [value]="opt.Value">{{opt.Text}}</option>
                                                        </select>    
                                                        <p-multiSelect *ngIf="field.AllowMultipleValues" [name]="'input_'+indx" [(ngModel)]="field.Value" [options]="field.Values | dropdownItemToSelectItemPipe" [style]="{width:'100%'}" ngDefaultControl></p-multiSelect>
                                                    </div>                                                
                                                </div>
                                                <div class="col s12">&nbsp;</div>                                                                                        
                                            </div>
                                        </div>
                                        <div class="col s12" *ngIf="hasObjectReassign">
                                                <p-checkbox [(ngModel)]="isReassignEnabled" name="reassign" binary="true" label="Check here to re-assign this action."></p-checkbox>
                                        </div>
                                        <div class="col s12" *ngIf="isReassignEnabled">
                                            <div class="row">
                                                <div class="col l3 s4">
                                                    <div class="FieldName">Re-assign to:</div>
                                                    <select name="reassignType" [(ngModel)]="reassignType" style="height:auto;width:100%;">
                                                        <option *ngFor="let opt of reassignAvailableTypes" [value]="opt.value">{{opt.text}}</option>                                                        
                                                    </select>
                                                </div>
                                                <div class="col l3 s4" *ngIf="reassignType == 'object'">   
                                                    <div class="FieldName">Select object:</div>
                                                    <p-autoComplete size="100"                                                
                                                            scrollHeight="300px"
                                                            name="other"
                                                            appendTo="body"
                                                            [inputStyle]="{width:'100%'}"
                                                            [(ngModel)]="term" 
                                                            [suggestions]="terms" 
                                                            (completeMethod)="search($event)"                                                 
                                                            placeholder="Select an item"
                                                            field="TextPath"           
                                                            [forceSelection]="true"
                                                            (onSelect)="selectItem()">     
                                                        <ng-template let-item>
                                                           <span style="color:#999999;">{{userFriendlyObjectName(item.Object)}} - <span *ngIf="item.ObjectTypeName">{{item.ObjectTypeName}} -</span></span> {{item.TextPath}} <span *ngIf="item.GoverningDomain">({{item.GoverningDomain}})</span>
                                                      </ng-template>                  
                                                    </p-autoComplete>                                         
                                                </div>
                                                <div class="col l3 s4" *ngIf="reassignType == 'resource'"> 
                                                    <div class="FieldName">Select user:</div>
                                                    <select name="reassignResources" [(ngModel)]="selectedReassignResource" style="height:auto;width:100%;">
                                                        <option *ngFor="let opt of resources" [value]="opt.ResourceID">{{opt.FirstName}} {{opt.LastName}}</option>                                                        
                                                    </select>
                                                </div>
                                                <div class="col l1 s4" *ngIf="reassignType">
                                                    <div class="FieldName">&nbsp;</div>
                                                    <button pButton type="button" (click)="reassign()" [disabled]="selectedReassignResource == null && (term == null || term.ObjectID == null)" style="width: 150px;" label="Assign"></button>                                    
                                                </div>
                                            </div>                                            
                                        </div>
                                        <div class="col s12">&nbsp;</div>                                                                                        
                                        <div class="col s12">
                                                <button pButton type="submit" [disabled]="!workflowForm.valid || isReassignEnabled" style="width: 150px;" label="Submit"></button>                                    
                                                <button pButton *ngIf="hasCloseButton" type="button" (click)="close();" label="Cancel" style="width: 150px;"></button>
                                        </div>
                                    </div>                                       
                                </form>                                                                                     
                            </div>
                            <div *ngIf="isCompleted" class="tile tile-detail">
                                <header>{{title}}</header>
                                <div class="row">
                                    <div class="col s12">Thank you, your responses have been submitted.</div>
                                    <div class="col s12">&nbsp;</div>
                                    <div class="col s12">
                                        <button pButton *ngIf="hasCloseButton" type="button" (click)="close();" label="Close" style="width: 150px;"></button>
                                    </div>
                                </div>
                            </div>                           
                            <div *ngIf="!isUserAllowedToComplete && !isCompleted && !isItemDeleted" class="tile tile-detail">
                                <header>{{title}}</header>
                                <div class="row">
                                    <div class="col s12">You currently do not have access to complete this form.</div>
                                    <div class="col s12">&nbsp;</div>
                                    <div class="col s12">
                                        <button pButton *ngIf="hasCloseButton" type="button" (click)="close();" label="Close" style="width: 150px;"></button>
                                    </div>
                                </div>
                            </div>
                            <div *ngIf="isItemDeleted && !isCompleted" class="tile tile-detail">
                                <header>{{title}}</header>
                                <div class="row">
                                    <div class="col s12">The item for this form has been deleted.</div>
                                    <div class="col s12">&nbsp;</div>
                                    <div class="col s12">
                                        <button pButton *ngIf="hasCloseButton" type="button" (click)="close();" label="Close" style="width: 150px;"></button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>                                               
                `,
    providers: [WorkflowService, ResourcesService, TagService]
})

export class WorkflowFormComponent extends BaseComponent implements OnInit, OnDestroy {    
    private sub: any;
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
    private objectTypeID: number;
    private typeName: string;
    private hasObjectReassign: boolean = true;
    
    fieldType = WorkflowFormFieldType;
    private isCompleted: boolean = false;
    private isUserAllowedToComplete: boolean = false;
    private isItemDeleted: boolean = false;
    private isReassignEnabled: boolean = false;
    private reassignType: string;
    private reassignAvailableTypes = [];
    private term: Tag;
    private terms: Tag[] = [];
    private resources: Resource[] = [];

    private selectedReassignObjectId: number;
    private selectedReassignObjectType: string;
    private selectedReassignResource: number;
    private searchSub: ISubscription;
    @Input() hasCloseButton: boolean = true;

    constructor(private route: ActivatedRoute,
            private location: Location,
            private router: Router,
            protected titleService: Title,
            protected headerBreadcrumbService: HeaderBreadcrumbService,
            protected workflowService: WorkflowService,
            protected tagService: TagService,
            protected resourcesService: ResourcesService,
            protected messagesService: MessagesService
        )
    {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();

        this.sub = this.route.params.subscribe(params => {            
            this.workflowId = +params['workflowId'];
            this.workflowItemStepId = +params['stepId'];
            this.workflowItemId = +params['itemId'];
            if (!window.history || window.history.length <= 2) this.hasCloseButton = false;
            this.load();
        });
    }

    
    ngOnDestroy() {
        this.sub.unsubscribe();
        if (this.searchSub) this.searchSub.unsubscribe();
    }

    get objectUrl() {
        let path = SiteUrlHelpers.getObjectUrl(this.objectType, this.objectID, this.objectTypeID); 
        return path == null ? null : '/' + path; 
    }

    private onSubmit() {
        for (var i = 0; i < this.fields.length;i++) {
            if (Array.isArray(this.fields[i].Value)) {
                this.fields[i].Value = this.fields[i].Value.join();
            }            
        }
        //save form values with stepid and itemid
        this.workflowService.submitWorkflowForm(this.workflowItemId, this.workflowItemStepId, this.fields);

        this.isCompleted = true;
    }

    private load() {
        this.isLoading = true;
        this.workflowService.getWorkflowForm(this.workflowId, this.workflowItemStepId)
            .then(res => {                
                this.title = res.Title;
                this.description = res.Description;
                this.fields = res.Fields;
                this.isLoading = false;
                this.isCompleted = res.IsCompleted;
                this.isItemDeleted = res.IsItemDeleted;
                this.objectName = res.ObjectName;
                this.objectType = res.ObjectType;    
                this.objectID = res.ObjectID;    
                this.isUserAllowedToComplete = res.IsUserAllowedToComplete;
                this.issueObject = res.IssueObject;
                this.issueObjectID = res.IssueObjectID;
                this.issueObjectName = res.IssueObjectName;
                this.issueTypeName = res.IssueTypeName;
                this.objectTypeID = res.ObjectTypeID;
                this.typeName = res.TypeName;
                if (res.AllowReassignObject)
                    this.reassignAvailableTypes.push({ value: 'object', text: 'Object' });
                if (res.AllowReassignResource)
                {
                    this.reassignAvailableTypes.push({ value: 'resource', text: 'Resource' });
                    this.loadResources();
                }
                this.hasObjectReassign = (this.reassignAvailableTypes.length > 0);                
            }).catch(res => {
                this.isLoading = false;
                this.isCompleted = false;
                this.isItemDeleted = true;
                this.title = "Cannot find the requested item.";
            });
    }

    private close() {        
        if (window.history.length > 1) {
            this.location.back();
        }
        else {
            this.router.navigate([SiteUrlHelpers.SITE_URL_HOME_ROOT]);
        }
    }
    
    private reassign() {
        this.isLoading = true;
        if (this.reassignType == 'object') {
            this.workflowService.reassignObject(this.workflowItemId, this.workflowId, this.selectedReassignObjectId, this.selectedReassignObjectType, this.workflowItemStepId).then(result => {
                this.showMessageForResult(this.messagesService, result, 'Successfully Assigned');
                this.isLoading = false;
                this.isCompleted = true;
            });
        }
        else if (this.reassignType == 'resource') {
            this.workflowService.reassignUser(this.workflowItemStepId, this.selectedReassignResource).then(result => {
                this.showMessageForResult(this.messagesService, result, 'Successfully Assigned');
                this.isLoading = false;
                this.isCompleted = true;
            });
        }
        else {
            this.isLoading = false;
        }
    }

    private search(event) {
       this.searchSub= this.tagService.getTags(event.query,'Resource').pipe(
            debounceTime(400))
            .subscribe(data => {
            this.terms = data;
        });
    }

    private userFriendlyObjectName(objectType: string) {
        return D3SObjectHelpers.getObjectTypeFriendlyName(objectType);
    }

    private selectItem() {
        this.selectedReassignObjectType = this.term.Object;
        this.selectedReassignObjectId = this.term.ObjectID;
    }

    private loadResources() {
        this.resourcesService.getResources().then(result => {
            this.resources = result;
        });
    }
};