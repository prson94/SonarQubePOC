import { ChangeDetectorRef, Component, Input, OnChanges } from '@angular/core';
import { LinkClickInterceptor } from '../../../../../services/href-click-service';
import { WorkflowService } from '../../../../../services/workflow.service';
import { AssignmentVersionItem } from '../../../../../models/workflow.model';

@Component({
	selector: 'd3s-workflow-version-details',
	templateUrl: './workflow-version-details.component.html',
	styleUrls: ['./workflow-version-details.component.less']
})
export class WorkflowVersionDetailsComponent implements OnChanges {
	@Input({ required: true }) workflowTypeUid: string;
	@Input({ required: true }) workflowTypeVersion: number;
	@Input({ required: true }) title: string;

	isLoading: boolean;
	selectedAssignmentVersion: AssignmentVersionItem;

	constructor(private workflowService: WorkflowService, private linkClickInterceptor: LinkClickInterceptor, private changeDetector: ChangeDetectorRef) {
	}

	ngOnChanges(): void {
		this.loadAssignmentsByVersion();
	}

	onClickResource(event: MouseEvent): void {
		if (this.selectedAssignmentVersion) {
			this.linkClickInterceptor.sendEvent(event, {
				ResourceUid: this.selectedAssignmentVersion.UpdatedByUid
			}, 'users/' + this.selectedAssignmentVersion.UpdatedByUid);
		}
	}

	private loadAssignmentsByVersion(): void {
		this.isLoading = true;
		const advancedFilterString: string = this.workflowTypeVersion ? `(Version eq ${this.workflowTypeVersion})` : '';
		this.workflowService.getAssignmentsByVersion(1, 1, '', advancedFilterString, '', null, '', this.workflowTypeUid).subscribe((response) => {
			this.selectedAssignmentVersion = response.items?.[0];
			this.isLoading = false;
			this.changeDetector.markForCheck();
		});
	}
}
