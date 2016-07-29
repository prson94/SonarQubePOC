///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';
import {AutoComplete} from 'primeng/primeng';

@Component({
    selector: 'd3s-fusion-list',
    directives: [AutoComplete],
    template: ` <p-autoComplete size="50"                                                         
                            [suggestions]="results" 
                            (completeMethod)="search($event)" 
                            >                       
                    </p-autoComplete>
                `
})

export class FusionListComponent extends BaseComponent implements OnInit {
    results: any[] = [];
    result: any;

    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, 'Fusion');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Fusion'));        
    }

    search(event) {
        this.results = [];
        this.results.push('test1');
        this.results.push( 'test2' );
        this.results.push('test3');
        console.log(this.results);
    }
};