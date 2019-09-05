import { Component, Input, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { BaseComponent } from '../shared/base.component';
import { SearchFullResult } from '../../models/search-result.model';
import { SiteUrlHelpers } from '../../static/site-url-helpers';
import { ShoppingCartService } from '../../services/shopping-cart.service';
import { MessagesObservableService } from '../../services/messages-observable.service';

declare var CompanySettings;

@Component({
    selector: 'd3s-search-result-item',
    template: `       
                <div class="search-res-container">
                    <h4 class="search-result-name">
                        <i *ngIf="result?.Icon" class="icon fa {{result?.Icon}}"></i><span *ngIf="result?.ImageUrl" class="icon"><img [src]="result.ImageUrl" /></span> <a (click)="navigateLink()" class="search-result-link" [innerHtml]="result?.Name"></a>
                        <span *ngIf="showShoppingCart && result.Group != 'Synonym' && result.Group != 'Attribute'" style="float: right; cursor: pointer; padding-right: 10px;" (click)="add()">
                            <i class="fa fa-cart-plus"></i>
                        </span>
                    </h4>
                    <p class="search-result-desc" *ngIf="result?.Description" [innerHtml]="result.Description"></p>
                    <h5 class="search-result-attributes"><span *ngIf="result?.Type">Category: <em class="result-category" [innerHtml]="result?.Type"></em>&nbsp;&nbsp;</span>Type: <em class="result-type">{{result?.Group}}</em></h5>
                </div>        
                `,
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [ShoppingCartService]
})

export class SearchResultItemComponent extends BaseComponent implements OnInit {
    @Input() result: SearchFullResult;

    showShoppingCart = false;

    get type() {
        if (this.result) {
            switch (this.result.Group) {
                case 'FusionAttributes':
                    return 'FusionAttribute';
                case 'Reference':
                    return 'ReferenceItemType';
                default:
                    return this.result.Group;
            }
        }
    }

    constructor(private router: Router, private shoppingCartService: ShoppingCartService, private messagesService: MessagesObservableService) {
        super();
    }

    ngOnInit() {
        if (CompanySettings != null && CompanySettings.EnableShoppingCart != null && CompanySettings.EnableShoppingCart.toString() == 'true')
            this.showShoppingCart = true;
    }

    private navigateLink() {
        this.router.navigateByUrl(SiteUrlHelpers.convertClassicUrl(this.result.Url));
    }

    private add() {
        var type = this.result.ID.toString().split('|')[0];
        var id = this.result.ID.toString().split('|')[1];
        this.shoppingCartService.addShoppingCartItem(this.type, +id, 1)
            .subscribe(r => this.showMessageForResult(this.messagesService, r));
    }
};