import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import {    
    SharedModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { SharedRelationshipModule } from '../../shared/relationship/shared-relationship.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { RelationshipsRoutingModule } from './relationships.routes';

import { RelationshipsComponent } from './relationships.component';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        //routing 
        RelationshipsRoutingModule,

        //d3s        
        CoreModule,
        SharedRelationshipModule,
        TilesModule,

        //prime        
        SharedModule,
    ],
    declarations: [
        RelationshipsComponent,        
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class RelationshipsModule { }