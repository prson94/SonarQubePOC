import { ChangeDetectionStrategy, Component, Input, ViewEncapsulation } from '@angular/core';
import { LinkClickInterceptor } from '../../../../services/href-click-service';
import { AssetTypeDetailField, AssetTypeDetailFieldType } from "../asset-type-detail-v2.model";

@Component({
	selector: 'ig-asset-type-detail-field',
	templateUrl: './asset-type-detail-field.component.html',
	providers: [],
	styleUrls: ['asset-type-detail-field.component.less'],
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})


export class AssetTypeDetailFieldComponent {
	@Input() field: AssetTypeDetailField;

	constructor(private linkClickInterceptor: LinkClickInterceptor) {

	}

	get fieldType(): typeof AssetTypeDetailFieldType {
		return AssetTypeDetailFieldType;
	}

	onResourceClick($event: PointerEvent) {
		$event.stopPropagation();
		$event.preventDefault();
		this.linkClickInterceptor.sendEvent($event, { type: 'Resource', uid: this.field.value.value }, "");
	}
}
