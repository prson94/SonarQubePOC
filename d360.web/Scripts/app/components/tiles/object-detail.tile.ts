///<reference path="../../es6-shim.d.ts"/>
import {Input, Output, Component, OnChanges, SimpleChange } from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers} from '@angular/http';

@Component({
    selector: 'object-detail',
    templateUrl: 'scripts/app/components/tiles/object-detail.tile.html',
    viewProviders: [HTTP_PROVIDERS]
})

export class ObjectDetailTile implements OnChanges {
    @Input() objectType: string;
    @Input() objectID: string;

    private isLoading = false;

    rows = new Array<DetailRow>();
    columns: number;
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

    private load(): void {
        this.isLoading = true;

        if (this.objectType && this.objectID)
            this.http.get('/api/' + this.objectType + '/' + this.objectID + '/detail').map(data => data.json()).subscribe(data => {
                this.rows = [];

                //console.log(data);

                this.columns = data.columns;
                data.rows.forEach(r => this.rows.push(r));

                this.isLoading = false;
            });
    }



}

class DetailRow {
    Category: any;
    columns: number;
    FirstColumnFields = new Array<DetailField>();
    SecondColumnFields = new Array<DetailField>();
}

class DetailField {
    Column: any;
    FieldDescription: string;
    FieldName: string;
    Group: any;
    HideFooter: boolean;
    HideHeader: boolean;
    LookupGridUrl: string;
    MultipleValues: any;
    Name: string;
    Row: any;
    ScriptProperty: any;
    TooltipContext: any;
    TooltipID: any;
    TooltipType: any;
    TooltipUrl: string;
    Value: string;
}
