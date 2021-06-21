import {
    ShoppingCart,
    CartModel,
} from '../models/shopping-cart.model';

import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { catchError, map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { BaseObservableService } from './baseObservable.service';
import { MessagesObservableService } from './messages-observable.service';

@Injectable({
    providedIn: 'root'
})
export class ShoppingCartService extends BaseObservableService  {

    constructor(private http: HttpClient, messagesService: MessagesObservableService) { super(messagesService); }


    getMyShoppingCartItems(shoppingCartTypeID: number): Observable<CartModel> {
        return this.http.get(`form/shoppingcart/list/${shoppingCartTypeID}`)
            .pipe(
                map(response => <CartModel>response),
                catchError(err => this.handleError(err))
            );
    }

    addShoppingCartItem(type: string, id: number, cartTypeID: number) {
        return this.http.put(`form/shoppingcart/add?id=${id}&type=${type}&cartTypeID=${cartTypeID}`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    removeShoppingCartItem(type: string, id: number, shoppingCartID: number) {
        return this.http.delete(`form/shoppingcart/remove?id=${id}&type=${type}&shoppingCartID=${shoppingCartID}`)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    requestShoppingCart(cart: ShoppingCart) {
        return this.http.post('form/shoppingcart/request', cart)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    emptyShoppingCart(cartID: number) {
        return this.http.post(`form/shoppingcart/clear?cartID=${cartID}`, null)
            .pipe(
                map(response => response),
                catchError(err => this.handleError(err))
            );
    }

    getShoppingCartItems(cartID: number): Observable<CartModel> {
        return this.http.get(`form/shoppingcart/list/1/${cartID}`)
            .pipe(
                map(response => <CartModel>response),
                catchError(err => this.handleError(err))
            );
    }

}