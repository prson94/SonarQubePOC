import { Component, Input, ViewEncapsulation } from '@angular/core';
import { SingleAssignment } from '../../../models/workflow.model';

@Component({
	selector: 'd3s-assignment-asset-list',
	templateUrl: './assignment-asset-list.component.html',
	styleUrls: ['./assignment-asset-list.component.less'],
	encapsulation: ViewEncapsulation.None
})
export class AssignmentAssetListComponent {
	@Input() items: SingleAssignment[] = [];
}
