import * as _ from "lodash";
import { ChangeDetectorRef, Component, Input, OnDestroy, ViewChild } from "@angular/core";
import { SelectItem, TreeNode } from "primeng/api";
import { forkJoin, Subject, Subscription } from "rxjs";
import { AssetCount, AssetTypeClass, FlowObjectType } from "../../../../models/asset.model";
import { AssetService } from "../../../../services/asset.service";
import { NumberOfRowsByCategoryService } from "../../../../services/number-of-rows-by-category.service";
import { AppConstants } from "../../../../static/constants";
import { takeUntil } from "rxjs/operators";
import { Router } from "@angular/router";
import { featuresToTypeClasses } from "../shared/featuresToTypeClasses";
import { CompanySettingsService } from "../../../../services/settings.service";
import { CompanySettingEnum } from "../../../../models/settings.model";
import { IconService } from "../../../../services/icon.service";
import { IconProperties } from "../../../../models/icon-properties.model";
import { TreeTable } from "primeng/treetable";
import { AssetTypeListSidePanelWrapperComponent } from "./asset-type-list-sidepanel-wrapper.component";

/*global $localize*/
// eslint-disable-next-line no-var
declare var CurrentResourceID;

@Component({
	selector: "d3s-configuration-asset-type-list",
	templateUrl: './configuration-asset-type-list.component.html',
	styleUrls: ['./configuration-asset-type-list.component.less']
})
export class ConfigurationAssetTypeListComponent implements OnDestroy {
	@Input() assetTypeClass: AssetTypeClass;

	rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;
	defaultPagingOptions = AppConstants.DEFAULT_PAGING_OPTIONS;

	selectedRow: TreeNode;
	first: number = 0;

	artifactTypes: TreeNode[] = [];
	dataCyPrefix = 'AssetType_';
	destroy = new Subject<void>();
	simpleFilterValue = '';
	public tabTitle: string = $localize`Admin`;
	isLoading: boolean = false;
	isModalVisible: boolean = false;
	assetTypeToDelete: any;

	gridDataSub: Subscription;
	defaultColors: SelectItem[] = [];
	icons: IconProperties[] = [];

	flatNodes = [];

	@ViewChild('dt', { static: false }) treeTable: TreeTable;
	@ViewChild('sidepanelWrapper', { static: false }) sidepanelWrapper: AssetTypeListSidePanelWrapperComponent;

	constructor(
		private assetsService: AssetService,
		private iconService: IconService,
		public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
		private router: Router,
		private cdRef: ChangeDetectorRef,
		protected settingsService: CompanySettingsService) {
	}

	ngOnChanges() {
		this.load();
	}

	ngOnDestroy() {
		if (this.gridDataSub) {
			this.gridDataSub.unsubscribe();
		}
	}

	get sidePanelStorageKey() {
		return 'configuration_' + this.assetTypeClass + '_' + CurrentResourceID;
	}

	load(preselectedUid: string = null) {
		this.isLoading = true;
		this.gridDataSub = forkJoin(
			this.assetsService.getAssetCountsByAssetType(this.assetTypeClass, false),
			this.assetsService.getAllColors(),
			this.iconService.getIconProperties()
		).subscribe((result) => {
			const items = result[0];
			this.defaultColors = result[1];
			this.icons = result[2];

			this.flatNodes = items.map(AssetCount.ConvertToTreeNode);
			this.artifactTypes = AssetCount.ListToTree(this.flatNodes, this.listItemTransform.bind(this));

			if (preselectedUid) {
				this.selectedRow = this.flatNodes.find((x => x.key === preselectedUid));

				this.focusToPreselctedNode(preselectedUid);
			}
			else {
				this.selectedRow = _.first(this.artifactTypes);
			}
			this.isLoading = false;
			this.cdRef.markForCheck();
		});
	}

	onEditClick() {
		this.openEditForm(this.selectedRow.data.uid, this.selectedRow.data.parentUid);
	}

	listItemTransform(type) {
		//set menu items
		const menuItems = [];
		menuItems.push({ "title": $localize`View Information`, callback: () => { this.selectedRow = type; this.sidepanelWrapper.expandPanel(); } });
		menuItems.push({ "title": $localize`Open`, callback: () => this.open(type.data.uid) });
		menuItems.push({ "title": $localize`Open In A New Tab`, callback: () => this.open(type.data.uid, true) });
		if (this.hasAssetTypeChildsFeature) {
			menuItems.push({ "title": $localize`Add Child Asset Type`, callback: () => this.openEditForm(null, type.data.uid) });
		}
		menuItems.push({ "title": $localize`Edit`, callback: () => this.openEditForm(type.data.uid, type.data.parentUid) });
		menuItems.push({ "title": $localize`Delete`, callback: () => { this.assetTypeToDelete = type; } });
		type.data["MenuItems"] = menuItems;

		//resolve color names
		const colorCode = (type?.data?.backColor ?? '') as string;
		const defColor = this.defaultColors.find((c) => c.title.toLowerCase() === colorCode.toLowerCase());
		type.data["backColorName"] = defColor ? defColor.value : $localize`Custom`;

		//resolve icons
		if (!type?.data?.icon) {
			type.data["iconName"] = '---';
		}
		else {
			const icon = this.icons.find((x) => x.id.toLowerCase() === type.data.icon.replace('fa-', '').toLowerCase());
			type.data["iconName"] = icon?.name;
		}

		if (this.hasFlowObjectType) {
			const flowObjectType = type.data["flowObjectType"] as FlowObjectType;

			switch (flowObjectType) {
				case FlowObjectType.Activity:
					type.data["flowObjectTypeName"] = $localize`Activity`;
					return;
				case FlowObjectType.Event:
					type.data["flowObjectTypeName"] = $localize`Event`;
					return;
				case FlowObjectType.Gateway:
					type.data["flowObjectTypeName"] = $localize`Gateway`;
					return;
			}
		}

	}

	ngOnInit() {
		this.setRowsPerPage();
		this.numberOfRowsByCategoryService.defineNumberOfRows();
	}

	setRowsPerPage(): void {
		this.numberOfRowsByCategoryService.rowsPerPage.pipe(
			takeUntil(this.destroy)
		).subscribe((rowsPerPage) => {
			this.rowsPerPage = rowsPerPage['Main'];
		});
	}

	open(uid: string, newTab: boolean = false) {
		const url = `${this.baseUrl}/${uid}/fields`;
		if (newTab) {
			window.open(url, "_blank");
		}
		else {
			this.router.navigateByUrl(url);
		}
	}

	get baseUrl() {
		return `/admin/configuration/assets/${AssetTypeClass[this.assetTypeClass]}`;
	}

	get hasAssetTypeChildsFeature() {
		return featuresToTypeClasses.assetTypeChilds.includes(this.assetTypeClass);
	}

	get hasMaxDepthColumn() {
		return featuresToTypeClasses.assetTypeMaxDepth.includes(this.assetTypeClass);
	}

	get hasBackgroundColor() {
		return featuresToTypeClasses.backgroundColor.includes(this.assetTypeClass);
	}
	get hasIcon() {
		return featuresToTypeClasses.icon.includes(this.assetTypeClass);
	}
	get hasFlowObjectType() {
		return featuresToTypeClasses.flowObjectType.includes(this.assetTypeClass);
	}

	get addNewAssetTypeWarning() {
		const governanceRoleIsSet = this.settingsService.getSettingById(CompanySettingEnum.GovernanceRoleReferenceListUid).GuidSetting.Value
			!== '00000000-0000-0000-0000-000000000000';

		if (this.assetTypeClass === AssetTypeClass.DiagramAsset && !governanceRoleIsSet) {
			return $localize`Cannot add new Diagram Asset Type before Governance Role is set.`;
		}

		return null;
	}

	formAssetUid: string;
	formParentUid: string;
	openEditForm(assetUid: string, parentUid: string) {
		this.formAssetUid = assetUid;
		this.formParentUid = parentUid;
		this.isModalVisible = true;
	}

	onEditSaveFinished($event: any) {
		this.isModalVisible = false;
		this.load(($event.Uid as string).toLowerCase());
	}

	onDeleteClose($event) {
		this.assetTypeToDelete = null;
		if ($event) {
			this.load();
		}
	}

	onEditFormClose() {
		this.isModalVisible = false;
		this.formAssetUid = null;
		this.formParentUid = null;
	}

	afterFilter() {
		if (this.simpleFilterValue) {
			this.expandAll();
		}
		else {
			this.collapseAll();
		}
	}

	public expandAll(): void {
		this.artifactTypes.forEach((node) => {
			this.expandCollapseRecursive(node, true);
		});
	}

	public collapseAll(): void {
		this.artifactTypes.forEach((node) => {
			this.expandCollapseRecursive(node, false);
		});
	}

	private expandCollapseRecursive(node: TreeNode, isExpand: boolean): void {
		node.expanded = isExpand;
		if (node.children) {
			node.children.forEach((childNode) => {
				this.expandCollapseRecursive(childNode, isExpand);
			});
		}
	}


	private focusToPreselctedNode(preselectedUid: string) {
		try {
			//populate all parents for selected node
			const parents: TreeNode[] = [];
			this.getParents(this.selectedRow, parents);

			//get top most parent, if there is no such node, our select node is top most parent
			let topMostParent = parents[parents.length - 1];
			if (!topMostParent) {
				topMostParent = this.selectedRow;
			}

			//expand all parents of selected node
			parents.forEach((parent) => {
				parent.expanded = true;
			});

			//find index of topmost parent and naviate to its page
			const idx = this.artifactTypes.indexOf(topMostParent);
			const pageNumber = Math.floor(idx / this.rowsPerPage);

			if (pageNumber >= 0) {
				this.first = pageNumber * this.rowsPerPage;
				setTimeout(() => {
					//find preselected element and focus to it
					const htmlElement = document.querySelectorAll(`[data-uid='${preselectedUid}']`)[0] as HTMLElement;
					const treeTable = document.getElementsByClassName(`p-treetable-wrapper`)[0];
					treeTable.scrollTo({ top: htmlElement.offsetTop - 200 });
				}, 250);
			}
		}
		catch {
			console.warn("failed to focus element");
		}
	}

	getParents(node: TreeNode, parentNodes: TreeNode[], lvl: number = 0) {
		if (lvl > 100) {
			return null;
		}

		if (node["parentid"]) {
			const result = this.findSelectedNode(this.flatNodes, node["parentid"]);
			parentNodes.push(result);
			this.getParents(result, parentNodes, lvl++);
		}

		return parentNodes;
	}

	findSelectedNode(nodes: TreeNode[], uid: string, lvl: number = 0) {
		let result: TreeNode;
		if (lvl > 100) {
			return null;
		}
		nodes.forEach((node) => {
			if (result) {
				return;
			}
			else if (node["key"] === uid) {
				result = node;
			}
			else {
				result = this.findSelectedNode(node.children, uid, lvl++);
			}
		});
		return result;
	}
}
