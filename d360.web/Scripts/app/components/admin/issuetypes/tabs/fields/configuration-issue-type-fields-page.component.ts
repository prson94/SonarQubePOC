import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
    selector: "d3s-configuration-issue-type-fields-page",
	templateUrl: './configuration-issue-type-fields-page.component.html'
})
export class ConfigurationIssueTypeFieldsPageComponent {
	issueTypeUid: string;

    constructor(
        private route: ActivatedRoute) {
    }

    ngOnInit() {
		this.route.params.subscribe((params) => {
			this.issueTypeUid = params["uid"];
        });
    }
}
