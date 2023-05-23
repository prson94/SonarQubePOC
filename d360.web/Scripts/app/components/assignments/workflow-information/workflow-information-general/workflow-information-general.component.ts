import { Component, Input, OnInit } from '@angular/core';
import { WorkflowService } from '../../../../services/workflow.service';
import { WorkflowDiagramModel } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-workflow-information-general',
	templateUrl: './workflow-information-general.component.html',
	styleUrls: ['./workflow-information-general.component.less']
})
export class WorkflowInformationGeneralComponent implements OnInit {
	private id: number = 0;
	private uid: string = '00000000-0000-0000-0000-000000000000';
	workflowDiagramModel: WorkflowDiagramModel;
	isLoading: boolean = false;

	@Input() set workflowTypeId(value: number) {
		this.id = value;
		this.getWorkflowTypeDetails();
	}

	@Input() set workflowTypeUid(value: string) {
		this.uid = value;
		this.getWorkflowTypeDetails();
	}

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
	}

	private getWorkflowTypeDetails() {
		this.isLoading = true;
		this.workflowService.getWorkflowTypeModel(this.id, this.uid).subscribe((response: WorkflowDiagramModel): void => {
			this.isLoading = false;
			this.workflowDiagramModel = response;
		});
	}

}
