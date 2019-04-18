import { Input, Output, Component, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Title } from '@angular/platform-browser';
import { WorkflowService } from '../../services/workflow.service';
import { EventEmitter } from '@angular/core';
import { BulkWorkflowFormModel } from '../../models/workflow.model';
import { ResourcesService } from '../../services/resources.service';
import { FormEvents, FormHelper } from '../../models/form.model';
import { EditorField } from '../../models/editor-field.model';

@Component({
    selector: 'd3s-workflow-bulk-reassign',
    templateUrl: 'workflow-bulk-reassign.component.html',
    providers: [WorkflowService, ResourcesService]
})

export class WorkflowBulkReassignComponent extends BaseComponent implements OnInit, OnDestroy {
    @Output() onClose = new EventEmitter();
    @Output() onComplete = new EventEmitter();
    @Input() model: BulkWorkflowFormModel = null;
    @Input() title: string = 'Form Reassignment';
    field: EditorField;
    selectedResourceName: string = '';
    sendFormEmails: boolean = false;


    constructor(
        protected titleService: Title,
        protected headerBreadcrumbService: HeaderBreadcrumbService,
        protected workflowService: WorkflowService
    ) {
        super();
    }

    ngOnInit() {
        this.field = new EditorField();
        this.field.TypeaheadUri = `services/workflow/resources?excludedResourceId=${this.model.AsigneeResourceID}`;
        this.field.FieldName = "resources";
        this.field.MultiSelect = false;
    }

    ngOnDestroy() {

    }

    private set fieldValue(value) {
        this.field.Value = value;
        if (this.field.Value != null && this.field.Value.length > 0) {
            this.selectedResourceName = this.field.Value[0].split('|')[2];
        }
    }

    save() {

    }

    valid() {
        let valid = true;

        if (this.field.Value == null || this.field.Value.length < 1)
            valid = false;

        return valid;
    }
}