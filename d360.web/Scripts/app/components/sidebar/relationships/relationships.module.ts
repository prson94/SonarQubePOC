import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

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
        FormsModule,
        HttpModule,
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
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class RelationshipsModule { }