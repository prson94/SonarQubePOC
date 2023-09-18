import { Component } from "@angular/core";
import { ActivatedRoute } from "@angular/router";
import { AuthenticationService } from "../../../../services/authentication.service";

@Component({
	selector: "d3s-reference-item-type-responsibilities",
	templateUrl: './reference-item-type-responsibilities.component.html'
})
export class ReferenceItemTypeResponsibilitiesComponent {
	uid: string;
	showControls: boolean = false;

	constructor(
		protected authenticationService: AuthenticationService,
		private route: ActivatedRoute) {
	}

	ngOnInit() {
		this.route.params.subscribe((params) => {
			this.uid = params["uid"];
		});
		this.authenticationService.checkCurrentUserAdmin().subscribe((isAdmin) => {
			this.showControls = isAdmin;
		});

	}

}
