import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { AdminModule } from '../admin.module';
import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';

import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedAssetTypeEditorModule } from '../../shared/assettypeeditor/shared-asset-type-editor.module';
import { D3SSharedModule } from '../../shared/shared.module';

import { AdminFusionComponent } from './admin-fusion.component';
import { FusionAttributesTile } from './fusion-attributes.tile';
import { FusionConfigurationTile } from './fusion-configuration.tile';
import { FusionScheduleComponent } from './fusion-schedule.component';
import { FusionScheduleEditorComponent } from './fusion-schedule-editor.component';

import { FusionAttributeTypeCustomQueryComponent } from './fusion-attribute-type-custom-query.component';
import { FusionAttributeTypeCustomQueryEditorComponent } from './fusion-attribute-type-custom-query-editor.component';


import { AdminFusionRoutingModule } from './admin-fusion.routes';

import {
    ButtonModule,
    CalendarModule,
    ColorPickerModule,
    DropdownModule,
    EditorModule,
    GrowlModule,
    InputMaskModule,
    InputTextModule,
    SharedModule,
    TreeTableModule,
    TooltipModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { CodemirrorModule } from 'ng2-codemirror';
import { DirectivesModule } from '../../../directives/directives.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminFusionRoutingModule,

        //prime  
        ButtonModule,
        CalendarModule,
        DropdownModule,
        EditorModule,
        GrowlModule,
        InputMaskModule,
        InputTextModule,
        SharedModule,
        TreeTableModule,
        TableModule,

        //color picker
        ColorPickerModule,

        //editor
        CodemirrorModule,

        //d3s                
        CoreModule,
        AdminModule,
        D3SSharedModule,
        
        SharedDeleteFormModule,
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        SharedFieldDefinitionModule,
        SharedResponsibilitiesModule,
        SharedDynamicGridEditorModule,
        SharedAssetTypeEditorModule,
        TilesModule,
        DirectivesModule,
        TooltipModule,
    ],
    declarations: [
        AdminFusionComponent,
        FusionAttributesTile,
        FusionConfigurationTile,
        FusionScheduleComponent,
        FusionScheduleEditorComponent,
        FusionAttributeTypeCustomQueryComponent,
        FusionAttributeTypeCustomQueryEditorComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminFusionModule { }