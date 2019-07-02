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
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedAssetTypeEditorModule } from '../../shared/assettypeeditor/shared-asset-type-editor.module';

import { AdminModule } from '../admin.module';

import { AdminTaxonomiesComponent } from './admin-taxonomies.component';
import { AdminTaxonomyDetailComponent } from './admin-taxonomy-detail.component';

import { AdminTaxonomiesRoutingModule } from './admin-taxonomies.routes';

import {
    ButtonModule,
    ColorPickerModule,
    EditorModule,
    DropdownModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';



@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

        AdminTaxonomiesRoutingModule,

        //prime
        ButtonModule,
        EditorModule,
        DropdownModule,
        InputTextModule,
        SharedModule,
        TableModule,

        // color picker
        ColorPickerModule,

        //d3s       
        AdminModule,
        CoreModule,
        PipesModule,
        
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedObjectDetailsModule,
        SharedResponsibilitiesModule,
        SharedFieldDefinitionModule,
        SharedDynamicGridEditorModule,
        SharedAssetTypeEditorModule,
        TilesModule,
    ],
    declarations: [
        AdminTaxonomiesComponent,
        AdminTaxonomyDetailComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernPostRequestInterceptor,
            multi: true },
    ]
})
export class AdminTaxonomiesModule { }