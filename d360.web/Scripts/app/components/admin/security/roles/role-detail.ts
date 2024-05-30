import { Component, Input, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { ReadRole } from '../../../../models/security.model';

/*global $localize*/

@Component({
	selector: 'role-detail',
	templateUrl: './role-detail.html',
	styleUrls: ['./role-detail.less'],
	encapsulation: ViewEncapsulation.None
})
export class RoleDetail {
	@Input() item: ReadRole;

	constructor(private router: Router) {

	}

	getPermissionIcon(permission: number) {
		return ((this.item.permissions & permission) === permission) ?
				"fa fa-check-circle enabled ig-bool" :
				"fa fa-times-circle disabled ig-bool";
	}

}
