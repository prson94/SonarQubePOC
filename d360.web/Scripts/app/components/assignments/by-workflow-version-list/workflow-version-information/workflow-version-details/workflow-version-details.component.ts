import { Component, Input } from '@angular/core';
import { LinkClickInterceptor } from '../../../../../services/href-click-service';
import { WorkflowService } from '../../../../../services/workflow.service';
import { AssignmentVersionItem } from '../../../../../models/workflow.model';

@Component({
	selector: 'd3s-workflow-version-details',
	templateUrl: './workflow-version-details.component.html',
	styleUrls: ['./workflow-version-details.component.less']
})
export class WorkflowVersionDetailsComponent {
	@Input() set workflowTypeUid(value: string) {
		this.loadAssignmentsByVersion(value);
	}

	@Input() set workflowTypeVersion(value: number) {
		this._workflowTypeVersion = value;
		this.setSelectedAssignmentVersion();
	}

	@Input() title: string = 'Workflow Version Details';

	isLoading: boolean;
	assignmentVersionItems: AssignmentVersionItem[] = [];
	selectedAssignmentVersion: AssignmentVersionItem;

	private _workflowTypeVersion: number;

	constructor(private workflowService: WorkflowService, private linkClickInterceptor: LinkClickInterceptor) {
	}

	onClickResource(event: MouseEvent): void {
		if (this.assignmentVersionItems) {
			this.linkClickInterceptor.sendEvent(event, {
				ResourceUid: this.selectedAssignmentVersion?.UpdatedByUid
			}, 'users/' + this.selectedAssignmentVersion?.UpdatedByUid);
		}
	}

	private loadAssignmentsByVersion(workflowTypeUid: string) {
		this.isLoading = true;
		this.assignmentVersionItems = [];
		this.workflowService.getAssignmentsByVersion(1, 10, '', '', '', null, '', workflowTypeUid).subscribe((response) => {
			this.assignmentVersionItems = response.items;
			this.isLoading = false;
			this.setSelectedAssignmentVersion();
		});
	}

	private setSelectedAssignmentVersion() {
		if (this.assignmentVersionItems.length > 0 && this._workflowTypeVersion) {
			this.selectedAssignmentVersion = this.assignmentVersionItems.filter((assignmentVersionItem: AssignmentVersionItem): boolean => assignmentVersionItem.Version === this._workflowTypeVersion)?.[0];
		} else {
			this.selectedAssignmentVersion = null;
		}
	}
}
