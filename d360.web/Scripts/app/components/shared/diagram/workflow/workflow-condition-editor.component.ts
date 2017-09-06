import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { Title } from '@angular/platform-browser';
import {
    WorkflowEventRegistration,
    EventCondition,
    WorkflowChangeType,
} from '../../../../models/workflow.model';
import { FieldType } from '../../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';

@Component({
    selector: 'd3s-workflow-condition-editor',
    providers: [WorkflowService],
    templateUrl: './workflow-condition-editor.component.html'
})

export class WorkflowConditionEditorComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() formFields: any[] = [];
    @Input() condition: any = null;
    @Input() changeType: WorkflowChangeType = null;
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();


    //private condition: any = {};
    private fields: FieldType[] = [];
    private selectedField;
    private selectedType;
    private lookups: any[] = [];
    private fieldList: any[] = [];
    private contextualFields: any[] = [];

    private operators = [
        { value: '=', label: '=' },
        { value: '!=', label: '!=' },
        { value: '>', label: '>' },
        { value: '<', label: '<' },
        { value: '>=', label: '>=' },
        { value: '<=', label: '<=' },
        { value: 'C', label: 'value changed' },
    ];

    private bool = [
        { value: 'true', label: 'True' },
        { value: 'false', label: 'False' }
    ];

    constructor(private workflowService: WorkflowService, private workflowFieldsService: WorkflowFieldsService) {
        super();
    }

    ngOnInit() {
        this.setOperators();
        this.load();
        if (this.condition == null) this.condition = {};

    }

    ngOnChanges(changes: SimpleChanges) {

        //this.load();

        if ((changes['formFields'] != null && !changes['formFields'].isFirstChange()) ||
            (changes['changeType'] != null && !changes['changeType'].isFirstChange()))    {

            this.loadContextualFields();
            this.loadFormFields();

            this.fieldList = [];

            this.fields.forEach(f => {
                this.fieldList.push({
                    value: 'FieldType|' + f.ID.toString(),
                    label: f.FriendlyName
                });
            });

            if (this.formFields.length > 0) {
                this.formFields.forEach(f => {
                    this.fieldList.push({
                        value: 'FormInput|' + f['@id'],
                        label: 'Form :: ' + f['@label']
                    });
                });
            }

            if (this.contextualFields.length > 0) {
                this.fieldList = this.fieldList.concat(this.contextualFields);
            }


        }
    }

    load() {
        this.isLoading = true;
        this.loadObjectFields()
            //.then(() => this.loadFormFields())
            .then(() => this.loadContextualFields())
            .then(() => {
                this.fieldList = [];

                this.fields.forEach(f => {
                    this.fieldList.push({
                        value: 'FieldType|' + f.ID.toString(),
                        label: f.FriendlyName
                    });
                });

                if (this.formFields.length > 0) {
                    this.formFields.forEach(f => {
                        this.fieldList.push({
                            value: 'FormInput|' + f['@id'],
                            label: 'Form :: ' + f['@label']
                        });
                    });
                }

                if (this.contextualFields.length > 0) {
                    this.fieldList = this.fieldList.concat(this.contextualFields);
                }

            });
       
    }

    save() {
        this.onSave.emit(this.condition);
    }

    close() {
        this.onClose.emit();
    }

    loadObjectFields(): Promise<any> {
        return this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType)
            .then(r => {
                this.fields = [];
                this.fields = r;
            });
    }

    loadFormFields() {
        if (this.formFields.length > 0) {
            this.formFields.forEach(f => {
                this.fieldList.push({
                    value: 'FormInput|' + f['@id'],
                    label: 'Form :: ' + f['@label']
                });
            });
        }
    }

    loadContextualFields() {
        this.contextualFields = this.workflowFieldsService.getContextualFieldsForType(this.changeType, this.objectType);
    }

    selectField(e: any) {
        this.selectedField = e;
        

        if (this.selectedField.split('|')[0] == 'FieldType') {

            let field = this.fields.find(f => f.ID == +this.selectedField.split('|')[1]);

            //console.log('selectField: ', e, field);

            this.selectedType = field.Type.toLowerCase();

            delete this.condition['@FormInputID'];
            delete this.condition['@VersionStepID'];
            delete this.condition['@label'];
            delete this.condition['@id'];
            delete this.condition['@type'];
            delete this.condition['@ContextualFieldID'];
            delete this.condition['@Operator'];
            delete this.condition['@Value'];

            this.setOperators(field.Type, this.selectedField.split('|')[0]);

            this.condition['@FieldTypeID'] = field.ID.toString();
            this.condition['@FieldName'] = field.FriendlyName;
            this.condition['@ValueType'] = this.getValueType(field.Type);

            this.lookups = [];

            if (this.condition['@ValueType'] == 'L') {
                this.workflowService.getLookupList(this.condition['@FieldTypeID'])
                    .then(r => {
                        //console.log(r);
                        this.lookups = r;
                    });
            }
        } else if (this.selectedField.split('|')[0] == 'FormInput') {
            let input = this.formFields.find(f => f['@id'] == this.selectedField.split('|')[1]);

            this.selectedType = input['@type'].toLowerCase();

            this.setOperators(this.selectedType, this.selectedField.split('|')[0]);

            delete this.condition['@FieldTypeID'];
            delete this.condition['@label'];
            delete this.condition['@id'];
            delete this.condition['@type'];
            delete this.condition['@ContextualFieldID'];
            delete this.condition['@Operator'];
            delete this.condition['@Value'];

            this.condition['@VersionStepID'] = input['@stepId'];
            this.condition['@FormInputID'] = input['@id'];
            this.condition['@ValueType'] = this.getValueType(this.selectedType);
            this.condition['@FieldName'] = 'Form :: ' + input['@label']

        } else if (this.selectedField.split('|')[0] == 'Contextual') {
            let special = this.contextualFields.find(s => s.value == this.selectedField);
            this.selectedType = special.type.toLowerCase();

            delete this.condition['@FormInputID'];
            delete this.condition['@VersionStepID'];
            delete this.condition['@FieldTypeID'];
            delete this.condition['@label'];
            delete this.condition['@id'];
            delete this.condition['@type'];
            delete this.condition['@Operator'];
            delete this.condition['@Value'];

            this.setOperators(this.selectedType, this.selectedField.split('|')[0]);

            this.condition['@ContextualFieldID'] = this.selectedField.split('|')[1];
            this.condition['@FieldName'] = special.label;
            this.condition['@ValueType'] = this.getValueType(this.selectedType);

        }
    }

    setOperators(type: string = '', fieldType: string = '') {
        switch (type.toLowerCase()) {
            case 'boolean':
            case 'lookup':
            case 'fusionlookup':
            case 'text':
                this.operators = [
                    { value: '=', label: '=' },
                    { value: '!=', label: '!=' },
                ];
                break;
            case 'decimal':
            case 'number':
            case 'integer':
            case 'date':
            case 'datetime':
            default:
                this.operators = [
                    { value: '=', label: '=' },
                    { value: '!=', label: '!=' },
                    { value: '>', label: '>' },
                    { value: '<', label: '<' },
                    { value: '>=', label: '>=' },
                    { value: '<=', label: '<=' },
                ];
                break;
        }
        //only supporting fields at the moment
        if (fieldType == 'FieldType') {
            this.operators.push({ value: 'C', label: 'value changed' });
        }
    }

    getValueType(type: string): string {
        switch (type.toLowerCase()) {
            case 'boolean':
                return 'B';
            case 'lookup':
                return 'L';
            //case 'FusionLookup':
            //    return 'FL';
            case 'decimal':
            case 'number':
            case 'integer':
                return 'D';
            case 'date':
            case 'datetime':
                return 'DT';
            case 'text':
                return 'T';
            default:
                return 'U';
        }
    }

    valid() {
        if (this.condition['@Operator'] == null)
            return false;
        if ((this.condition['@FieldTypeID'] == null || this.condition['@FieldTypeID'] == '') &&
            (this.condition['@ContextualFieldID'] == null || this.condition['@ContextualFieldID'] == '') &&
            this.condition['@FormInputID'] == null)
            return false;
        if (this.condition['@Value'] == null && this.condition['@Operator'] != 'C')
            return false;
        if (this.condition['@Operator'] == '') return false;
        if (this.condition['@Value'] == '') return false;

        return true;
    }

}