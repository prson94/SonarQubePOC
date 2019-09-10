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
                <div *ngIf="showResults && autocompletions.length > 0" class="search-typeahead-menu" style="position:absolute;top:-3px;left:0;min-width:400px;max-height:400px;overflow:auto;" [ngStyle]="{'width':width}">                         
                    <div class="header">Select an item from the dropdown to go directly to it, or to see more search results type in the text you want to search by.</div>
                    <div *ngFor="let autocomplete of autocompletions" class="search-typeahead-suggestion" (click)="goTo(autocomplete)">
                        <i *ngIf="autocomplete.Icon" class="folder-icon fa {{autocomplete.Icon}}"></i>
                        <span *ngIf="autocomplete.ImageUrl" class="folder-icon"><img [src]="autocomplete.ImageUrl" /></span>
                        <span [innerHtml]="highlightedResult(autocomplete.DisplayName)"></span>
                        <span *ngIf="autocomplete?.Tags" class="tags">
                            <span *ngFor="let restag of autocomplete?.Tags" class="tag-item-wrapper" [innerHtml]="restag.Highlight" style="background: #BDC3C7;margin-left: 4px;border-radius: 3px;padding: 4px;text-align: center;"></span>
                        </span>
                        <span class="type">&nbsp;{{GetDisplayType(autocomplete)}}</span>
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
        console.log('elemental',this.element);
        if (this.element && this.element.offsetWidth) this.width = this.element.offsetWidth + 'px';        
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        this.showResults = true;
    }

    private goTo(item: SearchResult) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(item.Url));
    }

    GetDisplayType(item: SearchResult) {
        var displayType = '';
        if (item.Group == 'Glossary')
            displayType += item.Group + ' - ';
        displayType += item.Type;
        return displayType;
    }
    
    onClick(event) {
        if (this.showResults && !this.elementRef.nativeElement.contains(event.target)) { // or some similar check
            this.showResults = false;
        }
    }

    private highlightedResult(item: string): string {
        if (!item) return "";        
        return item;
    }
};