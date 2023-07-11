import { Component, Input } from '@angular/core';
import { AssignmentVersionItem } from '../../../../../models/workflow.model';
import { LinkClickInterceptor } from '../../../../../services/href-click-service';

@Component({
	selector: 'd3s-workflow-version-details',
	templateUrl: './workflow-version-details.component.html',
	styleUrls: ['./workflow-version-details.component.less']
})
export class WorkflowVersionDetailsComponent {

	@Input() assignmentVersionItem: AssignmentVersionItem;

	constructor(private linkClickInterceptor: LinkClickInterceptor) {
	}

	onClickResource(event: MouseEvent): void {
		if(this.assignmentVersionItem) {
			this.linkClickInterceptor.sendEvent(event, {
				ResourceUid: this.assignmentVersionItem.UpdatedByUid
			}, 'users/' + this.assignmentVersionItem.UpdatedByUid);
		}
	}
}
