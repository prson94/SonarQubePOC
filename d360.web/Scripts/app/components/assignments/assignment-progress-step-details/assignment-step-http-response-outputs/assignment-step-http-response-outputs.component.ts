import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';

@Component({
	selector: 'd3s-assignment-step-http-response-outputs',
	templateUrl: './assignment-step-http-response-outputs.component.html'
})
export class AssignmentStepHttpResponseOutputsComponent extends BaseComponent implements OnInit {
	@Input() step: any;

	outputs: any[] = [];

	constructor(
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit() {
		if (this.step != null && this.step.ItemFields != null && this.step.ItemSettings != null) {
			let stepFieldOutputs = this.step.ItemFields.Outputs.Output;
			let stepSettingOutputs = this.step.ItemSettings.HTTPResponse.Outputs.Output;

			if (stepFieldOutputs != null && stepFieldOutputs.length == null) {
				stepFieldOutputs = [stepFieldOutputs];
			}

			if (stepSettingOutputs != null) {
				if (stepSettingOutputs.length == null) {
					stepSettingOutputs = [stepSettingOutputs];
				}

				stepSettingOutputs.forEach((o) => {
					let field = stepFieldOutputs.find((f) => f.Id === o.Id);
					if (field == null) {
						field = {};
					}
					this.outputs.push({
						Id: o.Id,
						Name: o.Name,
						Path: o.Path,
						Value: field.Value
					});
				});
			}
		}
	}
}
