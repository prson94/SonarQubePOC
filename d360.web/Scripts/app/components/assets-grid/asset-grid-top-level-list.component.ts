import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { TreeNode } from 'primeng/api';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { AssetCount, AssetTypeClass } from '../../models/asset.model';
import { AssetService } from '../../services/asset.service';
import { AssetGridBaseComponent } from './asset-grid-base.component';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
	selector: 'd3s-asset-grid-top-level-list',
	templateUrl: './asset-grid-top-level-list.component.html',
	providers: [AssetService],
})

export class AssetGridTopLevelListComponent extends AssetGridBaseComponent implements OnInit {
	searchFilter: string = "";
	objectType: string = "ArtifactType";
	adminType: string = "Artifacts";
	selectedRow: TreeNode;
	ArtifactTypes: TreeNode[];
	private sub: any;
	assetTypeClass: AssetTypeClass;
	public searchValue: string;

	constructor(
		private router: Router,
		private route: ActivatedRoute,
		private assetService: AssetService,
		headerBreadcrumbService: HeaderBreadcrumbService,
		private titleService: Title,
		secondaryNavService: SecondaryNavService,
		protected settingsService: CompanySettingsService
	) {
		super(headerBreadcrumbService, settingsService, secondaryNavService);
	}

	ngOnInit() {
		const assetTypeClassString: keyof typeof AssetTypeClass = this.route.snapshot.data.type;
		try {

			this.assetTypeClass = AssetTypeClass[assetTypeClassString];
			if (!this.assetTypeClass) {
				this.assetTypeClass = AssetTypeClass.BusinessAsset;
			}
		} catch (e) {
			this.assetTypeClass = AssetTypeClass.BusinessAsset;
		}

		switch (this.assetTypeClass) {
			case AssetTypeClass.BusinessAsset:

				this.headerBreadcrumbService.getFolderTitle('#Business').then((res) => {
					this.folderTitle = res;
					this.setBrowserTitle(this.titleService, res);
					this.area = res;
				});

				break;
			case AssetTypeClass.TechnicalAsset:

				this.headerBreadcrumbService.getFolderTitle('#Technical').then((res) => {
					this.folderTitle = res;
					this.setBrowserTitle(this.titleService, res);
					this.area = res;
				});

				break;
			case AssetTypeClass.Rule:
				// false alarm from codacy, $localize is declared globaly
				// eslint-disable-next-line
				const assetType = $localize`Rules`;
				this.folderTitle = assetType;
				this.setBrowserTitle(this.titleService, assetType);
				this.area = assetType;
				break;
			default:
				const className: string = AssetTypeClass[this.assetTypeClass];
				this.folderTitle = `${className} Assets`;
				this.setBrowserTitle(this.titleService, this.folderTitle);
				this.area = this.folderTitle;
				break;
		}

		this.load();
	}

	private load() {
		this.isLoading = true;
		this
			.assetService
			.getAssetCountsByAssetType(this.assetTypeClass)
			.subscribe((data) => {
				const dataNodes: TreeNode[] = [];

				for (let i = 0; i < data.length; i++) {
					if (data[i].description != null)
						{data[i].description = this.htmlDecode(data[i].description);}
					else {
						data[i].description = '';
					}

					dataNodes.push(AssetCount.ConvertToTreeNode(data[i]));
				}
				this.ArtifactTypes = AssetCount.ListToTree(dataNodes);
				if (this.ArtifactTypes != null && this.ArtifactTypes.length > 0) {
					this.selectedRow = this.ArtifactTypes[0];
				}

				this.headerBreadcrumbService.clearBreadcrumbs();
				this.headerBreadcrumbService.clearCurrentObjectInfo();
				this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.folderTitle ? this.folderTitle : this.area));
				this.headerBreadcrumbService.getFolderIcon(this.folderTitle ? this.folderTitle : this.area).subscribe((res) => {
					this.secondaryNavService.clearCurrentObject();
					this.secondaryNavService.clearItems();
					this.secondaryNavService.setCurrentArea(this.folderTitle ? this.folderTitle : this.area, res, null);
				});

				this.isLoading = false;
			}
			);
	}

	private htmlDecode(val: string): string {
		return val ? String(val).replace(/<[^>]+>/gm, '') : '';
	}

	navigate(uid: string) {
		this.router.navigateByUrl("assets/" + uid);

	}
}