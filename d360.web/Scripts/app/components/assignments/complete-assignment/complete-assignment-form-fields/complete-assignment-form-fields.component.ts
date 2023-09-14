import { Component, EventEmitter, Input, OnInit, Output, ViewChild, ViewEncapsulation } from '@angular/core';
import { WorkflowFormField, WorkflowFormFieldType } from '../../../../models/workflow.model';
import { ControlContainer, NgForm } from '@angular/forms';
import { AssignmentService } from '../../assignment.service';
import { unset } from 'lodash-es';
import { FormHelpers } from '../../../../static/form-helpers';

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

	constructor(private assignmentService: AssignmentService) {
	}

	ngOnInit(): void {
		this.assignmentService.setFormValidators.subscribe(() => {
			this.setValidators();
		});
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
		this.fields.forEach((x, i) => {
			if (x.Required) {
				this.form.form.controls[`input_${i}`]?.setErrors({
					required: true
				});
			}
		});
	}

	public getLocaleDateString(): string {
		return FormHelpers.getLocaleDateString();
	}
}

