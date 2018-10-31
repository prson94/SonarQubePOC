import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { PipesModule } from '../../../pipes/pipes.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';

import { AdminPredicatesComponent } from './admin-predicates.component';

import { AdminPredicateRoutingModule } from './admin-predicates.routes';

import {
    ButtonModule,
    InputTextModule,
    SharedModule,
    DataTableModule,
    GrowlModule
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminPredicateRoutingModule,

        //prime
        ButtonModule,
        InputTextModule,
        SharedModule,
        DataTableModule,
        GrowlModule,
        TableModule,

        //d3s        
        CoreModule,
        PipesModule,        
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,        
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [        
        AdminPredicatesComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminPredicatesModule { }