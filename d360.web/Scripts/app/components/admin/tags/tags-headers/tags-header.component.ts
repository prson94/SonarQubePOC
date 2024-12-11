import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Tab } from '../../../shared/tabs/tabs.models';
import { TagTypesViewModel } from '../tag-types/tag-types.model';
import { LaunchDarklyService } from '@precisely/prism-ng/launch-darkly';
import { FeatureFlags } from '../../../../services/feature-flags.enum';

/*global $localize*/

@Component({
	selector: 'd3s-tags-header',
	templateUrl: './tags-header.component.html',
	styleUrls: ['./tags-header.component.less'],
})
export class TagsHeaderComponent implements OnInit {

	@Input() flowContext: string = 'Tags';
	@Output() onTagTypeSelected = new EventEmitter<string>();

	icon: string;
	iconPath: string;
	header: string;
	showTagTypes = false;
	isTagTypesOpen = false;
	tabs: Tab[] = [];
	btnTagsText = 'Tag Types';
	get isTagTypesFeatureEnabled(): boolean {
        return this.featureFlagService.variation<boolean>(FeatureFlags.TagTypesEnabled);
    }

	constructor(private featureFlagService: LaunchDarklyService) {}

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
		
	tagTypeSelectedHandler(tagType: TagTypesViewModel){
		this.onTagTypeSelected.emit(tagType?.uid);
		this.btnTagsText = tagType?.Value ?? 'Tag Types';
		this.toggleTagTypesPanel();
	} 

}
