import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
	selector: 'd3s-assignment-step-http-response-details',
	templateUrl: './assignment-step-http-response-details.component.html'
})
export class AssignmentStepHttpResponseDetailsComponent extends BaseComponent implements OnInit {

	@Input() workflowItemUId: string;
	@Input() step: any;
	private inputStepId: number;
	public stepName: string;
	public isLoading: boolean = false;

	constructor(
		private workflowService: WorkflowService,
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.inputStepId = parseInt(this.step.ItemSettings.HTTPResponse.InputStepId);
		this.isLoading = true;
		this.workflowService.getWorkflowDetailsV2ByUid(this.workflowItemUId).subscribe((workflowDetails) => {
			for (const element of workflowDetails.Steps) {
				if (element.ID === this.inputStepId) {
					this.stepName = element.Name;
					break;
				}
			}
			this.isLoading = false;
		});
	}

}
