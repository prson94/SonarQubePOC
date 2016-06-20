///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { PageHeader } from '../../services/page-header.service';
import { TreeTable, TreeNode, Column, Header, InputText } from 'primeng/primeng';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { ClaimsTile } from '../tiles/claims.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ArtifactsService } from '../../services/artifacts.service';

@Component({
    selector: 'd3s-admin-artifacts',
    providers: [ArtifactsService],
    directives: [TreeTable, Column, Header, InputText, PeopleResponsibilitiesTile, ClaimsTile ],
    templateUrl: 'scripts/app/components/admin/admin-artifacts.component.html',
})

export class AdminArtifactsComponent {

    isLoading = false;
    searchFilter: string = "";
    objectType: string = "ArtifactType";
    selectedRow: TreeNode;

    ArtifactTypes: TreeNode[];

    constructor(private pageHeader: PageHeader, private headerBreadcrumbService: HeaderBreadcrumbService, private artifactsService: ArtifactsService) {
        this.pageHeader.title = 'Artifact Types';
        this.pageHeader.description = 'Here you will find all artifact types and custom fields associated with them.';

        headerBreadcrumbService.clearBreadcrumbs();
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Administration", ""));
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Artifacts", ""));

        this.load();
    }

    load() {
        this.isLoading = true;
        this.artifactsService.getArtifactTypeTree()
            .then(data => {
                this.ArtifactTypes = data;
                this.isLoading = false;
            });
    }
}


