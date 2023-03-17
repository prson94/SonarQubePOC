import { NgModule, Component, Input } from "@angular/core";
import { RouterModule } from "@angular/router";
import { CommonModule } from "@angular/common";
import { TooltipModule } from "primeng/tooltip";

@Component({
	selector: "d3s-link-display",
	templateUrl: "link-display.component.html"
})
export class LinkDisplayComponent {

	@Input() value: string;
	@Input() showTooltip: boolean = true;

	get linkName(): string {
		if (this.value === null || this.value.indexOf("|") === -1) {
			return null;
		}
		const index = this.value.indexOf("|");
		if (index === 0) {
			return this.linkUrl;
		}
		else {
			return this.value.split("|")[0];
		}
	}

	get linkUrl(): string {
		if (this.value === null || this.value.indexOf("|") === -1) {
			return null;
		}
		const index = this.value.indexOf("|");
		const url = this.value.substring(index + 1);

		if (url.startsWith("route:")) {
			return null;
		} 
		return url;
	}

	get routeUrl(): string {
		const index = (this.value == null) ? -1 : this.value.indexOf("|route:");
		if (index === -1) {
			return null;
		}
		const url = this.value.substring(index + 7);
		return url;
	}

	get isRoute(): boolean {
		return this.routeUrl !== null;
	}

	get tooltipVisible(): boolean {
		return this.showTooltip && !this.isRoute;
	}

}

@NgModule({
	imports: [
		CommonModule,
		TooltipModule,
		RouterModule
	],
	declarations: [
		LinkDisplayComponent,
	],
	exports: [
		LinkDisplayComponent,
	],
	providers: [

	]
})
export class LinkDisplayModule { }