import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ShoppingCartComponent } from './shopping-cart.component';
import { ShoppingCartRequestComponent } from './shopping-cart-request.component';

const routes: Routes = [
    { path: '', component: ShoppingCartComponent },
    { path: ':cartId', component: ShoppingCartRequestComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule],
})
export class ShoppingCartRoutingModule { }