import { Component, NgZone, OnDestroy } from '@angular/core';
import { TreeNode } from 'primeng/primeng';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ArtifactTypeService, AuditService, HeaderBreadcrumbService, RightSidebarService, StateService, MessagesService } from '../../services/index';
import { AdminBaseComponent } from './admin-base.component'
import { Title } from '@angular/platform-browser';



@Component({
    selector: 'd3s-admin-artifacts',
    providers: [ArtifactTypeService, AuditService],
    templateUrl: './admin-artifacts.component.html',
})

export class AdminArtifactsComponent extends AdminBaseComponent implements OnDestroy { 
    searchFilter: string = "";
    objectType: string = "ArtifactType";
    selectedRow: TreeNode;

    isAdding = false;
    isEditing = false;
    isDeleting = false;
    isEditingFieldType = false;
    isAddingFieldType = false;
    ArtifactTypes: TreeNode[];
    

    constructor(private stateService: StateService, rightSidebarService: RightSidebarService, headerBreadcrumbService: HeaderBreadcrumbService, private artifactsService: ArtifactTypeService, titleService: Title, protected messagesService: MessagesService) {        
        super(headerBreadcrumbService, titleService, rightSidebarService);        
        this.areaName = "Artifacts";
        this.setCommonItems();        
        this.load();
        this.setCommonRightSideBar(true);
    }

    ngOnDestroy() {
        this.clearSidebar();
    }

    load() {
        this.isLoading = true;
        this.artifactsService.getArtifactTypeTree()
            .then(data => {
                this.ArtifactTypes = data;
                this.selectedRow = this.ArtifactTypes[0];
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
        if (id == 0)
            this.selectedRow = { data: { ID: 0 } };
        else
            this.selectedRow = this.artifactsService.findArtifactType(this.ArtifactTypes, id);
        this.isEditing = false;
        this.isAdding = true;
        this.isDeleting = false;
    }

    cancel() {
        this.isAdding = false;
        this.isEditing = false;
        this.isDeleting = false;
    }

    actionComplete(e: any, type: string = ''): void {
        var msg = e;
        if (type != '') {
            if (type == 'success') {
                msg = {
                    type: type,
                    title: 'Success',
                    message: 'Item deleted successfully'
                }
            } else {
                msg = {
                    type: type,
                    title: 'Error',
                    message: 'An error occurred'
                }
            }
        }

        this.isAdding = false;
        this.isEditing = false;
        this.isDeleting = false;
        this.load();    
        this.showMessageForResult(this.messagesService, msg);    
        this.stateService.reloadLeftNavMenu();
    }
}


