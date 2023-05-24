import { Component, Input } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { WorkflowStepRelationshipChangeDetail } from '../../../../models/workflow.model';
import { CompanySettingsService } from '../../../../services/settings.service';

@Component({
  selector: 'd3s-assignment-step-relationship-change-details',
  templateUrl: './assignment-step-relationship-change-details.component.html'
})
export class AssignmentStepRelationshipChangeDetailsComponent extends BaseComponent {
	@Input() relationshipChange: WorkflowStepRelationshipChangeDetail;
	constructor(
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}
}
