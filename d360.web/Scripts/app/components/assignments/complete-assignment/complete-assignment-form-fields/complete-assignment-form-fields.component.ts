import { Component, EventEmitter, Input, OnInit, Output, ViewEncapsulation } from '@angular/core';
import { WorkflowFormField, WorkflowFormFieldType } from '../../../../models/workflow.model';
import { ControlContainer, NgForm } from '@angular/forms';
import { unset } from 'lodash-es';
import { FormHelpers } from '../../../../static/form-helpers';
import { OverlayOptions } from 'primeng/api';

@Component({
	selector: 'd3s-complete-assignment-form-fields',
	templateUrl: './complete-assignment-form-fields.component.html',
	styleUrls: ['complete-assignment-form-fields.component.less'],
	encapsulation: ViewEncapsulation.None,
	viewProviders: [{ provide: ControlContainer, useExisting: NgForm }]
})
export class CompleteAssignmentFormFieldsComponent implements OnInit {
	@Input() fields: WorkflowFormField[] = [];
	@Input() form: NgForm;
	@Input() formElement;
	@Output() discardForm: EventEmitter<boolean> = new EventEmitter<boolean>();

	fieldType = WorkflowFormFieldType;

	public overlayOpts: OverlayOptions = {
		contentStyleClass: "bodymultiselectoverlay",
	}

	ngOnInit(): void {
		this.setValidators();
		this.handleFormInput();
	}

	handleFormInput() {
		this.form.valueChanges
			.pipe()
			.subscribe((value) => {
				for (const propName in value) {
					if (!Object.getOwnPropertyDescriptor(value, propName).value) {
						unset(value, propName);
					}
				}
				if (Object.keys(value).length) {
					this.discardForm.emit(true);
				} else {
					this.discardForm.emit(false);
				}
			});
	}

	public setValidators() {
		setTimeout(() => {
			this.fields.forEach((x, i) => {
				if (x.Required && x.Value == null) {
						this.form.controls[`input_${i}`]?.setErrors({
							required: true
						});
					}
			});
		}, 20);
	}

	public getLocaleDateString(): string {
		return FormHelpers.getLocaleDateString();
	}
}

