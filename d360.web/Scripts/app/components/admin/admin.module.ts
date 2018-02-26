import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
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
import { SharedAssetTypeEditorModule } from '../shared/assettypeeditor/shared-asset-type-editor.module';


import {        
    ButtonModule,  
    ColorPickerModule,
    DropdownModule,
    SpinnerModule,
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
import { SimpleAccordionModule } from "../shared/simple-accordion.part";

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
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,
        AdminRoutingModule,

        SimpleAccordionModule,

        //primeng                
        InputTextModule,
        ColorPickerModule,
        SpinnerModule,
        DataTableModule,
        DropdownModule,
        EditorModule,
        ButtonModule,
        SharedModule,
        
        //d3s        
        CoreModule,                                      
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,    
        SharedGridPagingInfoModule,
        SharedAssetTypeEditorModule,
        TilesModule,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})

export class AdminModule { }