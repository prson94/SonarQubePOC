import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { ArtifactType } from "../../../../models/artifact-type.model";

@Component({
	selector: "d3s-reference-item-type-fields",
	templateUrl: './reference-item-type-fields.component.html'
})
export class ReferenceItemTypeFieldsComponent {
	uid: string;
	assetType: ArtifactType;
	objectName: string = null;

	constructor(
		private route: ActivatedRoute) {
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];
		});
	}

}
