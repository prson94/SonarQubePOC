import { Injectable } from '@angular/core';
import { Subject} from 'rxjs';
import { WorkflowChangeType, NodeModel, EmailTaskRecipientType } from '../models/workflow.model';


@Injectable()
export class WorkflowFieldsService {

    private formFieldsSource = new Subject<any[]>();
    formFields$ = this.formFieldsSource.asObservable();

    private httpFieldsSource = new Subject<any[]>();
    httpFields$ = this.httpFieldsSource.asObservable();

    private httpFields: any[] = [];
    private formFields: any[] = [];
    private usedFields: any[] = [];

    private conditionOperators = [
        { value: '=', label: '=' },
        { value: '!=', label: '!=' },
        { value: '>', label: '>' },
        { value: '<', label: '<' },
        { value: '>=', label: '>=' },
        { value: '<=', label: '<=' },
        { value: 'C', label: 'value changed' },
        { value: 'P', label: 'is populated' },
        { value: 'NP', label: 'is not populated' },
    ];

    private scoreTypes: any[] = [];
    private recipientTypes: EmailTaskRecipientType[] = [];
    private contextualFields: any[] = [];

    private objectType: string;
    private objectId: number;
    private changeType: WorkflowChangeType;


    setWorkflow(objectType: string, objectId: number, changeType: WorkflowChangeType) {
        this.objectType = objectType;
        this.objectId = objectId;
        this.changeType = changeType;

        this.contextualFields = [];
        this.scoreTypes = [];

        if (this.changeType == WorkflowChangeType.ScoreUpdate) {

            let ix = this.conditionOperators.findIndex(c => c.value == 'C');
            if (ix > -1) {
                this.conditionOperators.splice(ix, 1);
            }
        }

        if (this.objectType == 'ShoppingCartType') {
            this.contextualFields.push({
                value: 'Contextual|RequestedOn',
                label: 'Requested On',
                type: 'date'
            });
        }

    }

    getConditionOperators() {
        return this.conditionOperators;
    }

    getRecipientTypes() {
        return this.recipientTypes;
    }

    getScoreTypes() {
        return this.scoreTypes;
    }

    setAvailableScoreTypes(scoreTypes: any[]) {
        this.scoreTypes = scoreTypes;
        if (this.changeType == WorkflowChangeType.ScoreUpdate) {
            this.scoreTypes.forEach(s => {
                this.contextualFields.push({
                    value: 'Contextual|Score|' + s.value,
                    label: s.label + ' (System Field)',
                    type: 'number'
                });
            });
        }
    }

    pushUsedField(fieldId: string, stepId: string, transitionId: string, transitionName: string) {
        this.usedFields.push({ fieldId: fieldId, stepId: stepId, transitionId: transitionId, transitionName: transitionName });
    } 

    deleteUsedField(fieldId: string, stepId: string, transitionId: string) {
        let i = this.usedFields.findIndex(u => u.fieldId == fieldId && u.stepId == stepId && u.transitionId == transitionId);

        if (i > -1) {
            this.usedFields.splice(i, 1);
        }
    }

    clearUsedFields() {
        this.usedFields = [];
    }

    getUsedFields() {
        return this.usedFields;
    }

    getFields() {
        return this.formFields;
    }

    getHttpFields() {
        return this.httpFields;
    }

    clearHttpFields() {
        this.httpFields = [];
    }

    setHttpFields(fields: any[]) {
        this.httpFields = fields;
        this.httpFieldsSource.next(this.httpFields);
    }

    pushHttpField(field: any) {
        this.httpFields.push(field);
        this.httpFieldsSource.next(this.httpFields);
    }

    pushHttpFields(step: NodeModel) {
        let f: any;
        let i: number;

        i = this.httpFields.findIndex(f => f['@stepId'] == step.key && f['@id'] == 'statusCode');
        if (i == -1) {
            f = {};
            f['@stepId'] = step.key;
            f['@id'] = 'statusCode';
            f['@label'] = 'Status Code';
            f['@type'] = 'number';
            this.httpFields.push(f);
        }

        i = this.httpFields.findIndex(f => f['@stepId'] == step.key && f['@id'] == 'responseBody');
        if (i == -1) {
            f = {};
            f['@stepId'] = step.key;
            f['@id'] = 'responseBody';
            f['@label'] = 'Response Body';
            f['@type'] = 'text';
            this.httpFields.push(f);
        }
        this.httpFieldsSource.next(this.httpFields);
    }

    forceHttpFieldUpdate() {
        this.httpFieldsSource.next(this.httpFields);
    }

    deleteHttpField(field: any) {
        let i = this.httpFields.findIndex(f => f['@stepId'] == field['@stepId'] && f['@id'] == field['@id']);
        if (i > -1) {
            this.httpFields.splice(i, 1);
            this.httpFieldsSource.next(this.httpFields);
        }
    }

    clearFormFields() {
        this.formFields = [];
        this.formFieldsSource.next(this.formFields);
    }

    setFormFields(fields: any[]) {
        this.formFields = fields;
        this.formFieldsSource.next(this.formFields);
    }

    pushFormField(field: any) {
        this.formFields.push(field);
        this.formFieldsSource.next(this.formFields);
    }

    pushFormFields(fields: any[]) {
        this.formFields.concat(fields);
        this.formFieldsSource.next(this.formFields);
    }

    forceFormFieldUpdate() {
        this.formFieldsSource.next(this.formFields);
    }


    deleteFormField(field: any) {
        let i = this.formFields.findIndex(f => f['@stepId'] == field['@stepId'] && f['@id'] == field['@id']);
        if (i > -1) {
            this.formFields.splice(i, 1);
            this.formFieldsSource.next(this.formFields);
        }
    }

    getContextualFieldsForType() {
        return this.contextualFields;

    }
}