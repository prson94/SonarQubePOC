///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone, OnDestroy } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { TreeTable, TreeNode, Column, Header, InputText } from 'primeng/primeng';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { ClaimsTile } from '../tiles/claims.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { ArtifactTypeService, AuditService, HeaderBreadcrumbService, RightSidebarService } from '../../services/index';
import { ArtifactTypeForm } from '../forms/artifact-type.form';
import { FieldDefinitionTile } from '../tiles/field-definition.tile';
import { DeleteForm } from '../forms/delete.form';
import { TileActionsComponent } from '../tiles/tile-actions.component';
import { AdminBaseComponent } from './admin-base.component'
import { RelationshipsTile } from '../tiles/relationships.tile'
import { Title } from '@angular/platform-browser';
import { AuditComponent} from '../shared/audit.component';


@Component({
    selector: 'd3s-admin-artifacts',
    providers: [ArtifactTypeService, AuditService],
    directives: [
        TreeTable,
        Column,
        Header,
        InputText,
        PeopleResponsibilitiesTile,
        ClaimsTile,
        ArtifactTypeForm,
        FieldDefinitionTile,
        DeleteForm,
        TileActionsComponent,
        RelationshipsTile, 
        AuditComponent
    ],
    templateUrl: 'scripts/app/components/admin/admin-artifacts.component.html',
})

export class AdminArtifactsComponent extends AdminBaseComponent implements OnDestroy { 
    searchFilter: string = "";
    objectType: string = "ArtifactType";
    selectedRow: TreeNode;

    isAdding = false;
    isEditing = false;
    isDeleting = false;
    ArtifactTypes: TreeNode[];
    

    constructor(rightSidebarService: RightSidebarService, pageHeader: PageHeader, headerBreadcrumbService: HeaderBreadcrumbService, private artifactsService: ArtifactTypeService, titleService: Title) {        
        super(headerBreadcrumbService, pageHeader, titleService, rightSidebarService);
        this.areaDescription = "Here you will find all artifact types and custom fields associated with them.";
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

    actionComplete(): void {
        this.isAdding = false;
        this.isEditing = false;
        this.isDeleting = false;
        this.load();
    }
}


