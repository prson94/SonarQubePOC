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
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';

import { ArtifactRoutingModule } from './artifact.routes';

import { ArtifactColumnFilterComponent } from './artifact-column-filter.component';
import { ArtifactComponent } from './artifact.component';
import { ArtifactDefnintionComponent } from './artifact-definition.component';
import { ArtifactGridComponent } from './artifact-grid.component';
import { ArtifactItemComponent } from './artifact-item.component';
import { ArtifactListComponent } from './artifact-list.component';
import { ArtifactTopLevelListComponent } from './artifact-top-level-list.component';
import { ArtifactTypeMetricsComponent } from './artifact-type-metrics.component';
import { ArtifactTopLevelFilterComponent } from './artifact-top-level-filter.component';

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
        
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,        
        SharedDynamicGridEditorModule,
        SharedObjectGovernanceModule,        
        TilesModule,
        WorkflowModule,        
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
        ArtifactTopLevelFilterComponent,
    ],
    providers: [
        { provide: XHRBackend, useClass: AuthenticationConnectionBackend },
    ]
})
export class ArtifactModule { }