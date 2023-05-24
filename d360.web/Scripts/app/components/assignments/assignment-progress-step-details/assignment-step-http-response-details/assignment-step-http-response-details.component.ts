import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
  selector: 'd3s-assignment-step-http-response-details',
  templateUrl: './assignment-step-http-response-details.component.html'
})
export class AssignmentStepHttpResponseDetailsComponent extends BaseComponent implements OnInit {

	@Input() itemId: number
	@Input() step: any
	private inputStepId: number
	public stepName: string

	constructor(
		private workflowService: WorkflowService,
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit(): void {
		this.inputStepId = parseInt(this.step.ItemSettings.HTTPResponse.InputStepId)
		this.workflowService.getWorkflowDetailsV2(this.itemId).subscribe((workflowDetails: any) => {
			for(let i=0;i<workflowDetails.Steps.length;i++) {
				if(workflowDetails.Steps[i].ID === this.inputStepId) {
					this.stepName = workflowDetails.Steps[i].Name
					break
				}
			}
		})
	}

}
