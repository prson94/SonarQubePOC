import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AdminModule } from '../admin.module';
import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';

import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { SharedAssetTypeEditorModule } from '../../shared/assettypeeditor/shared-asset-type-editor.module';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';

import { AdminExportTemplatesComponent } from './admin-export-templates.component';
import { AdminExportTemplateFieldsComponent } from './admin-export-template-fields.component';

import { AdminExportTemplatesRoutingModule } from './admin-export-templates.routes';

import { SimpleAccordionModule } from '../../shared/simple-accordion.part';

import { SharedModule } from 'primeng/api';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { ColorPickerModule } from 'primeng/colorpicker';
import { SpinnerModule } from 'primeng/spinner';
import { EditorModule } from 'primeng/editor';
import { TableModule } from 'primeng/table';
import { CheckboxModule } from 'primeng/checkbox';
import { MultiSelectModule } from 'primeng/multiselect';
import { FileUploadModule } from 'primeng/fileupload';

import { AdminResponsibilitiesModule } from '../responsibilities/admin-responsibilities.module';
import { AdminExportTemplateStylesComponent } from './admin-export-template-styles.component';
import { AdminExportTemplateStyleFormComponent } from './admin-export-template-style-form.component';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,


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
        TableModule,
        FileUploadModule,

        //color picker 
        ColorPickerModule,
        SimpleAccordionModule,

        //d3s  
        AdminModule,        
        CoreModule,
        PipesModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,
        SharedAssetTypeEditorModule,

        AdminResponsibilitiesModule,

        SharedObjectDetailsModule,
        SharedFieldDefinitionModule,
        SharedDynamicGridEditorModule,
        TilesModule,
    ],
    declarations: [
        AdminExportTemplatesComponent,  
        AdminExportTemplateFieldsComponent,
        AdminExportTemplateStylesComponent,
        AdminExportTemplateStyleFormComponent,
    ],
    providers: [
    ]
})
export class AdminExportTemplatesModule { }