import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';

import { TableModule } from 'primeng/table';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { ShoppingCartRoutingModule } from './shopping-cart.routes';
import { ShoppingCartComponent } from './shopping-cart.component';
import { ShoppingCartRequestComponent } from './shopping-cart-request.component';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

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

    ]
})
export class ShoppingCartModule { }