import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import {
    SharedModule,
    ButtonModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { ShoppingCartRoutingModule } from './shopping-cart.routes';
import { ShoppingCartComponent } from './shopping-cart.component';
import { ShoppingCartRequestComponent } from './shopping-cart-request.component';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        ShoppingCartRoutingModule,

        //d3s        
        CoreModule,        
        TilesModule,
        SharedGridPagingInfoModule,

        //prime                
        SharedModule,
        ButtonModule,
        TableModule,
    ],
    declarations: [
        ShoppingCartComponent,
        ShoppingCartRequestComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class ShoppingCartModule { }