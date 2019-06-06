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
    EditorModule,
    InputTextModule,    
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';

import { AdminAllocationComponent } from './admin-allocation.component';
import { AdminClassificationsComponent } from './admin-classifications.component';
import { AdminComponent } from './admin.component';
import { AdminLevelEditorComponent } from './admin-level-editor.component';
import { AdminLevelListComponent } from './admin-level-list.component';
import { AdminNymAllocationsComponent } from './admin-nym-allocations.component';
import { SimpleAccordionModule } from "../shared/simple-accordion.part";
import { Dialog, DialogModule } from 'primeng/dialog';

@NgModule({
    declarations: [        
        AdminAllocationComponent,
        AdminClassificationsComponent,
        AdminComponent,                
        AdminLevelListComponent,
        AdminLevelEditorComponent,   
        AdminNymAllocationsComponent     
    ],
    exports: [
        AdminAllocationComponent,
        AdminClassificationsComponent,
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
        DropdownModule,
        EditorModule,
        ButtonModule,
        SharedModule,
        TableModule,
        Dialog,
        DialogModule,
        
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