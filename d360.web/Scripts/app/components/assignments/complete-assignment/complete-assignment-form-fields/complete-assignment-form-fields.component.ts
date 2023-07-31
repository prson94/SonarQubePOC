import {

	Component,
	
	Input,
	OnInit,
} from "@angular/core";
import { BaseComponent } from "../../../shared/base.component";
import {
	WorkflowFormField,
	WorkflowFormFieldType,
} from "../../../../models/workflow.model";
import { ControlContainer, NgForm, Validators } from "@angular/forms";
import { CompanySettingsService } from "../../../../services/settings.service";

@Component({
	selector: "d3s-complete-assignment-form-fields",
	templateUrl: "./complete-assignment-form-fields.component.html",
	styleUrls: ['./complete-assignment-form-fields.component.less'],
	viewProviders: [{ provide: ControlContainer, useExisting: NgForm }],
})
export class CompleteAssignmentFormFieldsComponent implements OnInit {
	@Input() fields: WorkflowFormField[] = [];
	@Input() form: NgForm;
	@Input() formElement;

	fieldType = WorkflowFormFieldType;

	private isSetValidatior: boolean = false;

	constructor() {}

	ngOnInit(): void {
		this.setValidators()
	}

	public setValidators() {
		setTimeout(() => {
			this.fields.forEach((x, i) => {
				if (x.Required) {
					this.form.form.controls[`input_${i}`].setErrors({
						required: true,
					});
				}
			});
		});
	}

	public prepareValuesForSubmit() {
		this.fields.forEach((x, i) => {
			if (x.FieldType === WorkflowFormFieldType.Link) {
				const name = this.form.form.controls[`inputName_${i}`].value;
				const url = this.form.form.controls[`inputUrl_${i}`].value;
				x.Value =
					name.length + url.length === 0 ? "" : name + "|" + url;
			} else if (Array.isArray(x.Value)) {
				x.Value = x.Value.join();
			}
		});
	}
}

