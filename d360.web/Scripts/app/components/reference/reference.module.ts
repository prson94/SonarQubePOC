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
import { SharedAuditModule } from '../shared/audit/shared-audit.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedFieldDefinitionModule } from '../shared/fielddefinition/shared-field-definition.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDiagramModule } from '../shared/diagram/shared-diagram.module';
import { SharedResponsibilitiesModule } from '../shared/responsibilities/shared-responsibilities.module';

import { ReferenceRoutingModule } from './reference.routes';
import { ReferenceComponent } from './reference.component';
import { ReferenceListComponent } from './reference-list.component';
import { ReferenceItemTypeEditorComponent } from './reference-item-type-editor.component';
import { ReferenceItemTypeGridComponent } from './reference-item-type-list.component';


import {
    ButtonModule,
    DataTableModule,
    EditorModule,
    InputTextModule,
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
        InputTextModule,                       
        SharedModule,
        TooltipModule,
        
        //d3s        
        CoreModule,
        D3SSharedModule,
        PipesModule,    
        SharedAuditModule,    
        SharedDeleteFormModule,
        SharedFieldDefinitionModule,
        SharedDiagramModule,
        SharedGridPagingInfoModule,   
        SharedResponsibilitiesModule,     
        TilesModule,
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