import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../../http-interceptors/govern-post-request.interceptor";
import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { D3SSharedModule } from '../../shared/shared.module';

import { AdminGroupsComponent } from './admin-groups.component';
import { GroupForm } from './group.form';

import { AdminGroupsRoutingModule } from './admin-groups.routes';

import {
    ButtonModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminGroupsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s        
        CoreModule,  
        D3SSharedModule,      
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedFieldDefinitionModule,
        TilesModule,        
    ],
    declarations: [
        AdminGroupsComponent,
        GroupForm,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class AdminGroupsModule { }