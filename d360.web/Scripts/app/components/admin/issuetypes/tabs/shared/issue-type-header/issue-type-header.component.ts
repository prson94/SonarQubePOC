import { Component, Input } from "@angular/core";
import { Breadcrumb } from "../../../../../../models/breadcrumb.model";
import { HeaderBreadcrumbService } from "../../../../../../services/header-breadcrumb.service";
import { Tab } from "../../../../../shared/tabs/tabs.models";


@Component({
	selector: "d3s-configuration-issue-type-header",
	templateUrl: './issue-type-header.component.html',
	styleUrls: ['./issue-type-header.component.less']
})
export class ConfigurationIssueTypeHeaderComponent {
	@Input() uid: string;

	issueType: { Name: string };

	constructor(private headerBreadcrumbService: HeaderBreadcrumbService) {

		this.headerBreadcrumbService.clearBreadcrumbs();

		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Configuration`));
		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb($localize`Workflow Actions`, `admin/configuration/WorkflowActions`));
	}

	get icon() {
		return "fa-sliders";
	}

	get header() {
		return this.issueType?.Name ?? '…';
	}

	ngOnChanges() {
		this.loadAssetType(this.uid);
	}

	async loadAssetType(uid: string) {
		if (uid !== this.uid) {
			this.issueType = null;
		}

		this.issueType = { Name: 'some issue type name' };
		this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.issueType.Name));
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
