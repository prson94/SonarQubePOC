import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { GroupBasePage } from './_base';

@Component({
	selector: "group-log",
	templateUrl: './log.html'
})
export class GroupChangeLog extends GroupBasePage {
	uid: string = '';

	constructor(
		private route: ActivatedRoute) {
		super();
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];
		});
	}
}
