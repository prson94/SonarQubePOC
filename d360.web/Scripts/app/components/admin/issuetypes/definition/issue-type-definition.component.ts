import { Component, Input, ViewEncapsulation, EventEmitter, Output } from "@angular/core";
import { Router } from "@angular/router";
import { WorkflowIssueType } from "../../../../models/workflow.model";
import { SidePanelService } from "../../../../services/side-panel.service";
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

	constructor(private router: Router,
		private sidePanelService: SidePanelService
	) {
	}

	open(newTab: boolean = false) {
		const url = `/admin/configuration/WorkflowActions/${this.workflowIssueType.Uid}/fields`;
		if (newTab) {
			// eslint-disable-next-line detect-non-literal-fs-filename
			window.open(url, "_blank");
		}
		else {
			this.router.navigateByUrl(url);
		}
	}

	resourceClicked(uid: string) {
		this.onLinkClicked.emit({ uid, type: 'Resource' });
	}

	editClick() {
		this.sidePanelService.editClick(this.workflowIssueType);
	}
}
