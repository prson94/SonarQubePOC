import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
	selector: "d3s-reference-item-type-relationships",
	templateUrl: './reference-item-type-relationships.component.html'
})
export class ReferenceItemTypeRelationshipsComponent {
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
