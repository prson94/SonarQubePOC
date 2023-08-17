import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
	selector: "d3s-reference-item-type-responsibilities",
	templateUrl: './reference-item-type-responsibilities.component.html'
})
export class ReferenceItemTypeResponsibilitiesComponent {
	uid: string;
	showControls: boolean = false;

	constructor(
		private route: ActivatedRoute) {
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];
		});
	}

}
