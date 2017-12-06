import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import {
    ButtonModule,
} from 'primeng/primeng';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { MemberGroupRoutingModule } from './membergroup.routes';

import { MemberGroupComponent } from './membergroup.component';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';


@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        MemberGroupRoutingModule,

        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        SharedFieldDefinitionModule,

    ],
    declarations: [
        MemberGroupComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class MemberGroupModule { }