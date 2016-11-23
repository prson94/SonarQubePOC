import { Component, Input, ElementRef, HostBinding, OnChanges, OnInit, SimpleChange} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchResult} from '../../models/search-result.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';

@Component({
    selector: 'd3s-search-autocomplete-list',
    host: {
        '(document:click)': 'onClick($event)',
    },  
    styles: [`
                :host{
                    position:relative;
                    margin-left:11.25px;
                }                                     
            `],
    template: ` 
                <div *ngIf="showResults && autocompletions.length > 0" class="tt-menu" style="position:absolute;top:-3px;left:0;min-width:400px" [ngStyle]="{'width':width}">                         
                    <div class="header">Select an item from the dropdown to go directly to it, or to see more search results type in the text you want to search by.</div>
                    <div *ngFor="let autocomplete of autocompletions" class="tt-suggestion tt-selectable" (click)="goTo(autocomplete)">
                        <span class="type">{{autocomplete.Type}}</span> <span [innerHtml]="highlightedResult(autocomplete.Name)"></span>
                    </div>                    
                </div>
                
                `,    
})

export class SearchAutocompleteListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() autocompletions: SearchResult[] = [];    
    @Input() searchText: string;
    @Input() element: any;
    
    private showResults = true;
    private width: string = '400px';
    
    constructor(private elementRef: ElementRef, private router: Router) {
        super();
    }

    ngOnInit() {            
        if (this.element && this.element.offsetWidth) this.width = this.element.offsetWidth + 'px';        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.showResults = true;
    }

    private goTo(item: SearchResult) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(item.Url));
    }
    
    onClick(event) {
        if (this.showResults && !this.elementRef.nativeElement.contains(event.target)) { // or some similar check
            this.showResults = false;
        }
    }

    private highlightedResult(item: string): string {
        if (!item) return "";
        //var regEx = new RegExp(this.searchText, "ig");
        //return item.replace(regEx, `<strong class="item-highlight">${this.searchText}</strong>`);
        return item;
    }
};