import { Component, Input, OnInit } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { WorkflowStepRelationshipChangeDetail } from '../../../../models/workflow.model';
import { CompanySettingsService } from '../../../../services/settings.service';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
	selector: 'd3s-assignment-step-relationship-change-details',
	templateUrl: './assignment-step-relationship-change-details.component.html'
})
export class AssignmentStepRelationshipChangeDetailsComponent extends BaseComponent implements OnInit {
	@Input() relationshipChange: WorkflowStepRelationshipChangeDetail;
	@Input() relationshipUpdate: any;
	@Input() workflowItemUId: string;
	public relationshipFormField: string;
	public isLoading: boolean = false;

	constructor(
		private workflowService: WorkflowService,
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}

	ngOnInit(): void {
		let formStepId: number = parseInt(this.relationshipUpdate?.Relationship['@FormStepId']) ?? null;
		let formFieldId: string = this.relationshipUpdate?.Relationship['@FormFieldId'] ?? null;
		this.isLoading = true;
		if (formStepId && formFieldId) {
			this.workflowService.getWorkflowDetailsV2ByUid(this.workflowItemUId).subscribe((workflowDetails: any) => {
				for (const step of workflowDetails.ItemSteps) {
					if (step.StepID === formStepId) {
						this.workflowService.getWorkflowStepDetail(step.ID).subscribe((stepDetails: any) => {
							if (stepDetails?.Fields?.form?.field) {
								for (const formField of stepDetails.Fields.form.field) {
									if (formField['@id'] === formFieldId) {
										this.relationshipFormField = formField['@label'] ?? '';
										break;
									}
								}
							}
						});
						break;
					}
				}
				this.isLoading = false;
			});
		}
	}
}
