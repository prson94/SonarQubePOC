import { Component, Input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AssetOwnerModel } from '../../../../models/security.model';
import { PropertyGroupModule } from '../../../../components/shared/controls/property-group/property-group.component';

@Component({
	selector: 'owner-detail',
	templateUrl: './owner-detail.html',
	styleUrls: ['./owner-detail.less'],
	standalone: true,
	imports: [
		PropertyGroupModule,
		RouterLink
	]
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
