import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

@Component({
	selector: "d3s-reference-item-type-log",
	templateUrl: './reference-item-type-log.component.html'
})
export class ReferenceItemTypeLogComponent {
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
