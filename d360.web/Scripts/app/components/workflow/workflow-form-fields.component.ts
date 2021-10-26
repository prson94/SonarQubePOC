import { Input, Component } from '@angular/core';
import { NgForm, FormGroup, FormBuilder, Validators, FormControl, ControlContainer } from '@angular/forms';

import { BaseComponent } from '../shared/base.component';
import { WorkflowFormField, WorkflowFormFieldType } from '../../models/workflow.model';
import { CompanySettingsService } from '../../services/settings.service';

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

    constructor(protected settingsService: CompanySettingsService) {
        super(settingsService);
    }

    public setValidators() {
        if (this.isSetValidatior) {
            return false;
        }
        if (!(this.form?.form && this.form.form.controls)) {
            return true;
        }
        let assignValidation: boolean = false;
        this.isSetValidatior = true;
        this.fields.forEach((x, i) => {
            if (x.Required) {
                assignValidation = true;
                if (x.FieldType === WorkflowFormFieldType.Link) {
                    this.form.form.controls[`inputUrl_${i}`].setValidators([Validators.required]);
                    this.form.form.controls[`inputUrl_${i}`].updateValueAndValidity();
                } else if (x.FieldType !== WorkflowFormFieldType.Boolean) {
                    this.form.form.controls[`input_${i}`].setValidators([Validators.required]);
                    this.form.form.controls[`input_${i}`].updateValueAndValidity();
                }
            }
        });
        return assignValidation;
    }

    public prepareValuesForSubmit() {
        this.fields.forEach((x, i) => {
            if (x.FieldType === WorkflowFormFieldType.Link) {
                const name = this.form.form.controls[`inputName_${i}`].value;
                const url = this.form.form.controls[`inputUrl_${i}`].value;
                x.Value = name + '|' + url;
            } else if (Array.isArray(x.Value)) {
                x.Value = x.Value.join();
            }
        });
    }
}