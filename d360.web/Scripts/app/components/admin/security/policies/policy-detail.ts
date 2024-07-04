import { Component, Input, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { ReadSecurityPolicy } from '../../../../models/security.model';

/*global $localize*/

@Component({
	selector: 'policy-detail',
	templateUrl: './policy-detail.html',
	styleUrls: ['./policy-detail.less'],
	encapsulation: ViewEncapsulation.None
})
export class PolicyDetail {
	@Input() item: ReadSecurityPolicy;

	constructor(private router: Router) {

	}
}
