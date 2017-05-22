import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
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

import { AdminLookupTypeEditorComponent } from './admin-lookup-type-editor.component';
import { AdminLookupsComponent } from './admin-lookups.component';

import { AdminLookupRoutingModule } from './admin-lookups.routes';

import {
    ButtonModule,
    InputTextModule,
    SharedModule,
    DataTableModule,
    GrowlModule
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        
        AdminLookupRoutingModule,
                
        //prime
        ButtonModule,
        InputTextModule,
        SharedModule,
        DataTableModule,
        GrowlModule,

        //d3s        
        CoreModule,
        PipesModule,
        
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedFieldDefinitionModule,
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    declarations: [
        AdminLookupTypeEditorComponent,
        AdminLookupsComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class AdminLookupsModule { }