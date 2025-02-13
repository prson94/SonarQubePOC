import {
	ChangeDetectorRef,
	Component,
	Input,
	Output,
	OnChanges,
	OnDestroy,
	OnInit,
	SimpleChanges,
	EventEmitter,
	ViewChild
} from '@angular/core';
import { BaseComponent } from '../base.component';
import { AssetService } from '../../../services/asset.service';
import { GridDefinitionService } from '../../../services/grid-definition.service';
import { GridColumn, GridField } from '../../../models/grid-definition.model';
import { Subject, Subscription } from 'rxjs';
import { AdvancedFiltersHelper } from '../../../static/advanced-filter-helpers';
import { CompanySettingsService } from '../../../services/settings.service';
import { Table } from 'primeng/table';
import { NumberOfRowsByCategoryService } from '../../../services/number-of-rows-by-category.service';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { IOutputData } from "angular-split";
import { SidePanelService } from "../../../services/side-panel.service";
import { PermissionsService } from '../../../services/permissions.service';
import { PopupMenuItem } from '../controls/popup-menu/popup-menu.component';
import { LinkClickInterceptor } from '../../../services/href-click-service';
import { AssetEditorComponent } from '../asset-editor/asset-editor.component';
import { cloneDeep } from "lodash-es";

/*global $localize*/

export class ReferenceItemAPIModel {
	AssetUid: string;
	DisplayPath: string;
}

@Component({
	selector: 'd3s-reference-items',
	templateUrl: './reference-items.component.html',
	styleUrls: ['reference-items.component.less'],
	providers: [AssetService, GridDefinitionService]
})

export class ReferenceItemsComponent extends BaseComponent implements OnInit, OnChanges, OnDestroy {
	subjectLoadGrid = new Subject<boolean>();

	constructor(
		public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
		private assetService: AssetService,
		private gridDefinitionService: GridDefinitionService,
		protected settingsService: CompanySettingsService,
		private cdRef: ChangeDetectorRef,
		public sidePanelService: SidePanelService,
		protected permissionsService: PermissionsService,
		private linkClickInterceptor: LinkClickInterceptor
	) {
		super(settingsService);

		this.subjectLoadGrid.pipe(
			debounceTime(300))
			.subscribe(() => {
				this.getDataDebounced();
			});
	}

	@Input() referenceItemTypeUid: string;
	@Input() typeName: string;
	@Input() hasAdd: boolean = false;
	@Input() readOnly: boolean = false;
	@Input() isForAssetDetailPage: boolean = false;
	@Input() highlightUid: string = '';
	@Input() isSidePanel: boolean = false;

	@Output() itemCount: EventEmitter<number> = new EventEmitter<number>();

	public rowsPerPage: number;
	private sortField: string;
	items: ReferenceItemAPIModel[] = [];
	private totalRecords: number = 10000;
	private destroy = new Subject<void>();

	columns: GridColumn[] = [];
	fields: GridField[] = [];

	showEditor: boolean = false;
	showDelete: boolean = false;
	private sub: Subscription;
	private getAssetSub: Subscription;

	selectedItem: ReferenceItemAPIModel = null;
	selectedForInfoPanel: unknown = null;
	sidePanelOpen: boolean = true;
	sidePanelStorageKey: string = "";
	simpleFilterValue: string = "";
	pageTitle: string = "Placeholder";
	readonly menuKey = '~menu';
	canAddReferenceItem: boolean = false;
	editorSelected: unknown = null;
	editorObjectId: number = null;
	deleteName: string = null;
	globalFilterFields: string[] = ["Code"];

	@ViewChild('dt', { static: false }) table: Table;
	@ViewChild('dynamicEditor', { static: false }) dynamicEditor: AssetEditorComponent;

	private loadParams = {
		_loadPermissionDetails: true, _includeParent: true, _direction: 'ASC', _pageSize: 10, _pageNum: 1, useGraphForParent: true, _listColorsAsJSON: true, _onlyListableFields: true, _includeOwnershipLookup: true, };

	exportMessage: string = '';
	isExportInProgress: boolean = false;
	private title: string = 'Items';
	private editReferenceItem = $localize`Edit Reference Item`;
	private addReferenceItem = $localize`Add Reference Item`;
	deleteReferenceItem = $localize`Delete Reference Item`;
	referenceItemTitle: string = "";

	ngOnInit() {
		this.exportMessage = $localize`Export not available for over ${this.maxExportRows} rows`;
		this.setRowsPerPage();
		this.sidePanelStorageKey = "side_panel_reference_items_" + this.referenceItemTypeUid;

		this.numberOfRowsByCategoryService.defineNumberOfRows(this.defaultInitialItemsPerPage, this.title);
	}

	setRowsPerPage(): void {
		this.numberOfRowsByCategoryService.rowsPerPage.pipe(
			takeUntil(this.destroy)
		).subscribe((rowsPerPage) => {
			this.rowsPerPage = rowsPerPage[this.title];
		});
	}

	// ignore complexity codacy issue
	// eslint-disable-next-line
	ngOnChanges(changes: SimpleChanges) {
		if (changes.referenceItemTypeUid && changes.referenceItemTypeUid.currentValue !== changes.referenceItemTypeUid.previousValue) {
			this.load();
			this.loadParams._direction = 'ASC';
			this.loadParams._pageNum = 1;
			this.loadParams._pageSize = 10;
			this.loadParams.useGraphForParent = true;
			this.loadParams._listColorsAsJSON = true;
			this.loadParams._onlyListableFields = true;
			this.loadParams._includeOwnershipLookup = true;
			delete this.loadParams['_simpleFilter'];
			delete this.loadParams['_filter'];
		}

		if (changes.highlightUid && changes.highlightUid.currentValue !== changes.highlightUid.previousValue && this.highlightUid) {
			const highlightedAsset = this.items.filter((a) => (a.AssetUid as string).toLowerCase() === this.highlightUid.toLowerCase());
			if (highlightedAsset && highlightedAsset[0]) {
				this.selectedItem = highlightedAsset[0];
			}
			else {
				this.load();
			}
		}
	}
	ngOnDestroy() {
		this.getAssetSub?.unsubscribe();
		this.sub?.unsubscribe();
		this.destroy.next();
		this.destroy.complete();
	}

	private load() {
		if (!this.referenceItemTypeUid) { return; }

		this.isLoading = true;

		this.permissionsService.getAssetTypePermissions(this.referenceItemTypeUid)
			.subscribe((res) => {
				this.objectPermission = res;
				this.canAddReferenceItem = this.hasAddAssetPermissions();
			});

		this.gridDefinitionService.getGridDefinition(this.referenceItemTypeUid, 'ReferenceItemType').subscribe(
			(result) => {
				this.columns = result.Columns;
				const flds = result.Columns.map((c) => c.datafield);
				this.globalFilterFields = result.Fields.filter((f) => flds.includes(f.name)).map((f) => f.apiName ?? f.name);
				this.fields = result.Fields;
				this.loadItems();
			}
		);
	}

	private loadItems() {
		this.subjectLoadGrid.next(true);
	}

	getDataDebounced() {
		if (this.getAssetSub) {
			this.getAssetSub.unsubscribe();
		}

		this.loadParams.useGraphForParent = false;

		if (this.highlightUid) {
			this.loadParams["_pageWithAsset"] = this.highlightUid;
		}

		this.isLoading = true;
		this.getAssetSub = this.assetService.getAssets(this.referenceItemTypeUid, this.loadParams).subscribe((result) => {
			this.items = result.items;

			this.items.forEach((asset) => {
				if (asset.DisplayPath) {
					const pathSegments = (asset.DisplayPath as string).split('>').map((item) => item.trim());
					for (let i = 0; i < pathSegments.length; i++) {
						// eslint-disable-next-line
						asset["PATH_SEGMENT_IDX_" + i] = pathSegments[i];
					}
				}
				this.listItemTransform(asset);
			});

			this.totalRecords = result.total;

			if (this.items.length > 0) {
				this.selectedItem = this.items[0];
			}

			if (this.highlightUid) {
				const highlighted = this.items.filter((a) => (a.AssetUid as string).toLowerCase() === this.highlightUid.toLowerCase());
				if (highlighted) {
					this.selectedItem = highlighted[0];
				}

				setTimeout(() => {
					if (this.table) {
						this.table.first = (+result.pageSize) * (+result.pageNum - 1);
						this.highlightUid = null;
						delete this.loadParams["_pageWithAsset"];
						this.cdRef.markForCheck();
					}
				}, 100);
			}

			if (this.totalRecords < 1000) {
				this.loadParams.useGraphForParent = false;
			}
			this.isLoading = false;
			this.itemCount.emit(this.totalRecords);
			this.cdRef.detectChanges();
		},
			() => { //err
				this.items = [];
				this.totalRecords = 0;
				this.isLoading = false;
				this.cdRef.detectChanges();
			});
	}

	listItemTransform(item: unknown) {
		//set menu items
		const menuItems = [];
		if (!this.isSidePanel) {
			menuItems.push({ "action": "view", "title": $localize`View Information` });
		}
		menuItems.push({ "action": "edit", "title": $localize`Edit`, "disabled": !this.hasModifyAssetPermissions() });
		menuItems.push({ "action": "delete", "title": $localize`Delete`, "disabled": !this.hasDeleteAssetPermissions() });
		item[this.menuKey] = menuItems;
	}

	clickMenuItem(item, $event: PopupMenuItem) {
		this.selectedItem = item;

		switch ($event.action) {
			case "view":
				this.expandPanel();
				break;
			case "edit":
				if (item) {
					this.editorSelected = item;
				}
				this.referenceItemTitle = this.editReferenceItem;
				this.showEditor = true;
				break;
			case "delete":
				this.deleteName = item.DisplayPath;
				this.showDelete = true;
				break;
		}
	}

	sidePanelFieldClicked($event, item) {
		if (this.isSidePanel) {
			this.linkClickInterceptor.sendEvent($event, item, "iteminformation");
			return;
		}
	}

	// ignore complexity codacy issue
	// eslint-disable-next-line
	private loadAssets(event) {
		if (event) {
			let sort = event.sortField;

			if (!event.sortField || event.sortField === '') {
				delete this.loadParams['_order'];
			} else {
				const field = this.fields.filter((x) => x.name.toLowerCase() === event.sortField.toLowerCase())[0];
				if (field) {
					sort = field.apiName;
				}

				if (event.sortField === 'Color') {
					sort = 'Color';
				}

				this.loadParams['_order'] = sort;
			}

			if (event.globalFilter && event.globalFilter.length > 0) {
				let simpleFilter: string = event.globalFilter;
				if (!simpleFilter.startsWith('*')) {
					simpleFilter = '*' + simpleFilter;
				}
				this.loadParams['_simpleFilter'] = simpleFilter;
			}
			else {
				delete this.loadParams['_simpleFilter'];
			}

			const advancedFilter = AdvancedFiltersHelper.parseFiltersFromTableFilters(event.filters, this.fields);
			if (advancedFilter.length > 0) {
				this.loadParams['_filter'] = advancedFilter;
			}
			else {
				delete this.loadParams['_filter'];
			}

			this.loadParams._direction = event.sortOrder === 1 ? 'ASC' : 'DESC';
			this.loadParams._pageSize = +event.rows;
			this.loadParams._pageNum = (+event.first / +event.rows) + 1;
		}
		this.loadItems();
	}

	private export() {
		this.isExportInProgress = true;
		const exportParam = cloneDeep(this.loadParams);
		exportParam._onlyListableFields = false;
		this.assetService
			.downloadAssetsExcel(
				this.referenceItemTypeUid,
				exportParam,
				this.typeName,
				() => { this.isExportInProgress = false; }
			);
	}

	public onDeleted() {
		this.selectedItem = null;
		this.showDelete = false;
		this.loadItems();
	}

	public saveReferenceItem() {
		this.showEditor = false;
	}
	private canExportRecords() {
		return this.totalRecords <= this.maxExportRows;
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

	selectRow(item: ReferenceItemAPIModel) {
		this.selectedItem = item;
	}

	saveItem($event) {
		this.showEditor = false;
		if ($event) {
			if ($event.addAnother) {
				this.openAddForm();
			} else {
				this.highlightUid = $event.assetUid;
			}
		}
		this.load();
	}

	openAddForm() {
		this.selectedItem = null;
		this.editorSelected = null;
		this.referenceItemTitle = this.addReferenceItem;
		this.showEditor = true;
		//reload dynamic editor if it already exists to trigger change detection
		if (this.dynamicEditor) {
			this.dynamicEditor.load();
		}

	}
}
