import { Component, Input, OnInit } from '@angular/core';
import { WorkflowService } from '../../../../services/workflow.service';
import { ChangeTypeInfo, WorkflowDiagramModel } from '../../../../models/workflow.model';

@Component({
	selector: 'd3s-workflow-information-general',
	templateUrl: './workflow-information-general.component.html',
	styleUrls: ['./workflow-information-general.component.less']
})
export class WorkflowInformationGeneralComponent implements OnInit {
	@Input() set workflowDiagramModel(value: WorkflowDiagramModel) {
		this._workflowDiagramModel = value;
		this.changeType = this.changeTypeInfos?.find((changeTypeInfo: ChangeTypeInfo): boolean => changeTypeInfo.ID === this.workflowDiagramModel?.Event?.ChangeType)?.Description;
	}

	get workflowDiagramModel(): WorkflowDiagramModel {
		return this._workflowDiagramModel;
	}

	changeType: string;
	private _workflowDiagramModel: WorkflowDiagramModel;
	private changeTypeInfos: ChangeTypeInfo[];

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
		this.workflowService.getChangeTypes().subscribe((response: ChangeTypeInfo[]) => this.changeTypeInfos = response);
	}
}
