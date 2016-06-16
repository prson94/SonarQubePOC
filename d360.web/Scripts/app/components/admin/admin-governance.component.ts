///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader} from '../../services/page-header.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { DataTable, DataTableDirectives } from 'angular2-datatable/datatable';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';

@Component({
    selector: 'admin-governance',
    viewProviders: [HTTP_PROVIDERS],
    directives: [ObjectDetailTile, DataTableDirectives],
    templateUrl: 'scripts/app/components/admin/admin-governance.component.html',
    styles: [`
        .selected {
        background-color: #86ccf9;        
        }
        tbody tr:not(.selected):hover {
        background-color: #ddd;
        }
        td {
            padding-left:3px;
        }
    `]
})

export class AdminGovernanceComponent {
    isLoading = false;
    http: Http;
    pageHeader: PageHeader;

    private governanceItems = new Array<GovernanceItem>();
    private selectedRow = new GovernanceItem();

    constructor(http: Http, pageHeader: PageHeader, private headerBreadcrumbService: HeaderBreadcrumbService) {
        this.http = http;
        this.pageHeader = pageHeader;

        this.pageHeader.title = 'Responsibility Types';
        this.pageHeader.description = 'Assign which objects can be owned, and whether groups, users or both may own them. You may also define application and licensing source types.';

        headerBreadcrumbService.clearBreadcrumbs();
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Administration", ""));
        headerBreadcrumbService.showBreadcrumb(new Breadcrumb("Responsibility Types", ""));

        this.load();
        //console.log(this);
    }

    load(): void {

        this.http.get('api/ownership/types')
            .map(data => data.json())
            .subscribe(data => {
                this.governanceItems = data;
            });
    }

    selectRow(id: number): void {
        this.selectedRow = this.governanceItems[this.governanceItems.findIndex(d => d.ID == id)];
    }
}

class GovernanceItem {
    ID: number;
    Name: string;
}