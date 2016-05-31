///<reference path="../es6-shim.d.ts"/>
import {Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers} from '@angular/http';
import { DataTable, DataTableDirectives } from 'angular2-datatable/datatable';

@Component({
    selector: 'people-responsibilities',
    directives: [DataTableDirectives],
    templateUrl: 'scripts/app/parts/people-responsibilities.part.html',
    viewProviders: [HTTP_PROVIDERS],
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

export class PeopleResponsibilitiesPart implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: string;
    @Input() title: string;
    @Input() showHidden: boolean = false;

    private responsibilities = new Array<Responsibility>();
    private selectedRow = new Responsibility();
    private isLoading = false;

    http: Http;

    constructor(http: Http) {
        this.http = http;
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let p in changes) {
            if (p == 'objectType') {
                this.objectType = changes['objectType'].currentValue;
            }
            if (p == 'objectID') {
                this.objectID = changes['objectID'].currentValue;
            }
        }

        this.load();
    }

    load(): void {

        if (this.objectType == null || this.objectID == null)
            return;

        this.isLoading = true;
        this.http.get('/api/' + this.objectType + '/' + this.objectID + '/ownership?showHidden=' + this.showHidden )
            .map(data => data.json())
            .subscribe(data => {
                this.responsibilities = data;
                this.selectedRow = null; //this.responsibilities[0];

                this.isLoading = false;
            });

    }

    selectRow(id: string): void {
        this.selectedRow = this.responsibilities[this.responsibilities.findIndex(d => d.ResponsibilityID == id)];
    }
}






class Responsibility {
    ResponsibilityID: string;
    AssigningItemType: string;
    AssigningItemID: string;
    ResponsibleObjectType: string;
    ResponsibleObjectID: string;
    ResponsibleObjectName: string;
    PrimaryOwnerResourceID: string;
    PrimaryOwnerResourceName: string;
    PrimaryOwnerResourceUrl: string;
    ObjectType: string;
    ObjectID: string;
    Role: string;
    ResponsibleObjectUrl: string;
    ContextItems: string;
}