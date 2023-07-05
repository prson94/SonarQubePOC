import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { NodeSettings, WorkflowStepItemFields } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-step-http-response-outputs',
	templateUrl: './assignment-step-http-response-outputs.component.html'
})
export class AssignmentStepHttpResponseOutputsComponent extends BaseComponent implements OnInit {
	@Input() settings: NodeSettings;
	@Input() itemFields: WorkflowStepItemFields;
	outputs: any[] = [];

	constructor(
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit() {
			let stepFieldOutputs = this.itemFields?.Outputs.Output ?? null;
			let stepSettingOutputs = this.settings?.HTTPResponse.Outputs ?? null;

			if (stepFieldOutputs != null && !Array.isArray(stepFieldOutputs)) {
				stepFieldOutputs = [stepFieldOutputs];
			}

			if (stepSettingOutputs != null) {
				if (!Array.isArray(stepSettingOutputs)) {
					stepSettingOutputs = [stepSettingOutputs];
				}

				for (const stepSettingOutput of stepSettingOutputs) {
					const field = stepFieldOutputs?.find((f) => f.Id === stepSettingOutput.Id);
					this.outputs.push({
						Name: stepSettingOutput.Name,
						Path: stepSettingOutput.Path,
						Value: field?.Value
					});
				}
			}
	}
}
