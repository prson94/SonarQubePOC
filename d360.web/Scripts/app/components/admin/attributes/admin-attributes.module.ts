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

import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';

import { AdminAttributesComponent } from './admin-attributes.component';
import { AdminAttributeTypeEditor } from './admin-attribute-type-editor.component';
import { AdminAttributeAllocationComponent } from './admin-attribute-allocation.component';

import { AdminAttributesRoutingModule } from './admin-attributes.routes';

import {
    ButtonModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    MultiSelectModule,
    SharedModule,
    TreeTableModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminAttributesRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        TreeTableModule,
        TableModule,

        //d3s        
        CoreModule,
        PipesModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        
        SharedObjectDetailsModule,
        SharedDynamicGridEditorModule,
        SharedFieldDefinitionModule,
        TilesModule,
    ],
    declarations: [
        AdminAttributesComponent,
        AdminAttributeTypeEditor,
        AdminAttributeAllocationComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true
        },
    ]
})
export class AdminAttributesModule { }