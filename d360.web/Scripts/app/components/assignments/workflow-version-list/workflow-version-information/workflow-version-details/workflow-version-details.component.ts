import {Component, Input} from '@angular/core';
import {ChangeTypeInfo, WorkflowDiagramModel} from "../../../../../models/workflow.model";
import {WorkflowService} from "../../../../../services/workflow.service";

@Component({
  selector: 'd3s-workflow-version-details',
  templateUrl: './workflow-version-details.component.html',
  styleUrls: ['./workflow-version-details.component.less']
})
export class WorkflowVersionDetailsComponent {
	@Input() set workflowDiagramModel(value: WorkflowDiagramModel) {
		this._workflowDiagramModel = value;
		this.changeType = this.changeTypeInfos?.find((changeTypeInfo: ChangeTypeInfo): boolean => changeTypeInfo.ID === this.workflowDiagramModel?.Event?.ChangeType)?.Description;
	};

	@Input() version: number;

	get workflowDiagramModel(): WorkflowDiagramModel {
		return this._workflowDiagramModel;
	}

	changeType: string;
	private _workflowDiagramModel: WorkflowDiagramModel;
	private changeTypeInfos: ChangeTypeInfo[];

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
		this.workflowService.getChangeTypes().subscribe(response => this.changeTypeInfos = response);
	}
}
