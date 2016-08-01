///<reference path="../../es6-shim.d.ts"/>
import { Component, Input, ElementRef, ViewChildren, AfterViewInit } from '@angular/core';
import { Router }       from '@angular/router';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import {AutoComplete} from 'primeng/primeng';
import { ROUTER_DIRECTIVES } from '@angular/router';
import { TypeaheadSearchService } from '../../services/index';
import { TypeaheadSearchResult } from '../../models/typeahead-search-result.model';

@Component({
    selector: 'd3s-header-breadcrumb-item',
    directives: [ROUTER_DIRECTIVES, AutoComplete],
    providers: [TypeaheadSearchService],    
    host: {
        '(document:click)': 'onClick($event)',
    },
    styles: [`
    a.breadcrumb {
        color:#54a4da;
    }
    .breadcrumb {
        font-weight:bold;
        text-transform:uppercase;
    }           
  `],
    template: ` <a *ngIf="breadcrumb.hasLink()" [routerLink]="[breadcrumb.link]" class="breadcrumb">{{ breadcrumb.text }}</a>
                <span *ngIf="!breadcrumb.hasLink() && !showSearch" (mouseover)="in()" class="breadcrumb">{{ breadcrumb.text }}</span>
                <p-autoComplete size="50"                                                      
                            *ngIf="showSearch" 
                            [inputStyle]="{'border':'2px solid #54a4da','border-radius':'4px'}"
                            styleClass="searchTypeahead"             
                            [minLength]="1"                               
                            [(ngModel)]="result" 
                            [suggestions]="results" 
                            (completeMethod)="search($event)" 
                            field="Name"  
                            [placeholder]="breadcrumb.text"
                            (onSelect)="selectItem()">                       
                    </p-autoComplete>
                <span *ngIf="!lastItem" class="sep breadcrumb"> :: </span>                
              `
})

export class HeaderBreadcrumbItemComponent implements AfterViewInit {    
    @Input() breadcrumb: Breadcrumb;
    @Input() lastItem: boolean;

    //@ViewChildren('input') inputs;
    
    results: TypeaheadSearchResult[];
    result: TypeaheadSearchResult;
    showSearch: boolean;


    constructor(private elementRef: ElementRef, private router: Router,
                private typeaheadSearchService: TypeaheadSearchService) { }

    ngAfterViewInit() {        
      /*  this.inputs.changes.subscribe(elements => {
            console.log(3);
            elements.last.nativeElement.focus();
        });*/
    }
    

    private in() {
        if (this.breadcrumb.objectType && this.breadcrumb.objectId) {
            this.showSearch = true;
        }        
    }    

    search(event) {
        this.typeaheadSearchService.getObjectTypeItems(10, event.query, this.breadcrumb.objectType, this.breadcrumb.objectId).then(data => {
            this.results = data;
        });
    }

    selectItem() {
        this.router.navigateByUrl(this.result.Url)
    }

    onClick(event) {
        if (this.showSearch && !this.elementRef.nativeElement.contains(event.target)) { // or some similar check
            this.showSearch = false;
        }
    }
}
