import { Component, Input, ViewEncapsulation, EventEmitter, Output } from "@angular/core";
import { Router } from "@angular/router";
import { WorkflowIssueType } from "../../../../models/workflow.model";
import { WorkflowService } from "../../../../services/workflow.service";


@Component({
	selector: 'd3s-issue-type-definition',
	templateUrl: './issue-type-definition.component.html',
	styleUrls: ['./issue-type-definition.component.less'],
	encapsulation: ViewEncapsulation.None,
	providers: [WorkflowService]
})

export class IssueTypeDefinitionComponent {
	@Input() workflowIssueType: WorkflowIssueType;
	@Input() isSidePanel: boolean = true;
	@Output() onLinkClicked = new EventEmitter();
	@Output() onEdit = new EventEmitter();

	constructor(private router: Router,
		private workflowService: WorkflowService
	) {
	}

	open(newTab: boolean = false) {
		const url = `/admin/configuration/WorkflowActions/${this.workflowIssueType.Uid}/fields`;
		if (newTab) {
			window.open(url, "_blank");
		}
		else {
			this.router.navigateByUrl(url);
		}
	}

	resourceClicked(uid: string) {
		this.onLinkClicked.emit({ uid, type: 'Resource' });
	}
}
