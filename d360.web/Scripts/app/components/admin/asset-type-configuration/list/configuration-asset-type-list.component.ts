import * as _ from "lodash";
import { Component, Input } from "@angular/core";
import { TreeNode } from "primeng/api";
import { Subject } from "rxjs";
import { AssetCount, AssetTypeClass } from "../../../../models/asset.model";
import { AssetService } from "../../../../services/asset.service";
import { NumberOfRowsByCategoryService } from "../../../../services/number-of-rows-by-category.service";
import { AppConstants } from "../../../../static/constants";
import { takeUntil } from "rxjs/operators";
import { Router } from "@angular/router";
import { featuresToTypeClasses } from "../shared/featuresToTypeClasses";
import { CompanySettingsService } from "../../../../services/settings.service";
import { CompanySettingEnum } from "../../../../models/settings.model";

// eslint-disable-next-line no-var
declare var CurrentResourceID;

@Component({
	selector: "d3s-configuration-asset-type-list",
	templateUrl: './configuration-asset-type-list.component.html',
	styleUrls: ['./configuration-asset-type-list.component.less'],
})
export class ConfigurationAssetTypeListComponent {
	@Input() assetTypeClass: AssetTypeClass;

	rowsPerPage: number = AppConstants.DEFAULT_ROWS_PER_PAGE;
	defaultPagingOptions = AppConstants.DEFAULT_PAGING_OPTIONS;

	selectedRow: TreeNode;

	artifactTypes = [];
	loadingCounter = 0;
	dataCyPrefix = 'AssetType_';
	destroy = new Subject<void>();
	simpleFilterValue = '';
	public tabTitle: string = $localize`Admin`;

	isModalVisible: boolean = false;
	assetTypeToDelete: any;

	constructor(
		private assetsService: AssetService,
		public numberOfRowsByCategoryService: NumberOfRowsByCategoryService,
		private router: Router,
		protected settingsService: CompanySettingsService) {
	}

	ngOnChanges() {
		this.load();
	}

	get sidePanelStorageKey() {
		return 'configuration_' + this.assetTypeClass + '_' + CurrentResourceID;
	}

	async load(preselectedUid: string = null) {
		this.loadingCounter++;
		try {
			const items = await this.assetsService.getAssetCountsByAssetType(this.assetTypeClass, false).toPromise();
			const treeNodes = items.map(AssetCount.ConvertToTreeNode);
			this.artifactTypes = AssetCount.ListToTree(treeNodes);
			this.artifactTypes.forEach((type) => {
				this.setMenuItems(type);
				if (type.children) {
					type.children.forEach((childType) => {
						this.setMenuItems(childType);
					});
				}
			});

			if (preselectedUid) {
				this.selectedRow = this.artifactTypes.find((x) => x.id === preselectedUid);
			}
			else {
				this.selectedRow = _.first(this.artifactTypes);
			}
		}
		finally {
			this.loadingCounter--;
		}
	}

	setMenuItems(type) {
		let menuItems = [];
		menuItems.push({ "title": $localize`View Information`, callback: () => { this.selectedRow = type; } });
		menuItems.push({ "title": $localize`Open`, callback: () => this.open(type.data.uid) });
		menuItems.push({ "title": $localize`Open In A New Tab`, callback: () => this.open(type.data.uid, true) });
		if (this.hasAssetTypeChildsFeature) {
			menuItems.push({ "title": $localize`Add Child Asset Type`, callback: () => this.openEditForm(null, type.data.uid) });
		}
		menuItems.push({ "title": $localize`Edit`, callback: () => this.openEditForm(type.data.uid, type.data.parentUid) });
		menuItems.push({ "title": $localize`Delete`, callback: () => { this.assetTypeToDelete = type } });
		type["data"]["MenuItems"] = menuItems;
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

	onPopupMenuClick($event) { }

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
}
