import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnInit, Output, Renderer2, ViewChild } from '@angular/core';
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
export class TagsHeaderComponent implements OnInit, AfterViewInit {

	@Input() flowContext: string = 'Tags';
	@Output() onTagTypeSelected = new EventEmitter<string>();
	@ViewChild('tabsContainer', { static: false }) tabsContainer: ElementRef;

	icon: string;
	iconPath: string;
	header: string;
	showTagTypes = false;
	isTagTypesOpen = false;
	preventBodyClick = false;
	tabs: Tab[] = [];
	btnTagsText = 'Tag Types';
	get isTagTypesFeatureEnabled(): boolean {
        return this.featureFlagService.variation<boolean>(FeatureFlags.TagTypesEnabled);
    }

	constructor(
		private featureFlagService: LaunchDarklyService,
		private changeDetectorRef: ChangeDetectorRef,
		private renderer: Renderer2

	) {	}

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

	ngAfterViewInit(): void{
		this.renderer.listen('window', 'click',(e:Event)=>{
			e.stopPropagation();
			if(e.target != this.tabsContainer.nativeElement && !this.tabsContainer.nativeElement.contains(e.target)){
			  this.showTagTypes = false;
			  this.isTagTypesOpen = false;
			  this.changeDetectorRef.detectChanges();
			}
		});
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
