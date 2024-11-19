import { Component, Input, OnInit } from '@angular/core';
import { Tab } from '../../../shared/tabs/tabs.models';

/*global $localize*/

@Component({
	selector: 'd3s-tags-header',
	templateUrl: './tags-header.component.html',
	styleUrls: ['./tags-header.component.less'],
})
export class TagsHeaderComponent implements OnInit {

	@Input() flowContext: string = 'Tags';
	icon: string;
	iconPath: string;
	header: string;
	showTagTypes = false;
	isTagTypesOpen = false;
	tabs: Tab[] = [];

	constructor() {}

	ngOnInit(): void {
		this.header = $localize`Tags`;
		this.icon = 'fa-tag';
		this.tabs = [
			{
				url: '/admin/tags',
				title: $localize`General`
			},
		]

	}

	toggleTagTypesPanel() {
		this.isTagTypesOpen = !this.isTagTypesOpen;
		this.showTagTypes = !this.showTagTypes;
	}

}
