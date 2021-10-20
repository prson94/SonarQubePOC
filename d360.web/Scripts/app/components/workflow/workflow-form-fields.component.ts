import { Input, Component } from '@angular/core';
import { NgForm, FormGroup, FormBuilder, Validators, FormControl, ControlContainer } from '@angular/forms';

import { BaseComponent } from '../shared/base.component';
import { WorkflowFormField, WorkflowFormFieldType } from '../../models/workflow.model';

@Component({
    selector: 'd3s-workflow-form-fields',
    templateUrl: './workflow-form-fields.component.html',
    viewProviders: [{ provide: ControlContainer, useExisting: NgForm }]
})

export class WorkflowFormFieldsComponent extends BaseComponent {
    @Input() fields: WorkflowFormField[] = [];
    @Input() form: NgForm;

    fieldType = WorkflowFormFieldType;

    private isSetValidatior: boolean = false;

    constructor() {
        super();
    }

    public setValidators() {
        if (this.isSetValidatior) return false;
        if (!(this.form?.form && this.form.form.controls)) return true;
        let count: number = 0;
        let assignValidation: boolean = false;
        this.isSetValidatior = true;
        this.fields.forEach(x => {
            if (x.Required && x.FieldType == WorkflowFormFieldType.Link) {
                assignValidation = true;
                this.form.form.controls[`inputUrl_${count}`].setValidators([Validators.required]);
                this.form.form.controls[`inputUrl_${count}`].updateValueAndValidity();
            }
            else if (x.Required && x.FieldType != WorkflowFormFieldType.Boolean) {
                assignValidation = true;
                this.form.form.controls[`input_${count}`].setValidators([Validators.required]);
                this.form.form.controls[`input_${count}`].updateValueAndValidity();
            }
            count++;
        });
        return assignValidation;
    }

    public prepareValuesForSubmit() {
        for (var i = 0; i < this.fields.length; i++) {
            var isLink = this.fields[i].FieldType == WorkflowFormFieldType.Link;
            if (isLink) {
                let name = this.form.form.controls[`inputName_${i}`].value;
                let url = this.form.form.controls[`inputUrl_${i}`].value;
                var linkString = name + '|' + url;
                this.fields[i].Value = linkString;
            }
            else if (Array.isArray(this.fields[i].Value)) {
                this.fields[i].Value = this.fields[i].Value.join();
            }
        }
    }

};
