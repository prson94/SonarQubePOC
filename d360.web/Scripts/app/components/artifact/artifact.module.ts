import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpModule }     from '@angular/http';
import { RouterModule } from '@angular/router';

import { ChartModule } from 'angular2-highcharts';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';

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
    GrowlModule,
    InputTextModule,
    InputMaskModule,
    DataTableModule,
    TreeTableModule,
    ButtonModule,
    DropdownModule,
    CheckboxModule,
    CalendarModule,
    MenuModule,
    MenubarModule,
    AccordionModule,
    SelectButtonModule,    
    MultiSelectModule,    
    TooltipModule,
    PaginatorModule,
    TreeModule,
    FileUploadModule,
    SharedModule,
} from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        FormsModule,
        HttpModule,
        RouterModule,

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
        MenuModule,
        MenubarModule,
        AccordionModule,
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,
        TreeModule,
        PaginatorModule,
        FileUploadModule,
        SharedModule,

        //highcharts
        ChartModule,

        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
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
    ]
})
export class ArtifactModule { }