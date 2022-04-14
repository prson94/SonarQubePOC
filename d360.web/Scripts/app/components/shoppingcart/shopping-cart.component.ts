import { Component, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ShoppingCartService } from '../../services/shopping-cart.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { Location } from '@angular/common';
import { ShoppingCart, ShoppingCartListItem } from '../../models/shopping-cart.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { SecondaryNavService } from '../../services/right-sidebar.service';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-shopping-cart',
    templateUrl: './shopping-cart.component.html',
    providers: [ShoppingCartService]
})

export class ShoppingCartComponent extends BaseComponent implements OnInit {
    private cartIsEmpty = true;
    private title = $localize`Your Shopping Cart`;
    private cart: ShoppingCart;
    private items: ShoppingCartListItem[] = [];
    private mode: CartMode = CartMode.Default;
    CartMode = CartMode;

    constructor(
        private headerBreadcrumbService: HeaderBreadcrumbService,
        protected secondaryNavService: SecondaryNavService,
        private titleService: Title,
        private locationService: Location,
        private shoppingCartService: ShoppingCartService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private router: Router) {
        super(settingsService);
        this.secondaryNavService = secondaryNavService;
    }

    ngOnInit() {
        this.headerBreadcrumbService.clearBreadcrumbs();
        this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.title));
        this.setBrowserTitle(this.titleService, this.title);
        this.clearSidebar();
        this.secondaryNavService.clearCurrentObject();
        this.secondaryNavService.setCurrentArea($localize`Shopping Cart`, 'fa-shopping-cart', null);
        this.secondaryNavService.showHeader(true);
        this.load();
    }

    load() {
        this.isLoading = true;
        this.shoppingCartService.getMyShoppingCartItems(1)
            .subscribe(r => {
                this.cart = r.Cart;
                this.items = r.Items;

                this.cartIsEmpty = (this.items == null || this.items.length == 0);
                this.isLoading = false;
            });
    }

    back() {
        this.locationService.back();
    }

    delete(item: ShoppingCartListItem) {
        this.shoppingCartService.removeShoppingCartItem(item.Object, item.ObjectID, this.cart.ID)
            .subscribe(r => {
                this.showMessageForResult(this.messagesService, r);
                this.load();
            });
    }

    request() {
        this.isLoading = true;
        this.shoppingCartService.requestShoppingCart(this.cart)
            .subscribe(r => {
                this.showMessageForResult(this.messagesService, r);
                this.mode = CartMode.Default;
                this.load();
            });
    }

    clear() {
        this.isLoading = true;
        this.shoppingCartService.emptyShoppingCart(this.cart.ID)
            .subscribe(r => {
                this.showMessageForResult(this.messagesService, r);
                this.mode = CartMode.Default;
                this.load();
            });
    }

    navigate(item: ShoppingCartListItem) {
        this.router.navigateByUrl(item.Url);
    }
}

enum CartMode {
    Default,
    ConfirmDelete,
    ConfirmRequest
}
