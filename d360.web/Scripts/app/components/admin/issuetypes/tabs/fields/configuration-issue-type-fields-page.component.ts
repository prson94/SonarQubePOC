import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { WorkflowService } from "../../../../../services/workflow.service";

@Component({
    selector: "d3s-configuration-issue-type-fields-page",
	templateUrl: './configuration-issue-type-fields-page.component.html'
})
export class ConfigurationIssueTypeFieldsPageComponent {
	issueTypeUid: string;
	issueTypeName: string;

    constructor(
		private route: ActivatedRoute,
		private workflowService: WorkflowService) {
    }

    ngOnInit() {
		this.route.params.subscribe((params) => {
			this.issueTypeUid = params["uid"];

			this.workflowService.getActionTypeByUid(this.issueTypeUid)
				.subscribe((res) => {
					this.issueTypeName = res[0].Name;
				})
        });
    }
}
