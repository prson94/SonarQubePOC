import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-resource-change-pwd',
    template: `            
            <div class="row">
                <div class="col s12">
                    <div class="tile tile-detail">  
                                <d3s-resource-password></d3s-resource-password>      
                    </div>                    
                </div>
            </div>
        `,
})

export class ResourceChangePwdComponent extends BaseComponent {
    constructor(private headerBreadcrumbService: HeaderBreadcrumbService, private titleService: Title) {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Change Your Password'));
        this.setBrowserTitle(this.titleService, 'Change Your Password');
    }
}