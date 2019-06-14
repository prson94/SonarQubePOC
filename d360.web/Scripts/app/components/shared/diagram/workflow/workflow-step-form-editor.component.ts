import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import * as _ from 'lodash';
import { Column, Header, MenuItem, Editor } from 'primeng/primeng';

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
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';
import { ResponsibilityTypeService } from '../../../../services/responsibility-type.service';
import { FormMode } from '../../../../models/form.model';

@Component({
    selector: 'd3s-workflow-step-form-editor',
    providers: [WorkflowService, ResponsibilityTypeService],
    templateUrl: './workflow-step-form-editor.component.html'
})

export class WorkflowStepFormEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() step: NodeModel;
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() issueObject: string;
    @Output() stepChange = new EventEmitter();
    @ViewChild('ed') ed: Editor;
    @ViewChild('fed') fed: Editor;
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

    private intersectType = null;

    private destination = [];
    private lookups = null;
    private intersectTypes = null;
    private isListLoading = false;

    private allowReassignResource = false;
    private allowReassignObject = false;

    private types = [
        { value: WorkflowFormFieldType.Boolean, label: 'boolean' },
        { value: WorkflowFormFieldType.Integer, label: 'integer' },
        { value: WorkflowFormFieldType.Text, label: 'text' },
        { value: WorkflowFormFieldType.Date, label: 'date' },
        { value: WorkflowFormFieldType.List, label: 'list' },
        { value: WorkflowFormFieldType.RelationshipType, label: 'relationshipType' },

    ];

    FormResponseType = FormResponseType;
    EmailTaskRecipientType = EmailTaskRecipientType;

    private responseTypes = [
        { value: FormResponseType[FormResponseType.FirstResponse], label: 'First Response' },
        { value: FormResponseType[FormResponseType.Majority], label: 'Majority' },
        { value: FormResponseType[FormResponseType.All], label: 'All' },
    ];

    constructor(
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService,
        private responsibilityService: ResponsibilityTypeService) {
        super();
    }

    ngOnInit() {
        this.originalStep = _.cloneDeep(this.step);
        let promises = [];

        this.usedFields = this.workflowFieldsService.getUsedFields();

        if (this.destination.length < 1)
            promises.push(this.workflowService.getEmailTaskRecipientType()
                .then(r => {
                    r.forEach(e => {
                        if (e.ID < 1)
                            return;
                        this.destination.push({
                            value: EmailTaskRecipientType[e.ID],
                            label: e.Name
                        });
                    });
                }));

        this.isLoading = true;
        Promise.all(promises).then(() => this.isLoading = false);
    }

    ngOnChanges() {
        this.initFields();

        if (this.ed != null && this.ed.quill != null) {
            this.quill = this.ed.quill;
        } else {
            this.quill = null;
        }
    }

    ngAfterViewChecked() {
        if (this.ed != null && this.ed.quill != null) {
            this.quill = this.ed.quill;
        }
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

        if (this.step.settings == null) {
            this.step.settings = {};
        }

        //parse bool fields
        if (this.step.settings.SendFormEmail == null)
            this.step.settings.SendFormEmail = false;
        else
            this.step.settings.SendFormEmail = this.step.settings.SendFormEmail.toString().toLowerCase() === 'true' ? true : false;

        if (this.step.settings.IncludePreviousFormResponses == null)
            this.step.settings.IncludePreviousFormResponses = false;
        else
            this.step.settings.IncludePreviousFormResponses = this.step.settings.IncludePreviousFormResponses.toString().toLowerCase() === 'true' ? true : false;

        if (this.step.fields.form['@allowReassignObject'] != null)
            this.allowReassignObject = this.step.fields.form['@allowReassignObject'].toString().toLowerCase() === 'true' ? true : false;

        if (this.step.fields.form['@allowReassignResource'] != null)
            this.allowReassignResource = this.step.fields.form['@allowReassignResource'].toString().toLowerCase() === 'true' ? true : false;

        this.usedFields = this.workflowFieldsService.getUsedFields();

        //load lists, needed for labels
        this.changeType('list');
        this.changeType('relationshipType');
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

    edit(item: any) {
        this.usedIn = [];
        this.usedIn = this.usedFields.filter(u => u.stepId == this.step.key && u.fieldId == item['@id']);

        let i = this.step.fields.form.field.find(f => f['@id'] == item['@id']);
        this.newField = i;
        if ((item['@required'] == 'true' || item['@required'] == true || (item['@type'] == 'boolean')))
            this.newField['@required'] = true;
        else
            this.newField['@required'] = false;
        this.newField['@oldId'] = this.newField['@id'];
        this.newField['@oldType'] = this.newField['@type'];

        //trigger load of type list
        this.changeType(this.newField['@type']);

        this.formMode = FormMode.Editing;
    }

    confirmDelete() {
        let i = this.step.fields.form.field.findIndex(f => f['@id'] == this.deletingField['@id']);

        if (i >= 0) {
            this.step.fields.form.field.splice(i, 1);

            //primeng v4.1 issue
            let fields = _.cloneDeep(this.step.fields.form.field);
            this.step.fields.form.field = null;
            this.step.fields.form.field = fields;

            //another prime issue. prime adds _$visited property sometimes, fix pending release
            //but we need to remove it to avoid polluting the XML
            this.step.fields.form.field.forEach(f => {
                if (f['_$visited']) delete f['_$visited'];
            });

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
        let typeChanged = (this.newField['@oldType'] != this.newField['@type']);
        let existing = null;
        let f = {};

        if (this.newField['@oldId'] != null) {
            if (typeChanged) {
                let i = this.step.fields.form.field.findIndex(f => f['@id'] == this.newField['@oldId']);

                if (i >= 0) {
                    existing = _.cloneDeep(this.step.fields.form.field[i]);
                    this.step.fields.form.field.splice(i, 1);
                }

                this.newField['@id'] = this.newField['@type'].toString().toLowerCase() + count.toString();
            } else {
                existing = this.step.fields.form.field.find(e => e['@id'] == this.newField['@id']);
            }

            delete this.newField['@oldType'];
            delete this.newField['@oldId'];
        }
        else
            this.newField['@id'] = this.newField['@type'].toString().toLowerCase() + count.toString();

        if (existing != null) {
            this.workflowFieldsService.deleteFormField({'@stepId':this.step.key,'@id':existing['@id']});
            f = existing;
        }

        f['@id'] = this.newField['@id'];
        f['@label'] = this.newField['@label'];
        f['@type'] = this.newField['@type'];
        f['@required'] = this.newField['@required'];
        if (this.newField['@type'] == 'list')
            f['@referenceFieldId'] = this.newField['@referenceFieldId'];
        else
            delete f['@referenceFieldId'];

        f['@stepId'] = this.step.key;

        if (existing == null || typeChanged)
            this.step.fields.form.field.push(_.cloneDeep(this.newField));
        else
            this.workflowFieldsService.forceFormFieldUpdate();

        this.newField = {};
        this.formMode = FormMode.Default;
        this.stepChange.emit(this.step);

        this.step.fields.form.field = this.step.fields.form.field.slice();
        this.workflowFieldsService.pushFormField(f);
    }

    appendFieldDescription(e: string) {
        if (this.fed != null && this.fed.quill != null)
            this.quill = this.fed.quill;

        if (this.quill != null) {
            let pos = this.quill.getSelection(true);
            let len = pos.index || this.quill.getLength();
            this.quill.insertText(len > 0 ? len - 1 : 0, e, 'api');

            //manually set the html in the model
            this.step.fields.form['@description'] = this.quill.container.querySelector('.ql-editor').innerHTML;

        } else {
            this.step.fields.form['@description'] =
                ((this.step.fields.form['@description'] == null) ? '' :
                this.step.fields.form['@description'])
                + e;
        }
        this.stepChange.emit(this.step);
    }

    appendField(e: string) {
        if (this.ed != null && this.ed.quill != null)
            this.quill = this.ed.quill;

        if (this.quill != null) {
            let pos = this.quill.getSelection(true);
            let len = pos.index || this.quill.getLength();
            this.quill.insertText(len > 0 ? len - 1 : 0, e, 'api');

            //manually set the html in the model
            this.step.settings.MessageBodyTemplate = this.quill.container.querySelector('.ql-editor').innerHTML;

        } else {
            this.step.settings.MessageBodyTemplate =
                ((this.step.settings.MessageBodyTemplate == null) ? '' :
                    this.step.settings.MessageBodyTemplate)
                + e;
        }

        this.stepChange.emit(this.step);
    }

    changeType(e: any) {
        this.newField['@type'] = e;
        if (e == 'boolean') this.newField['@required'] = true;
        if (e == 'relationshipType' && this.intersectTypes == null) {
            this.workflowService.getAllowIntersectTypes(this.objectType, this.objectId)
                .then(r => {
                    this.intersectTypes = r;
                });
        }
        if (e == 'list' && this.lookups == null) {
            this.workflowService.getWorkflowVersionStepFormLookups(this.objectType, this.objectId)
                .then(r => {
                    this.lookups = r;
                });
        }
    }

    private mapHTMLToFormProperty(html: string, prop: string) {
        if (html == null)
            delete this.step.fields.form[prop];
        else
            this.step.fields.form[prop] = html;
    }

    private getTypeLabel(i: any) {
        switch (i['@type']) {
            case 'list':
                if (this.lookups == null)
                    return 'List';
                let list = this.lookups.find(l => l.value.toString() == i['@referenceFieldId']);
                return 'List' + (list == null ? '' : ' :: ' + list.label);
            case 'relationshipType':
                if (this.intersectTypes == null)
                    return 'Relationship';
                let rel = this.intersectTypes.find(l => l.IntersectTypeID.toString() == i['@intersectTypeId']);
                return 'Relationship' + (rel == null ? '' : ( ' :: ' + ((rel.PredicateName != null && rel.PredicateName.length > 0) ? `[${rel.PredicateName}] ` : ' ') + rel.TargetName));
            default:
                return (i['@type'].charAt(0).toUpperCase() + i['@type'].substr(1));
        }
    }

    validateField() {
        if (this.newField['@label'] == null || this.newField['@label'].length < 1 || this.newField['@type'] == null || this.newField['@type'] == '')
           return false;

        if (this.newField['@type'] == 'list' && this.newField['@referenceFieldId'] == null)
            return false;

        if (this.newField['@type'] == 'relationshipType' && (this.newField['@intersectTypeId'] == null || this.newField['@intersectTypeId'] == ''))
            return false;

        return true;
    }
}
