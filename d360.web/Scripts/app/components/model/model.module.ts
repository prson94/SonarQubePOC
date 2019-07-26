import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';


import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { ModelRoutingModule } from './model.routes';

import { ModelComponent } from './model.component';
import { ModelListComponent } from './model-list.component';
import { ModelItemComponent } from './model-item.component';
import { ModelItemStructureComponent } from './model-item-structure.component';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,        
    SelectButtonModule,    
    MultiSelectModule,    
    TooltipModule,    
    TreeModule,
    SharedModule,
} from 'primeng/primeng';
import { TableModule } from 'primeng/table';
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';


@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpModule,
        RouterModule,

        ModelRoutingModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,        
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,
        TreeModule,        
        SharedModule,
        TableModule,


        
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,  
              
        SharedGridPagingInfoModule,        
        SharedDeleteFormModule,
        SharedObjectGovernanceModule,
        SharedDynamicGridEditorModule,
        TilesModule,
    ],
    declarations: [
        ModelComponent,
        ModelListComponent,
        ModelItemComponent,
        ModelItemStructureComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class ModelModule { }