///<reference path="../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../page-header.service';
import { ObjectDetail } from '../parts/object-detail.part';
import { DataTable, DataTableDirectives } from 'angular2-datatable/datatable';

@Component({
    selector: 'admin-domain',
    viewProviders: [HTTP_PROVIDERS],
    directives: [ObjectDetail, DataTableDirectives],
    templateUrl: 'scripts/app/templates/admin-domain.component.html',
    styles: [`
        .selected {
        background-color: #86ccf9;        
        }
        tbody tr:not(.selected):hover {
        background-color: #ddd;
        }
    `]
})

export class AdminDomainComponent {
    http: Http;
    pageHeader: PageHeader;
    domainTypes = new Array<DomainType>();
    objectType = 'Domain';
    selectedRow: DomainType;

    constructor(http: Http, pageHeader: PageHeader) {
        this.http = http;
        this.pageHeader = pageHeader;
        this.pageHeader.title = 'Reference Types';
        this.pageHeader.description = 'All type of reference data lists for the organization are defined here. To add a new type of list, go under Actions and select Add type.';

        this.load();
    }

    load() {

        this.http.get('/services/domains')
            .map(data => data.json())
            .subscribe(data => {
                //console.log(data);

                data.push({ ID: 9, Name: 'test', Description: '<p>hello <strong>world</strong></p>' });
                data.push({ ID: 10, Name: 'test 2', Description: '<p>hello <strong>world</strong></p>' });

                //NOTE: array.push does not work with angular2-datatable, known issue. Need to set array directly
                this.domainTypes = data;
                this.selectedRow = this.domainTypes[0];
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