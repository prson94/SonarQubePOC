import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { HeaderBreadcrumbService } from '../../services/header-breadcrumb.service';
import { ShoppingCartService } from '../../services/shopping-cart.service';
import { Breadcrumb } from '../../models/breadcrumb.model';
import { Title } from '@angular/platform-browser';
import { Location } from '@angular/common';
import { ShoppingCart, ShoppingCartListItem } from '../../models/shopping-cart.model';
import { MessagesObservableService } from '../../services/messages-observable.service';
import { CompanySettingsService } from '../../services/settings.service';

@Component({
    selector: 'd3s-shopping-cart-request',
    templateUrl: './shopping-cart-request.component.html',
    providers: [ShoppingCartService]
})

export class ShoppingCartRequestComponent extends BaseComponent implements OnInit, OnDestroy {
    private title = $localize`Shopping Cart Request`;
    private cart: ShoppingCart;
    private items: ShoppingCartListItem[] = [];
    private sub;
    private cartId;

    constructor(
        private headerBreadcrumbService: HeaderBreadcrumbService,
        private titleService: Title,
        private locationService: Location,
        private shoppingCartService: ShoppingCartService,
        private messagesService: MessagesObservableService,
        protected settingsService: CompanySettingsService,
        private route: ActivatedRoute,
        private router: Router) {
        super(settingsService);
    }

    ngOnInit() {

        this.sub = this.route.params.subscribe(params => {
            this.cartId = +params['cartId'];

            this.headerBreadcrumbService.clearBreadcrumbs();
            this.headerBreadcrumbService.showBreadcrumb(new Breadcrumb(this.title));
            this.setBrowserTitle(this.titleService, this.title);

            this.load();
        });


    }

    ngOnDestroy() {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    load() {
        this.isLoading = true;
        this.shoppingCartService.getShoppingCartItems(this.cartId)
            .subscribe(r => {
                this.cart = r.Cart;
                this.items = r.Items;
                this.isLoading = false;
            });
    }

    back() {
        this.locationService.back();
    }

    navigate(item: ShoppingCartListItem) {
        this.router.navigateByUrl(item.Url);
    }
}

