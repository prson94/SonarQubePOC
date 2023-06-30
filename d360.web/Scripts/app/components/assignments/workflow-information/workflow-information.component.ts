import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { WorkflowDiagramModel } from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';

@Component({
	selector: 'd3s-workflow-information',
	templateUrl: './workflow-information.component.html',
	styleUrls: ['./workflow-information.component.less']
})
export class WorkflowInformationComponent {

	@Input() shouldBePadded: boolean = true;
	@Input() showHeaderLine: boolean = true;
	@Input() isSidePanel: boolean = false;
	@Input() interceptLinkClick: boolean = false;
	@Output() linkClick = new EventEmitter();
	@Output() close: EventEmitter<void> = new EventEmitter<void>();

	workflowDiagramModel: WorkflowDiagramModel;
	isLoading: boolean = false;

	private id: number = 0;
	private uid: string = '00000000-0000-0000-0000-000000000000';
	private version: number;

	@Input() set workflowTypeId(value: number) {
		this.id = value;
		this.getWorkflowTypeDetails();
	}

	@Input() set workflowTypeUid(value: string) {
		this.uid = value;
		this.getWorkflowTypeDetails();
	}

	@Input() set workflowTypeVersion(value: number) {
		this.version = value;
		this.getWorkflowTypeDetails();
	}

	constructor(private workflowService: WorkflowService) {
	}

	@HostListener('document:click', ['$event'])
	clickedOutside(event: PointerEvent): void {
		if (!(event.composedPath().filter((eventTarget) => (<Element>eventTarget)?.classList?.contains('secondary-side-panel')).length > 0)) {
			this.close.emit();
		}
	}

	private getWorkflowTypeDetails() {
		this.isLoading = true;
		this.workflowService.getWorkflowDiagram(this.id, this.uid, this.version).subscribe((response: WorkflowDiagramModel): void => {
			this.isLoading = false;
			this.workflowDiagramModel = response;
		});
	}
}
