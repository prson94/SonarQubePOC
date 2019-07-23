import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';

import { AuthenticationConnectionBackend } from '../../../authentication-connection-backend';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { AdminTagsComponent } from './admin-tags.component';
import { AdminTagsActionComponent } from './admin-tags-action.component';
import { AdminTagsConsolidateComponent } from './admin-tags-consolidate.component'
import { D3SCheckbox } from '../../shared/controls/gov-checkbox';


import { AdminTagsRoutingModule } from './admin-tags.routes';

import {
    ButtonModule,
    EditorModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminTagsRoutingModule,

        //prime      
        ButtonModule,
        EditorModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s                
        CoreModule,                
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [
        AdminTagsComponent,
        AdminTagsActionComponent,
        AdminTagsConsolidateComponent,
        D3SCheckbox
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminTagsModule { }