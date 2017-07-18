import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import {
    NodeModel,
    WorkflowForm,
    WorkflowFormField,
    WorkflowFormFieldType,
    FormResponseType,
    EmailTaskRecipientType,
    EmailTaskRecipientTypeInfo,
} from '../../../../models/workflow.model';
import { FieldType } from '../../../../models/fields.model';
import { Column, Header, MenuItem, Editor } from 'primeng/primeng';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';
import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';
import { FormMode } from '../../../../models/form.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-step-form-editor',
    providers: [WorkflowService, ResponsibilityTypeService],
    templateUrl: './workflow-step-form-editor.component.html'
})

export class WorkflowStepFormEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() step: NodeModel;
    @Input() objectId: number;
    @Input() objectType: string;
    @Output() stepChange = new EventEmitter();
    @ViewChild('ed') ed: Editor;
    private quill;

    private model: WorkflowForm = new WorkflowForm();

    private originalStep: NodeModel;
    WorkflowFormFieldType = WorkflowFormFieldType;
    FormMode = FormMode;
    private newField: any = {};
    private formMode = FormMode.Default;
    private usedIn: any[] = [];
    private deletingField;

    private usedFields: any[] = [];
    private showHelp = false;
    private responsibilities = [];
    private destination = [];

    private types = [
        { value: WorkflowFormFieldType.Boolean, label: 'boolean' },
        { value: WorkflowFormFieldType.Integer, label: 'integer' },
        { value: WorkflowFormFieldType.Text, label: 'text' },
        { value: WorkflowFormFieldType.Date, label: 'date' },

    ];

    FormResponseType = FormResponseType;
    EmailTaskRecipientType = EmailTaskRecipientType;

    private responseTypes = [
        { value: FormResponseType[FormResponseType.FirstResponse], label: 'First Response' },
        { value: FormResponseType[FormResponseType.Majority], label: 'Majority' },
        { value: FormResponseType[FormResponseType.All], label: 'All' },
    ];

    private fieldsSub: any;

    constructor(
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService,
        private responsibilityService: ResponsibilityTypeService) {
        super();
    }

    ngOnInit() {
        this.originalStep = _.cloneDeep(this.step);

        this.usedFields = this.workflowFieldsService.getUsedFields();

        this.responsibilityService.getResponsibilityTypes()
            .then(r => {
                this.responsibilities = r;
            });

        this.workflowService.getEmailTaskRecipientType()
            .then(r => {
                r.forEach(e => {
                    if (e.ID < 1)
                        return;
                    this.destination.push({
                        value: EmailTaskRecipientType[e.ID],
                        label: e.Name
                    });
                });
            });

    }

    ngOnChanges() {
        this.initFields();

        if (this.ed != null && this.ed.quill != null)
            this.quill = this.ed.quill;
        else
            this.quill = null;
    }

    ngAfterViewChecked() {
        if (this.ed != null && this.ed.quill != null)
            this.quill = this.ed.quill;
    }

    ngOnDestroy() {
        this.quill = null;
        this.ed = null;

    }

    initFields() {
        //deal with xml-json nonsense
        if (this.step.fields == null || this.step.fields.form == null) {
            this.step.fields = {};
            this.step.fields.form = {};
            this.step.fields.form.field = [];
        }
        if (this.step.fields.form.field == null) {
            this.step.fields.form.field = [];
        }

        if (this.step.fields.form.field.length == null) {
            let f = _.cloneDeep(this.step.fields.form.field);
            this.step.fields.form.field = [];
            this.step.fields.form.field.push(f);
        }

        if (this.step.settings == null)
            this.step.settings = {};

        if (this.step.settings.SendFormEmail == null)
            this.step.settings.SendFormEmail = false;
        else
            //convert to bool
            this.step.settings.SendFormEmail = this.step.settings.SendFormEmail.toString().toLowerCase() === 'true' ? true : false;
        
        this.usedFields = this.workflowFieldsService.getUsedFields();
    }


    add() {
        this.formMode = FormMode.Adding;
    }

    remove(item: any) {
        this.deletingField = item;

        this.usedIn = [];
        this.usedIn = this.usedFields.filter(u => u.stepId == this.step.key && u.fieldId == item['@id']);

        this.formMode = FormMode.Deleting;
    }

    confirmDelete() {
        let i = this.step.fields.form.field.findIndex(f => f['@id'] == this.deletingField['@id']);

        if (i >= 0) {
            this.step.fields.form.field.splice(i, 1);

            //primeng v4.1 issue
            let fields = _.cloneDeep(this.step.fields.form.field);
            this.step.fields.form.field = null;
            this.step.fields.form.field = fields;

            this.stepChange.emit(this.step);
            this.deletingField['@stepId'] = this.step.key;
            this.workflowFieldsService.deleteFormField(this.deletingField);
        }

        this.formMode = FormMode.Default;
    }

    cancel() {
        this.formMode = FormMode.Default;
        this.newField = {};
    }

    save() {
        let count = this.step.fields.form.field.filter(f => f['@type'] == this.newField['@type']).length + 1;

        this.newField['@id'] = this.newField['@type'].toString().toLowerCase() + count.toString();

        let f = {};
        f['@id'] = this.newField['@id'];
        f['@label'] = this.newField['@label'];
        f['@type'] = this.newField['@type'];
        f['@stepId'] = this.step.key;


        this.step.fields.form.field.push(_.cloneDeep(this.newField));

        this.newField = {};
        this.formMode = FormMode.Default;
        this.stepChange.emit(this.step);


        this.workflowFieldsService.pushFormField(f);
    }

    appendField(e: string) {
        //console.log(this.step.settings.MessageBodyTemplate, this.quill);

        if (this.quill != null) {
            let len = this.quill.getLength();
            this.quill.insertText(len > 0 ? len - 1 : 0, e, 'api');

        } else {
            this.step.settings.MessageBodyTemplate =
                ((this.step.settings.MessageBodyTemplate == null) ? '' :
                    this.step.settings.MessageBodyTemplate)
                + e;
        }

    }

}