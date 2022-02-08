import { ChangeDetectorRef, Component, OnDestroy, OnInit, ViewChild } from '@angular/core';
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
import { AssetTypeService } from '../../../services/asset-type.service';
import { CompanySettingsService } from '../../../services/settings.service';

@Component({
    selector: 'd3s-admin-artifacts',
    providers: [ArtifactTypeService, AuditService, AssetService, AssetTypeService],
    templateUrl: './admin-artifacts.component.html'
})

export class AdminArtifactsComponent extends AdminBaseComponent implements OnInit, OnDestroy {
    dataCyPrefix: string = 'AssetType_';
    searchFilter: string = "";
    objectType: string = "ArtifactType";
    adminType: string = "Artifacts";
    addClassName: string;
    selectedRow: TreeNode;
    private sub: any;
    isAdding = false;
    isEditing = false;
    isDeleting = false;
    isEditingFieldType = false;
    isAddingFieldType = false;
    artifactTypes: TreeNode[];
    editorModel: any;
    theDeleteCallback: Function;
    assetTypeClass: AssetTypeClass;
    formTitle: string;

    @ViewChild("dt", { static: false }) dt: TreeTable;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private stateService: StateService,
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private artifactsService: ArtifactTypeService,
        private assetTypeService: AssetTypeService,
        private assetsService: AssetService,
        titleService: Title,
        protected messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private cdRef: ChangeDetectorRef
    ) {
        super(headerBreadcrumbService, titleService, settingsService, secondaryNavService);
        this.theDeleteCallback = this.deleteArtifactType.bind(this);
    }

    ngOnInit() {
        this.sub = this.route.params.subscribe((params) => {
            try {
                let assetTypeClassString: keyof typeof AssetTypeClass = params['class'];
                this.assetTypeClass = AssetTypeClass[assetTypeClassString];
                if (!this.assetTypeClass) {
                    this.assetTypeClass = AssetTypeClass.BusinessAsset;
                }
            } catch (e) {
                this.assetTypeClass = AssetTypeClass.BusinessAsset;
            }

            let className: string = this.assetTypeClass == AssetTypeClass.BusinessAsset ? 'Business Asset' : 'Technical Asset';
            this.addClassName = "Add " + className;
            let singularLabel: string = `${className} Type`;

            this.tabTitle = `${singularLabel}s`;
            this.formTitle = `Edit ${singularLabel}`;
            this.load();
        });
    }

    selectedItemChange() {
        this.loadDataAndExecuteAction(() => {
            this.buildSecondaryNavigationForObject(this.selectedRow ? this.selectedRow.data.ID : 0, this.objectType, null, this.assetTypeClass);
        });
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    onPageEvent(evt) {
        this.selectedRow = this.artifactTypes[evt.first];
        this.selectedItemChange();
    }

    load(uid: string = '') {
        this.isLoading = true;
        this.assetsService.getAssetCountsByAssetType(this.assetTypeClass, false)
            .subscribe(data => {                
                this.isLoading = false;
                let temp: TreeNode[] = [];
                data.forEach(n => {
                    temp.push(AssetCount.ConvertToTreeNode(n));
                })

                this.artifactTypes = AssetCount.ListToTree(temp);
                if (!uid) {
                    this.selectedRow = this.artifactTypes[0];
                    this.selectedItemChange();
                } else {
                    this.selectedRow = this.artifactsService.findArtifactTypeByUid(this.artifactTypes, uid);
                    if (this.selectedRow.data.parentUid) {
                        this.expandParent(this.selectedRow.data.parentUid);
                    }
                    this.selectedItemChange();
                }
            });
    }

    expandParent(uid) {
        let node = this.artifactsService.findArtifactTypeByUid(this.artifactTypes, uid);
        if (node) {
            node.expanded = true;
        }
        if (node.data.parentUid) {
            this.expandParent(node.data.parentUid)
        }
    }

    delete(uid: string) {
        this.selectedRow = this.artifactsService.findArtifactTypeByUid(this.artifactTypes, uid);

        if (this.assetTypeClass === AssetTypeClass.BusinessAsset || this.assetTypeClass === AssetTypeClass.TechnicalAsset) {
            this.assetsService.getAssetCountOfArtifactTypeUid(uid)
                .subscribe(data => {
                    this.selectedRow.data.count = data.count;
                    this.loadDataAndExecuteAction(() => {
                        this.isAdding = false;
                        this.isEditing = false;
                        this.isDeleting = true;
                    });
                });

        }
        else {
            this.loadDataAndExecuteAction(() => {
                this.isAdding = false;
                this.isEditing = false;
                this.isDeleting = true;
            });
        }

    }

    edit(uid: string) {
        this.selectedRow = this.artifactsService.findArtifactTypeByUid(this.artifactTypes, uid);

        this.loadDataAndExecuteAction(() => {
            this.editorModel = this.selectedRow;
            this.isAdding = false;
            this.isEditing = true;
            this.isDeleting = false;
        });
    }

    add(uid: string) {
        if (uid)
            this.selectedRow = this.artifactsService.findArtifactTypeByUid(this.artifactTypes, uid);
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
        this.load(this.selectedRow ? this.selectedRow.data.uid : null);
    }

    actionComplete(e: any, type: string = ''): void {
        this.isAdding = false;
        this.isEditing = false;
        this.isDeleting = false;
        this.load(e.Uid);
        this.stateService.reloadLeftNavMenu();
    }

    private deleteArtifactType(uid: string) {
        let node = this.artifactsService.findArtifactTypeByUid(this.artifactTypes, uid);
        let data: any = node ? node.data : null;
        this.isLoading = true;
        if (data) {
            this.assetTypeService.deleteSingleAssetType(data.uid).subscribe(result => {
                this.isLoading = false;
                result.title = 'Success!';
                this.showMessageForResult(this.messagesService, result, 'Item successfully removed.');
                this.isDeleting = false;
                this.selectedRow = { data: { ID: 0 } };
                this.load();
                this.stateService.reloadLeftNavMenu();
            })
        }
    }

    private loadDataAndExecuteAction(action: Function) {
        if (this.selectedRow) {
            this.assetsService.getAssetTypeLegacyData(this.selectedRow.data.uid)
                .subscribe(res => {
                    this.selectedRow.data.ID = res.ObjectID;
                    this.selectedRow.data.AssetTypeID = res.AssetTypeID;
                    if (action) {
                        action();
                    }
                });
        }
        else {
            action();
        }
    }

    private expandNodes() {
        if (this.dt.filters["global"]) { // only expand if global filter populated.
            this.expandChildNodes(this.dt.filteredNodes, this.dt.globalFilterFields, this.dt.filters["global"].value);
        }
    }

    private expandChildNodes(nodes: TreeNode[], fields: string[], search: string) {
        var match = false;
        nodes.forEach((node) => {
            fields.forEach(field => { if (node.data[field].includes(search)) { match = true } }); //check each of the global filterfields for filter value            
            if (node.children && node.children.length > 0) {
                node.expanded = this.expandChildNodes(node.children, fields, search);   //expand the node if any child matches.          
                if (node.expanded) {
                    match = true; // if current node doesn't match but a child does.
                }
            }
        }
        );
        return match;
    }
}
