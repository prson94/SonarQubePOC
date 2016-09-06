///<reference path="../../es6-shim.d.ts"/>
import { Component, OnInit } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService, RulesService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-search',
    template: ` Search Page
             ` ,
})

export class SearchComponent extends BaseComponent implements OnInit { 
    
    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();        
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Search');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Search'));
    }


}
