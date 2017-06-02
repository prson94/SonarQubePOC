import {
    ShoppingCartType,
    ShoppingCart,
    ShoppingCartItem,
    ShoppingCartListItem,
    CartModel,
} from '../models/shopping-cart.model';

import { Injectable } from '@angular/core';
import { Headers, Http } from '@angular/http';
import { BaseService } from './base.service';
import { MessagesService } from './messages.service';

@Injectable()
export class ShoppingCartService extends BaseService {

    constructor(private http: Http, messagesService: MessagesService) { super(messagesService); }


    getMyShoppingCartItems(shoppingCartTypeID: number): Promise<CartModel> {
        return this.http.get(`form/shoppingcart/list/${shoppingCartTypeID}`)
            .toPromise()
            .then(response => <CartModel>response.json())
            .catch(err => this.handleError(err));
    }

    addShoppingCartItem(type: string, id: number, cartTypeID: number) {
        return this.http.put(`form/shoppingcart/add?id=${id}&type=${type}&cartTypeID=${cartTypeID}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    removeShoppingCartItem(type: string, id: number, shoppingCartID: number) {
        return this.http.delete(`form/shoppingcart/remove?id=${id}&type=${type}&shoppingCartID=${shoppingCartID}`)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    requestShoppingCart(cart: ShoppingCart) {
        return this.http.post('form/shoppingcart/request', cart)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    emptyShoppingCart(cartID: number) {
        return this.http.post(`form/shoppingcart/clear?cartID=${cartID}`, null)
            .toPromise()
            .then(response => response.json())
            .catch(err => this.handleError(err));
    }

    getShoppingCartItems(cartID: number): Promise<CartModel> {
        return this.http.get(`form/shoppingcart/list/1/${cartID}`)
            .toPromise()
            .then(response => <CartModel>response.json())
            .catch(err => this.handleError(err));
    }

}