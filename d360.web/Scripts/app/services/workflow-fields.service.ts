import { Injectable } from '@angular/core';
import { Subject} from 'rxjs';
import { WorkflowChangeType, NodeModel } from '../models/workflow.model';


@Injectable()
export class WorkflowFieldsService {

    private formFieldsSource = new Subject<any[]>();
    formFields$ = this.formFieldsSource.asObservable();

    private httpFieldsSource = new Subject<any[]>();
    httpFields$ = this.httpFieldsSource.asObservable();

    private httpFields: any[] = [];
    private formFields: any[] = [];
    private usedFields: any[] = [];


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

    getContextualFieldsForType(changeType: WorkflowChangeType, objectType: string) {
        let fields = [];
        switch (+changeType) {
            case WorkflowChangeType.ScoreUpdate:
                fields.push({
                    value: 'Contextual|score',
                    label: 'Score',
                    type: 'number'
                });
                break;
        }

        switch (objectType) {
            case 'ShoppingCartType':
                fields.push({
                    value: 'Contextual|RequestedOn',
                    label: 'Requested On',
                    type: 'date'
                });
                break;

        }

        return fields;
    }
}