import { ChangeDetectorRef, Component, Input, OnChanges } from '@angular/core';
import { AssignmentByVersion } from '../../../../models/workflow.model';
import { WorkflowService } from '../../../../services/workflow.service';

@Component({
	selector: 'd3s-workflow-version-information',
	templateUrl: './workflow-version-information.component.html',
	styleUrls: ['./workflow-version-information.component.less']
})
export class WorkflowVersionInformationComponent implements OnChanges {
	assignmentByVersion: AssignmentByVersion;
	isLoading: boolean = false;

	@Input({ required: true }) workflowTypeUid: string;
	@Input({ required: true }) workflowTypeVersion: number;
	@Input() showHeader: boolean = true;
	@Input() showCountPanel: boolean = true;
	@Input() nodeClickPropagate: boolean = false;
	@Input() versionDetailsTitle: string = $localize`Workflow Version Details`;
	@Input() workflowDiagramTitle: string = $localize`Pending Assignments`;

	constructor(private workflowService: WorkflowService, private changeDetector: ChangeDetectorRef) {
	}

	ngOnChanges(): void {
		if (this.workflowTypeUid) {
			this.getWorkflowTypeDetails();
		}
	}

	private getWorkflowTypeDetails() {
		this.isLoading = true;
		const advancedFilterString: string = this.workflowTypeVersion ? `(Version eq ${this.workflowTypeVersion})` : '';
		this.workflowService.getAssignmentsByVersion(1, 1, null, advancedFilterString, null, null, null, this.workflowTypeUid).subscribe((response: AssignmentByVersion): void => {
			this.assignmentByVersion = response;
			this.isLoading = false;
			this.changeDetector.markForCheck();
		});
	}
}
