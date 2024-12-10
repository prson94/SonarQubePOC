import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { GroupBasePage } from "./_base";

@Component({
    selector: "group-fields",
    templateUrl: './fields.html'
})
export class GroupFieldsList extends GroupBasePage {

    constructor(
		private route: ActivatedRoute) {
		super();
    }

    ngOnInit() {
        this.route.params.subscribe((params) => {

        });
    }
}
