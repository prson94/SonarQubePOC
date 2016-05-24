///<reference path="../es6-shim.d.ts"/>
import {Input, Output, Component, OnInit } from '@angular/core';
import {Http, HTTP_PROVIDERS, Headers} from '@angular/http';

@Component({
    selector: 'object-detail',
    templateUrl: 'scripts/app/parts/object-detail.part.html',
    viewProviders: [HTTP_PROVIDERS]
})

export class ObjectDetail implements OnInit {
    @Input() objectType: string;
    @Input() objectId: string;

    rows = new Array<DetailRow>();
    columns: number;
    http: Http;

    constructor(http: Http) {
        this.http = http;
    }

    ngOnInit() {
        this.http.get('/api/' + this.objectType + '/' + this.objectId + '/detail').map(data => data.json()).subscribe(data => {
            this.columns = data.columns;
            data.rows.forEach(r => this.rows.push(r));
            //console.log(this.rows);
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
