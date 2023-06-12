import {Component, EventEmitter, Input, Output} from '@angular/core';
import {WorkflowDiagramModel} from "../../../../models/workflow.model";
import {WorkflowService} from "../../../../services/workflow.service";

@Component({
  selector: 'd3s-workflow-version-information',
  templateUrl: './workflow-version-information.component.html',
  styleUrls: ['./workflow-version-information.component.less']
})
export class WorkflowVersionInformationComponent {
	@Input() shouldBePadded: boolean = true;
	@Input() showHeaderLine: boolean = true;
	@Input() isSidePanel: boolean = false;
	@Input() interceptLinkClick: boolean = false;
	@Output() linkClick = new EventEmitter();
	@Output() close: EventEmitter<void> = new EventEmitter<void>();

	workflowDiagramModel: WorkflowDiagramModel;
	isLoading: boolean = false;
	version: number;

	private id: number = 0;
	private uid: string = '00000000-0000-0000-0000-000000000000';

	@Input() set workflowTypeId(value: number) {
		this.id = value;
		this.getWorkflowTypeDetails();
	}

	@Input() set workflowTypeUid(value: string) {
		this.uid = value;
		this.getWorkflowTypeDetails();
	}

	get workflowTypeUid(): string {
		return this.uid;
	}

	@Input() set workflowTypeVersion(value: number){
		this.version = value;
		this.getWorkflowTypeDetails();
	}

	constructor(private workflowService: WorkflowService) {
	}

	ngOnInit(): void {
	}

	private getWorkflowTypeDetails() {
		this.isLoading = true;
		this.workflowService.getWorkflowDiagram(this.id, this.uid, this.version).subscribe(response => {
			this.isLoading = false;
			this.workflowDiagramModel = response;
		});
	}
}
