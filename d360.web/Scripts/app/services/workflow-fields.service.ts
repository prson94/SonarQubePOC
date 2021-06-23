import { Injectable } from '@angular/core';
import { Subject} from 'rxjs';
import { WorkflowChangeType, NodeModel, EmailTaskRecipientType, HTTPResponseOutput } from '../models/workflow.model';


@Injectable({
    providedIn: 'root'
})
export class WorkflowFieldsService {

    private formFieldsSource = new Subject<any[]>();
    formFields$ = this.formFieldsSource.asObservable();

    private httpFieldsSource = new Subject<any[]>();
    httpFields$ = this.httpFieldsSource.asObservable();

    private outputFieldsSource = new Subject<HTTPResponseOutput[]>();
    outputFields$ = this.outputFieldsSource.asObservable();

    private httpRequestSource = new Subject<any[]>();
    httpRequest$ = this.httpRequestSource.asObservable();



    private httpFields: any[] = [];
    private outputFields: HTTPResponseOutput[] = [];
    private formFields: any[] = [];
    private usedFields: any[] = [];
    private httpRequestFields: any[] = [];

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
        this.contextualFields = this.contextualFields.filter(c => !c.value.startsWith('Contextual|Score|'));
        this.scoreTypes = scoreTypes.filter(s => s.value != null);
            this.scoreTypes.forEach(s => {
                this.contextualFields.push({
                    value: 'Contextual|Score|' + s.value,
                    label: s.label + ' (System Field)',
                    type: 'number'
                });
            });
    }

    getContextualFieldsForType() {
        return this.contextualFields;
    }

    //#region Form Fields

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

    getFields() {
        return this.formFields;
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

    //#endregion

    //#region Http Request Fields

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

    clearHttpRequestFields() {
        this.httpRequestFields = [];
        this.httpRequestSource.next(this.httpRequestFields);
    }

    setHttpRequestFields(fields: any[]) {
        this.httpRequestFields = fields;
        this.httpRequestSource.next(this.httpRequestFields);
    }

    pushHttpRequestField(field: any) {
        let i = this.httpRequestFields.findIndex(f => f.key == field.key);
        if (i > -1) {
            this.httpRequestFields[i].name = field.name;
        } else {
            this.httpRequestFields.push(field);
        }
        
        this.httpRequestSource.next(this.httpRequestFields);
    }

    deleteHttpRequestField(key: string) {
        let i = this.httpRequestFields.findIndex(f => f.key == key);
        if (i > -1) {
            this.httpRequestFields.splice(i, 1);
            this.httpRequestSource.next(this.httpRequestFields);
        }
    }

    getHttpRequestFields() {
        return this.httpRequestFields;
    }

    //#endregion

    //#region Http Response Fields

    getOutputFields() {
        return this.outputFields;
    }

    clearOutputFields() {
        this.outputFields = [];
    }

    setOutputFields(fields: HTTPResponseOutput[]) {
        this.outputFields = fields;
        this.outputFieldsSource.next(this.outputFields);
    }

    pushOutputField(field: HTTPResponseOutput) {
        let i = this.outputFields.findIndex(o => o.StepId == field.StepId && o.Id == field.Id);
        if (i == -1) {
            this.outputFields.push(field);
        }

        
        this.outputFieldsSource.next(this.outputFields);
    }

    updateOutputField(field: HTTPResponseOutput) {
        let i = this.outputFields.findIndex(f => f.Id == field.Id);
        if (i > -1) {
            this.outputFields[i].Name = field.Name;
            this.outputFields[i].Path = field.Path;
            this.outputFields[i].StepId = field.StepId;
            this.outputFieldsSource.next(this.outputFields);
        }
    }

    deleteOutputField(stepId: string, id: string) {
        let i = this.outputFields.findIndex(f => f.Id == id && f.StepId == stepId);
        if (i > -1) {
            this.outputFields.splice(i, 1);
            this.outputFieldsSource.next(this.outputFields);
        }
    }

    //#endregion
}