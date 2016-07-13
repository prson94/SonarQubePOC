///<reference path="../../es6-shim.d.ts"/>
import { Component, ElementRef } from '@angular/core';
import {AutoComplete} from 'primeng/primeng';
import { TypeaheadSearchService } from '../../services/index';
import { TypeaheadSearchResult } from '../../models/typeahead-search-result.model';
import { ROUTER_DIRECTIVES, Router, NavigationEnd } from '@angular/router';

@Component({
    selector: 'd3s-header-typeahead-search',
    host: {
        '(document:click)': 'onClick($event)',
    },
    template: ` <span style="display:table;" id="typesearch" [ngClass]="{'active':showSearch}" (mouseover)="in()" >
                    <a style="display:table-cell;" (click)="showSearch=!showSearch;" ><i class="fa fa-search"></i></a>
                    <p-autoComplete size="50" 
                            styleClass="searchTypeahead" 
                            scrollHeight="400px" *ngIf="showSearch" 
                            [(ngModel)]="result" 
                            [suggestions]="results" 
                            (completeMethod)="search($event)" 
                            field="Name"  
                            placeholder="Search Data3Sixty"
                            (onSelect)="selectItem()">                       
                    </p-autoComplete>
                <span>`,
    directives: [AutoComplete],
    providers: [TypeaheadSearchService]
})

export class HeaderTypeaheadSearchComponent {
    constructor(private elementRef: ElementRef, private router: Router, private typeaheadSearchService : TypeaheadSearchService) { }

    result: TypeaheadSearchResult;
    showSearch: boolean = false;
    hideTimeoutID: number = 0;

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

    hide() {
        console.log(3);
        this.showSearch = false;
        this.hideTimeoutID = 0;
    }

    out() {
        if (this.hideTimeoutID <= 0)
            this.hideTimeoutID = window.setTimeout(() => this.hide(), 2000);
    }

    in() {
        if (this.hideTimeoutID > 0) window.clearTimeout(this.hideTimeoutID);
        this.showSearch = true;
    }

    onClick(event) {
        if (this.showSearch && !this.elementRef.nativeElement.contains(event.target)) { // or some similar check
            this.showSearch = false;
        }        
    }
}

