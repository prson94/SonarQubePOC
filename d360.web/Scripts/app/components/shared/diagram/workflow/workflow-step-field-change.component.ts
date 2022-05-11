import { Component, Output, EventEmitter, Input, OnChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { FieldType } from '../../../../models/fields.model';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';
import { FormMode } from '../../../../models/form.model';
import { map } from 'rxjs/operators';
import * as _ from 'lodash';
import { NodeModel } from '../../../../models/lineage.model';
import { CompanySettingsService } from '../../../../services/settings.service';

@Component({
    selector: 'd3s-workflow-step-field-change',
    providers: [WorkflowService],
    templateUrl: 'workflow-step-field-change.component.html'
})

export class WorkflowStepFieldChangeComponent extends BaseComponent implements OnChanges {
    @Input() step: NodeModel;
    @Input() diagram: go.Diagram;
    @Input() objectId: number;
    @Input() objectType: string;
    @Input() fieldUpdate: any = {};
    @Input() formFields = [];
    @Input() httpFields = [];
    @Input() outputFields = [];
    @Input() issueObject: string;
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
    private hasOutputFields = false;
    private canSelectFromAction = false;
    private selectedFormFieldId;

    private selectedField: any;
    private selectedFieldIndex = -1;
    FormMode = FormMode;
    private formMode = FormMode.Default;
    private valueType = 'manual';
    private allowMultiple = false;

    get buttonLabel(): string { return this.formMode == FormMode.Adding ? $localize`Add` : `Update`; }

    constructor(
        protected settingsService: CompanySettingsService,
        private workflowService: WorkflowService,
        private workflowFieldsService: WorkflowFieldsService) {
        super(settingsService);
    }

    ngOnChanges() {
        this.load();
    }

    load() {
        this.isLoading = true;
        this.fields = [];
        this.initField(this.selectedField);
        this.hasFormResponses = this.formFields != null && this.formFields.length > 0;
        this.hasOutputFields = this.outputFields != null && this.outputFields.length > 0;

        this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType, true, this.issueObject)
            .pipe(
                map(r => {
                    this.fields = r.filter(x => x.Type != "JsonElement");//Exclude Json Element Fields
                }),
                map(() => {
                    if (_.isEmpty(this.fieldUpdate.Field)) {
                        this.fieldUpdate.Field = [];
                    } else if (this.fieldUpdate.Field.length == null) {
                        let f = _.cloneDeep(this.fieldUpdate.Field);
                        this.fieldUpdate.Field = [];
                        this.fieldUpdate.Field.push(f);
                    }

                    this.fieldUpdate.Field.forEach(f => {
                        this.initField(f);
                        let fieldIndex = this.fields.findIndex((i) => i.ID.toString() == f['@FieldId'].toString());

                        if (fieldIndex > -1) {
                            this.usedFields.push(this.fields[fieldIndex]);
                            this.fields.splice(fieldIndex, 1);
                        }

                    });
                }),
                map(() => {
                    if (this.issueObject != '') {
                        var actionFields = this.fields.filter((x) => x.Object == 'IssueType');
                        actionFields.forEach(function (item) {
                            var actionFormField: any = {};
                            actionFormField['@FieldName'] = 'Action Type::' + item.FriendlyName;
                            actionFormField['@FormFieldId'] = item.Object + '|' + item.ID;
                            actionFormField['@FormLabel'] = 'Action Type::' + item.FriendlyName;
                            actionFormField['@VersionStepID'] = '-1';
                            actionFormField['@id'] = item.ID;
                            actionFormField['@label'] = item.FriendlyName;
                            actionFormField['@stepId'] = '-1';
                            actionFormField['@type'] = this.getFieldTypeForFormType(item.Type);
                            actionFormField['@isActionType'] = true;
                            actionFormField['@UseFormValue'] = true;
                            if (item.Type == 'Lookup') {
                                actionFormField['@LookupFieldID'] = item.LookupObjectType + '|' + item.LookupObjectID;
                            }
                            this.formFields.push(actionFormField);
                        }, this);
                    }
                }),
                map(() => this.isLoading = false)
            ).subscribe();


    }

    getFieldTypeForFormType(fieldType: string): string {
        switch (fieldType) {
            case 'Lookup':
                return 'list';
            case 'Number':
            case 'Decimal':
                return 'integer';
            case 'Boolean':
                return 'boolean';
            case 'Date':
            case 'DateTime':
                return 'date';
            case 'Html':
                return 'html';
            case 'Link':
                return 'link';
            case 'Text':
            default:
                return 'text';
        }
    }

    initField(f: any) {
        if (f == null) f = {};

        if (f['@ClearValue'] != null)
            f['@ClearValue'] = f['@ClearValue'].toString().toLowerCase() == 'true' ? true : false;
        if (f['@UseCurrentDate'] != null)
            f['@UseCurrentDate'] = f['@UseCurrentDate'].toString().toLowerCase() == 'true' ? true : false;
        if (f['@UseFormValue'] != null)
            f['@UseFormValue'] = f['@UseFormValue'].toString().toLowerCase() == 'true' ? true : false;
        if (f['@UseOutputValue'] != null)
            f['@UseOutputValue'] = f['@UseOutputValue'].toString().toLowerCase() == 'true' ? true : false;

    }

    selectField(i: number) {
        this.selectedFieldIndex = i;
        this.selectedField = this.fieldUpdate.Field[i];
    }

    select(e: any, clear: boolean = true) {
        this.field = null;
        this.allowMultiple = false;
        this.selectedField['@FieldId'] = e;
        if (clear) {
            delete this.selectedField['@FormStepId'];
            delete this.selectedField['@FormFieldId'];
            delete this.selectedField['@Value'];
            this.selectedFormFieldId = null;
        }

        let f = this.fields.find(f => f.ID == +e);
        if (f) this.field = f;

        if (this.field) {
            this.selectedField['@FieldName'] = this.field.FriendlyName;

            if (this.field.Type == 'Lookup') {
                this.workflowService.getLookupList(this.field.ID)
                    .subscribe(r => {
                        this.lookups = r;
                        this.lookups = this.lookups.filter(l => l.value != '');
                    });
            }
            this.allowMultiple = this.field.AllowMultipleValues;
        }

        if (typeof this.selectedField["@AppendValue"] == 'string') {
            this.selectedField["@AppendValue"] = this.selectedField["@AppendValue"].toLowerCase() == 'true' ? true : false;
        }

        this.fieldUpdateChange.emit(this.fieldUpdate);
        if (typeof f !== 'undefined' && this.issueObject != '' && f.Object != 'IssueType') {
            this.canSelectFromAction = true;
        }
        else this.canSelectFromAction = false;
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
            delete this.selectedField['@IsActionForm'];
        }
    }

    changeFormValue(e: any) {
        this.selectedFormFieldId = e;
        let field = this.formFields.find(f => f['@FormFieldId'] == e);
        if (field == null) {
            this.selectedField['@FormFieldId'] = null;
            return;
        }
        if (field["@isActionType"] == true) {
            this.selectedField['@FormFieldId'] = field["@FormFieldId"];
            this.selectedField['@IsActionForm'] = true;
        }
        else {
            this.selectedField['@FormFieldId'] = field['@id'];
            this.selectedField['@IsActionForm'] = false;
        }
        this.selectedField['@FormStepId'] = field['@stepId'];
        this.selectedField['@FormLabel'] = field['@label'];

    }

    changeOutputValue(e: any) {
        this.selectedFormFieldId = e;
        let stepId = this.selectedFormFieldId.split("|")[1];
        let fieldId = this.selectedFormFieldId.split("|")[0];
        let field = this.outputFields.find(f => f.Id == fieldId && f.StepId == stepId);

        if (field == null) {
            this.selectedField['@FormFieldId'] = null;
            this.selectedField['@FormStepId'] = null;
            return;
        } else {
            this.selectedField['@FormFieldId'] = field.Id;
            this.selectedField['@FormStepId'] = field.StepId;
            this.selectedField['@FormLabel'] = field['@FormLabel'];
        }

    }

    add() {
        this.selectedField = {};
        this.initField(this.selectedField);
        this.select(null);
        this.setValueType();
        this.selectedFormFieldId = null;
        this.formMode = FormMode.Adding;
    }

    formatObjectTypeName(str: any): string {
        return str.replace('Type', '');
    }

    save() {
        let field = _.cloneDeep(this.selectedField);
        let fieldTypeIndex = this.fields.findIndex(f => f.ID.toString() == field['@FieldId'].toString());

        var selectedField = this.fields.filter(f => f.ID.toString() == field['@FieldId'].toString())[0];
        field["@ObjectType"] = this.formatObjectTypeName(selectedField.Object);
        if (this.field.Type.toLowerCase() == 'lookup') {
            //join multiselect value into a comma delimited string
            if (this.field.AllowMultipleValues) {
                let valueLabels = [];
                //primeng junk value
                if (field['@Value'] != null && field['@Value']._$visited != null)
                    delete field['@Value']._$visited;
                if (field['@Value'] != null && Array.isArray(field['@Value'])) {
                    field['@Value'].forEach(v => {
                        let label = this.lookups.find(l => l.value == v);
                        if (label != null)
                            valueLabels.push(label.label);
                    });
                    field['@Value'] = field['@Value'].join();
                    field['@ValueLabel'] = valueLabels.join();
                }

            } else {
                let valueLabel = this.lookups.find(l => l.value == field['@Value']);
                field['@ValueLabel'] = valueLabel == null ? field['@Value'] : valueLabel.label;
            }

        }
        else if (this.field.Type.toLowerCase() == 'link') {
            field['@Value'] = field['@Value'] + '|' + field['@Url'];
        }
        else {
            delete field['@AppendValue'];
        }

        if (fieldTypeIndex > -1) {
            this.usedFields.push(this.fields[fieldTypeIndex]);
            this.fields.splice(fieldTypeIndex, 1);
        }

        let useCurrentDate = field['@UseCurrentDate'] == null ? false : (field['@UseCurrentDate'].toString() == 'true' ? true : false);
        let useFormValue = field['@UseFormValue'] == null ? false : (field['@UseFormValue'].toString() == 'true' ? true : false);
        let useActionValue = field['@IsActionForm'] == null ? false : (field['@IsActionForm'].toString() == 'true' ? true : false);

        let clearValue = field['@ClearValue'] == null ? false : (field['@ClearValue'].toString() == 'true' ? true : false);

        if (clearValue || useCurrentDate || useFormValue) {
            delete field['@Value'];
            delete field['@ValueLabel'];
        }
        if (useFormValue || useActionValue) {
            delete field['@UseCurrentDate'];
            delete field['@ClearValue'];
            delete field['@UseCurrentDate'];
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
        this.selectedFieldIndex = i;
        this.selectedField = _.cloneDeep(this.fieldUpdate.Field[i]);
        this.selectedFormFieldId = null;

        //free up the field so it can be selected and changed
        let usedFieldIndex = this.usedFields.findIndex(f => f.ID.toString() == this.selectedField['@FieldId'].toString());
        if (usedFieldIndex > -1) {
            this.fields.push(this.usedFields[usedFieldIndex]);
            this.usedFields.splice(usedFieldIndex, 1);
        }

        let field = this.fields.find(f => f.ID.toString() == this.selectedField['@FieldId']);
        if (field != null) {
            //for multiselect we need to split the value back into an array
            if (field.Type.toLowerCase() == 'lookup' && field.AllowMultipleValues == true) {
                if (this.selectedField['@Value'] != null && !Array.isArray(this.selectedField['@Value'])) {
                    this.selectedField['@Value'] = this.selectedField['@Value'].split(',');
                }
            }
        }

        this.setValueType();

        if (this.valueType == 'form' || this.valueType == 'output') {
            this.selectedFormFieldId = this.selectedField['@FormFieldId'] + '|' + this.selectedField['@FormStepId'];
        }

        if (this.valueType == "actionForm") {
            this.selectedFormFieldId = this.selectedField['@FormFieldId'];
        }

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

    cancel() {

        let field = _.cloneDeep(this.selectedField);
        if (field && field['@FieldId']) {
            let fieldTypeIndex = this.fields.findIndex(f => f.ID.toString() == field['@FieldId'].toString());
            if (fieldTypeIndex > -1) {
                this.usedFields.push(this.fields[fieldTypeIndex]);
                this.fields.splice(fieldTypeIndex, 1);
            }
        }
        this.selectedField = null;
        this.formMode = FormMode.Default;
        this.valueType = null;
        this.field = null;
        this.selectedFormFieldId = null;
    }

    valid() {
        //TODO: enforce single Field item per field
        if (this.selectedField == null) return false;
        if (this.selectedField['@FieldId'] == null) return false;


        let useCurrentDate = this.selectedField['@UseCurrentDate'] == null ? false : (this.selectedField['@UseCurrentDate'].toString() == 'true' ? true : false);
        let useOutputValue = this.selectedField['@UseOutputValue'] == null ? false : (this.selectedField['@UseOutputValue'].toString() == 'true' ? true : false);
        let useFormValue = this.selectedField['@UseFormValue'] == null ? false : (this.selectedField['@UseFormValue'].toString() == 'true' ? true : false);

        if (!useFormValue) {
            useFormValue = this.selectedField['@IsActionForm'] == null ? false : (this.selectedField['@IsActionForm'].toString() == 'true' ? true : false);

        }

        let clearValue = this.selectedField['@ClearValue'] == null ? false : (this.selectedField['@ClearValue'].toString() == 'true' ? true : false);

        if (useFormValue) {
            if (this.selectedField['@FormStepId'] == null || this.selectedField['@FormFieldId'] == null)
                return false;
        }

        if (!useCurrentDate && !clearValue && !useFormValue && !useOutputValue) {
            if (this.selectedField['@Value'] == null || this.selectedField['@Value'] == '')
                return false;

        }

        return true;
    }

    changeValueType(type: string) {
        this.valueType = type;
        switch (type.toLowerCase()) {
            case 'manual':
                delete this.selectedField['@UseFormValue'];
                delete this.selectedField['@ClearValue'];
                delete this.selectedField['@UseCurrentDate'];
                delete this.selectedField['@IsActionForm'];
                delete this.selectedField['@UseOutputValue'];
                break;
            case 'clear':
                delete this.selectedField['@UseFormValue'];
                delete this.selectedField['@Value'];
                delete this.selectedField['@UseCurrentDate'];
                delete this.selectedField['@IsActionForm'];
                delete this.selectedField['@UseOutputValue'];
                this.selectedField['@ClearValue'] = true;
                break;
            case 'form':
                delete this.selectedField['@ClearValue'];
                delete this.selectedField['@Value'];
                delete this.selectedField['@UseCurrentDate'];
                delete this.selectedField['@AppendValue'];
                delete this.selectedField['@UseOutputValue'];
                this.selectedField['@UseFormValue'] = true;
                this.selectedField['@IsActionForm'] = false;

                break;
            case 'actionForm':
                this.changeFormValue('form');
                delete this.selectedField['@ClearValue'];
                delete this.selectedField['@Value'];
                delete this.selectedField['@UseCurrentDate'];
                delete this.selectedField['@AppendValue'];
                delete this.selectedField['@UseOutputValue'];
                this.selectedField['@UseFormValue'] = true;
                this.selectedField['@IsActionForm'] = true;
                break;
            case 'timestamp':
                delete this.selectedField['@UseFormValue'];
                delete this.selectedField['@IsActionForm'];
                delete this.selectedField['@ClearValue'];
                delete this.selectedField['@Value'];
                delete this.selectedField['@UseOutputValue'];
                this.selectedField['@UseCurrentDate'] = true;
                break;
            case 'output':
                delete this.selectedField['@UseFormValue'];
                delete this.selectedField['@IsActionForm'];
                delete this.selectedField['@ClearValue'];
                delete this.selectedField['@Value'];
                delete this.selectedField['@UseCurrentDate'];
                delete this.selectedField['@IsActionForm'];
                this.selectedField['@UseOutputValue'] = true;
                break;
        }
    }

    setValueType() {
        if (this.selectedField == null)
            this.valueType = null;
        else if (this.selectedField['@UseFormValue'] != null && (!this.selectedField['@IsActionForm'] || this.selectedField['@IsActionForm'].toString() != 'true'))
            this.valueType = 'form';
        else if (this.selectedField['@IsActionForm'] != null && this.selectedField['@IsActionForm'].toString() == 'true')
            this.valueType = 'actionForm';
        else if (this.selectedField['@ClearValue'] != null)
            this.valueType = 'clear';
        else if (this.selectedField['@UseCurrentDate'] != null)
            this.valueType = 'timestamp';
        else if (this.selectedField['@UseOutputValue'] != null && this.selectedField['@UseOutputValue'].toString() == 'true')
            this.valueType = 'output';
        else
            this.valueType = 'manual';
    }

    isHtml(i: any): boolean {
        if (i == null) return false;
        let f = this.usedFields.find(f => f.ID == +i['@FieldId']);
        if (f == null) return false;
        return f.Type == 'Html';
    }

    getValue(i: any): string {
        let val = "";
        if (i != null) {
            if (i['@ValueLabel'] != null)
                val = i['@ValueLabel'];
            else
                val = i['@Value'];
        }

        if (val != undefined && val.length > 50) {
            val = val.substr(0, 47) + '...';
        }

        return val;
    }

    getTableFieldName(item: any): string {
        if (this.issueObject == "") return item['@FieldName'];
        if (typeof item['@ObjectType'] == 'undefined' || item['@ObjectType'] == 'Issue')
            return $localize`Action Field` + "::" + item['@FieldName'];
        return $localize`Asset Field` + "::" + item['@FieldName'];
    }

    getFieldNameForDropDown(f: any): string {
        if (this.issueObject == "") return f.FriendlyName;
        if (f.Object == "IssueType")
            return $localize`Action Field` + "::" + f.FriendlyName;
        return $localize`Asset Field` + "::" + f.FriendlyName;
    }

    get availableActionFields(): any[] {
        let field = this.fields.find(f => f.ID.toString() == this.selectedField['@FieldId']);
        if (field == null)
            return null;

        let fieldType = field.Type;


        var formFieldsWithAction = this.formFields.filter(f => f["@isActionType"] == true);
        switch (fieldType) {
            case 'Lookup':
                var lookupField = field.LookupObjectType + '|' + field.LookupObjectID;
                return formFieldsWithAction.filter(f => f['@type'] == 'list' && f['@LookupFieldID'] == lookupField);
            case 'Number':
            case 'Decimal':
                return formFieldsWithAction.filter(f => f['@type'] == 'integer');
            case 'Boolean':
                return formFieldsWithAction.filter(f => f['@type'] == 'boolean');
            case 'Date':
            case 'DateTime':
                return formFieldsWithAction.filter(f => f['@type'] == 'date');
            case 'Link':
                return formFieldsWithAction.filter(f => f['@type'] == 'link');
            case 'Html':
                return formFieldsWithAction.filter(f => f['@type'] == 'text' || f['@type'] == 'html');
            case 'Text':
            default:
                return formFieldsWithAction.filter(f => f['@type'] == 'text');
        }
    }

    get availableFormFields(): any[] {
        let field = this.fields.find(f => f.ID.toString() == this.selectedField['@FieldId']);
        if (field == null)
            return null;

        let fieldType = field.Type;

        var formFieldsWithoutAction = this.formFields.filter(f => f["@isActionType"] != true);

        switch (fieldType) {
            case 'Lookup':
                return formFieldsWithoutAction.filter(f => f['@type'] == 'list' && f['@referenceFieldId'] == field.ID.toString());
            case 'Number':
            case 'Decimal':
                return formFieldsWithoutAction.filter(f => f['@type'] == 'integer');
            case 'Boolean':
                return formFieldsWithoutAction.filter(f => f['@type'] == 'boolean');
            case 'Date':
            case 'DateTime':
                return formFieldsWithoutAction.filter(f => f['@type'] == 'date');
            case 'Text':
                return formFieldsWithoutAction.filter(f => f['@type'] == 'text');
            case 'Html':
                return formFieldsWithoutAction.filter(f => f['@type'] == 'html' || f['@type'] == 'text');
            case 'Link':
                return formFieldsWithoutAction.filter(f => f['@type'] == 'link');
            default:
                return formFieldsWithoutAction.filter(f => f['@type'] == 'text');
        }

    }

    get availableOutputFields(): any[] {
        let field = this.fields.find(f => f.ID.toString() == this.selectedField['@FieldId']);
        if (field == null)
            return null;

        let fieldType = field.Type;
        switch (fieldType) {
            case 'Html':
            case 'Text':
                return this.outputFields.filter(f => f.Type == "text").map(f => { return { key: f.Id + '|' + f.StepId, '@FormFieldId': f.Id, '@FormStepId': f.StepId, '@FormLabel': f['@FormLabel'] }; });
        }
        return null;
    }
}

class FieldUpdate {
    FieldId: string;
    Value: string;
    UseCurrentDate: boolean = false;
}