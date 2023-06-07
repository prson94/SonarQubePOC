import {
	ChangeDetectorRef,
	Component,
	ElementRef,
	HostListener,
	Input,
	OnDestroy,
	OnInit,
	QueryList,
	ViewChild,
	ViewChildren
} from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { AssetTypeApiModel, AssetTypeClass, AssetTypeLevelApiModel } from '../../models/asset.model';
import { Router } from '@angular/router';
import { AssetTypeService } from '../../services/asset-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { StringConstants } from '../../static/string-constants';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { AssetService } from '../../services/asset.service';
import { PermissionsService } from '../../services/permissions.service';
import { GridDefinitionService } from '../../services/grid-definition.service';
import { Title } from '@angular/platform-browser';
import { SecondaryNavCurrentObject, SecondaryNavItem } from '../../models/secondaryNav.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { TreeNode } from 'primeng/api';
import { GridColumn, GridField, GridScoreAllocation } from '../../models/grid-definition.model';
import { HeaderActionsService } from '../../services/header-actions.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { TreeTable } from 'primeng/treetable';
import { V2ApiFilters } from '../../models/asset-search.model';
import { WebAnalyticsService } from '../../services/web-analytics.service';
import { Filters } from '../assets-grid/advanced-filtering/advanced-filtering.models';
import { forkJoin, Observable, Subject, Subscription } from 'rxjs';
import { DataProfileService } from '../../services/dataprofile.service';
import { CompanySettingsService } from '../../services/settings.service';
import { AssetEditorComponent } from '../shared/asset-editor/asset-editor.component';
import { LinkClickInterceptor } from '../../services/href-click-service';
import { SemanticType } from '../../models/semantic-type.model';
import { NumberOfRowsByCategoryService } from '../../services/number-of-rows-by-category.service';
import { AppConstants } from '../../static/constants';
import { takeUntil } from 'rxjs/operators';
import { PopupMenu } from "../shared/controls/popup-menu/popup-menu.component";
import { AssetDetailComponent } from "../shared/asset-detail/asset-detail.component";
import { SidePanelService } from '../../services/side-panel.service';
import { IOutputData } from 'angular-split';
import { LocalStorageKey } from "../../enums/localstorage.enum";
import { UsageAction } from '../../models/web-analytics-activity.model';
import { GridSortData } from '../../services/state.service';
import { isEmpty } from "lodash-es";

/*global $localize*/

@Component({
	selector: 'd3s-hierarchy-item-structure',
	providers: [
		AssetTypeService,
		GridDefinitionService,
		PermissionsService,
		AssetService,
		WebAnalyticsService,
		DataProfileService,
	],
	templateUrl: 'hierarchy-item-structure.component.html',
	styleUrls: ['hierarchy-item-structure.component.less']
})

export class HierarchyItemStructureComponent extends BaseComponent implements OnInit, OnDestroy {
	@Input() assetTypeApiModel: AssetTypeApiModel;
	@Input() assetTypeClass: AssetTypeClass;
	@Input() assetTypeUid: string;

	@ViewChildren('tableRow') tableRows: QueryList<ElementRef>;
	gridSortData: GridSortData;

	@HostListener('document:keydown.arrowup', ['$event'])
	@HostListener('document:keydown.arrowdown', ['$event'])
	onArrowKeysDownHandler($event: KeyboardEvent) {
		$event.preventDefault();
		const selectedRow = this.tableRows.toArray().find((elRef) => {
			return elRef.nativeElement.classList.contains('p-highlight');
		});
		if (selectedRow && document.activeElement !== selectedRow.nativeElement) {
			selectedRow.nativeElement.dispatchEvent(
				new KeyboardEvent($event.type, { key: $event.key })
			);
		}
	}

	rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;

	objectTypeId: number;
	object: string;
	assetType: any;
	type: string;
	navFolderName: string;
	showDiagram: boolean = false;

	levels: AssetTypeLevelApiModel[] = [];
	maxLevelAllowed: number = 1;
	hierarchy: any[] = [];
	timeouthandle: any;
	PermissionInterval: number;

	rowID: string = 'AssetUid';
	routeSub: any;
	filterTimer: any;

	selectedParentId: number;
	treeNodeArray: TreeNode[] = [];
	selected: TreeNode;

	columnsOrg: GridColumn[] = [];
	columns: GridColumn[] = [];
	fields: GridField[] = [];
	scoreAllocations: GridScoreAllocation[] = [];

	searchValue: string = "";
	showEditor: boolean;
	showDelete: boolean;
	selectedLevel: number = 0;
	filterColumns: string[] = ['Path'];
	totalRecords: number = 0;
	totalRecordsFiltered: number = 0;
	linkColumnIndex: number = -1;
	readonly excludedLinkColumnTypes = [
		'Tag',
		'OwnershipLookup',
		'Boolean'
	];

	@ViewChild("treeTable", { static: false }) treeTable: TreeTable;
	@ViewChild("inputBox", { static: false }) filterText: any;
	@ViewChild('dynamicEditor', { static: false }) dynamicEditor: AssetEditorComponent;
	@ViewChild('assetDetail', { static: false }) assetDetail: AssetDetailComponent;

	simpleFilterValue: string = '';
	areAllExpanded: boolean = false;
	levelSearchActive: boolean = false;
	loadNodesSub: Subscription;

	sidePanelOpen: boolean = false;
	sidePanelLoading: boolean = false;
	sidePanelTab: string;
	sidePanelStorageKey: string;

	hasProfiling: boolean = false;
	dataProfile: any;

	hrefSub: Subscription;
	selectedAsset: any;
	selectedReferenceItem: any;
	selectedTag: any;
	semanticType: SemanticType;
	secondarySidePanelOpen: boolean;

	destroy = new Subject<void>();

	readonly menuKey: string = '~menu';
	baseMenuItems: any[] = [
		{ title: $localize`Open` },
		{ title: $localize`Open in New Tab` },
	];
	secondarySidePanel: string = "detail";
	isDescriptionVisible: boolean = false;
	resourceUid: string;

	constructor(
		public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
		private assetService: AssetService,
		public sidePanelService: SidePanelService,
		private assetTypeService: AssetTypeService,
		private dataProfileService: DataProfileService,
		protected gridDefinitionService: GridDefinitionService,
		private headerActionsService: HeaderActionsService,
		protected headerBreadcrumbService: HeaderBreadcrumbService,
		protected permissionsService: PermissionsService,
		protected titleService: Title,
		protected secondaryNavService: SecondaryNavService,
		protected settingsService: CompanySettingsService,
		webAnalyticsService: WebAnalyticsService,
		private router: Router,
		private changeDetectorRef: ChangeDetectorRef,
		private linkClickInterceptor: LinkClickInterceptor,
		private elRef: ElementRef
	) {
		super(settingsService);

		this.webAnalyticsService = webAnalyticsService;
		this.secondaryNavService = secondaryNavService;

		this.hrefSub = this.linkClickInterceptor.getEvents().subscribe((ev) => {
			this.linkClickInterceptor.handleEvent(this, ev);
		});
	}

	get assetEditorTitle(): string {
		return this.selected ? $localize`Edit Asset` : $localize`Create New Asset`;
	}

	get exportTooltip(): string {
		return this.canExportRecords() ? $localize`Export to Excel` : $localize`Export not available for over ${this.maxExportRows} rows`;
	}

	ngOnInit() {
		switch (this.assetTypeClass) {
			case AssetTypeClass.Model:
				this.objectType = StringConstants.ObjectTaxonomyType;
				this.object = StringConstants.ObjectTaxonomy;
				this.objectName = $localize`Model`;
				this.navFolderName = '#Models';
				this.showDiagram = true;
				break;
			case AssetTypeClass.Policy:
				this.objectType = StringConstants.ObjectPolicyType;
				this.objectName = $localize`Policy`;
				this.object = StringConstants.ObjectPolicy;
				this.navFolderName = '#Policy';
				this.showDiagram = false;
				break;
		}

		this.gridSortData = new GridSortData("HierarchyTree_" + this.assetTypeUid);
		this.sidePanelStorageKey = 'list_' + AssetTypeClass[this.assetTypeClass] + '_' + this.settingsService.CurrentResourceID;

		const uriParams: any = {};

		const obs = new Observable((observer) => {
			if (this.assetTypeUid) {
				this.assetTypeService.getAssetTypeObjectAndID(this.assetTypeUid).subscribe((response) => {
					this.objectTypeId = response.ObjectID;
					observer.next();
				});
			}
			else {
				observer.next();
			}
		});

		obs.subscribe((r) => {
			uriParams.obj = this.objectType;
			uriParams.objId = this.objectTypeId;
			uriParams.includelevels = "true";
			uriParams.includedashboardflag = "true";

			this.assetTypeService.getAssetTypes(uriParams).subscribe((result) => {
				this.assetType = result[0];
				this.assetTypeUid = result[0].uid;
				this.baseAssetTypeUid = this.assetTypeUid;
				this.uid = this.assetTypeUid;

				this.logAssetTypeAction(UsageAction.View, this.assetTypeUid);

				const descriptionVisibilitySavedState = localStorage.getItem(
					`${LocalStorageKey.IsAssetTypeDescriptionVisible}_${this.assetTypeApiModel.uid}`
				);
				
				if (descriptionVisibilitySavedState !== null) {
					this.isDescriptionVisible = JSON.parse(descriptionVisibilitySavedState);
				} else {
					this.isDescriptionVisible = this.assetTypeApiModel.IsDescriptionVisibleByDefault;
				}

				this.levels = result[0].Levels;
				this.maxLevelAllowed = result[0].HierarchyMaximumDepth;
				this.load();
			});
		});


		this.setRowsPerPage();
		this.numberOfRowsByCategoryService.defineNumberOfRows();
	}

	setDescriptionVisibility(state: boolean): void {
		this.isDescriptionVisible = state;
		localStorage.setItem(
			`${LocalStorageKey.IsAssetTypeDescriptionVisible}_${this.assetTypeApiModel.uid}`,
			state.toString()
		);
	}

	setRowsPerPage(): void {
		this.numberOfRowsByCategoryService.rowsPerPage.pipe(
			takeUntil(this.destroy)
		).subscribe((rowsPerPage) => {
			this.rowsPerPage = rowsPerPage['Main'];
		});
	}

	ngOnDestroy() {
		if (this.loadNodesSub) {
			this.loadNodesSub.unsubscribe();
		}
		if (this.hrefSub) {
			this.hrefSub.unsubscribe();
		}

		this.destroy.next();
		this.destroy.complete();
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

    selectAsset(event: any, forceRefresh: boolean = false) {
        this.selectedAsset = this.selectedReferenceItem = this.selectedTag = null;
        this.selected = event;
		
		if (forceRefresh) {
			this.assetDetail.load();
		}

		if (this.selected && this.selected.data && this.selected.data.HasProfiling) {
			this.sidePanelLoading = true;
			this.dataProfileService.getDataProfiles(this.selected.data.AssetUid).subscribe(
				(r) => {
					if (r && r.items && r.items.length > 0) {
						this.dataProfile = r.items[0];

						forkJoin(
							this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Structure'),
							this.dataProfileService.getMatchCounts(this.dataProfile.assetUid, 'Data')
						).subscribe((res) => {
							this.dataProfile['matches'] = {
								structure: res[0],
								data: res[1]
							};
						});
					}
					this.sidePanelLoading = false;
				});
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
			if (element.tagName === 'A') {return true;}
			element = element.parentElement;
		}
		return false;
	}

	get panelApplies(): boolean {
		if (this.selected == null || this.selected.data == null || this.sidePanelTab === 'detail') {
			return true;
		}
		if (this.selected != null && this.selected.data != null && this.sidePanelTab === 'dataprofile') {
			return this.selected.data.HasProfiling;
		}
	}


	clickMenuItem(menuItem: any, item: any) {
		const key = menuItem.value.toLowerCase();
		const event = menuItem.event;
		if (key === $localize`View Information`.toLowerCase()) {
			event['from-context-method'] = 'info';
			this.showHierarchy(event, item.data);
		} else if (key === $localize`Open`.toLowerCase()) {
			event['from-context-method'] = 'open';
			this.showHierarchy(event, item.data);
		} else if (key === $localize`Open in New Tab`.toLowerCase()) {
			event['from-context-method'] = 'new-tab';
			this.showHierarchy(event, item.data);
		} else if (key === $localize`Edit`.toLowerCase()) {
			this.selectAsset(item);
			this.showEditor = true;
		} else if (key === $localize`Delete`.toLowerCase()) {
			this.selectAsset(item);
			this.showDelete = true;
		} else if (key === $localize`Add Child`.toLowerCase()) {
			this.showAdd(item.data.Level, item.data.AssetUid);

		}
	}


	load() {
		this.setObjectInfo(this.objectType, this.objectTypeId);
		this.setCommonSecondaryNavTabs({ hasAudit: true });

        this.getFieldsDefinition();
        this.PermissionInterval = 500;
        this.loadPermissions(this.permissionsService, this.objectType, this.objectTypeId).then((perms) => {
            this.PermissionInterval = 100;
        });
        this.setObjectInfo(this.objectType, this.objectTypeId);
		this.headerBreadcrumbService.setCurrentObjectInfo(this.objectType, this.objectTypeId, this.assetTypeUid);

		this.searchValue = "";
		this.buildNav();
	}

	async buildNav() {
		const currentAreaName = await this.headerBreadcrumbService
			.getAreaName(this.objectType, this.objectTypeId)
			.toPromise();

		this.headerBreadcrumbService.getFolderTitle(this.navFolderName).then((res) => {
			this.headerBreadcrumbService.clearBreadcrumbs();
			let rootUrl = '';
			switch (this.assetTypeClass) {
				case AssetTypeClass.Model:
					rootUrl = `/${SiteUrlHelpers.SITE_URL_ASSETS_CLASS_ROOT}/Model`;
					break;
				case AssetTypeClass.Policy:
					rootUrl = `/${SiteUrlHelpers.SITE_URL_ASSETS_CLASS_ROOT}/Policy`;
					break;
			}

			this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(currentAreaName ? currentAreaName : res,
				rootUrl,
				null,
				this.objectType,
				this.objectTypeId,
				null,
				null,
				false
			));
			this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.assetType.Name, SiteUrlHelpers.getAssetTypeUrl(this.assetTypeUid), undefined, this.objectType, this.assetType.ID, undefined, undefined, true));

			this.headerBreadcrumbService.getAssetFolderIcon(this.objectType, this.objectTypeId, currentAreaName ? currentAreaName : res)
				.subscribe((icon) => {
					this.secondaryNavService.setCurrentArea(this.assetType.Name, icon, this.objectName);

					this.secondaryNavService.setCurrentObject(new SecondaryNavCurrentObject(this.objectType, this.assetType.ID, this.assetType.Name, null, true, null, this.assetType.AssetTypeUID));
					this.setCommonSecondaryNavTabs({ hasAudit: true, hasOwnership: false, hasDashboard: this.assetType.HasDashboards });

					if (this.showDiagram) {
						this.secondaryNavService.showItem(new SecondaryNavItem($localize`Diagram`, 'modeldiagram', ['fa-sitemap'], `/assets/${this.baseAssetTypeUid}/diagrams`, null, 7));
					}

					if (this.auditSidebar) {
						this.auditSidebar.url = `/assets/${this.baseAssetTypeUid}/log`;
					}

					this.secondaryNavService.showHeader(true);
				});

			this.setBrowserTitle(this.titleService, this.assetType.Name);
		});
	}

	private getFieldsDefinition() {
		this.gridDefinitionService.getGridDefinition(this.objectTypeId, this.objectType).subscribe(
			(result) => {
				this.scoreAllocations = result.ScoreAllocations;
				this.columnsOrg = result.Columns;
				this.columns = result.Columns;
				this.fields = result.Fields;
				var filterfields = this.fields.filter(function (item) { return item.apiName && item.name.startsWith("Field"); });
				this.filterColumns = this.filterColumns.concat(filterfields.map(({ name }) => name));

				for (let i = 0; i < this.columns.length; i++) {
					if (this.excludedLinkColumnTypes.findIndex((e) => e === (this.columns[i] as any).fieldType) === -1) {
						this.linkColumnIndex = i;
						break;
					}
				}
			}
		);
	}

	private buildTreeNodeArray(hierarchies: any[], levelNumber: number, Parent?: string): TreeNode[] {
		const rootNodes = hierarchies.filter((x) => (Parent !== undefined ? x.ParentAssetUid === Parent : !x.ParentAssetUid));

		if (rootNodes.length === 0) {
			return null;
		}

		const res: TreeNode[] = [];


		for (const root of rootNodes) {
			const isExpanded = this.expandedNodes.indexOf(root.AssetUid) !== -1 || this.areAllExpanded;
			root.Level = levelNumber;

			root[this.menuKey] = [
				{ title: $localize`View Information` },
				{ title: $localize`Open` },
				{ title: $localize`Open in New Tab` },
			];

			if (this.displayChildAdd(levelNumber) && this.hasAddAssetPermissions()) {
				root[this.menuKey].push({ title: $localize`Add Child` });
			}

			if (root.Permissions.ModifyAsset) {
				root[this.menuKey].push({ title: $localize`Edit` });
			}

			const children = (this.buildTreeNodeArray(hierarchies, levelNumber + 1, root.AssetUid));

			if (root.Permissions.DeleteAsset && (!children || children?.length === 0)) {
				root[this.menuKey].push({ title: $localize`Delete` });
			}

			res.push({
				key: root.AssetUid,
				label: root.Path,
				expanded: isExpanded,
				data: root,
				children
			});
		}
		return res;
	}

	private buildTreeNodeArraylevelSearch(hierarchies: any, levelNumber: number): TreeNode[] {
		const rootNodes = hierarchies;

		if (rootNodes.length === 0) {
			return null;
		}

		const res: TreeNode[] = [];


		for (const root of rootNodes) {
			const isExpanded = this.areAllExpanded;
			root.Level = levelNumber;

			root[this.menuKey] = [
				{ title: $localize`View Information` },
				{ title: $localize`Open` },
				{ title: $localize`Open in New Tab` },
			];

			if (this.displayChildAdd(levelNumber) && this.hasAddAssetPermissions()) {
				root[this.menuKey].push({ title: $localize`Add Child` });
			}

			if (root.Permissions.ModifyAsset) {
				root[this.menuKey].push({ title: $localize`Edit` });
			}

			const children: TreeNode[] = [];

			if (root.Permissions.DeleteAsset && (root?.ChildID == null)) {
				root[this.menuKey].push({ title: $localize`Delete` });
			}

			res.push({
				key: root.AssetUid,
				label: root.Path,
				expanded: isExpanded,
				data: root,
				children
			});
		}
		return res;
	}

	private resetColumns(): GridColumn[] {
		const res: GridColumn[] = [];
		if ((this.levelSearchActive && this.columnsOrg.findIndex((e) => e.fieldType === "Path") !== -1) || (!this.levelSearchActive)) {
			for (const root of this.columnsOrg) {
				res.push(root);
			}
		}
		else {
			let isAddAssetPath = false;
			for (const root of this.columnsOrg) {
				res.push(root);
				if (!isAddAssetPath && this.excludedLinkColumnTypes.findIndex((e) => e === root.fieldType) === -1) {
					res.push({ text: $localize`Asset Path`, datafield: "DisplayPath", columnWidth: 0, fieldType: "Path", type: "", cellsformat: "", description: "" });
					isAddAssetPath = true;
				}
			}
		}
		return res;
	}	

	private buildScoreAllocationThresholds() {
		if (this.scoreAllocations && this.scoreAllocations.length > 0) {
			if (this.hierarchy) {
				this.hierarchy.forEach((i) => {
					this.scoreAllocations.forEach((s) => {
						var field = this.fields.find((f) => f.apiName === s.Name);
						if (field) {
							i[field.apiName + '_threshold'] = this.getThreshold(i[field.apiName], s.LowerThreshold, s.UpperThreshold);
						}
					});
				});
			}
		}
	}

	public onDeleted() {
		this.headerActionsService.emitFavoritesChange(); // favorites need to be reloaded if an object was removed        
		this.deleteSelectedTreeNode(this.selected.data.AssetUid);
		this.hierarchy = this.hierarchy.filter((x) => x.AssetUid !== this.selected.data.AssetUid);
		this.treeNodeArray = this.buildTreeNodeArray(this.hierarchy, 1);

		this.selected = null;
		this.selectedLevel = null;
		this.selectedParentId = null;
		this.showDelete = false;
		this.isLoading = false;
	}

	private deleteSelectedTreeNode(id: number): TreeNode {
		const nodes: TreeNode[] = [];

		// add root nodes
		for (let i = 0; i < this.treeNodeArray.length; i++) {
			if (this.treeNodeArray[i].data.AssetUid && this.treeNodeArray[i].data.AssetUid === id) {
				this.treeNodeArray.splice(i, 1);
				return;
			}
			nodes.push(this.treeNodeArray[i]);
		}

		//do a breadth first search for the given treenode
		if (nodes.length === 0) {
			return;
		}

		let node = nodes[0];

		while (node) {
			if (node.data.AssetUid && node.data.AssetUid === id) {
				return node;
			}

			//push children
			if (node.children) {
				for (let i = 0; i < node.children.length; i++) {
					if (node.children[i].data.AssetUid && node.children[i].data.AssetUid === id) {
						node.children.splice(i, 1);
						return;
					}
					nodes.push(node.children[i]);
				}
			}

			//remove this node
			nodes.splice(0, 1);

			if (nodes.length === 0) {
				return null;
			}
			node = nodes[0];
		}
	}

	private save($event) {
		if ($event && $event.addAnother) {
			this.showAdd(this.selectedLevel, this.selectedParentId);
			this.loadNodes(false);
		}
		else if ($event && $event.action === 'new') {
			var newUrl = '/asset/' + $event.assetUid;
			this.router.navigateByUrl(this.federateUrl(newUrl));
		}
		else {
			this.showEditor = false;
			this.loadNodes(true, { keyFieldChanged: $event.keyFieldChanged });
			this.headerActionsService.emitFavoritesChange();
			this.isLoading = false;
		}
		this.changeDetectorRef.markForCheck();

	}

	private closeEditor() {
		this.showEditor = false;
		this.selected = null;
		this.selectedLevel = null;
		this.selectedParentId = null;

	}

	private exportExcel(level: number) {
		var params = new V2ApiFilters();
		params._onlyListableFields = false;
		params._direction = this.treeTable._sortOrder === 1 ? 'ASC' : 'DESC';
		if (!isEmpty(this.treeTable._sortField)) {
			var field = this.columns.filter((f) => f.datafield === this.treeTable._sortField)[0];
			params._order = field["apiName"];
		}
		else {
			params.useTypeLevelDefaultSorts = true;
			delete params._order;
		}

		if (this.simpleFilterValue) {
			params["_simpleFilter"] = "*" + this.simpleFilterValue;
			this.areAllExpanded = true;
		}

		if (this.newAdvancedFilters && this.newAdvancedFilters.filter) {
			params["_filter"] = this.newAdvancedFilters.filter;
			this.areAllExpanded = true;
		}

		params["isForTreeGrid"] = true;

		params._isHierachyItem = true;
		this.isLoading = true;
		this.assetService.downloadAssetsExcel(this.assetTypeUid, params, 'Filtered ' + this.assetType.Name, () => { this.isLoading = false; });
	}

	private showAdd(level: number, parentId: number) {
		this.selectedParentId = parentId;
		this.selectedLevel = level;
		this.selected = null;
		this.showEditor = true;
		//reload dynamic editor if it already exists to trigger change detection
		if (this.dynamicEditor) {
			this.dynamicEditor.load();
		}
	}

	private displayChildAdd(level: number) {
		return (level < this.maxLevelAllowed);
	}

	setTreeNodeStyles(node) {
		if (!node.data) {return null;}

		const styles = {
			'font-weight': node.data.hasRelations ? 'bold' : 'normal',
		};
		return styles;
	}

	get assetTypeTitle(): string {
		if (this.levels == null) {
			return $localize`(Level Unknown Item)`;
		}

		if (!this.selected) {
			const thisLevel = this.levels.filter((x) => x.Level === this.selectedLevel + 1);

			if (thisLevel && thisLevel.length > 0)
				{return thisLevel[0].Name;}
			else
				{return $localize`(Level ${this.selectedLevel + 1}) Item`;}
		}

		const thisLevel = this.levels.filter((x) => x.Level === this.selected.data.Level);

		if (thisLevel && thisLevel.length > 0) {return thisLevel[0].Name;}
		return $localize`(Level ${this.selected.data.Level}) Item`;
	}

	getThreshold(value: string, lower: number, upper: number): string {
		if (value == null || value.length < 1)
			{return '';}
		if (value.indexOf('%') > -1) {
			value = value.replace('%', '');
		}
		if (isNaN(+value))
			{return '';}

		const v = +value;

		if (v <= lower)
			{return 'poor';}
		else if (v > lower && v <= upper)
			{return 'average';}
		else
			{return 'good';}
	}

	showHierarchy($event, asset) {
		this.assetService.getUIDetailsForAssetUID(asset.AssetUid)
			.subscribe((res) => {
				const url = SiteUrlHelpers.getAssetUrl(asset.AssetUid);
				if ($event['from-context-method']) {
					this.linkClickInterceptor.sendEvent($event, {
						Values: [{
							TooltipContext: "Preview",
							TooltipID: res.ObjectId,
							TooltipType: "Artifact",
							Value: asset.Name,
							assetTypeUid: asset.AssetTypeUid,
							uid: asset.AssetUid,
						}],
						DataType: 'Lookup'
					}, url);
				} else {
					this.router.navigateByUrl(this.federateUrl(url));
				}
			});
	}

	onSort() {
		this.gridSortData.sortField = this.treeTable.sortField;
		this.gridSortData.sortOrder = this.treeTable.sortOrder;
		this.gridSortData.save();

		setTimeout(() => this.loadNodes(), 20);
	}

	loadNodes(autoSelect: boolean = true, edit?: { keyFieldChanged: boolean }) {
		this.expandedNodes = this.treeState;
		this.areAllExpanded = false;
		this.levelSearchActive = false;
		if (this.assetTypeUid) {
			this.isLoading = true;

			if (this.loadNodesSub) {
				this.loadNodesSub.unsubscribe();
			}

			const uriParams: any = {
				_pageSize: 50000,
				_includeParent: "true",
				_pageNum: 1,
				_loadPermissionDetails: "true",
				_listColorsAsJSON: "true",
				isForTreeGrid: true
			};

			if (this.treeTable) {
				uriParams._direction = this.gridSortData.sortOrder === 1 ? 'ASC' : 'DESC';
			}
			if (this.gridSortData.sortField && this.columns.some((f) => f.datafield === this.gridSortData.sortField)) {
				const field = this.columns.filter((f) => f.datafield === this.gridSortData.sortField)[0];
				uriParams._order = field["apiName"];
			}
			else {
				uriParams.useTypeLevelDefaultSorts = true;
				delete uriParams._order;
			}

			if (this.simpleFilterValue) {
				uriParams["_simpleFilter"] = "*" + this.simpleFilterValue;
				this.areAllExpanded = true;
			}

			if (this.newAdvancedFilters && this.newAdvancedFilters.filter) {
				uriParams["_filter"] = this.newAdvancedFilters.filter;
				this.areAllExpanded = true;
				if (this.newAdvancedFilters.filter.includes("[Level]")) {
					this.levelSearchActive = true;
				}
			}

			this.loadNodesSub = this.assetService.getAssets(this.assetTypeUid, uriParams, true).subscribe((result) => {
				this.totalRecords += result.total;
				this.hierarchy = result.items;

				if (this.hierarchy.length !== 0) {
					clearTimeout(this.timeouthandle);
					const lvlnumber = this.levelSearchActive ? this.hierarchy[0]?.Level : 1;
					this.timeouthandle = window.setTimeout(() => {
						this.columns = this.resetColumns();
						if (this.levelSearchActive) {
							this.treeNodeArray = this.buildTreeNodeArraylevelSearch(this.hierarchy, lvlnumber);
						}
						else {
							this.treeNodeArray = this.buildTreeNodeArray(this.hierarchy, 1, undefined);
						}
						if (autoSelect) {
							if (this.treeNodeArray.length > 0) {
								if (this.selected && edit && !edit.keyFieldChanged) {
									const asset = this.findAssetInTree(this.treeNodeArray, this.selected.key);
									if (asset) {
										this.selectAsset(asset, true);
									} else {
										this.selectAsset(this.treeNodeArray[0]);
									}
								} else {
									this.selectAsset(this.treeNodeArray[0]);
								}
							} else {
								this.selectAsset(null);
							}
						}
						this.buildScoreAllocationThresholds();
						this.isLoading = false;
					}, this.PermissionInterval);
				}
				else {
					this.treeNodeArray = [];
					this.selectAsset(null);
					this.isLoading = false;
				}
				this.updatePaginatorIcons();
			});
		}
	}

	findAssetInTree(tree: TreeNode[], assetUid: string): TreeNode {
		if (!tree) { return; }

		for (const item of tree) {
			if (item.key === assetUid) {
				return item;
			}
			const child = this.findAssetInTree(item.children, assetUid);
			if (child) {
				return child;
			}
		}
	}

	canExportRecords() {
		var isfilter = false;

		if (this.treeTable != null) {
			if (this.treeTable.filters != null) {
				if (this.treeTable.filters["global"]) {
					isfilter = true;
				}
			}
		}

		if (isfilter) {
			return this.totalRecordsFiltered <= this.maxExportRows;
		}
		else {
			return this.totalRecords <= this.maxExportRows;
		}
	}

	newAdvancedFilters: Filters;
	advancedFiltersChanged($event) {
		this.newAdvancedFilters = $event;
		this.loadNodes();
	}

	onFiltersLoaded() {
		this.loadNodes();
	}

	expandedNodes: string[] = [];
	nodeExpanded($event) {
		this.expandedNodes.push($event.node.key);
		this.saveTreeState();
	}
	nodeCollapsed($event) {
		var idx = this.expandedNodes.indexOf($event.node.key);
		if (idx !== -1) {
			this.expandedNodes.splice(idx, 1);
		}
		this.saveTreeState();
	}

	saveTreeState() {
		localStorage.setItem(this.getNodesStateKey, JSON.stringify(this.expandedNodes));
	}

	get treeState(): string[] {
		var loadedData = localStorage.getItem(this.getNodesStateKey);
		if (!loadedData) {
			return [];
		}
		else {
			return JSON.parse(loadedData) as string[];
		}
	}

	get getNodesStateKey() {
		return "nodeState_" + this.assetTypeUid;
	}

	getAssetPath() {
		if (this.selected && this.selected?.data?.Path) {
			let path = this.selected.data.Path as string;
			path = path.substring(1, path.length - 1);
			path = path.split("].[").join(` > `);
			return this.assetType?.Path + ' > ' + path;
		}
		return this.assetType?.Path;
	}

	secondaryPanelOpen(event: any) {
		this.secondarySidePanelOpen = true;
		if (event) {
			if (event.resourceUid) {
				this.secondarySidePanel = "user";
				this.resourceUid = event.resourceUid;
			}
			if (event.semanticType) {
				this.secondarySidePanel = "detail";
				this.semanticType = event.semanticType;
			}
		} else {
			this.secondarySidePanel = "status";
		}
	}
	getlevelmessage() {
		return $localize`Filtering by level will return the child asset items only and their asset path. Results will not be shown in a hierarchy.`;
	}

	//https://jira.syncsort.com/browse/GOV-28192
	//workaround for prime ng icon issues on tree grids
	iconsMap = [
		{
			selector: '.p-paginator-next .p-paginator-icon',
			html: `<anglerighticon class="p-element p-icon-wrapper ng-star-inserted" ng-reflect-style-class="p-paginator-icon"><svg width="14" height="14" viewBox="0 0 14 14" fill="none" xmlns="http://www.w3.org/2000/svg" class="p-icon p-paginator-icon" aria-hidden="true"><path d="M5.25 11.1728C5.14929 11.1694 5.05033 11.1455 4.9592 11.1025C4.86806 11.0595 4.78666 10.9984 4.72 10.9228C4.57955 10.7822 4.50066 10.5916 4.50066 10.3928C4.50066 10.1941 4.57955 10.0035 4.72 9.86283L7.72 6.86283L4.72 3.86283C4.66067 3.71882 4.64765 3.55991 4.68275 3.40816C4.71785 3.25642 4.79932 3.11936 4.91585 3.01602C5.03238 2.91268 5.17819 2.84819 5.33305 2.83149C5.4879 2.81479 5.64411 2.84671 5.78 2.92283L9.28 6.42283C9.42045 6.56346 9.49934 6.75408 9.49934 6.95283C9.49934 7.15158 9.42045 7.34221 9.28 7.48283L5.78 10.9228C5.71333 10.9984 5.63193 11.0595 5.5408 11.1025C5.44966 11.1455 5.35071 11.1694 5.25 11.1728Z" fill="currentColor"></path></svg></anglerighticon>`
		},
		{
			selector: '.p-paginator-last .p-paginator-icon',
			html: `<angledoublerighticon class="p-element p-icon-wrapper ng-star-inserted" ng-reflect-style-class="p-paginator-icon"><svg width="14" height="14" viewBox="0 0 14 14" fill="none" xmlns="http://www.w3.org/2000/svg" class="p-icon p-paginator-icon" aria-hidden="true"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.68757 11.1451C7.7791 11.1831 7.8773 11.2024 7.9764 11.2019C8.07769 11.1985 8.17721 11.1745 8.26886 11.1312C8.36052 11.088 8.44238 11.0265 8.50943 10.9505L12.0294 7.49085C12.1707 7.34942 12.25 7.15771 12.25 6.95782C12.25 6.75794 12.1707 6.56622 12.0294 6.42479L8.50943 2.90479C8.37014 2.82159 8.20774 2.78551 8.04633 2.80192C7.88491 2.81833 7.73309 2.88635 7.6134 2.99588C7.4937 3.10541 7.41252 3.25061 7.38189 3.40994C7.35126 3.56927 7.37282 3.73423 7.44337 3.88033L10.4605 6.89748L7.44337 9.91463C7.30212 10.0561 7.22278 10.2478 7.22278 10.4477C7.22278 10.6475 7.30212 10.8393 7.44337 10.9807C7.51301 11.0512 7.59603 11.1071 7.68757 11.1451ZM1.94207 10.9505C2.07037 11.0968 2.25089 11.1871 2.44493 11.2019C2.63898 11.1871 2.81949 11.0968 2.94779 10.9505L6.46779 7.49085C6.60905 7.34942 6.68839 7.15771 6.68839 6.95782C6.68839 6.75793 6.60905 6.56622 6.46779 6.42479L2.94779 2.90479C2.80704 2.83757 2.6489 2.81563 2.49517 2.84201C2.34143 2.86839 2.19965 2.94178 2.08936 3.05207C1.97906 3.16237 1.90567 3.30415 1.8793 3.45788C1.85292 3.61162 1.87485 3.76975 1.94207 3.9105L4.95922 6.92765L1.94207 9.9448C1.81838 10.0831 1.75 10.2621 1.75 10.4477C1.75 10.6332 1.81838 10.8122 1.94207 10.9505Z" fill="currentColor"></path></svg></angledoublerighticon>`
		},
		{
			selector: '.p-paginator-prev .p-paginator-icon',
			html: `<anglelefticon class="p-element p-icon-wrapper ng-star-inserted" ng-reflect-style-class="p-paginator-icon"><svg width="14" height="14" viewBox="0 0 14 14" fill="none" xmlns="http://www.w3.org/2000/svg" class="p-icon p-paginator-icon" aria-hidden="true"><path d="M8.75 11.185C8.65146 11.1854 8.55381 11.1662 8.4628 11.1284C8.37179 11.0906 8.28924 11.0351 8.22 10.965L4.72 7.46496C4.57955 7.32433 4.50066 7.13371 4.50066 6.93496C4.50066 6.73621 4.57955 6.54558 4.72 6.40496L8.22 2.93496C8.36095 2.84357 8.52851 2.80215 8.69582 2.81733C8.86312 2.83252 9.02048 2.90344 9.14268 3.01872C9.26487 3.134 9.34483 3.28696 9.36973 3.4531C9.39463 3.61924 9.36303 3.78892 9.28 3.93496L6.28 6.93496L9.28 9.93496C9.42045 10.0756 9.49934 10.2662 9.49934 10.465C9.49934 10.6637 9.42045 10.8543 9.28 10.995C9.13526 11.1257 8.9448 11.1939 8.75 11.185Z" fill="currentColor"></path></svg></anglelefticon>`
		},
		{
			selector: '.p-paginator-first .p-paginator-icon',
			html: `<angledoublelefticon class="p-element p-icon-wrapper ng-star-inserted" ng-reflect-style-class="p-paginator-icon"><svg width="14" height="14" viewBox="0 0 14 14" fill="none" xmlns="http://www.w3.org/2000/svg" class="p-icon p-paginator-icon" aria-hidden="true"><path fill-rule="evenodd" clip-rule="evenodd" d="M5.71602 11.164C5.80782 11.2021 5.9063 11.2215 6.00569 11.221C6.20216 11.2301 6.39427 11.1612 6.54025 11.0294C6.68191 10.8875 6.76148 10.6953 6.76148 10.4948C6.76148 10.2943 6.68191 10.1021 6.54025 9.96024L3.51441 6.9344L6.54025 3.90855C6.624 3.76126 6.65587 3.59011 6.63076 3.42254C6.60564 3.25498 6.525 3.10069 6.40175 2.98442C6.2785 2.86815 6.11978 2.79662 5.95104 2.7813C5.78229 2.76598 5.61329 2.80776 5.47112 2.89994L1.97123 6.39983C1.82957 6.54167 1.75 6.73393 1.75 6.9344C1.75 7.13486 1.82957 7.32712 1.97123 7.46896L5.47112 10.9991C5.54096 11.0698 5.62422 11.1259 5.71602 11.164ZM11.0488 10.9689C11.1775 11.1156 11.3585 11.2061 11.5531 11.221C11.7477 11.2061 11.9288 11.1156 12.0574 10.9689C12.1815 10.8302 12.25 10.6506 12.25 10.4645C12.25 10.2785 12.1815 10.0989 12.0574 9.96024L9.03158 6.93439L12.0574 3.90855C12.1248 3.76739 12.1468 3.60881 12.1204 3.45463C12.0939 3.30045 12.0203 3.15826 11.9097 3.04765C11.7991 2.93703 11.6569 2.86343 11.5027 2.83698C11.3486 2.81053 11.19 2.83252 11.0488 2.89994L7.51865 6.36957C7.37699 6.51141 7.29742 6.70367 7.29742 6.90414C7.29742 7.1046 7.37699 7.29686 7.51865 7.4387L11.0488 10.9689Z" fill="currentColor"></path></svg></angledoublelefticon>`
		}
	];

	updatePaginatorIcons() {
		const element = this.elRef.nativeElement as HTMLElement;
		const paginator = element.getElementsByTagName('P-PAGINATOR');
		if (paginator.length > 0) {
			this.iconsMap.forEach((item) => {
				const el = paginator[0].querySelector(item.selector) as HTMLElement;
				if (el) {
					el.innerHTML = item.html;
				}
			});
		}
	}
}
