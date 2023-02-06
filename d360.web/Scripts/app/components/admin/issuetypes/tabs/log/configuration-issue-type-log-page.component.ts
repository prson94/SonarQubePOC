import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetTypeClass } from "../../../../../models/asset.model";
import { AssetTypeService } from "../../../../../services/asset-type.service";

@Component({
	selector: "d3s-configuration-issue-type-log-page",
	templateUrl: './configuration-issue-type-log-page.component.html',
	styleUrls: ['./configuration-issue-type-log-page.component.less'],
})
export class ConfigurationIssueTypeLogPageComponent {
	uid: string;


	constructor(
		private route: ActivatedRoute,
		private assetTypeService: AssetTypeService) {
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];
		});
	}
}
