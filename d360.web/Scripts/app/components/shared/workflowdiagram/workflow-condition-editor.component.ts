import { Component, NgZone, OnDestroy, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import {
    WorkflowEventRegistration,
    EventCondition,
} from '../../../models/workflow.model';
import { FieldType } from '../../../models/fields.model';
import { Column, Header } from 'primeng/primeng';
import { WorkflowService } from '../../../services/workflow.service';

@Component({
    selector: 'd3s-workflow-condition-editor',
    providers: [WorkflowService],
    templateUrl: './workflow-condition-editor.component.html'
})

export class WorkflowConditionEditorComponent extends BaseComponent implements OnInit {
    @Input() objectType: string;
    @Input() objectId: number;
    @Input() formFields: any[] = [];
    @Output() onSave = new EventEmitter();
    @Output() onClose = new EventEmitter();


    private condition: any = {};
    private fields: FieldType[] = [];
    private selectedField;
    private lookups: any[] = [];
    private fieldList: any[] = [];

    private operators = [
        { value: '=', label: 'equal to' },
        { value: '!=', label: 'not equal to' },
        { value: '>', label: 'greater than' },
        { value: '<', label: 'less than' },
        { value: '>=', label: 'greater than or equal to' },
        { value: '<=', label: 'less than or equal to' },
    ];

    private bool = [
        { value: 'true', label: 'True' },
        { value: 'false', label: 'False' }
    ];

    constructor(private workflowService: WorkflowService) {
        super();
    }

    ngOnInit() {
        this.setOperators();
        this.load();
    }

    load() {
        this.isLoading = true;
        this.workflowService.getWorkflowFieldTypes(this.objectId, this.objectType)
            .then(r => {
                this.fields = r;
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
                            value: 'FormInput|' + f['@FormInputID'],
                            label: 'Form :: ' + f['@FormInputID']
                        });
                    });
                }

                this.isLoading = false;
            });
    }

    save() {
        this.onSave.emit(this.condition);
    }

    close() {
        this.onClose.emit();
    }


    selectField(e: any) {
        this.selectedField = e;

        if (this.selectedField.split('|')[0] == 'FieldType') {

            let field = this.fields.find(f => f.ID == +this.selectedField.split('|')[1]);

            delete this.condition['@FormInputID'];
            delete this.condition['@VersionStepID'];

            this.setOperators(field.Type);

            this.condition['@FieldTypeID'] = field.ID.toString();
            this.condition['@FieldName'] = field.FriendlyName;
            this.condition['@ValueType'] = this.getValueType(field.Type);

            this.lookups = [];

            if (this.condition['@ValueType'] == 'L') {
                this.workflowService.getLookupList(this.condition['@FieldTypeID'])
                    .then(r => {
                        console.log(r);
                        this.lookups = r;
                    });
            }
        } else if (this.selectedField.split('|')[0] == 'FormInput') {
            let input = this.formFields.find(f => f['@FormInputID'] == this.selectedField.split('|')[1]);

            delete this.condition['@FieldTypeID'];
            delete this.condition['@FieldName'];
            delete this.condition['@ValueType'];

            this.condition['@VersionStepID'] = input['@VersionStepID'];
            this.condition['@FormInputID'] = input['@FormInputID'];
        }

        
        //else if (this.condition.ValueType == 'FL') {
        //    this.workflowService.getFusionLookupList(this.condition.FieldTypeID)
        //        .then(r => this.lookups = r);
        //}
    }

    setOperators(type: string = '') {
        switch (type) {
            case 'Boolean':
            case 'Lookup':
            case 'FusionLookup':
            case 'Text':
                this.operators = [
                    { value: '=', label: 'equal to' },
                    { value: '!=', label: 'not equal to' },
                ];
                break;
            case 'Decimal':
            case 'Number':
            case 'Date':
            case 'DateTime':
            default:
                this.operators = [
                    { value: '=', label: 'equal to' },
                    { value: '!=', label: 'not equal to' },
                    { value: '>', label: 'greater than' },
                    { value: '<', label: 'less than' },
                    { value: '>=', label: 'greater than or equal to' },
                    { value: '<=', label: 'less than or equal to' },
                ];
                break;
        }
    }

    getValueType(type: string): string {
        switch (type) {
            case 'Boolean':
                return 'B';
            case 'Lookup':
                return 'L';
            //case 'FusionLookup':
            //    return 'FL';
            case 'Decimal':
            case 'Number':
                return 'D';
            case 'Date':
            case 'DateTime':
                return 'DT';
            case 'Text':
                return 'T';
            default:
                return 'U';
        }
    }

}