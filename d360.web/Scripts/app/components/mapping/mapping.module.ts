import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';

import { MappingRoutingModule } from './mapping.routes';

import { MappingComponent } from './mapping.component';

import {    
    DataTableModule,        
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,

        //routing 
        MappingRoutingModule,

        //d3s        
        CoreModule,        
        
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedGridPagingInfoModule,
        TilesModule,

        //prime
        DataTableModule,
        SharedModule,
        TableModule,
    ],
    declarations: [
        MappingComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class MappingModule { }