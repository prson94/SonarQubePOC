///<reference path="../../../../node_modules/typings/index.d.ts"/>  
import { Component, Input, ElementRef, HostBinding, OnChanges, OnInit, SimpleChange} from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchResult} from '../../models/search-result.model';

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
                .searchHeader{
                    color: #b0b2b6;
                    text-align: center;
                    padding: 5px;
                    background: #f0f3f8;
                }   
            `],
    template: ` 
                <div *ngIf="showResults" class="tt-menu" style="position:absolute;top:-3px;left:0;background:white;min-width:400px" [ngStyle]="{'width':width}">                         
                    <div class="searchHeader">Select an item from the dropdown to go directly to it, or to see more search results type in the text you want to search by.</div>
                    <div *ngFor="let autocomplete of autocompletions" class="tt-suggestion tt-selectable" (click)="goTo(autocomplete)">
                        <span class="type">{{autocomplete.Type}}</span> {{autocomplete.Name}}<strong class="tt-highlight"></strong>
                    </div>                    
                </div>
                
                `,    
})

export class SearchAutocompleteListComponent extends BaseComponent implements OnInit, OnChanges {
    @Input() autocompletions: SearchResult[] = [];    
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
        this.router.navigateByUrl(this.convertUrl(item));
    }

    public convertUrl(item: SearchResult): string {
        if (item.Url.startsWith('#/artifacts'))
            return item.Url.replace('#/artifacts', '/a/artifact');
        return item.Url.replace('#', '/a');
    }

    onClick(event) {
        if (this.showResults && !this.elementRef.nativeElement.contains(event.target)) { // or some similar check
            this.showResults = false;
        }
    }
};