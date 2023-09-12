import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { CompanySettingsService } from "../../../../services/settings.service";
import { BaseComponent } from "../../../shared/base.component";


@Component({
	selector: "d3s-reference-item-type-assignments",
	templateUrl: './reference-item-type-assignments.component.html'
})
export class ReferenceItemTypeAssignmentsComponent extends BaseComponent{
	uid: string;

	constructor(
		private route: ActivatedRoute,
		protected settingsService: CompanySettingsService,
	) {
		super(settingsService);
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];
		});
	}

}
