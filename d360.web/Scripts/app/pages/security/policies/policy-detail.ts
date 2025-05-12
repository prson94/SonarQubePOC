import { Component, Input, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { ReadSecurityPolicy } from '../../../models/security.model';
import { PropertyGroupModule } from '../../../components/shared/controls/property-group/property-group.component';
import { DirectivesModule } from '../../../directives/directives.module';

@Component({
	selector: 'policy-detail',
	templateUrl: './policy-detail.html',
	styleUrls: ['./policy-detail.less'],
	encapsulation: ViewEncapsulation.None,
	standalone: true,
	imports: [
		DirectivesModule,
		PropertyGroupModule
	]
})
export class PolicyDetail {
	@Input() item: ReadSecurityPolicy;

	constructor(private router: Router) {

	}
}
