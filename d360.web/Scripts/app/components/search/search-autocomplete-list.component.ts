import { Component, Input, ElementRef, HostBinding, OnChanges, OnInit, SimpleChange} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchResult} from '../../models/search-result.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { StringConstants } from '../../static/string-constants';

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
                <div *ngIf="showResults && autocompletions.length > 0" class="search-typeahead-menu" style="position:absolute;left:0;min-width:400px;max-height:400px;overflow:auto;" [ngStyle]="{'width':width, 'top':top}">                         
                    <div class="header">Select an item from the dropdown to go directly to it, or to see more search results type in the text you want to search by.</div>
                    <div *ngFor="let autocomplete of autocompletions" class="search-typeahead-suggestion" (click)="goTo(autocomplete)">
                        <i *ngIf="autocomplete.Icon" class="folder-icon fa {{autocomplete.Icon}}"></i>
                        <span *ngIf="autocomplete.ImageUrl" class="folder-icon"><img [src]="autocomplete.ImageUrl" /></span>
                        <span [innerHtml]="highlightedResult(autocomplete.DisplayName)"></span>
                        <span *ngIf="autocomplete?.Tags" class="tag-list-container">
                            <button *ngFor="let tag of autocomplete?.Tags" class="button">
                                <span class="tag-item-wrapper" [innerHtml]="tag.Value"></span>
                            </button>
                        </span>
                        <span class="category">
                            {{autocomplete?.Group}}<span *ngIf="autocomplete?.Type"><i class="fa fa-angle-right"></i><span [innerHtml]="autocomplete?.Type"></span></span>
                        </span>
                    </div>                    
                </div>
                
                `,    
})

export class SearchAutocompleteListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() autocompletions: SearchResult[] = [];    
    @Input() searchText: string;
    @Input() setwidth: any;
    @Input() setTop: number = 4;
    private showResults = true;
    private width: string = '400px';
    private top: string = '4px';
    
    constructor(private elementRef: ElementRef, private router: Router) {
        super();
    }

    ngOnInit() {
        this.setWidth();      
    }

    ngOnChanges(changes: { [propName: string]: SimpleChange }) {
        for (let propName in changes) {
            if (propName == 'setwidth' || propName == 'setTop') {
                this.setWidth();
            }
        }
        this.showResults = true;
    }

    private setWidth() {
        if (this.setwidth && this.setwidth > 400) this.width = this.setwidth + 'px';   
        if (this.setTop && this.setTop > 4) this.top = this.setTop + 'px';
    }

    private goTo(item: SearchResult) {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(item.Url));
    }

    GetDisplayType(item: SearchResult) {
        var displayType = '';
        if (item.Group == StringConstants.AssetTypeClass_Business || item.Group == StringConstants.AssetTypeClass_Technical)
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