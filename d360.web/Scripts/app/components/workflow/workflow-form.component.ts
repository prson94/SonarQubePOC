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


@Component({
    selector: 'd3s-workflow-form',
    template: `                 
                    <d3s-loading [isLoading]="isLoading"></d3s-loading>
                    <div class="row" *ngIf="!isLoading">
                        <div class="col s12">
                            <div class="tile tile-detail" *ngIf="!isCompleted">                        
                                <header>{{title}}</header>
                                <div class="form-instructions">{{description}}</div>            
                                <form (ngSubmit)="onSubmit()" #workflowForm="ngForm">                           
                                    <div class="row">
                                        <div *ngFor="let field of fields;let indx=index" class="row">
                                            <div [ngSwitch]="field.FieldType" class="col s12">
                                                <div class="FieldName">{{field.Label}}</div>
                                                <input *ngSwitchCase="fieldType.Text" [name]="'input_'+indx" style="width: 100%;" type="string" [(ngModel)]="field.Value" >  
                                                <input *ngSwitchCase="fieldType.Boolean" type="checkbox" [(ngModel)]="field.Value" [name]="'input_'+indx"/> 
                                                <input *ngSwitchCase="fieldType.Integer" [name]="'input_'+indx" style="width: 100%;" type="number" [(ngModel)]="field.Value" >  
                                                <p-calendar *ngSwitchCase="fieldType.Date" [(ngModel)]="field.Value" [name]="'input_'+indx"></p-calendar>
                                            </div>
                                            <div class="col s12">&nbsp;</div>
                                        </div>                                        
                                        <div class="col s12">
                                                <button pButton type="submit" [disabled]="!workflowForm.valid" style="width: '150px';" label="Submit"></button>                                    
                                        </div>
                                    </div>                                        
                                </form>                                                                                     
                            </div>
                            <div *ngIf="isCompleted" class="tile tile-detail">
                                <header>{{title}}</header>
                                <div class="row">
                                    <div class="col s12">Thank you, your responses have been submitted.</div>
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
    fieldType = WorkflowFormFieldType;
    private isCompleted: boolean = false;

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
            this.load();
        });
    }

    
    ngOnDestroy() {
        this.sub.unsubscribe();
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
            });
    }
};