import { Component, Input } from '@angular/core';
import { BaseComponent } from '../../../shared/base.component';
import { CompanySettingsService } from '../../../../services/settings.service';

@Component({
	selector: 'd3s-assignment-step-http-details',
	templateUrl: './assignment-step-http-details.component.html'
})
export class AssignmentStepHttpDetailsComponent extends BaseComponent {
	@Input() step: any;

	constructor(
		protected settingsService: CompanySettingsService) {
		super(settingsService);
	}
}
