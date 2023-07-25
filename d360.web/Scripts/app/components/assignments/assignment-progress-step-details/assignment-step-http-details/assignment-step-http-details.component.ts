import { Component, Input } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { NodeSettings, WorkflowStepItemFields } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-step-http-details',
	templateUrl: './assignment-step-http-details.component.html'
})
export class AssignmentStepHttpDetailsComponent extends BaseComponent {
	@Input() settings: NodeSettings;
	@Input() itemFields: WorkflowStepItemFields;

	constructor(
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}
}
