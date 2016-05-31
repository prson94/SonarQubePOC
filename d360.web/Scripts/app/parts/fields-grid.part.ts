///<reference path="../es6-shim.d.ts"/>
import {Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers} from '@angular/http';
import { DataTable, DataTableDirectives } from 'angular2-datatable/datatable';


@Component({
    selector: 'fields-grid',
    directives: [DataTableDirectives],
    templateUrl: 'scripts/app/parts/fields-grid.part.html',
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

export class FieldsGridPart implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: string;
    @Input() title: string;

    private fieldDefinitions = new Array<FieldDefinition>();
    private selectedRow = new FieldDefinition();
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
        this.http.get('/fields/' + this.objectType + '/' + this.objectID + '.json')
            .map(data => data.json())
            .subscribe(data => {
                this.fieldDefinitions = data;
                this.selectedRow = null; //this.fieldDefinitions[0];

                this.isLoading = false;
            });

    }

    selectRow(id: string): void {
        this.selectedRow = this.fieldDefinitions[this.fieldDefinitions.findIndex(d => d.ID == id)];
    }
}

class FieldDefinition {

    ObjectType: string;
    ObjectID: string;
    ID: string;
    Category: string;
    FriendlyName: string;
    SortOrder: string;
    IsRequired: boolean;
    IsListable: boolean;
    DisplayDescription: string;
    FormDescription: string;
}