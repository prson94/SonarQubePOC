import { Component, Input, NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { JsonViewerNodeComponent } from "./json-viewer-node.component";

@Component({
	selector: "ig-json-viewer",
	templateUrl: "json-viewer.component.html",
})
export class IgJsonViewerComponent {

	@Input() data: object;

	public level: number = 0;
	@Input() levelLabels: { [key: number]: { [key: string]: string } };

	constructor() {
	}
}

@NgModule({
	declarations: [
		IgJsonViewerComponent,
		JsonViewerNodeComponent
	],
	exports: [
		IgJsonViewerComponent
	]
	, imports: [
		CommonModule,
	]
})
export class JsonViewerModule { }