import { Component, Input, OnChanges, SimpleChange, ViewChild } from '@angular/core';
import { TreeTable } from 'primeng/treetable';
import { HierarchyTypeLevel } from '../../../../models/hierarchy-type-level.model';
import { LevelsService } from '../../../../services/levels.service';
import { MessagesObservableService } from '../../../../services/messages-observable.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { BaseComponent } from '../../../shared/base.component';
import { PopupMenu } from '../../../shared/controls/popup-menu/popup-menu.component';

/*global $localize*/	

@Component({
	selector: 'd3s-admin-level-grid',
	providers: [LevelsService],
	templateUrl: './admin-level-list.component.html',
})

export class AdminLevelListComponent extends BaseComponent implements OnChanges {
	@Input() objectId: number;
	@Input() objectType: string;
	@Input() maxDepth: number;

	searchText = $localize`Search...`;

	error: any;
	levels: HierarchyTypeLevel[] = [];
	showEditor: boolean = false;
	showDelete: boolean = false;
	selectedLevel: HierarchyTypeLevel = null;
	theDeleteCallback: Function;
	@ViewChild('dt', { static: false }) dt: TreeTable;

	addTooltipText: string = $localize`Add Level`;
	disabledAddTooltipText: string = $localize`The maximum number of levels available for this item have already been allocated. In order to define new levels you can either increase the maximum available levels for this item or delete an existing level for this item.`;

	constructor(
		private levelsService: LevelsService,
		private messagesService: MessagesObservableService,
		protected settingsService: CompanySettingsService) {
		super(settingsService);
		this.theDeleteCallback = this.deleteLevel.bind(this);
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if (this.objectId > 0) { this.getLevels(); }
	}

	getLevels() {
		this.isLoading = true;

		this.levelsService.getObjectLevels(this.objectId, this.objectType).subscribe(
			(levels) => {
				this.levels = levels;
				this.isLoading = false;
				this.levels.forEach((lvl) => {
					const menuItems = [];
					menuItems.push({ "title": $localize`Edit`, callback: () => { this.selectedLevel = lvl; this.showEditor = true; } });
					menuItems.push({ "title": $localize`Delete`, callback: () => { this.selectedLevel = lvl; this.showDelete = true; } });
					lvl["MenuItems"] = menuItems;
				});
			}
		);
	}

	deleteLevel(id: number) {
		this.levelsService.deleteObjectLevel(this.objectType, this.objectId, id).subscribe(
			(result) => {
				this.showMessageForResult(this.messagesService, result);
				this.showDelete = false;
				this.levels = this.levels.filter((x) => x.Level !== id);
			}
		);
	}

	add() {
		this.showEditor = true;
		this.selectedLevel = null;
	}

	closeEditor() {
		this.showEditor = false;

		if (this.selectedLevel == null && this.levels.length > 0) {
			this.selectedLevel = this.levels[0];
		}
	}

	saveLevel(event) {
		this.levelsService.saveObjectLevel(event.level, this.objectType, this.objectId, event.action).subscribe(
			(result) => {
				this.showMessageForResult(this.messagesService, result);
				this.showEditor = false;
				this.getLevels();
			}
		);
	}

	get deleteModalTitle(): string {
		return $localize`Are you sure you want to delete the level [${this.selectedLevel?.Name}]?`;
	}

	get isDisabled(): boolean {
		const numberOfItems = this.levels ? this.levels.length : 0;
		return numberOfItems >= this.maxDepth;
	}

	positionContextMenu(
		$event: MouseEvent, container: HTMLElement, floatMenu: PopupMenu, assetGridTools: HTMLElement
	): void {
		if (!assetGridTools.contains(<Node>$event.target)) {
			container.style.top = `${$event['layerY']}px`;
			container.style.left = `${$event['layerX']}px`;
			floatMenu.toggle($event);
			$event.preventDefault();
		}
	}
}
