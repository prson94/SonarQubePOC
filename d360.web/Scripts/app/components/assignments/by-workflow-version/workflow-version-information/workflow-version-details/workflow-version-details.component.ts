import { Component, Input } from '@angular/core';
import { WorkflowService } from "../../../../../services/workflow.service";
import { ObjectDetailService } from '../../../../../services/object-detail.service';

@Component({
	selector: 'd3s-workflow-version-details',
	templateUrl: './workflow-version-details.component.html',
	styleUrls: ['./workflow-version-details.component.less']
})
export class WorkflowVersionDetailsComponent {
	isLoading: boolean;
	versionDetails: any;

	@Input() set workflowTypeVersionId(value: number) {
		if (value) {
			this.loadVersionDetails(value);
		}
	}

	constructor(private workflowService: WorkflowService,
				private objectDetailService: ObjectDetailService) {
	}

	private loadVersionDetails(versionId: number) {
		this.isLoading = true;
		this.objectDetailService.getObjectDetail(versionId, 'Monitor').subscribe(response => {
			this.versionDetails = response;
			this.isLoading = false;
		});
	}
}
