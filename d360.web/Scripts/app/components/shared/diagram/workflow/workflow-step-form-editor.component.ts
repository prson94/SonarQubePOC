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

    private intersectType = null;
    private responsibleObject: string;
    private responsibleObjectId: number;
    private responsibilities = [];
    private isLoadingRes = false;

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

        if (this.objectId != null && this.objectType != null) {
            if (this.objectType != 'IntersectType') {
                this.responsibleObject = this.objectType;
                this.responsibleObjectId = this.objectId;
                promises.push(this.getResponsibilityTypes());
            }
        }

        

        this.usedFields = this.workflowFieldsService.getUsedFields();

       

        //promises.push(this.responsibilityService.getResponsibilityTypes()
        //    .then(r => {
        //        this.responsibilities = r;
        //    }));

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

        //parse bool fields
        if (this.step.settings.SendFormEmail == null)
            this.step.settings.SendFormEmail = false;
        else
            this.step.settings.SendFormEmail = this.step.settings.SendFormEmail.toString().toLowerCase() === 'true' ? true : false;

        if (this.step.fields.form['@allowReassignObject'] != null)
            this.allowReassignObject = this.step.fields.form['@allowReassignObject'].toString().toLowerCase() === 'true' ? true : false;

        if (this.step.fields.form['@allowReassignResource'] != null)
            this.allowReassignResource = this.step.fields.form['@allowReassignResource'].toString().toLowerCase() === 'true' ? true : false;

        //convert single value to array
        if (this.step.settings.ResponsibilityTypeID != null && !_.isArray(this.step.settings.ResponsibilityTypeID)) {
            let id = this.step.settings.ResponsibilityTypeID;
            delete this.step.settings.ResponsibilityTypeID;
            this.step.settings.ResponsibilityTypeID = [];
            this.step.settings.ResponsibilityTypeID.push(id);
        } else if (this.step.settings.ResponsibilityTypeID == null) {
            this.step.settings.ResponsibilityTypeID = [];
            this.step.settings.ResponsibilityTypeID.push(null);
        }

        if (this.step.settings.MessageRecipientType != null && this.step.settings.MessageRecipientType == 'Responsibility') {
            if (this.objectType == 'IntersectType') {
                this.changeResponsibilitySide(this.step.settings.ResponsibilitySide || 'Subject');
            } else {
                this.responsibleObject = this.objectType;
                this.responsibleObjectId = this.objectId;
                this.getResponsibilityTypes();
            }
        }


        this.usedFields = this.workflowFieldsService.getUsedFields();



        //console.log('initFields', this.step.settings);
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

            //another prime issue. prime adds _$visited property sometimes, fix pending release
            //but we need to remove it to avoid polluting the XML
            this.step.fields.form.field.forEach(f => {
                if (f['_$visited']) delete f['_$visited'];
            });

            this.stepChange.emit(this.step);
            this.deletingField['@stepId'] = this.step.key;
            this.workflowFieldsService.deleteFormField(this.deletingField);

        }

        //console.log('confirmDelete', this.step.fields.form.field);
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
        if (this.newField['@type'] == 'list') f['@referenceFieldId'] = this.newField['@referenceFieldId'];

        f['@stepId'] = this.step.key;


        this.step.fields.form.field.push(_.cloneDeep(this.newField));

        this.newField = {};
        this.formMode = FormMode.Default;
        this.stepChange.emit(this.step);


        this.workflowFieldsService.pushFormField(f);
        //console.log(this.step.fields.form.field);
    }

    appendField(e: string) {
        //console.log(this.step.settings.MessageBodyTemplate, this.quill);

        if (this.ed != null && this.ed.quill != null)
            this.quill = this.ed.quill;

        if (this.quill != null) {
            let pos = this.quill.getSelection(true);
            let len = pos.index || this.quill.getLength();
            this.quill.insertText(len > 0 ? len - 1 : 0, e, 'api');

        } else {
            this.step.settings.MessageBodyTemplate =
                ((this.step.settings.MessageBodyTemplate == null) ? '' :
                    this.step.settings.MessageBodyTemplate)
                + e;
        }

    }

    changeType(e: any) {
        this.newField['@type'] = e;

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

    addResponsibility() {
        this.step.settings.ResponsibilityTypeID.push(null);
        this.step.settings.ResponsibilityTypeID = this.step.settings.ResponsibilityTypeID.slice();
        this.stepChange.emit(this.step);
    }

    removeResponsibility(i: number) {
        this.step.settings.ResponsibilityTypeID.splice(i, 1);
        this.step.settings.ResponsibilityTypeID = this.step.settings.ResponsibilityTypeID.slice();
        this.stepChange.emit(this.step);
    }

    changeResponsibilitySide(e: any) {
        //if we switch sides, clear the current values
        if (e != this.step.settings.ResponsibilitySide) {
            this.step.settings.ResponsibilityTypeID = [];
            this.addResponsibility();
        }

        this.step.settings.ResponsibilitySide = e;
        //console.log('changeResponsibilitySide', this.step, e, this.intersectType, this.responsibleObject, this.responsibleObjectId);
        let promises = [];
        this.isLoadingRes = true;

        if (this.intersectType == null)
            promises.push(this.workflowService.getIntersectType(this.objectId).then(r => {
                if (r == null || r.length < 1) {
                    this.intersectType = null;
                } else {
                    this.intersectType = r[0];
                }
                //console.log('changeResSide after inttype', this.intersectType);
            }));
        else
            promises.push(Promise.resolve());

        Promise.all(promises)
            .then(() => {
                if (this.intersectType == null || (e != 'Object' && e != 'Subject')) {
                    this.responsibleObjectId = null;
                    this.responsibleObject = null;
                    this.responsibilities = [];
                } else if (e == 'Object') {
                    this.responsibleObject = this.intersectType.Object;
                    this.responsibleObjectId = this.intersectType.ObjectID;
                } else if (e == 'Subject') {
                    this.responsibleObject = this.intersectType.Subject;
                    this.responsibleObjectId = this.intersectType.SubjectID;
                }
                //console.log('changeResSide after promises', this.intersectType, this.responsibleObject, this.responsibleObjectId, e);
            })
            .then(() => this.getResponsibilityTypes())
            .then(() => this.stepChange.emit(this.step))
            .then(() => this.isLoadingRes = false);
    }

    getResponsibilityTypes(): Promise<any> {
        //console.log('getResTypes', this.responsibleObject, this.responsibleObjectId);
        if (this.responsibleObject == null || this.responsibleObjectId == null || this.responsibleObjectId < 0 || this.objectType == 'IssueType') {
            this.responsibilities = [];
            return this.responsibilityService.getResponsibilityTypes()
                .then(r => this.responsibilities = r)
                .then(() => {
                    this.responsibilities.forEach(r => {
                        r.ResponsibilityTypeID = r.ID;
                    })
                });
        }

        return this.responsibilityService.getResponsibilityTypesByObject(this.responsibleObject, this.responsibleObjectId)
            .then(r => this.responsibilities = r);
    }

    changeResponsibility(e: any, i: number) {
        //console.log('changeResponsibility', e, i, this.responsibilities);
        this.step.settings.ResponsibilityTypeID[i] = e;
        this.step.settings.ResponsibilityTypeID = this.step.settings.ResponsibilityTypeID.slice();
        this.stepChange.emit(this.step)
    }

    trackRes(index, item) {
        //not sure why this is required, but without it Angular has trouble keeping track of the index based responsibility types
        return index;
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