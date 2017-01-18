import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { AdminRoutingModule } from './admin.routes';

import { CoreModule } from '../shared/core.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';

import {        
    ButtonModule,    
    DataTableModule,
    EditorModule,
    InputTextModule,    
    SharedModule,
} from 'primeng/primeng';

import { AdminAllocationComponent } from './admin-allocation.component';
import { AdminClassificationsComponent } from './admin-classifications.component';
import { AdminComponent } from './admin.component';
import { AdminLevelEditorComponent } from './admin-level-editor.component';
import { AdminLevelListComponent } from './admin-level-list.component';
import { AdminNymAllocationsComponent } from './admin-nym-allocations.component';
import { ClaimsTile } from './claims.tile';
import { ClaimsMatrixPart } from './claims-matrix.part';


@NgModule({
    declarations: [        
        AdminAllocationComponent,
        AdminClassificationsComponent,
        AdminComponent,                
        AdminLevelListComponent,
        AdminLevelEditorComponent,   
        AdminNymAllocationsComponent,             
        ClaimsMatrixPart,
        ClaimsTile,                          
    ],
    exports: [
        AdminAllocationComponent,
        AdminClassificationsComponent,
        ClaimsTile,
        AdminLevelListComponent,
    ],
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,
        AdminRoutingModule,
        
        //primeng                
        InputTextModule,
        DataTableModule,
        EditorModule,
        ButtonModule,
        SharedModule,
        
        //d3s        
        CoreModule,                                      
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,    
        SharedGridPagingInfoModule,
        TilesModule,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})

export class AdminModule { }