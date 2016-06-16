///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../../services/page-header.service';
import { TreeTable, TreeNode, Column, Header, InputText } from 'primeng/primeng';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { ClaimsTile } from '../tiles/claims.tile';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';

@Component({
    selector: 'd3s-admin-artifacts',
    viewProviders: [HTTP_PROVIDERS],
    directives: [TreeTable, Column, Header, InputText, PeopleResponsibilitiesTile, ClaimsTile ],
    templateUrl: 'scripts/app/components/admin/admin-artifacts.component.html',
})

export class AdminArtifactsComponent {
    http: Http;
    pageHeader: PageHeader;

    isLoading = false;
    searchFilter: string = "";
    objectType: string = "ArtifactType";
    selectedRow: TreeNode;

    ArtifactTypes: TreeNode[];

    constructor(http: Http, pageHeader: PageHeader, private headerBreadcrumbService: HeaderBreadcrumbService) {
        this.http = http;
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Artifact Types';
        this.pageHeader.description = 'Here you will find all artifact types and custom fields associated with them.';

        headerBreadcrumbService.clearBreadcrumbs();
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Administration", ""));
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Artifacts", ""));

        this.load();
    }

    load() {

        this.isLoading = true;
        this.http.get('/artifacts/types')
            .map(data => data.json())
            .subscribe(data => {
                //console.log(data);
                this.ArtifactTypes = this.formTree(data);
            });

    }

    private formTree(data): TreeNode[] {
        var tree = new Array <TreeNode>();

        data.filter(d => d.ParentID == null).forEach(d => {
            tree.push({ data: d, children: [] });
        });

        tree.forEach(t => {
            this.formTreeR(t, data);
        });

        return tree;
    }

    private formTreeR(node: TreeNode, data) {

        data.filter(d => d.ParentID == node.data.ID).forEach(d => {
            let child: TreeNode = { data: d, children: [] };
            node.children.push(child);
            this.formTreeR(child, data);
        });
    }

}


