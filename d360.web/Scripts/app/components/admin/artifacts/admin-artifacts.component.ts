import { Component, OnDestroy } from '@angular/core';
import { TreeNode } from 'primeng/api';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../../services/header-breadcrumb.service';
import { RightSidebarService } from '../../../services/right-sidebar.service';
import { AuditService } from '../../../services/audit.service';
import { StateService } from '../../../services/state.service';
import { ArtifactTypeService } from '../../../services/artifact-type.service';
import { AdminBaseComponent } from '../admin-base.component'
import { MessagesObservableService } from '../../../services/messages-observable.service';

@Component({
    selector: 'd3s-admin-artifacts',
    providers: [ArtifactTypeService, AuditService],
    templateUrl: './admin-artifacts.component.html'
})

export class AdminArtifactsComponent extends AdminBaseComponent implements OnDestroy {
    searchFilter: string = "";
    objectType: string = "ArtifactType";
    adminType: string = "Artifacts";
    selectedRow: TreeNode;

    isAdding = false;
    isEditing = false;
    isDeleting = false;
    isEditingFieldType = false;
    isAddingFieldType = false;
    ArtifactTypes: TreeNode[];
    theDeleteCallback: Function;

    constructor(
        private stateService: StateService,
        rightSidebarService: RightSidebarService,
        headerBreadcrumbService: HeaderBreadcrumbService,
        private artifactsService: ArtifactTypeService,
        titleService: Title,
        protected messagesService: MessagesObservableService        
    ) {
        super(headerBreadcrumbService, titleService, rightSidebarService);
        this.areaName = "Artifacts";
        this.tabTitle = 'Artifact Types';
        this.setCommonItems();
        this.load();
        this.setObjectInfo('ArtifactType', -1);
        this.setCommonRightSideBar(true);
        if (this.auditSidebar) {
            this.auditSidebar.hasDynamicUrl = true;
            this.auditSidebar.dynamicUrlCallback = (() => {
                return `/sidebar/audit/ArtifactType/${this.selectedRow.data.ID}`
            });
        }
        this.theDeleteCallback = this.deleteArtifactType.bind(this);
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load(selectionId:number = 0) {
        this.isLoading = true;
        this.artifactsService.getArtifactTypeTree()
            .subscribe(data => {
                this.ArtifactTypes = data;                
                if (selectionId <= 0) {
                    this.selectedRow = this.ArtifactTypes[0];
                } else {                    
                    this.selectedRow = this.artifactsService.findArtifactType(this.ArtifactTypes, selectionId);
                }
                this.isLoading = false;
            });
    }

    delete(id: number) {
        this.selectedRow = this.artifactsService.findArtifactType(this.ArtifactTypes, id);
        this.isAdding = false;
        this.isEditing = false;
        this.isDeleting = true;
    }

    edit(id: number) {
        this.selectedRow = this.artifactsService.findArtifactType(this.ArtifactTypes, id);
        this.isAdding = false;
        this.isEditing = true;
        this.isDeleting = false;
    }

    add(id: number) {
        if (id == 0) {
            this.selectedRow = { data: { ID: 0 } };
        } else {
            this.selectedRow = this.artifactsService.findArtifactType(this.ArtifactTypes, id);
        }

        this.isEditing = false;
        this.isAdding = true;
        this.isDeleting = false;
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
        this.load(e.id?(e.id-0):0);
        this.stateService.reloadLeftNavMenu();
    }

    private deleteArtifactType(id: number) {
        this.artifactsService.deleteArtifactType(id).subscribe(result => {
            this.showMessageForResult(this.messagesService, result);
            this.isDeleting = false;
            this.load();
            this.stateService.reloadLeftNavMenu();
        })
    }
}
