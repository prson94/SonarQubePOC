///<reference path="../es6-shim.d.ts"/>
import { Component, NgZone } from '@angular/core';
import { Http, HTTP_PROVIDERS, Headers } from '@angular/http';
import { PageHeader } from '../page-header.service';
import { ObjectDetail } from '../parts/object-detail.part';
import { NgTableComponent, NG_TABLE_DIRECTIVES } from 'ng2-table';

@Component({
    selector: 'admin-domain',
    viewProviders: [HTTP_PROVIDERS],
    directives: [ObjectDetail, NG_TABLE_DIRECTIVES],
    templateUrl: 'scripts/app/templates/admin-domain.component.html'
})

export class AdminDomainComponent {
    http: Http;
    pageHeader: PageHeader;

    domainTypes = new Array<DomainType>();
    objectType = 'DomainType';
    objectId = 0;

    public columns: Array<any> = [
        { title: 'ID', name: 'id' },
        { title: 'Name', name: 'name' },
        { title: 'Description', name: 'description' },
    ];

    public config: any = {
        paging: true,
        sorting: { columns: this.columns }
    };

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

                data.forEach(r => {
                    var d = new DomainType();
                    d.id = r.ID;
                    d.name = r.Name;
                    d.description = r.Description;
                    this.domainTypes.push(d);
                });
            });

        this.domainTypes.push({ id: 2, name: 'Test 1', description: 'Description 1' });
        this.domainTypes.push({ id: 3, name: 'Hello World', description: 'Description 2' });
        this.domainTypes.push({ id: 4, name: 'lorem ipsum', description: 'Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum' });
        this.domainTypes.push({ id: 5, name: 'Test 2', description: 'blah blah blah' });
        this.domainTypes.push({ id: 6, name: 'Last', description: 'Description Last' });
    }
}


class DomainType {
    id: number;
    name: string;
    description: string;
}