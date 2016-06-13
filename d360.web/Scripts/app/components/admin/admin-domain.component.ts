///<reference path="../../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../../services/page-header.service';
import { ObjectDetailTile } from '../tiles/object-detail.tile';
import { FieldsGridTile } from '../tiles/fields-grid.tile';
import { PeopleResponsibilitiesTile } from '../tiles/people-responsibilities.tile';
import { DataTable, DataTableDirectives } from 'angular2-datatable/datatable';

@Component({
    selector: 'admin-domain',
    viewProviders: [HTTP_PROVIDERS],
    directives: [ObjectDetailTile, DataTableDirectives, FieldsGridTile, PeopleResponsibilitiesTile],
    templateUrl: 'scripts/app/components/admin/admin-domain.component.html',
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

export class AdminDomainComponent {
    http: Http;
    pageHeader: PageHeader;
    domainTypes = new Array<DomainType>();
    objectType = 'DomainType';
    selectedRow: DomainType;

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
        this.http.get('/services/domains')
            .map(data => data.json())
            .subscribe(data => {
                //console.log(data);

                //test record
                //data.push({ ID: 9, Name: 'test', Description: '<p>hello <strong>world</strong></p>' });

                //NOTE: array.push does not work with angular2-datatable, known issue. Need to set array directly
                this.domainTypes = data;
                this.selectedRow = this.domainTypes[0];
                this.isLoading = false;
            });
       
    }


    selectRow(id: number): void {
        this.selectedRow = this.domainTypes[this.domainTypes.findIndex(d => d.ID == id)];
    }
}


class DomainType {
    ID: number;
    Name: string;
    Description: string;
}