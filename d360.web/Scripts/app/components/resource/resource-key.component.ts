import { Component, OnInit, OnDestroy } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-resource-key',
    template: `            
            <div class="row">
                <div class="col s12">
                    <div class="tile tile-detail">  
                                <d3s-resource-api></d3s-resource-api>      
                    </div>                    
                </div>
            </div>
        `,    
})

export class ResourceKeyComponent extends BaseComponent implements OnInit {
    
    constructor(private headerBreadcrumbService: HeaderBreadcrumbService, private titleService: Title) {
        super();        
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Your API Key'));
        this.setBrowserTitle(this.titleService, 'Your API Key');
    }
}