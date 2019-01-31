import { Injectable } from '@angular/core';
import { Subject} from 'rxjs';
import { WorkflowChangeType } from '../models/workflow.model';

@Injectable()
export class WorkflowFieldsService {

    private formFieldsSource = new Subject<any[]>();
    formFields$ = this.formFieldsSource.asObservable();


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
       // console.log(this.usedFields);
        return this.usedFields;
    }

    getFields() {
        //console.log(this.formFields);
        return this.formFields;
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
       // console.log(this.formFields);
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