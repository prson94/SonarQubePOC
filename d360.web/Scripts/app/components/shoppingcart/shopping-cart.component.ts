import { Component, Input, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { Location } from '@angular/common';

@Component({
    selector: 'd3s-shopping-cart',
    templateUrl: './shopping-cart.component.html'
})

export class ShoppingCartComponent extends BaseComponent implements OnInit {
    private cartIsEmpty = true;
    private title = 'Your Shopping Cart';

    constructor(private headerBreadcrumbService: HeaderBreadcrumbService, private titleService: Title, private locationService: Location)
    {
        super();
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.title));
        this.setBrowserTitle(this.titleService, this.title);
    }

    back() {
        this.locationService.back();
    }
}
