import { ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChange, ViewChild, ViewEncapsulation } from '@angular/core';
import { Router } from '@angular/router';
import { PredicateFriendlyType } from '../../../../models/predicate.model';
import { RelationshipType, RelationshipTypeSimpleUIModel } from '../../../../models/relationship.model';
import { MessagesObservableService } from '../../../../services/messages-observable.service';
import { RelationshipsService } from '../../../../services/relationships.service';
import { CompanySettingsService } from '../../../../services/settings.service';
import { SidePanelService } from '../../../../services/side-panel.service';
import { AppConstants } from '../../../../static/constants';
import { BaseComponent } from '../../../shared/base.component';
import { PopupMenu } from '../../../shared/controls/popup-menu/popup-menu.component';
import { Table } from "primeng/table";


@Component({
	selector: 'd3s-admin-relationships-list',
	providers: [RelationshipsService],
	templateUrl: 'admin-relationships-list.component.html',
	styleUrls: ['admin-relationships-list.component.less'],
	encapsulation: ViewEncapsulation.None
})

export class AdminRelationshipsListComponent extends BaseComponent implements OnChanges {
	relationships: RelationshipTypeSimpleUIModel[] = [];

	rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;
	defaultPagingOptions = AppConstants.DEFAULT_PAGING_OPTIONS;

	@Input() filterToName: string = "";
	@Input() assetTypeUid: string;
	@Input() showTitle = true;

	@Input() selected: RelationshipTypeSimpleUIModel;
	@Output() selectedChange = new EventEmitter();

	first: number = 0;

	showEditor: boolean = false;
	showDelete: boolean = false;
	gridStorageKey: string = "admin-relationships-grid";
	simpleFilterValue: string = "";

	editorSelectedUid: string = "";

	/*global $localize*/

	@ViewChild('dt', { static: false }) dataTable: Table;

	constructor(
		private messagesService: MessagesObservableService,
		private relationshipsService: RelationshipsService,
		protected settingsService: CompanySettingsService,
		private sidePanelService: SidePanelService,
		private cdRef: ChangeDetectorRef,
		private router: Router
	) {
		super(settingsService);
		this.filterToName = '';

		this.sidePanelService.editClickSource$.subscribe((res) => {
			const data = res as RelationshipType;
			this.edit(RelationshipType.ConvertToUIModeldata(data));
		});
	}

	ngOnInit() {
		this.getRelationships();
	}

	ngOnChanges(changes: { [propName: string]: SimpleChange }) {
		if ((changes['filterToName'] && changes['filterToName'].currentValue !== changes['filterToName'].previousValue) || (changes['objectID'] && changes['objectID'].currentValue !== changes['objectID'].previousValue)) {
			this.getRelationships();
		}
	}

	private updateStorageKey() {
		if (this.assetTypeUid) {
			this.gridStorageKey = `admin-relationships-grid_${this.assetTypeUid}`;
		}
	}

	export() {
		this.relationshipsService.exportRelationshipTypes(this.filterToName ?? "", this.assetTypeUid);
	}

	getFriendlyNameForFunctionalType(type: string): string {
		let friendly: string = type;

		friendly = PredicateFriendlyType[`${type}`];

		return friendly;
	}

	getRelationships(preselectedUid: string = null) {
		this.updateStorageKey();
		this.isLoading = true;
		let obs = this.relationshipsService.getRelationshipTypes(null, true);

		if (this.assetTypeUid) {
			obs = this.relationshipsService.getRelationshipTypes(this.assetTypeUid, null);
		}

		obs.subscribe((result) => {
			this.relationships = [];
			this.relationships = result.map((rel) => RelationshipType.ConvertToUIModeldata(rel));

			this.relationships =
				this.relationships.sort((a, b) => a.Subject > b.Subject ? 1 : -1);

			this.relationships.forEach((rel) => {
				const menuItems = [];
				menuItems.push({ "title": $localize`View Information`, callback: () => { this.selected = rel; this.sidePanelService.setSidePanelState({ expanded: true }); } });
				menuItems.push({ "title": $localize`Open`, callback: () => this.open(rel.Uid) });
				// false poisitve fs.open eslint error
				// eslint-disable-next-line
				menuItems.push({ "title": $localize`Open In New Tab`, callback: () => this.open(rel.Uid, true) });

				menuItems.push({ "title": $localize`Edit`, callback: () => this.edit(rel), disabled: rel.HasRelationships });
				menuItems.push({ "title": $localize`Delete`, callback: () => { this.editorSelectedUid = rel.Uid; this.showDelete = true; } });
				menuItems.push({ "title": $localize`Export`, callback: () => { this.downloadRel(rel); }, tooltip: $localize`Export all relationships in this type` });
				rel.MenuItems = menuItems;
			});

			this.isLoading = false;
			if (this.relationships && !this.showEditor) {
				if (preselectedUid) {
					this.focusToPreselectedNode(preselectedUid);
				}
				else if (this.relationships.length > 0) {
					this.selected = this.relationships[0];
					this.selectedChange.emit(this.selected);
				}
			}
			this.checkGridState();
		});
	}

	private checkGridState() {
		if (sessionStorage.getItem(this.gridStorageKey)) {
			const gridData = JSON.parse(sessionStorage.getItem(this.gridStorageKey));

			if (gridData.filters && gridData.filters.global) {
				this.simpleFilterValue = gridData.filters.global.value;
			}

			this.cdRef.detectChanges();
		}
	}

	findRelationshipIndex(uid: string) {
		let index: number = -1;
		for (const relationship of this.relationships) {
			index++;
			if (relationship.Uid === uid) { return index; }
		}
	}

	deleteRelationship($event) {
		this.showMessageForApiResult(this.messagesService, $event);
		this.showDelete = false;
		if ($event.Success === true) {
			this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
			this.relationships.splice(this.findRelationshipIndex($event.uid), 1);
		}
	}

	onSaveRelationship(result) {
		result = result[0];
		this.showMessageForApiResult(this.messagesService, result);

		if (result.Success === true) {
			this.getRelationships(result.uid);
			this.showEditor = false;
		}
	}

	closeEditor() {
		this.showEditor = false;
		if (this.selected == null) {
			this.selected = this.relationships.length > 0 ? this.relationships[0] : null;
		}
	}

	add() {
		this.showEditor = true;
		this.selected = null;
		this.editorSelectedUid = null;
	}

	edit(rel: RelationshipTypeSimpleUIModel) {
		this.showEditor = true;
		this.selected = rel;
		this.editorSelectedUid = rel.Uid;
	}

	public downloadRel(relationship: RelationshipTypeSimpleUIModel) {
		this.relationshipsService.exportRelationshipTypeItems(relationship);
	}

	get deletePromptText(): string {
		return $localize`Are you sure you want to delete the relationship[${this.selected?.RelationshipTypeName}]?`;
	}

	open(uid: string, newTab: boolean = false) {
		const url = `/admin/relationships/${uid}/fields`;
		if (newTab) {
			window.open(url, "_blank");
		}
		else {
			this.router.navigateByUrl(url);
		}
	}

	positionContextMenu(
		$event: MouseEvent, container: HTMLElement, floatMenu: PopupMenu, assetGridTools: HTMLElement
	): void {
		if (!assetGridTools.contains(<Node>$event.target) && !this.isElementLink(<HTMLElement>$event.target)) {
			container.style.top = `${$event['layerY']}px`;
			container.style.left = `${$event['layerX']}px`;
			floatMenu.toggle($event);
			$event.preventDefault();
		}
	}

	private isElementLink(element: HTMLElement): boolean {
		while (element.parentElement) {
			if (element.tagName === 'A') { return true; }
			element = element.parentElement;
		}
		return false;
	}

	focusToPreselectedNode(preselectedUid: string) {
		try {
			this.selected = this.relationships.find((x) => x.Uid === preselectedUid);
			this.selectedChange.emit(this.selected);

			//find index of topmost parent and naviate to its page
			const idx = this.relationships.indexOf(this.selected);
			const pageNumber = Math.floor(idx / this.dataTable.rows);

			if (pageNumber >= 0) {
				this.first = pageNumber * this.dataTable.rows;
				setTimeout(() => {
					//find preselected element and focus to it
					const htmlElement = document.querySelectorAll(`[data-uid='${preselectedUid}']`)[0] as HTMLElement;
					const treeTable = document.getElementsByClassName(`p-datatable-wrapper`)[0];
					treeTable.scrollTo({ top: htmlElement.offsetTop - 200 });
				}, 250);
			}
		}
		catch {
			// we want warning here instead of all ui breaking 
			// eslint-disable-next-line
			console.warn("failed to focus element");
		}
	}
}
