import { Input, Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { TreeNode } from 'primeng/api';

import { ArtifactTypeService } from '../../services/artifact-type.service';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { AssetTypeClass, AssetCount } from '../../models/asset.model';
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
        this.sub = this.route.params.subscribe(params => {
            try {
                let assetTypeClassString: keyof typeof AssetTypeClass = params['class'];
                this.assetTypeClass = AssetTypeClass[assetTypeClassString];
                if (!this.assetTypeClass) {
                    this.assetTypeClass = AssetTypeClass.BusinessAsset;
                }
            } catch (e) {
                this.assetTypeClass = AssetTypeClass.BusinessAsset;
            }

            switch (this.assetTypeClass) {
                case AssetTypeClass.BusinessAsset:

                    this.headerBreadcrumbService.getFolderTitle('#Business').then(res => {
                        this.folderTitle = res;
                        this.setBrowserTitle(this.titleService, res);
                        this.area = res;
                    });

                    break;
                case AssetTypeClass.TechnicalAsset:

                    this.headerBreadcrumbService.getFolderTitle('#Technical').then(res => {
                        this.folderTitle = res;
                        this.setBrowserTitle(this.titleService, res);
                        this.area = res;
                    });

                    break;
                default:
                    let className: string = AssetTypeClass[this.assetTypeClass];
                    this.folderTitle = `${className} Assets`;
                    this.setBrowserTitle(this.titleService, this.folderTitle);
                    this.area = this.folderTitle;
                    break;
            }

            this.load();
        });
    }

    private load() {
        this.isLoading = true;
        this
            .assetService
            .getAssetCountsByAssetType(this.assetTypeClass)
            .subscribe(data => {
                let dataNodes: TreeNode[] = [];

                for (let i = 0; i < data.length; i++) {
                    if (data[i].description != null)
                        data[i].description = this.htmlDecode(data[i].description);
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
                this.headerBreadcrumbService.getFolderIcon(this.folderTitle ? this.folderTitle : this.area).subscribe(res => {
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
        this.assetService.getAssetTypeLegacyData(uid)
            .subscribe(res => {
                this.router.navigateByUrl(SiteUrlHelpers.getObjectUrl('ArtifactType', res.ObjectID));
            })
    }
}