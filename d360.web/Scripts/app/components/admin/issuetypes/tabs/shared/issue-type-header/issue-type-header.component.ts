import { Component, Input } from "@angular/core";
import { Breadcrumb } from "../../../../../../models/breadcrumb.model";
import { WorkflowIssueType } from "../../../../../../models/workflow.model";
import { HeaderBreadcrumbService } from "../../../../../../services/header-breadcrumb.service";
import { WorkflowService } from "../../../../../../services/workflow.service";
import { Tab } from "../../../../../shared/tabs/tabs.models";

/*global $localize*/

@Component({
	selector: "d3s-configuration-issue-type-header",
	templateUrl: './issue-type-header.component.html',
	styleUrls: ['./issue-type-header.component.less']
})
export class ConfigurationIssueTypeHeaderComponent {
	@Input() uid: string;

	workflowIssueType: WorkflowIssueType;

	constructor(private headerBreadcrumbService: HeaderBreadcrumbService,
		private workflowService: WorkflowService
	) {

		this.headerBreadcrumbService.clearBreadcrumbs();

		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Configuration`));
		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Workflow Actions`, `admin/configuration/WorkflowActions`));
	}

	get icon() {
		return "fa-sliders";
	}

	get header() {
		return this.workflowIssueType?.Name ?? '…';
	}

	ngOnChanges() {
		this.loadAssetType(this.uid);
	}

	async loadAssetType(uid: string) {
		if (uid !== this.uid) {
			this.workflowIssueType = null;
		}

		this.workflowIssueType = await this.workflowService.getIssueByUID(this.uid).toPromise();
		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.workflowIssueType.Name));
	}

	get tabs(): Tab[] {
		const baseUrl = `/admin/configuration/WorkflowActions/${this.uid}`;
		return [
			{
				url: `${baseUrl}/fields`,
				title: $localize`Field Definition`
			},
			{
				url: `${baseUrl}/allocations`,
				title: $localize`Allocations`
			},
			{
				url: `${baseUrl}/log`,
				title: $localize`Change Log`
			}
		];
	}
}
