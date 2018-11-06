import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { AdminModule } from '../admin.module';
import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';

import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { SharedAssetTypeEditorModule } from '../../shared/assettypeeditor/shared-asset-type-editor.module';
import { AdminRelationshipEditorModule } from '../../shared/relationshipeditor/admin-relationship-editor.module';

import { AdminExportTemplatesComponent } from './admin-export-templates.component';

import { AdminExportTemplatesRoutingModule } from './admin-export-templates.routes';

import { SimpleAccordionModule } from '../../shared/simple-accordion.part';


import {
    ButtonModule,
    CheckboxModule,
    SpinnerModule,
    ColorPickerModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    MultiSelectModule,
    SharedModule,
    TreeTableModule,
} from 'primeng/primeng';
import { AdminResponsibilitiesModule } from '../responsibilities/admin-responsibilities.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminExportTemplatesRoutingModule,

        //prime
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        SpinnerModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        TreeTableModule,

        //color picker 
        ColorPickerModule,
        SimpleAccordionModule,

        //d3s  
        AdminModule,
        AdminRelationshipEditorModule,
        CoreModule,
        PipesModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        SharedAssetTypeEditorModule,

        AdminResponsibilitiesModule,

        SharedObjectDetailsModule,
        SharedFieldDefinitionModule,
        SharedResponsibilitiesModule,
        TilesModule,
    ],
    declarations: [
        AdminExportTemplatesComponent,        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminExportTemplatesModule { }