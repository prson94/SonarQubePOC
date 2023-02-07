import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
    selector: "d3s-configuration-issue-type-allocations-page",
	templateUrl: './configuration-issue-type-allocations-page.component.html'
})
export class ConfigurationIssueTypeAllocationsPageComponent {
    uid: string;

	constructor(
		private route: ActivatedRoute) {
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];
		});
	}
}