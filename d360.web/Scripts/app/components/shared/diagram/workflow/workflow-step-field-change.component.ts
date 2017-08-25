import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import {
    WorkflowEventRegistration,
    WorkflowObjectType,
    WorkflowChangeType,
    ChangeTypeInfo,
    EventCondition,
    WorkflowListItem,
    WorkflowDiagramModel,
    WorkflowDiagramNode,
    NodeModel,
    WorkflowActivityType,
    WorkflowTaskProcedure,
    EmailTaskRecipientType
} from '../../../../models/workflow.model';
import { FieldType } from '../../../../models/fields.model';
import { Column, Header, Editor } from 'primeng/primeng';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';
import { FormMode } from '../../../../models/form.model';

import * as _ from 'lodash';

@Component({
    selector: 'd3s-workflow-step-field-change',
    providers: [WorkflowService],
    templateUrl: 'workflow-step-field-change.component.html'
})

export class WorkflowStepFieldChangeComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() fieldUpdate: any = {};
    @Input() formFields = [];
    @Output() fieldUpdateChange = new EventEmitter();

    private fields: FieldType[] = [];
    private usedFields: FieldType[] = [];
    private field: FieldType;
    private lookups = [];
    private bools = [
        { value: 'false', label: 'False' },
        { value: 'true', label: 'True' }
    ];
    private hasFormResponses = false;
    private selectedFormFieldId;

    private selectedField: any;
    private selectedFieldIndex = -1;
    FormMode = FormMode;
    private formMode = FormMode.Default;


    constructor(private workflowService: WorkflowService, private workflowFieldsService: WorkflowFieldsService) {
        super();
    }

    ngOnInit() {
        //console.log(_.cloneDeep(this.fieldUpdate));
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.fields = [];
        this.initField(this.selectedField);
        this.hasFormResponses = this.formFields != null && this.formFields.length > 0;

        this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType)
            .then(r => {
                this.fields = r;
            })
            .then(() => {

                if (_.isEmpty(this.fieldUpdate.Field)) {
                    this.fieldUpdate.Field = [];
                } else if (this.fieldUpdate.Field.length == null) {
                    let f = _.cloneDeep(this.fieldUpdate.Field);
                    this.fieldUpdate.Field = [];
                    this.fieldUpdate.Field.push(f);
                }

                this.fieldUpdate.Field.forEach(f => {
                    this.initField(f);
                    let fieldIndex = this.fields.findIndex(i => i.ID.toString() == f['@FieldId'].toString());

                    if (fieldIndex > -1) {
                        this.usedFields.push(this.fields[fieldIndex]);
                        this.fields.splice(fieldIndex, 1);
                    }

                });
            })
            .then(() => this.isLoading = false);
    }

    initField(f: any) {
        if (f == null) f = {};

        if (f['@ClearValue'] != null)
            f['@ClearValue'] = f['@ClearValue'].toString().toLowerCase() == 'true' ? true : false;
        if (f['@UseCurrentDate'] != null)
            f['@UseCurrentDate'] = f['@UseCurrentDate'].toString().toLowerCase() == 'true' ? true : false;
        if (f['@UseFormValue'] != null)
            f['@UseFormValue'] = f['@UseFormValue'].toString().toLowerCase() == 'true' ? true : false;
    }

    selectField(i: number) {
        this.selectedFieldIndex = i;
        this.selectedField = this.fieldUpdate.Field[i];
    }

    select(e: any, clear: boolean = true) {
        this.field = null;
        this.selectedField['@FieldId'] = e;

        if (clear)
            delete this.selectedField['@Value'];

        let f = this.fields.find(f => f.ID == +e);
        if (f) this.field = f;

        if (this.field) {
            this.selectedField['@FieldName'] = this.field.FriendlyName;

            if (this.field.Type == 'Lookup') {
                this.workflowService.getLookupList(this.field.ID)
                    .then(r => {
                        this.lookups = r;
                        this.lookups = this.lookups.filter(l => l.value != '');
                    });
            }
        }

        this.fieldUpdateChange.emit(this.fieldUpdate);
    }

    changeDate(e: any) {
        let d = new Date(e);
        let dateString = "";

        dateString = (d.getMonth() + 1).toString()
    }

    changeUseFormValue(e: any) {
        this.selectedField['@UseFormValue'] = e;
        if (!e) {
            delete this.selectedField['@FormFieldId'];
            delete this.selectedField['@FormStepId'];
            delete this.selectedField['@FormLabel'];
        }
        //this.fieldUpdateChange.emit(this.fieldUpdate);
    }

    changeFormValue(e: any) {
        this.selectedFormFieldId = e;
        let field = this.formFields.find(f => f['@FormFieldId'] == e);
        if (field == null) {
            this.selectedField['@FormFieldId'] = null;
            return;
        }
        this.selectedField['@FormFieldId'] = field['@id'];
        this.selectedField['@FormStepId'] = field['@stepId'];
        this.selectedField['@FormLabel'] = field['@label'];

        //this.fieldUpdateChange.emit(this.fieldUpdate);
    }

    add() {
        this.selectedField = {};
        this.initField(this.selectedField);

        this.formMode = FormMode.Adding;
    }

    save() {
        let field = _.cloneDeep(this.selectedField);
        let fieldTypeIndex = this.fields.findIndex(f => f.ID.toString() == field['@FieldId'].toString());

        if (fieldTypeIndex > -1) {
            this.usedFields.push(this.fields[fieldTypeIndex]);
            this.fields.splice(fieldTypeIndex, 1);
        }

        let useCurrentDate = field['@UseCurrentDate'] == null ? false : (field['@UseCurrentDate'].toString() == 'true' ? true : false);
        let useFormValue = field['@UseFormValue'] == null ? false : (field['@UseFormValue'].toString() == 'true' ? true : false);
        let clearValue = field['@ClearValue'] == null ? false : (field['@ClearValue'].toString() == 'true' ? true : false);

        if (clearValue || useCurrentDate || useFormValue) {
            delete field['@Value'];
        }
        if (useFormValue) {
            delete field['@UseCurrentDate'];
            delete field['@ClearValue'];
        }
        if (clearValue) {
            delete field['@UseFormValue'];
            delete field['@UseCurrentDate'];
        }
        if (useCurrentDate) {
            delete field['@ClearValue'];
            delete field['@UseFormValue'];
        }

        if (!clearValue && !useCurrentDate && !useFormValue) {
            delete field['@UseFormValue'];
            delete field['@ClearValue'];
            delete field['@UseCurrentDate'];
        }

        if (this.selectedFieldIndex > -1) {
            this.fieldUpdate.Field[this.selectedFieldIndex] = field;
        } else {
            this.fieldUpdate.Field.push(field);
        }

        this.fieldUpdate.Field = [...this.fieldUpdate.Field];

        this.selectedFieldIndex = -1;
        this.selectedField = {};
        this.initField(this.selectedField);


        this.fieldUpdateChange.emit(this.fieldUpdate);
        this.formMode = FormMode.Default;
    }

    delete(i: any) {
        this.selectedFieldIndex = i;
        this.formMode = FormMode.Deleting;
    }

    edit(i: any) {
        console.log('edit', i);
        this.selectedFieldIndex = i;
        this.selectedField = _.cloneDeep(this.fieldUpdate.Field[i]);
        this.select(this.selectedField['@FieldId'], false);
        this.formMode = FormMode.Editing;
    }

    confirmDelete() {

        if (this.selectedFieldIndex > -1) {
            let field = this.fieldUpdate.Field[this.selectedFieldIndex];
            let usedFieldIndex = this.usedFields.findIndex(f => f.ID.toString() == field['@FieldId'].toString());

            if (usedFieldIndex > -1) {
                this.fields.push(this.usedFields[usedFieldIndex]);
                this.usedFields.splice(usedFieldIndex, 1);
            }

            this.fieldUpdate.Field.splice(this.selectedFieldIndex, 1);

        }
        this.fieldUpdateChange.emit(this.fieldUpdate);
        this.selectedFieldIndex = -1;
        this.formMode = FormMode.Default;
    }

    valid() {

        //TODO: enforce single Field item per field
        if (this.selectedField == null) return false;
        if (this.selectedField['@FieldId'] == null) return false;


        let useCurrentDate = this.selectedField['@UseCurrentDate'] == null ? false : (this.selectedField['@UseCurrentDate'].toString() == 'true' ? true : false);
        let useFormValue = this.selectedField['@UseFormValue'] == null ? false : (this.selectedField['@UseFormValue'].toString() == 'true' ? true : false);
        let clearValue = this.selectedField['@ClearValue'] == null ? false : (this.selectedField['@ClearValue'].toString() == 'true' ? true : false);

        if (useFormValue) {
            if (this.selectedField['@FormStepId'] == null || this.selectedField['@FormFieldId'] == null)
                return false;
        }

        if (!useCurrentDate && !clearValue && !useFormValue) {
            if (this.selectedField['@Value'] == null)
                return false;
        }

        //console.log(this.selectedField);
        return true;
    }

}

class FieldUpdate {
    FieldId: string;
    Value: string;
    UseCurrentDate: boolean = false;
}