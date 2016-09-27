
import { Component, ElementRef } from '@angular/core';
import { TypeaheadSearchService } from '../../services/index';
import { SearchResult } from '../../models/search-result.model';
import { Router, NavigationEnd } from '@angular/router';

@Component({
    selector: 'd3s-header-typeahead-search',
    host: {
        '(document:click)': 'onClick($event)',
    },  
    template: ` <span style="display:table;" id="typesearch" [ngClass]="{'active':showSearch}" (mouseover)="in()" (keyup)="checkKey($event)" >
                    <a style="display:table-cell;" (click)="showSearch=!showSearch;" ><i class="fa fa-search"></i></a>
                    <p-autoComplete size="50" 
                            styleClass="searchTypeahead" 
                            scrollHeight="400px" *ngIf="showSearch" 
                            [(ngModel)]="result" 
                            [suggestions]="results" 
                            field="Name"
                            (completeMethod)="search($event)"                              
                            placeholder="Search Data3Sixty"
                            (onSelect)="selectItem()">                       
                        <template let-result>
                            <div style="padding:5px 0;">                                
                                <div class="tt-suggestion tt-selectable"><span style="color:#999;">{{result.Type}}:</span> {{result.Name}}</div>
                            </div>                            
                        </template>
                    </p-autoComplete>
                <span>`,
    providers: [TypeaheadSearchService]
})

export class HeaderTypeaheadSearchComponent {
    constructor(private elementRef: ElementRef, private router: Router, private typeaheadSearchService : TypeaheadSearchService) { }

    result: SearchResult;
    showSearch: boolean = false;
    hideTimeoutID: number = 0;
    searchText: string;
    results: SearchResult[];

    search(event) {
        this.searchText = event.query;
        this.typeaheadSearchService.getResults(10, event.query).then(data => {
            this.results = data;
        });       
    }

    public convertUrl(item: SearchResult): string {
        if (item.Url.startsWith('#/artifacts'))
            return item.Url.replace('#/artifacts', '/a/artifact');
        else if (item.Url.startsWith('#/resources'))
            return item.Url.replace('#/resources', '/a/resource');
        else if (item.Url.startsWith('#/catalogs'))
            return item.Url.replace('#/catalogs', '/a/model');
        return item.Url;
    }

    selectItem() {
        let url = this.convertUrl(this.result);

        if (url.startsWith('/a')) {
            this.router.navigateByUrl(url);
        }
        else {
            window.location.href = url;
        }
    }

    hide() {        
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

    checkKey(event) {
        if (event.keyCode == 13) {
            this.showSearch = false;
            this.router.navigateByUrl(`/a/search?query=${encodeURIComponent(this.searchText)}`);
        }
    }
}

