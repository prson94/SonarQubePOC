import { Input, Output, Component, OnInit, OnDestroy, EventEmitter, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { WorkflowService } from '../../services/workflow.service';
import { WorkflowFormField, WorkflowFormFieldType, BulkWorkflowFormModel } from '../../models/workflow.model';
import { WorkflowFormFieldsComponent } from "./workflow-form-fields.component";
import { D3SObjectHelpers } from '../../static/d3s-object-helpers';
import { map } from 'rxjs/operators';
import { MessagesObservableService } from '../../services/messages-observable.service';

@Component({
    selector: 'd3s-workflow-bulk-form',
    templateUrl: "workflow-bulk-form.component.html",
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

    @ViewChild('fieldsComponent', { static: false }) fieldsComponent: WorkflowFormFieldsComponent

    constructor(private route: ActivatedRoute,
            protected headerBreadcrumbService: HeaderBreadcrumbService,
            protected workflowService: WorkflowService,
            protected messagesService: MessagesObservableService
        )
    {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearCurrentObjectInfo();
        this.load();
    }

    
    ngOnDestroy() {
    }

    private onSubmit() {
        if (this.fieldsComponent.setValidators()) {
            return false;
        }
        this.fieldsComponent.prepareValuesForSubmit();

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
            .pipe(
                map((res) => {
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
                }), map(() => {
                    window.setTimeout(() => {
                        this.fieldsComponent.setValidators();
                    }, 500);
                })).subscribe(() => { }, (error) => {
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