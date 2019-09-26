import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";


import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';

import { AdminCustomAPIComponent } from './admin-customapi.component';
import { AdminCustomAPIEndpointsComponent } from './admin-customapi-endpoint.component';
import { AdminCustomAPIServiceDetailComponent } from './admin-customapi-service-detail.component';
import { AdminCustomAPIEndpointDetailComponent } from './admin-customapi-endpoint-detail.component';
import { AdminCustomAPIEndpointVersionsComponent } from './admin-customapi-endpoint-version.component';
import { AdminCustomAPIEndpointVersionFieldsComponent } from './admin-customapi-endpoint-version-fields.component';
import { AdminCustomAPIEndpointVersionFieldsEditorComponent } from './admin-customapi-endpoint-version-fields-editor.component';
import { AdminCustomAPIEndpointVersionUriTypesComponent } from './admin-customapi-endpoint-version-uris.component';
import { AdminCustomAPIServiceNamespaceComponent } from './admin-customapi-service-namespace.component';

import { AdminCustomAPIRoutingModule } from './admin-customapi.routes';

import { SharedModule } from 'primeng/shared';
import { ButtonModule } from 'primeng/button';
import { MultiSelectModule } from 'primeng/multiselect';
import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminCustomAPIRoutingModule,

        //prime              
        SharedModule,
        MultiSelectModule,
        ButtonModule,
        TableModule, 

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
        AdminCustomAPIEndpointDetailComponent,
        AdminCustomAPIEndpointVersionsComponent,
        AdminCustomAPIEndpointVersionFieldsComponent,
        AdminCustomAPIEndpointVersionFieldsEditorComponent,
        AdminCustomAPIEndpointVersionUriTypesComponent,
        AdminCustomAPIServiceNamespaceComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class AdminCustomAPIModule { }