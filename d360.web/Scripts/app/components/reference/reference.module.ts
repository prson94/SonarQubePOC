import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';

import { ReferenceRoutingModule } from './reference.routes';
import { ReferenceComponent } from './reference.component';
import { ReferenceListComponent } from './reference-list.component';
import { ReferenceItemTypeEditorComponent } from './reference-item-type-editor.component';
import { ReferenceItemTypeGridComponent } from './reference-item-type-list.component';


import {
    ButtonModule,
    DataTableModule,
    EditorModule,
    GrowlModule,
    SharedModule,
    TooltipModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        ReferenceRoutingModule,

        //primeng
        ButtonModule,
        DataTableModule,
        EditorModule,
        GrowlModule,                
        SharedModule,
        TooltipModule,
        
        //d3s
        D3SSharedModule,
        CoreModule,
        PipesModule,
        TilesModule,
        SharedGridPagingInfoModule,
    ],
    declarations: [        
        ReferenceItemTypeEditorComponent,
        ReferenceItemTypeGridComponent,
        ReferenceListComponent,
        ReferenceComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]   
})
export class ReferenceModule { }