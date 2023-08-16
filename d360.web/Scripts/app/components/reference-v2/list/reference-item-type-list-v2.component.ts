import { Component, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild, OnChanges, SimpleChanges } from '@angular/core';
import { BaseComponent } from '../../shared/base.component';
import { Title } from '@angular/platform-browser';
import { Router } from "@angular/router";
import { PermissionsService } from '../../../services/permissions.service';
import { AssetTypeService } from '../../../services/asset-type.service';
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { AssetTypeApiModel, AssetTypeClass } from '../../../models/asset.model';
import { CompanySettingsService } from '../../../services/settings.service';
import { Table } from 'primeng/table';
import { NumberOfRowsByCategoryService } from '../../../services/number-of-rows-by-category.service';
import { takeUntil } from 'rxjs/operators';
import { Subject } from 'rxjs';
import { IOutputData } from "angular-split";
import { Subscription } from "rxjs";
import { AssetDetailClickType, LinkClickInterceptor } from "../../../services/href-click-service";
import { SidePanelService } from "../../../services/side-panel.service";

/*global $localize*/

@Component({
	selector: 'd3s-reference-item-type-list',
	templateUrl: './reference-item-type-list-v2.component.html',
	styleUrls: ['./reference-item-type-list-v2.component.less'],
	providers: [PermissionsService, AssetTypeService],
})

export class ReferenceItemTypeListV2Component extends BaseComponent implements OnInit, OnDestroy, OnChanges {
	@Input() selected: AssetTypeApiModel;
	@Output() selectedChange = new EventEmitter();

	@Input() initialSelectedListUid: string;
	public rowsPerPage: number;
	public title: string = $localize`Reference Lists`;
	deleteTitle: string = $localize`Are you sure you want to delete the selected item?`;

	private destroy = new Subject<void>();
	public referenceTypes: AssetTypeApiModel[];
	private _showEditor: boolean = false;
	private _showDelete: boolean = false;
	assetTypeClass: AssetTypeClass = AssetTypeClass.Reference;
	public simpleFilterValue: string = "";

	selectedItem: AssetTypeApiModel = null;
	selectedForInfoPanel: unknown = null;
	isModalVisible: boolean = false;
	sidePanelStorageKey: string = "ReferenceItemType";
	referenceItemTypeToDelete: AssetTypeApiModel = null;
	formParentName: string = "";
	parentTypeName: string = "";
	sidePanelOpen: boolean = false;
	secondarySidePanelOpen: boolean = false;
	secondarySidePanel: string = "item";
	selectedForSecondaryPanel: unknown;
	formAssetUid: string = "";
	formParentUid: string = "";

	readonly menuKey = '~menu';
	readonly parentKey = '~parent';
	hrefSub: Subscription;

	pageTitle: string = $localize`Reference`;

	@ViewChild('dt', { static: false }) table: Table;

	get showEditor(): boolean {
		return this._showEditor;
	}

	get assetTypEditorTitle(): string {
		return this.selected != null ? $localize`Edit Reference List` : $localize`Add Reference List`;
	}
	get baseUrl() {
		return `reference`;
	}


	constructor(
		public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
		private permissionsService: PermissionsService,
		private assetTypeService: AssetTypeService,
		private messagesService: MessagesObservableService,
		public sidePanelService: SidePanelService,
		private linkClickInterceptor: LinkClickInterceptor,
		private router: Router,
		protected titleService: Title,
		protected settingsService: CompanySettingsService) {
		super(settingsService);
		this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			if (ev.type === AssetDetailClickType.Field) {
				if (ev.url === "fieldinformation") {
					this.selectedForSecondaryPanel = ev.data;
					this.secondarySidePanel = "field";
					this.secondarySidePanelOpen = true;
				}
			} else if (ev.type === AssetDetailClickType.Asset) {
				if (ev.url === "iteminformation") {
					this.selectedForSecondaryPanel = ev.data;
					this.secondarySidePanel = "item";
					this.secondarySidePanelOpen = true;
				}

			}
		});

	}

	ngOnInit() {
		this.setBrowserTitle(this.titleService, this.pageTitle);
		this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);
		this.load();
		this.setRowsPerPage();
		this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage, this.title);
	}

	setRowsPerPage(): void {
		this.numberOfRowsByCategoryService.rowsPerPage.pipe(
			takeUntil(this.destroy)
		).subscribe((rowsPerPage) => {
			this.rowsPerPage = rowsPerPage[this.title];
		});
	}

	private load() {
		this.isLoading = true;
		this.loadPermissions(this.permissionsService, "ReferenceItemType", 0);
		this.assetTypeService.getAssetTypesByClass(AssetTypeClass.Reference)
			.subscribe((data) => {
				const result = data.map((x) => (x as unknown) as AssetTypeApiModel);
				this.referenceTypes = result.sort((a, b) => a.Name.localeCompare(b.Name));
				if (this.referenceTypes.length > 0) {
					if (this.initialSelectedListUid?.length > 0) {
						const index = this.referenceTypes.findIndex((x) => x.uid === this.initialSelectedListUid);
						this.initialSelectedListUid = '';
						if (index >= 0 && index < this.referenceTypes.length) {
							// eslint-disable-next-line
							this.selected = this.referenceTypes[index];

							const page = Math.floor(index / 10);
							if (this.table) {
								this.table.first = page * 10;
							}
						}
						else {
							this.selected = this.referenceTypes[0];
						}
					}
					else {
						this.selected = this.referenceTypes[0];
					}
					this.onSelect();
				}
				this.referenceTypes.forEach((i) => {
					this.listItemTransform(i);
				});

				this.isLoading = false;
			});
	}

	listItemTransform(type: AssetTypeApiModel) {
		//set menu items
		const menuItems = [];
		menuItems.push({ "title": $localize`View Information`, callback: () => { this.selectedItem = type; this.sidePanelOpen = true; } });
		menuItems.push({ "title": $localize`Open`, callback: () => this.openItem(type.uid) });
		menuItems.push({ "title": $localize`Open In New Tab`, callback: () => this.openItem(type.uid, true) });
		if (this.hasModifyAssetPermissions()) { menuItems.push({ "title": $localize`Edit`, callback: () => this.openEditForm(type.uid) }); }
		if (this.hasDeleteAssetPermissions) { menuItems.push({ "title": $localize`Delete`, callback: () => { this.referenceItemTypeToDelete = type; } }); }
		type[this.menuKey] = menuItems;

		type[this.parentKey] = this.getParent(type);
	}


	getParent(item: AssetTypeApiModel): string {
		const pathSegments = item.Path.split(" / ");
		if (pathSegments.length > 1) {
			return pathSegments.splice(-2, 1)[0];
		}
		return "---";
	}

	selectRow(row: AssetTypeApiModel) {
		this.secondarySidePanelOpen = false;
		this.selectedItem = row;
		this.selectedChange.emit(row);
	}

	selectReferenceListType($event, item: AssetTypeApiModel) {
		this.openItem(item.uid);
	}

	private onSelect() {
		this.selectedChange.emit(this.selected);
	}

	onDeleteClose($event) {
		this.selectedItem = null;
		this.referenceItemTypeToDelete = null;
		if ($event) {
			this.load();
		}
	}

	ngOnDestroy() {
		this.destroy.next();
		this.destroy.complete();
	}

	ngOnChanges(changes: SimpleChanges) {
		if (changes.selectedItem && changes.selectedItem.currentValue !== changes.selectedItem.previousValue) {
			this.selectedForInfoPanel = null;
		}
	}

	expandPanel() {
		this.sidePanelService.setSidePanelState({ expanded: true });
	}

	getSidePanelWidth(): number {
		return this.sidePanelService.getSidePanelWidth(this.sidePanelOpen, this.sidePanelStorageKey);
	}

	getSidePanelMaxWidth(): number {
		return this.sidePanelService.getSidePanelMaxWidth(this.sidePanelOpen);
	}

	getSidePanelMinWidth(): number {
		return this.sidePanelService.getSidePanelMinWidth(this.sidePanelOpen);
	}

	onSidePanelDragEnd(sidePanelStorageKey: string, event: IOutputData): void {
		this.sidePanelService.onSidePanelDragEnd(sidePanelStorageKey, event);
	}

	get anySelectedItem(): unknown {
		if (this.selectedItem) {
			return this.selectedItem;
		}
		else {
			return this.selectedForInfoPanel;
		}
	}

	openEditForm(uid: string) {
		this.formAssetUid = uid;
		this.isModalVisible = true;
	}

	onEditFormClose() {
		this.isModalVisible = false;
	}
	onEditSaveFinished() {
		this.load();
	}

	openItem(uid: string, newTab: boolean = false) {
		const url = `${this.baseUrl}/${uid}/details`;
		if (newTab) {
			// eslint-disable-next-line
			window.open(url, "_blank");
		}
		else {
			this.router.navigateByUrl(this.federateUrl(url));
		}
	}
}
