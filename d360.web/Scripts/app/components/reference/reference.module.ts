import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";

import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule } from '../shared/tiles/tiles.module';
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

import { ButtonModule } from 'primeng/button';
import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';
import { TooltipModule } from 'primeng/tooltip';
import { TableModule } from 'primeng/table';
import { ReferenceItemGridComponent } from './reference-item-list.component';
import { DirectivesModule } from '../../directives/directives.module';
import { SharedAssetEditorsModule } from '../shared/asseteditors/shared-asset-editor.module';
import { SearchFieldModule } from '../shared/controls/search-field/search-field.component';

@NgModule({
    imports: [
        CommonModule,
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
        DirectivesModule,

        SharedDeleteFormModule,
        SharedFieldDefinitionModule,
        SharedDynamicGridEditorModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        SharedAssetTypeEditorModule,
        SharedAssetEditorsModule,
        TilesModule,
        SearchFieldModule
    ],
    declarations: [
        ReferenceItemTypeGridComponent,
        ReferenceItemGridComponent,
        ReferenceListComponent,
        ReferenceComponent,
    ],
    exports: [
        ReferenceItemTypeGridComponent,
        ReferenceItemGridComponent,
        ReferenceListComponent,
        ReferenceComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
})
export class ReferenceModule { }