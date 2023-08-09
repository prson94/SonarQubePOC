import { Component, Input, ViewEncapsulation } from '@angular/core';
import { SingleAssignment } from '../../../models/workflow.model';
import { AssetDetailClickEvent, AssetDetailClickType, LinkClickInterceptor } from '../../../services/href-click-service';

@Component({
	selector: 'd3s-assignment-asset-list',
	templateUrl: './assignment-asset-list.component.html',
	styleUrls: ['./assignment-asset-list.component.less'],
	encapsulation: ViewEncapsulation.None
})
export class AssignmentAssetListComponent {
	@Input() items: SingleAssignment[] = [];

	constructor(private interceptor: LinkClickInterceptor) {

	}

	assetSelected($event: MouseEvent, assignment: SingleAssignment) {
		$event.preventDefault();
		$event.stopPropagation();
		this.interceptor.sendEvent($event, { AssetUid: assignment.AssetUid }, null, 0);
	}
}
