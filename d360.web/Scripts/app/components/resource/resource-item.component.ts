///<reference path="../../es6-shim.d.ts"/>
import { Input, Component, EventEmitter, Output, OnInit, OnDestroy } from '@angular/core';
import { BaseComponent } from '../shared/base.component';
import { Title } from '@angular/platform-browser';
import { HeaderBreadcrumbService } from '../../services/index';
import { Breadcrumb } from '../../models/breadcrumb.model';

@Component({
    selector: 'd3s-policy-item',
    template: `Resource / User Info 
                `
})

export class ResourceItemComponent extends BaseComponent implements OnInit {
    constructor(protected titleService: Title, protected headerBreadcrumbService: HeaderBreadcrumbService) {
        super();
    }

    ngOnInit() {
        this.setBrowserTitle(this.titleService, '- Resource');

        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Resource'));
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb('Put resource name here'));
    }
};