import { Component, Input } from '@angular/core';
import { SiteUrlHelpers } from '../../../static/site-url-helpers';

@Component({
	selector: 'd3s-assignment-detail-links',
	templateUrl: './assignment-detail-links.component.html',
	styleUrls: ['./assignment-detail-links.component.less']
})
export class AssignmentDetailLinksComponent {

	@Input({ required: true }) workflowItemUid: string;

	protected readonly SiteUrlHelpers = SiteUrlHelpers;
}
