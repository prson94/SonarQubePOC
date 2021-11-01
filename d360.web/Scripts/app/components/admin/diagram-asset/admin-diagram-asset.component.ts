import { Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { TreeNode } from 'primeng/api';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { SecondaryNavService } from '../../../services/right-sidebar.service';
import { AuditService } from '../../../services/audit.service';
import { StateService } from '../../../services/state.service';
import { ArtifactTypeService } from '../../../services/artifact-type.service';
import { AdminBaseComponent } from '../admin-base.component'
import { MessagesObservableService } from '../../../services/messages-observable.service';
import { ActivatedRoute, Router } from '@angular/router';
import { AssetTypeClass, AssetCount } from '../../../models/asset.model';
import { TreeTable } from 'primeng/treetable';
import { AssetService } from '../../../services/asset.service';
import { CompanySettingsService } from '../../../services/settings.service';
import { CompanySettingEnum } from '../../../models/settings.model';
import { AssetTypeService } from '../../../services/asset-type.service';

@Component({
    selector: 'd3s-admin-diagram-asset',
    providers: [AssetTypeService, AuditService, AssetService],
    templateUrl: './admin-diagram-asset.component.html'
})

export class AdminDiagramAssetComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    searchFilter: string = "";
    objectType: string = "TaskType";
    addClassName: string;
    selectedRow: any;
    private sub: any;
    isAdding = false;
    isEditing = false;
    isDeleting = false;
    isEditingFieldType = false;
    isAddingFieldType = false;
    artifactTypes: any[];
    editorModel: any;
    theDeleteCallback: Function;
    assetTypeClass: AssetTypeClass;
    formTitle: string;

    searchValue: string = '';
    @ViewChild("dt", { static: false }) dt: TreeTable;

    private disableAdd: boolean = false;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private stateService: StateService,
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private assetTypeService: AssetTypeService,
        private assetsService: AssetService,
        titleService: Title,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.theDeleteCallback = this.deleteArtifactType.bind(this);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe(params => {
            this.assetTypeClass = AssetTypeClass.DiagramAsset;

            let className: string = "Diagram Asset";
            this.addClassName = "Add " + className;
            let singularLabel: string = `${className} Type`;

            this.tabTitle = `${singularLabel}s`;
            this.formTitle = `Edit ${singularLabel}`;
            this.load();
        });

    }

    selectedItemChange() {
        this.loadDataAndExecuteAction(() => {
            this.buildSecondaryNavigationForObject(this.selectedRow ? this.selectedRow.ID : 0, this.objectType, null, this.assetTypeClass);
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load(uid: string = '') {
        this.isLoading = true;
        this.assetsService.getAssetCountsByAssetType(this.assetTypeClass)
            .subscribe(data => {
                this.artifactTypes = data;
                if (!uid) {
                    this.selectedRow = this.artifactTypes[0];
                } else {
                    this.selectedRow = this.getAssetTypeByUid(uid);
                }
                this.selectedItemChange();
                let setting = this.getGuidSetting(CompanySettingEnum.GovernanceRoleReferenceListUid);
                if (setting === '00000000-0000-0000-0000-000000000000') {
                    this.disableAdd = true;
                }
                this.isLoading = false;
            });
    }

    delete(uid: string) {
        this.selectedRow = this.getAssetTypeByUid(uid);
        this.loadDataAndExecuteAction(() => {
            this.isAdding = false;
            this.isEditing = false;
            this.isDeleting = true;
        });

    }

    edit(uid: string) {
        this.selectedRow = this.getAssetTypeByUid(uid);

        this.loadDataAndExecuteAction(() => {
            this.editorModel = this.selectedRow;
            this.isAdding = false;
            this.isEditing = true;
            this.isDeleting = false;
        });
    }

    add(uid: string) {
        if (uid) {
            this.selectedRow = this.getAssetTypeByUid(uid);
        }
        this.loadDataAndExecuteAction(() => {
            if (!uid) {
                this.editorModel = { data: { ID: 0 } };
            } else {
                this.editorModel = this.selectedRow;
            }

            this.isEditing = false;
            this.isAdding = true;
            this.isDeleting = false;
        });
    }

    cancel() {
        this.isAdding = false;
        this.isEditing = false;
        this.isDeleting = false;
        this.selectedRow = { data: { ID: 0 } };
        this.load();
    }

    actionComplete(e: any, type: string = ''): void {
        this.isAdding = false;
        this.isEditing = false;
        this.isDeleting = false;
        this.load(e.id ? e.id : '');
        this.stateService.reloadLeftNavMenu();
    }

    private deleteArtifactType(id: number) {
        var data = this.getAssetTypeById(id);
        if (data) {
            this.assetTypeService.deleteSingleAssetType(data.uid).subscribe((result) => {
                result.title = 'Success!';
                this.showMessageForResult(this.messagesService, result, 'Item successfully removed.');
                this.isDeleting = false;
                this.selectedRow = { data: { ID: 0 } };
                this.load();
                this.stateService.reloadLeftNavMenu();
            })
        }
    }

    private getAssetTypeByUid(uid: string): any {
        return this.artifactTypes.filter(x => x.uid == uid)[0];
    }
    private getAssetTypeById(id: number): any {
        return this.artifactTypes.filter(x => x.ID == id)[0];
    }

    private loadDataAndExecuteAction(action: Function) {
        if (this.selectedRow) {
            this.assetsService.getAssetTypeLegacyData(this.selectedRow.uid)
                .subscribe(res => {
                    this.selectedRow.ID = res.ObjectID;
                    this.selectedRow.AssetTypeID = res.AssetTypeID;
                    if (action) {
                        action();
                    }
                });
        }
        else {
            action();
        }
    }
}
