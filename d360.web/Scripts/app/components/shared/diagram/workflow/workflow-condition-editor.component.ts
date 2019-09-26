import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { Title } from '@angular/platform-browser';
import {
    WorkflowEventRegistration,
    EventCondition,
    WorkflowChangeType,
} from '../../../../models/workflow.model';
import { FieldType } from '../../../../models/fields.model';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowFieldsService } from '../../../../services/workflow-fields.service';

import * as go from 'gojs';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

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
    @Input() diagram: go.Diagram;
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();


    //private condition: any = {};
    private fields: FieldType[] = [];
    private selectedField;
    private selectedType;
    private lookups: any[] = [];
    private fieldList: any[] = [];
    private contextualFields: any[] = [];

    private selectedIssueObject = null;
    private suggestions = [];

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
                        value: 'FormInput|' + f['@stepId'] + '|' + f['@id'],
                        label: 'Form :: ' + f['@label']
                    });
                });
            }

            if (this.contextualFields.length > 0) {
                this.fieldList = this.fieldList.concat(this.contextualFields);
            }

        }
       // console.log(this.fields, this.fieldList, this.formFields);
    }

    load() {
        this.isLoading = true;
        this.loadObjectFields()
            .pipe(
            //.then(() => this.loadFormFields())
            map(() => this.loadContextualFields()),
            map(() => {
                    this.fieldList = [];

                    this.fields.forEach(f => {
                        this.fieldList.push({
                            value: 'FieldType|' + f.ID.toString(),
                            label: f.FriendlyName
                        });
                    });

                    if (this.formFields.length > 0) {
                        this.formFields.forEach(f => {
                            if (f['@type'] == 'relationshipType')
                                return;
                            this.fieldList.push({
                                value: 'FormInput|' + f['@stepId'] + '|' + f['@id'],
                                label: 'Form :: ' + f['@label']
                            });
                        });
                    }

                    if (this.contextualFields.length > 0) {
                        this.fieldList = this.fieldList.concat(this.contextualFields);
                    }

                })
            ).subscribe();
       
    }

    save() {
        this.onSave.emit(this.condition);
    }

    close() {
        this.onClose.emit();
    }

    loadObjectFields(): Observable<any> {
        return this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType, true)
            .pipe(
                map(r => {
                    this.fields = [];
                    this.fields = r;
                })
            );
    }

    loadFormFields() {
        if (this.formFields.length > 0) {
            this.formFields.forEach(f => {
              //  console.log(f);
                this.fieldList.push({
                    value: 'FormInput|' + f['@stepId'] + '|' + f['@id'],
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
        this.selectedIssueObject = null;

        //console.log('selectField: ', e);

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
                    .subscribe(r => {
                        console.log(r);
                        this.lookups = r;
                    });
            }
        } else if (this.selectedField.split('|')[0] == 'FormInput') {
            let input = this.formFields.find(f => f['@id'] == this.selectedField.split('|')[2] && f['@stepId'] == this.selectedField.split('|')[1]);

            this.selectedType = input['@type'].toLowerCase();

            this.setOperators(this.selectedType, this.selectedField.split('|')[0]);


            if (this.selectedType == 'list' || this.selectedType == 'relationshiptype') {
                if (this.diagram != null) {
                    //find the form step and figure out what the reference field is
                    let node = this.diagram.model.findNodeDataForKey(input['@stepId']);
                    if (node != null) {
                        let formField = node.fields.form.field.find(i => i['@id'] == input['@id']);
                        if (formField != null) {
                            let fieldId = +formField['@referenceFieldId'] || 0;
                            //console.log('formField', formField, fieldId);
                            if (fieldId != null && fieldId > 0) {
                                this.workflowService.getReferenceItemsForField(fieldId)
                                    .subscribe(r => {
                                        this.lookups = [];
                                        r.forEach(i => {
                                            this.lookups.push({
                                                value: i.ID,
                                                label: i.Code
                                            });
                                        });
                                    });
                            }
                        }
                    }
                }
            }

            //console.log(this.selectedType, this.selectedField, input, this.diagram);
            //this.diagram.model.nodeDataArray.filter(n => n.key == f['@stepId'])[0].fields.form.field.filter(f => f['@id'] == 'list1')[0]['@referenceFieldId']

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

            //console.log('selectField: ', e, special, this.selectedType, this.selectedField);

        }
    }

    setOperators(type: string = '', fieldType: string = '') {
        switch (type.toLowerCase()) {
            case 'boolean':
            case 'lookup':
            case 'list':
            case 'fusionlookup':
            case 'text':
                this.operators = [
                    { value: '=', label: '=' },
                    { value: '!=', label: '!=' },
                ];
                break;
            case 'html':
                this.operators = [ ];
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
            case 'list':
            case 'relationshiptype':
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
            case 'html':
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
        if (this.condition['@Value'] === '') return false;

        return true;
    }

    selectIssueObject(e: any) {
        //console.log(e);
        this.condition['@Object'] = e.Object;
        this.condition['@ObjectID'] = e.ObjectID;
        this.condition['@Value'] = e.TextPath;
        this.condition['@Operator'] = '=';
    }

    search(e: any) {
        this.workflowService.getIssueObjectSuggestions(e.query)
            .subscribe(r => {
                this.suggestions = r;
            });
    }

    changeValue(e: any) {
        this.condition['@Value'] = e;

        if (this.selectedType == 'lookup' || this.selectedType == 'list') {
            let lookup = this.lookups.find(l => l.value == e);

            this.condition['@ValueLabel'] = lookup == null ? e : lookup.label;
        } else {
            this.condition['@ValueLabel'] = e;
        }
    }
}