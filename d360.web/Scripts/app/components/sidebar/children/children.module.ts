import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';

import { SharedModule } from 'primeng/api';
import { TableModule } from 'primeng/table';

import { CoreModule } from '../../shared/core.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { TilesModule } from '../../shared/tiles/tiles.module';

import { ChildrenRoutingModule } from './children.routes';

import { ChildrenComponent } from './children.component';
import { ArtifactItemChildGridComponent } from './artifact-item-child-grid.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        //routing 
        ChildrenRoutingModule,

        //d3s        
        CoreModule,        
        TilesModule,
        SharedGridPagingInfoModule,
        SharedDynamicGridEditorModule,

        //prime        
        SharedModule,
        TableModule,
    ],
    declarations: [
        ChildrenComponent,
        ArtifactItemChildGridComponent
    ],
    providers: [

    ]
})
export class ChildrenModule { }