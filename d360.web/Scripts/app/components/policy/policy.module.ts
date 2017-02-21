import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedAuditModule } from '../shared/audit/shared-audit.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDiagramModule } from '../shared/diagram/shared-diagram.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedResponsibilitiesModule } from '../shared/responsibilities/shared-responsibilities.module';
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';
import { SharedRelationshipModule } from '../shared/relationship/shared-relationship.module';

import { PolicyRoutingModule } from './policy.routes';

import { PolicyComponent } from './policy.component';
import { PolicyItemComponent } from './policy-item.component';
import { PolicyItemStructureComponent } from './policy-item-structure.component';
import { PolicyListComponent } from './policy-list.component';

import {
    GrowlModule,
    InputSwitchModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,    
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

        PolicyRoutingModule,

        //primeng
        GrowlModule,
        InputSwitchModule,
        InputTextModule,
        InputMaskModule,
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,
        CheckboxModule,                
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,
        TreeModule,                
        FileUploadModule,
        SharedModule,

        //highcharts
        ChartModule,

        //d3s
        D3SSharedModule,
        CoreModule,        
        PipesModule,  
        SharedAuditModule,      
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedDiagramModule,
        SharedDynamicGridEditorModule,        
        SharedObjectGovernanceModule,
        SharedResponsibilitiesModule,
        SharedRelationshipModule,
        TilesModule,
    ],
    declarations: [
        PolicyComponent,
        PolicyItemComponent,
        PolicyItemStructureComponent,
        PolicyListComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class PolicyModule { }