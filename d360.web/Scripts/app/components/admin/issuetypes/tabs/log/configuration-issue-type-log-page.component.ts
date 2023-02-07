import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
	selector: "d3s-configuration-issue-type-log-page",
	templateUrl: './configuration-issue-type-log-page.component.html',
	styleUrls: ['./configuration-issue-type-log-page.component.less'],
})
export class ConfigurationIssueTypeLogPageComponent {
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
