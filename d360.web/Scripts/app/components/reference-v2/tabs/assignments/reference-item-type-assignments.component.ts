import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { LaunchDarklyService } from "@precisely/prism-ng/launch-darkly";
import { AssetService } from '../../../../services/asset.service';
import { HeaderBreadcrumbService } from "../../../../services/header-breadcrumb.service";
import { SecondaryNavService } from "../../../../services/right-sidebar.service";
import { CompanySettingsService } from "../../../../services/settings.service";
import { BaseComponent } from "../../../shared/base.component";


@Component({
	selector: "d3s-reference-item-type-assignments",
	templateUrl: './reference-item-type-assignments.component.html'
})
export class ReferenceItemTypeAssignmentsComponent extends BaseComponent{
	uid: string;

	// showMonitor: boolean = false;
	// objectID: number;
	// objectType: string;

	constructor(
		private route: ActivatedRoute,
		secondaryNavService: SecondaryNavService,
		headerBreadcrumbService: HeaderBreadcrumbService,
		protected settingsService: CompanySettingsService,
		launchDarklyService: LaunchDarklyService
	) {
		super(settingsService);
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];

			// this.assetService.GetObjectUIDetailsForUid(this.uid)
			// 	.subscribe((res) => {
			// 		this.objectID = +res.ObjectID;
			// 		this.objectType = res.Object;
			// 		this.showMonitor = true;
			// 	});
		});
	}

}
