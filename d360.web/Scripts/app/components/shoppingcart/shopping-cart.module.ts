import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import {
    SharedModule,
    ButtonModule,
    DataTableModule,
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
        HttpModule,
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
        DataTableModule,
        TableModule,
    ],
    declarations: [
        ShoppingCartComponent,
        ShoppingCartRequestComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class ShoppingCartModule { }