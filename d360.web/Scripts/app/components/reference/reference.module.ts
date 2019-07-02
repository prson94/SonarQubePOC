import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernPostRequestInterceptor } from "../../http-interceptors/govern-post-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedFieldDefinitionModule } from '../shared/fielddefinition/shared-field-definition.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';
import { SharedAssetTypeEditorModule } from '../shared/assettypeeditor/shared-asset-type-editor.module';

import { ReferenceRoutingModule } from './reference.routes';
import { ReferenceComponent } from './reference.component';
import { ReferenceListComponent } from './reference-list.component';
import { ReferenceItemTypeGridComponent } from './reference-item-type-list.component';


import {
    ButtonModule,
    EditorModule,
    InputTextModule,
    SharedModule,
    TooltipModule,
} from 'primeng/primeng';
import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        ReferenceRoutingModule,

        //primeng
        ButtonModule,
        EditorModule,
        InputTextModule,                       
        SharedModule,
        TooltipModule,
        TableModule,
        
        //d3s        
        CoreModule,      
        PipesModule,    
            
        SharedDeleteFormModule,
        SharedFieldDefinitionModule,        
        SharedDynamicGridEditorModule,
        SharedGridPagingInfoModule,        
        SharedObjectDetailsModule,
        SharedAssetTypeEditorModule,
        TilesModule,
    ],
    declarations: [                
        ReferenceItemTypeGridComponent,
        ReferenceListComponent,
        ReferenceComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]   
})
export class ReferenceModule { }