import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AssetService } from '../../../../services/asset.service';


@Component({
	selector: "d3s-reference-item-type-workflow",
	templateUrl: './reference-item-type-workflow.component.html'
})
export class ReferenceItemTypeWorkflowComponent {
	uid: string;

	showMonitor: boolean = false;
	objectID: number;
	objectType: string;

	constructor(
		private route: ActivatedRoute,
		private assetService: AssetService
	) {
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];

			this.assetService.GetObjectUIDetailsForUid(this.uid)
				.subscribe((res) => {
					this.objectID = +res.ObjectID;
					this.objectType = res.Object;
					this.showMonitor = true;
				});
		});
	}

}
