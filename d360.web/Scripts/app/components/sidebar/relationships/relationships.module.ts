import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';

import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';

import { CoreModule } from '../../shared/core.module';
import { SharedRelationshipModule } from '../../shared/relationship/shared-relationship.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { RelationshipsRoutingModule } from './relationships.routes';

import { RelationshipsComponent } from './relationships.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

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

    ]
})
export class RelationshipsModule { }