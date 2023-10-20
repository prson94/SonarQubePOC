import { Component, EventEmitter, HostListener, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { WorkflowDiagramModel } from '../../../models/workflow.model';
import { WorkflowService } from '../../../services/workflow.service';

@Component({
	selector: 'd3s-workflow-information',
	templateUrl: './workflow-information.component.html'
})
export class WorkflowInformationComponent implements OnChanges {

	@Input() shouldBePadded: boolean = true;
	@Input() showHeaderLine: boolean = true;
	@Input() isSidePanel: boolean = false;
	@Input() interceptLinkClick: boolean = false;
	@Output() linkClick = new EventEmitter();
	@Output() close: EventEmitter<void> = new EventEmitter<void>();

	workflowDiagramModel: WorkflowDiagramModel;
	isLoading: boolean = false;

	private id: number = 0;
	uid: string = '00000000-0000-0000-0000-000000000000';
	version: number;

	@Input() workflowTypeId: number;

	@Input() workflowTypeUid: string;

	@Input() workflowTypeVersion: number;

	constructor(private workflowService: WorkflowService) {
	}

    ngOnChanges(changes: SimpleChanges): void {
		this.uid = this.workflowTypeUid ?? this.uid;
		this.version = this.workflowTypeVersion ?? this.version;
		this.id = this.workflowTypeId ?? this.id;
		this.getWorkflowTypeDetails()
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
