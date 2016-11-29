import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';

import { AdminTemplateEditorComponent } from './admin-template-editor';
import { AdminTemplatesComponent } from './admin-templates.component';

import { AdminTemplatesRoutingModule } from './admin-templates.routes';

import {
    ButtonModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    MultiSelectModule,
    SharedModule,
    DataTableModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminTemplatesRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        MultiSelectModule,
        SharedModule,
        DataTableModule,

        //d3s        
        CoreModule,
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,                
        TilesModule,
    ],
    declarations: [
        AdminTemplateEditorComponent,
        AdminTemplatesComponent,        
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminTemplatesModule { }