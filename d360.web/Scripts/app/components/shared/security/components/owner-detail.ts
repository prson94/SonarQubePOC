import { Component, Input, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { AssetOwnerModel } from '../../../../models/security.model';

/*global $localize*/

@Component({
	selector: 'owner-detail',
	templateUrl: './owner-detail.html',
	styleUrls: ['./owner-detail.less'],
	encapsulation: ViewEncapsulation.None
})
export class OwnerDetail {
	@Input() item: AssetOwnerModel;

	constructor(private router: Router) {

	}

	getPermissionIcon(permission: number) {
		return "fa fa-check-circle enabled ig-bool";
		//return ((this.item.permissions & permission) === permission) ?
		//		"fa fa-check-circle enabled ig-bool" :
		//		"fa fa-times-circle disabled ig-bool";
	}

}
