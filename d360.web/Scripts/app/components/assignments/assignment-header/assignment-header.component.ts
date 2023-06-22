import { Component, Input } from '@angular/core';
import { Tab } from '../../shared/tabs/tabs.models';

@Component({
	selector: 'd3s-assignment-header',
	templateUrl: './assignment-header.component.html'
})
export class AssignmentHeaderComponent {
	@Input() icon: string = 'fa-list-ul';
	@Input() header: string = 'Assignments';
	@Input() showTabs: boolean = true;
	tabs: Tab[] = [
		{
			url: `/assignments`,
			title: $localize`All Assignments`
		},
		{
			url: `/assignments/by-workflow-version`,
			title: $localize`By Workflow Version`
		}
	];
}
