import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';


import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';

import { AdminCustomAPIComponent } from './admin-customapi.component';
import { AdminCustomAPIEndpointsComponent } from './admin-customapi-endpoint.component';
import { AdminCustomAPIServiceDetailComponent } from './admin-customapi-service-detail.component';

import { AdminCustomAPIRoutingModule } from './admin-customapi.routes';

import {
    ButtonModule,
    EditorModule,
    InputTextModule,
    SharedModule,
    DataTableModule
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminCustomAPIRoutingModule,

        //prime              
        SharedModule,
        DataTableModule,

        //d3s                
        CoreModule,
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,        
        SharedGridPagingInfoModule,    
        TilesModule,
    ],
    declarations: [
        AdminCustomAPIComponent,   
        AdminCustomAPIEndpointsComponent,    
        AdminCustomAPIServiceDetailComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminCustomAPIModule { }