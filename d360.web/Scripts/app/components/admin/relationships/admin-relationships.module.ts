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
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { AdminRelationshipEditorModule } from '../../shared/relationshipeditor/admin-relationship-editor.module';

import { AdminRelationshipsComponent } from './admin-relationships.component';


import { AdminRelationshipsRoutingModule } from './admin-relationships.routes';

import {
    ButtonModule,
    InputTextModule,
    SharedModule,
    GrowlModule
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,

        AdminRelationshipsRoutingModule,

        //prime        
        SharedModule,
        TableModule,

        //d3s        
        AdminRelationshipEditorModule,
        CoreModule,
        PipesModule,
        
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedFieldDefinitionModule,
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [
        AdminRelationshipsComponent,        
    ],    
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminRelationshipsModule { }