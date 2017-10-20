import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpModule, XHRBackend } from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { AdminModule } from '../admin.module';
import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';


import { AdminMapsComponent } from './admin-maps.component';
import { AdminMapsEditorComponent } from './admin-maps-editor.component';
import { AdminMapsListComponent } from './admin-maps-list.component';
import { AdminMapsTemplateListComponent } from './admin-maps-template-list.component';
import { AdminMapsTemplateEditorComponent } from './admin-maps-template-editor.component';
import { AdminMapsRoutingComponent } from './admin-maps.routes';


import {
    ButtonModule,
    ColorPickerModule,
    DropdownModule,
    EditorModule,
    InputTextModule,
    SharedModule,
    DataTableModule,
    OrderListModule,
    PickListModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,

        AdminMapsRoutingComponent,

        //prime
        ButtonModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        SharedModule,
        DataTableModule,
        OrderListModule,
        PickListModule,

        //color picker 
        ColorPickerModule,

        //d3s  
        AdminModule,
        CoreModule,
        PipesModule,
        SharedGridPagingInfoModule,

        TilesModule,
    ],
    declarations: [
        AdminMapsComponent,
        AdminMapsEditorComponent,
        AdminMapsListComponent,
        AdminMapsTemplateListComponent,
        AdminMapsTemplateEditorComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminMapsModule { }