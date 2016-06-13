///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../../services/page-header.service';
import { TreeTable, TreeNode } from 'primeng/primeng';

@Component({
    selector: 'admin-artifacts',
    viewProviders: [HTTP_PROVIDERS],
    directives: [ TreeTable ],
    templateUrl: 'scripts/app/components/admin/admin-artifacts.component.html',
})

export class AdminArtifactsComponent {
    http: Http;
    pageHeader: PageHeader;

    isLoading = false;

    constructor(http: Http, pageHeader: PageHeader) {
        this.http = http;
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Reference Types';
        this.pageHeader.description = 'All type of reference data lists for the organization are defined here. To add a new type of list, go under Actions and select Add type.';

        this.load();
    }

    load() {

        this.isLoading = true;
        this.http.get('/artifacts/types')
            .map(data => data.json())
            .subscribe(data => {
                console.log(data);

                var tree = [];

            });
        //    .map(data => data.json())
        //    .subscribe(data => {
        //        //console.log(data);

        //        //test record
        //        //data.push({ ID: 9, Name: 'test', Description: '<p>hello <strong>world</strong></p>' });

        //        //NOTE: array.push does not work with angular2-datatable, known issue. Need to set array directly
        //        this.domainTypes = data;
        //        this.selectedRow = this.domainTypes[0];
        //        this.isLoading = false;
        //    });

    }

    private formTree(data) {
        var tree = new Array<TreeNode>();
        var roots = data.filter(d => d.ParentID == null).forEach(d => tree.push({ data: d, children: [] }));

        //tree.forEach(t =>
       // tree.forEach(t => this.formTreeR(t, data));

    }

    private formTreeR(node: TreeNode, data) {
        //data.filter(d => d.ParentID == node.data.ID).forEach(d => node.children.push({ data: d, children: [] }));
    }

}


