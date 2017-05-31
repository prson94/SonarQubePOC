import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import {
    SharedModule,
    ButtonModule,
} from 'primeng/primeng';

import { CoreModule } from '../shared/core.module';
import { SharedDiagramModule } from '../shared/diagram/shared-diagram.module';
import { TilesModule  } from '../shared/tiles/tiles.module';

import { ShoppingCartRoutingModule } from './shopping-cart.routes';

import { ShoppingCartComponent } from './shopping-cart.component';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        ShoppingCartRoutingModule,

        //d3s        
        CoreModule,
        SharedDiagramModule,
        TilesModule,

        //prime                
        SharedModule,
        ButtonModule,
    ],
    declarations: [
        ShoppingCartComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class ShoppingCartModule { }