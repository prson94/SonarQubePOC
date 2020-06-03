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

@Component({
    selector: 'd3s-admin-diagram-asset',
    providers: [ArtifactTypeService, AuditService, AssetService],
    templateUrl: './admin-diagram-asset.component.html'
})

export class AdminDiagramAssetComponent extends AdminBaseComponent implements OnInit, OnDestroy {
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

    searchValue: string = '';
    @ViewChild("dt", { static: false }) dt: TreeTable;

    constructor(private route: ActivatedRoute,
        private router: Router,
        private stateService: StateService,
        secondaryNavService: SecondaryNavService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private artifactsService: ArtifactTypeService,
        private assetsService: AssetService,
        titleService: Title,
        protected messagesService: MessagesObservableService
    ) {
        super(headerBreadcrumbService, titleService, secondaryNavService);
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
            console.log("Ima here");
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

    load(uid: string = '') {
        this.isLoading = true;
        this.assetsService.getAssetCountsByAssetType(this.assetTypeClass)
            .subscribe(data => {
                let temp: TreeNode[] = [];
                data.forEach(n => {
                    temp.push(AssetCount.ConvertToTreeNode(n));
                })

                this.artifactTypes = AssetCount.ListToTree(temp);
                if (!uid) {
                    this.selectedRow = this.artifactTypes[0];
                } else {
                    this.selectedRow = this.artifactsService.findArtifactTypeByUid(this.artifactTypes, uid);

                }
                this.selectedItemChange();
                this.isLoading = false;
            });
    }

    delete(uid: string) {
        this.selectedRow = this.artifactsService.findArtifactTypeByUid(this.artifactTypes, uid);

        this.loadDataAndExecuteAction(() => {
            this.isAdding = false;
            this.isEditing = false;
            this.isDeleting = true;
        });

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
        this.artifactsService.deleteArtifactType(id).subscribe(result => {
            this.showMessageForResult(this.messagesService, result);
            this.isDeleting = false;
            this.selectedRow = { data: { ID: 0 } };
            this.load();
            this.stateService.reloadLeftNavMenu();
        })
    }

    private filterQ: any;
    filter(event) {
        if (event) {
            this.searchValue = event.target.value;
        }
        window.clearTimeout(this.filterQ);
        this.filterQ = setTimeout(() => {
            this.dt.reset();
            this.filterTreeTable(this.artifactTypes, this.searchValue, this.dt);
        }, event ? 600 : 0);
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
}
