import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { GovernRequestInterceptor } from '../../../http-interceptors/govern-request.interceptor';

import { RouterModule } from '@angular/router';

import { ButtonModule } from 'primeng/button';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { PermissionsRoutingModule } from './permissions.routes';

import { PermissionsComponent } from './permissions.component';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { AdminModule } from '../../admin/admin.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        PermissionsRoutingModule,
        
        //prime
        ButtonModule,

        //d3s        
        CoreModule,
        TilesModule,
        SharedFieldDefinitionModule,
        SharedResponsibilitiesModule,
        AdminModule,
    ],
    declarations: [
        PermissionsComponent,
    ],
    providers: [

    ]
})
export class PermissionsModule { }