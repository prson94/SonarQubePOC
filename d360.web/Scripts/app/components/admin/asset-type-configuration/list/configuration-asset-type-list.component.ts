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

    async load() {
        this.loadingCounter++;
        try {
            const items = await this.assetsService.getAssetCountsByAssetType(this.assetTypeClass, false).toPromise();
            const treeNodes = items.map(AssetCount.ConvertToTreeNode);
            this.artifactTypes = AssetCount.ListToTree(treeNodes);
        }
        finally {
            this.loadingCounter--;
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

    add(uid: string | undefined) {
        if (uid == null) {
            this.router.navigateByUrl(`${this.baseUrl}/new`);
        } else {
            this.router.navigateByUrl(`${this.baseUrl}/${uid}/new`);
        }
    }

    edit(uid: string) {
        this.router.navigateByUrl(`${this.baseUrl}/${uid}/edit`);
    }

    remove(uid: string) {
        this.router.navigateByUrl(`${this.baseUrl}/${uid}/delete`);
    }

    open(uid: string) {
        this.router.navigateByUrl(`${this.baseUrl}/${uid}/fields`);
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
}
