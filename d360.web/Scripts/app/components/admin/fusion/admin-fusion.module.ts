import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { ColorPickerModule } from 'angular2-color-picker';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedAuditModule } from '../../shared/audit/shared-audit.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { SharedResponsibilitiesModule } from '../../shared/responsibilities/shared-responsibilities.module';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { D3SSharedModule } from '../../shared/shared.module';

import { AdminFusionComponent } from './admin-fusion.component';
import { FusionAttributesTile } from './fusion-attributes.tile';
import { FusionConfigurationTile } from './fusion-configuration.tile';
import { FusionScheduleComponent } from './fusion-schedule.component';
import { FusionScheduleEditorComponent } from './fusion-schedule-editor.component';

import { AdminFusionRoutingModule } from './admin-fusion.routes';

import {
    ButtonModule,
    CalendarModule,    
    EditorModule,
    GrowlModule,
    InputMaskModule,
    InputTextModule,
    SharedModule,
    DataTableModule,
    TreeTableModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminFusionRoutingModule,

        //prime  
        ButtonModule,
        CalendarModule,
        EditorModule,
        GrowlModule,
        InputMaskModule,
        InputTextModule,
        SharedModule,
        DataTableModule,
        TreeTableModule,

        //color picker
        ColorPickerModule,

        //d3s                
        CoreModule,
        D3SSharedModule,
        SharedAuditModule,
        SharedDeleteFormModule,
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        SharedFieldDefinitionModule,
        SharedResponsibilitiesModule,
        SharedDynamicGridEditorModule,
        TilesModule,
    ],
    declarations: [
        AdminFusionComponent,
        FusionAttributesTile,
        FusionConfigurationTile,
        FusionScheduleComponent,
        FusionScheduleEditorComponent
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminFusionModule { }