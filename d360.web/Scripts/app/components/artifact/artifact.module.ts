import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule, XHRBackend  }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { AuthenticationConnectionBackend } from '../../authentication-connection-backend';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../shared/core.module';
import { WorkflowModule } from '../workflow/workflow.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedAuditModule } from '../shared/audit/shared-audit.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDashboardModule } from '../shared/dashboard/shared-dashboard.module'
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDiagramModule } from '../shared/diagram/shared-diagram.module';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';
import { SharedResponsibilitiesModule } from '../shared/responsibilities/shared-responsibilities.module';
import { SharedRelationshipModule } from '../shared/relationship/shared-relationship.module';
import { WorkflowMonitorModule } from '../shared/workflowmonitor/workflow-monitor.module';

import { ArtifactRoutingModule } from './artifact.routes';

import { ArtifactColumnFilterComponent } from './artifact-column-filter.component';
import { ArtifactComponent } from './artifact.component';
import { ArtifactDefnintionComponent } from './artifact-definition.component';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactItemComponent } from './artifact-item.component';
import { ArtifactListComponent } from './artifact-list.component';
import { ArtifactTopLevelListComponent } from './artifact-top-level-list.component';
import { ArtifactTypeMetricsComponent } from './artifact-type-metrics.component';
import { ArtifactTypeWorkflowStatusComponent } from './artifact-type-workflow-status.component';
import { ArtifactItemChildrenComponent } from './artifact-item-children.component';
import { ArtifactItemChildGridComponent } from './artifact-item-child-grid.component';

import {    
    InputTextModule,    
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,    
    SelectButtonModule,    
    MultiSelectModule,    
    TooltipModule,        
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

        ArtifactRoutingModule,

        //primeng        
        InputTextModule,        
        DataTableModule,
        TreeTableModule,
        ButtonModule,
        DropdownModule,        
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,             
        SharedModule,

        //highcharts
        ChartModule,

        //d3s
        D3SSharedModule,
        CoreModule,        
        PipesModule,
        SharedAuditModule,
        SharedDashboardModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedDiagramModule,
        SharedDynamicGridEditorModule,
        SharedObjectGovernanceModule,
        SharedResponsibilitiesModule,
        SharedRelationshipModule,
        TilesModule,
        WorkflowModule,
        WorkflowMonitorModule,
    ],
    declarations: [        
        ArtifactColumnFilterComponent,
        ArtifactComponent,
        ArtifactDefnintionComponent,
        ArtifactGridComponent,
        ArtifactItemComponent,
        ArtifactListComponent,
        ArtifactTopLevelListComponent,
        ArtifactTypeMetricsComponent,
        ArtifactTypeWorkflowStatusComponent,
        ArtifactItemChildrenComponent,
        ArtifactItemChildGridComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class ArtifactModule { }