import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";

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

import { SharedModule } from 'primeng/shared';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { ColorPickerModule } from 'primeng/colorpicker';
import { CalendarModule } from 'primeng/calendar';
import { EditorModule } from 'primeng/editor';
import { TableModule } from 'primeng/table';
import { GrowlModule } from 'primeng/growl';
import { InputMaskModule } from 'primeng/inputmask';
import { TreeTableModule } from 'primeng/treetable';
import { TooltipModule } from 'primeng/tooltip';

import { CodemirrorModule } from 'ng2-codemirror';
import { DirectivesModule } from '../../../directives/directives.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,

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
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class AdminFusionModule { }