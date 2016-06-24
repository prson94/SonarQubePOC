///<reference path="../../es6-shim.d.ts"/>
import { Component } from '@angular/core';
import {AutoComplete} from 'primeng/primeng';
import { TypeaheadSearchService } from '../../services/index';
import { TypeaheadSearchResult } from '../../models/typeahead-search-result.model';
import { ROUTER_DIRECTIVES, Router, NavigationEnd } from '@angular/router';

@Component({
    selector: 'd3s-header-typeahead-search',
    template: ` <span style="display:table;">
                    <a style="display:table-cell;" (click)="showSearch=!showSearch;"><i class="fa fa-search"></i></a>
                    <p-autoComplete size="50" *ngIf="showSearch" [(ngModel)]="result" [suggestions]="results" (completeMethod)="search($event)" field="Name" (onSelect)="selectItem()">                       
                    </p-autoComplete>
                <span>`,
    directives: [AutoComplete],
    providers: [TypeaheadSearchService]
})

export class HeaderTypeaheadSearchComponent {
    constructor(private router: Router, private typeaheadSearchService : TypeaheadSearchService) { }

    result: TypeaheadSearchResult;
    showSearch: boolean = false;

    results: TypeaheadSearchResult[];

    search(event) {
        this.typeaheadSearchService.getResults(10, event.query).then(data => {
            this.results = data;
        });       
    }

    selectItem() {
        console.log(this.result);
        console.log(window.location.host);
        window.location.href = this.result.Url;
        //window.location.hash = this.result.Url;        
    }
}

