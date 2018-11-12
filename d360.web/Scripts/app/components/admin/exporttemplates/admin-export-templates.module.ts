import { NgModule } from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

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
import { ErrorNotifyInterceptor } from '../../../http-interceptors/error-notify-interceptor';


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
    FileUploadModule,
} from 'primeng/primeng';
import { TableModule } from 'primeng/table';
import { AdminResponsibilitiesModule } from '../responsibilities/admin-responsibilities.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        HttpClientModule,

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
    ],
    providers: [
        {
            provide: XHRBackend,
            useClass: AuthenticationConnectionBackend
        },
        {
            provide: HTTP_INTERCEPTORS,
            useClass: ErrorNotifyInterceptor,
            multi: true
        }
    ]
})
export class AdminExportTemplatesModule { }