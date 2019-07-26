import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';

import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';

import { AdminLookupTypeEditorComponent } from './admin-lookup-type-editor.component';
import { AdminLookupsComponent } from './admin-lookups.component';

import { AdminLookupRoutingModule } from './admin-lookups.routes';

import {
    ButtonModule,
    InputTextModule,
    SharedModule,
    GrowlModule
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        
        AdminLookupRoutingModule,
                
        //prime
        ButtonModule,
        InputTextModule,
        SharedModule,
        GrowlModule,
        TableModule,
         
        //d3s        
        CoreModule,
        PipesModule,
        
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedFieldDefinitionModule,
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [
        AdminLookupTypeEditorComponent,
        AdminLookupsComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class AdminLookupsModule { }