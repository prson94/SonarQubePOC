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

@Component({
    selector: 'd3s-workflow-form',
    template: `                 
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div class="row" *ngIf="!isLoading">
                        <div class="col s12">
                            <div class="tile tile-detail" *ngIf="!isCompleted && isUserAllowedToComplete">                        
                                <header>{{title}}</header>
                                <div class="form-instructions" *ngIf="objectType != 'Issue'">The following form is for the [<b>{{typeName}}</b>] named [<b><d3s-tooltip [objectType]="objectType" [objectId]="objectID" tooltipType="preview"><a [routerLink]="objectUrl">{{objectName}}</a></d3s-tooltip></b>].  <span [innerHtml]="description"></span></div>                                            
                                <div class="form-instructions" *ngIf="objectType == 'Issue'">The following form is for the [<b><d3s-tooltip [objectType]="objectType" [objectId]="objectID" tooltipType="preview">{{issueTypeName}}</d3s-tooltip></b>] action raised on [<b><d3s-tooltip [objectType]="issueObject" [objectId]="issueObjectID" tooltipType="preview">{{issueObjectName}}</d3s-tooltip></b>].  <span [innerHtml]="description"></span></div>
                                <form (ngSubmit)="onSubmit()" #workflowForm="ngForm">                           
                                    <div class="row">
                                        <div *ngFor="let field of fields;let indx=index" class="row">
                                            <div [ngSwitch]="field.FieldType" class="col s12">
                                                <div class="FieldName" [innerHtml]="field.Label"></div>
                                                <input *ngSwitchCase="fieldType.Text" [name]="'input_'+indx" style="width: 100%;" type="string" [(ngModel)]="field.Value" >  
                                                <p-toggleButton onLabel="Yes" offLabel="No" onIcon="fa-check-square" offIcon="fa-times" *ngSwitchCase="fieldType.Boolean" [(ngModel)]="field.Value" [name]="'input_'+indx"></p-toggleButton>
                                                <input *ngSwitchCase="fieldType.Integer" [name]="'input_'+indx" style="width: 100%;" type="number" [(ngModel)]="field.Value" >  
                                                <textarea *ngSwitchCase="fieldType.TextArea" [name]="'input_'+indx" style="width: 100%;" [(ngModel)]="field.Value" ></textarea>
                                                <p-calendar *ngSwitchCase="fieldType.Date" [(ngModel)]="field.Value" [name]="'input_'+indx"></p-calendar>
                                                <div *ngSwitchCase="fieldType.List">
                                                    <select [name]="'input_'+indx" style="height:auto;width:100%;" [(ngModel)]="field.Value">
                                                        <option></option>
                                                        <option *ngFor="let opt of field.Values" [value]="opt.Value">{{opt.Text}}</option>
                                                    </select>                                                    
                                                </div>
                                            </div>
                                            <div class="col s12">&nbsp;</div>
                                        </div>                                        
                                        <div class="col s12">
                                                <button pButton type="submit" [disabled]="!workflowForm.valid" style="width: 150px;" label="Submit"></button>                                    
                                                <button pButton *ngIf="hasCloseButton" type="button" (click)="close();" label="Close" style="width: 150px;"></button>
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
                            <div *ngIf="!isUserAllowedToComplete" class="tile tile-detail">
                                <header>{{title}}</header>
                                <div class="row">
                                    <div class="col s12">You currently do not have access to complete this form.</div>
                                    <div class="col s12">&nbsp;</div>
                                    <div class="col s12">
                                        <button pButton *ngIf="hasCloseButton" type="button" (click)="close();" label="Close" style="width: 150px;"></button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>                                               
                `,
    providers: [WorkflowService]
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
    
    fieldType = WorkflowFormFieldType;
    private isCompleted: boolean = false;
    private isUserAllowedToComplete: boolean = false;

    @Input() hasCloseButton: boolean = true;

    constructor(private route: ActivatedRoute,
            private location: Location,
            private router: Router,
            protected titleService: Title,
            protected headerBreadcrumbService: HeaderBreadcrumbService,
            protected workflowService: WorkflowService
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
    }

    get objectUrl() {
        return '/' +SiteUrlHelpers.getObjectUrl(this.objectType, this.objectID, this.objectTypeID);
    }

    private onSubmit() {
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
            });
    }

    private close() {        
        this.location.back();
    }
};