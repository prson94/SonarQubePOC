import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import {AdminModule} from '../admin.module';
import {CoreModule} from '../../shared/core.module';
import {PipesModule} from '../../../pipes/pipes.module';
import {TilesModule} from '../../shared/tiles/tiles.module';
import {SharedGridPagingInfoModule} from '../../shared/grid-paging-info.component';
import {SharedDeleteFormModule} from '../../shared/delete.form';

import {SharedObjectDetailsModule} from '../../shared/objectdetails/shared-object-details.module';
import {SharedResponsibilitiesModule} from '../../shared/responsibilities/shared-responsibilities.module';
import {SharedFieldDefinitionModule} from '../../shared/fielddefinition/shared-field-definition.module';
import {SharedAssetTypeEditorModule} from '../../shared/assettypeeditor/shared-asset-type-editor.module';
import {AdminRelationshipEditorModule} from '../../shared/relationshipeditor/admin-relationship-editor.module';

import { AdminDiagramAssetComponent } from './admin-diagram-asset.component';


import {SimpleAccordionModule} from '../../shared/simple-accordion.part';

import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { SpinnerModule } from 'primeng/spinner';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { ColorPickerModule } from 'primeng/colorpicker';
import { EditorModule } from 'primeng/editor';

import {AdminResponsibilitiesModule} from '../responsibilities/admin-responsibilities.module';
import { AdminDiagramAssetRoutingModule } from './admin-diagram-asset.routes';
import { AssetTypeDeleteModule } from '../asset-type-delete/asset-type-delete.module';
import { TableModule } from 'primeng/table';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        AdminDiagramAssetRoutingModule,
        //prime
        ButtonModule,
        CheckboxModule,
        DropdownModule,
        SpinnerModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        TableModule,

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
        AssetTypeDeleteModule,
        AdminResponsibilitiesModule,

        SharedObjectDetailsModule,
        SharedFieldDefinitionModule,
        SharedResponsibilitiesModule,
        TilesModule,
    ],
    declarations: [
        AdminDiagramAssetComponent,
    ],
    providers: [
    ]
})

export class AdminDiagramAssetModule {
}
