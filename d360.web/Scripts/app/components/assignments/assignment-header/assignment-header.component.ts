import { Component, Input, OnInit } from '@angular/core';
import { Tab } from '../../shared/tabs/tabs.models';

/*global $localize*/

@Component({
	selector: 'd3s-assignment-header',
	templateUrl: './assignment-header.component.html'
})
export class AssignmentHeaderComponent implements OnInit {
	@Input() flowContext: string = 'Assignment';
	icon: string;
	iconPath: string;
	header: string;
	showTabs: boolean = true;
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

	ngOnInit(): void {
		if (this.flowContext === 'Assignment') {
			this.showTabs = true;
			this.icon = 'fa-list-ul';
			this.header = 'Assignments';
		} else if (this.flowContext === 'Request') {
			this.showTabs = false;
			this.header = 'Requests';
			this.iconPath = '../../../../../Content/images/request-icon.svg';
		}
	}
}
