import { Component, Input, OnChanges, OnInit } from '@angular/core';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowActivityType } from '../../../../models/workflow.model';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';
import { Router } from '@angular/router';

@Component({
	selector: 'd3s-pending-assignments-step',
	templateUrl: './pending-assignments-step.component.html',
	styleUrls: ['./pending-assignments-step.component.less']
})
export class PendingAssignmentsStepComponent extends BaseComponent implements OnInit, OnChanges {
	@Input() versionStepId: number;

	selection: any;

	history: any[];
	WorkflowActivityType = WorkflowActivityType;


	constructor(
		protected settingsService: CompanySettingsService,
		private workflowService: WorkflowService,
		private router: Router) {
		super(settingsService);

	}

	ngOnInit() {
		this.load();
	}

	ngOnChanges() {
		this.load();
	}

	load() {
		this.history = [];
		if (this.versionStepId != null) {
			this.isLoading = true;
			this.workflowService.getWorkflowVersionStepHistory(this.versionStepId)
				.subscribe((r) => {
					this.history = r;
					this.isLoading = false;
				});
		}
	}

	export() {
		this.workflowService.exportVersionStepHistory(this.versionStepId);
	}

	navigate(url: string) {
		this.router.navigateByUrl(this.federateUrl(url));
	}
}
