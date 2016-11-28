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
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDashboardModule } from '../shared/dashboard/shared-dashboard.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDiagramModule } from '../shared/diagram/shared-diagram.module';
import { SharedResponsibilitiesModule } from '../shared/responsibilities/shared-responsibilities.module';

import { ModelRoutingModule } from './model.routes';

import { ModelComponent } from './model.component';
import { ModelListComponent } from './model-list.component';
import { ModelItemComponent } from './model-item.component';
import { ModelItemStructureComponent } from './model-item-structure.component';

import {
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,    
    AccordionModule,
    SelectButtonModule,    
    MultiSelectModule,    
    TooltipModule,    
    TreeModule,
    FileUploadModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        ModelRoutingModule,

        //primeng
        GrowlModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,
        CalendarModule,        
        AccordionModule,
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,
        TreeModule,        
        FileUploadModule,
        SharedModule,

        
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,  
        SharedAuditModule,      
        SharedGridPagingInfoModule,
        SharedDashboardModule,
        SharedDeleteFormModule,
        SharedDiagramModule,
        SharedResponsibilitiesModule,
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