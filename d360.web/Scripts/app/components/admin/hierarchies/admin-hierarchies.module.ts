import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

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

import { AdminHierarchiesComponent } from './admin-hierarchies.component';
import { AdminLevelListComponent } from './admin-level-list.component';
import { AdminLevelEditorComponent } from './admin-level-editor.component';

import { AdminHierarchiesRoutingModule } from './admin-hierarchies.routes';

import { SharedModule } from 'primeng/shared';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';
import { ColorPickerModule } from 'primeng/colorpicker';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';



@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        

        AdminHierarchiesRoutingModule,

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
        AdminHierarchiesComponent,        
        AdminLevelListComponent,
        AdminLevelEditorComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class AdminHierarchiesModule { }