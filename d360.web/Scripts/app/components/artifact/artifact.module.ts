import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { WorkflowModule } from '../workflow/workflow.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedAssetEditorsModule } from '../shared/asseteditors/shared-asset-editor.module';

import { ArtifactRoutingModule } from './artifact.routes';

import { ArtifactComponent } from './artifact.component';
import { ArtifactItemComponent } from './artifact-item.component';
import { ArtifactListComponent } from './artifact-list.component';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TreeTableModule } from 'primeng/treetable';
import { CalendarModule } from 'primeng/calendar';
import { SelectButtonModule } from 'primeng/selectbutton';
import { DropdownModule } from 'primeng/dropdown';
import { TableModule } from 'primeng/table';
import { MultiSelectModule } from 'primeng/multiselect';
import { TooltipModule } from 'primeng/tooltip';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";
import { AssetGridModule } from '../assets-grid/asset-grid.module';
import { SharedAssetScoreModule } from '../shared/asset-score/shared-asset-score.module';
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { AssetDetailModule } from "../shared/asset-detail/asset-detail.module";
import { DataProfileModule } from '../shared/dataprofile/dataprofile.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        ArtifactRoutingModule,

        //primeng        
        InputTextModule, 
        CalendarModule, 
        TreeTableModule,
        ButtonModule,
        DropdownModule,        
        SelectButtonModule,        
        MultiSelectModule,        
        TooltipModule,             
        SharedModule,
        TableModule,

        
        //d3s
        D3SSharedModule,
        CoreModule,        
        PipesModule,
        
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,        
        SharedDynamicGridEditorModule,
        SharedAssetScoreModule,
        SharedAssetEditorsModule,
        TilesModule,
        WorkflowModule,
        AssetGridModule,
        SidePanelModule,
        AssetDetailModule,
        DataProfileModule,
    ],
    declarations: [        
        ArtifactComponent,
        ArtifactItemComponent,
        ArtifactListComponent,
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        }
    ]
})

export class ArtifactModule { }
