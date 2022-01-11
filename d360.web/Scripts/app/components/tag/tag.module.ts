import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule  } from '../shared/tiles/tiles.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';

import { TableModule } from 'primeng/table';
import { TagComponent } from './tag.component';
import { TagItemComponent } from './tag-item.component';
import { TagRoutingModule } from './tag.routes';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { TagViewModule } from '../shared/tags/d3s-tag-view.module';
import { SharedAssetScoreModule } from '../shared/asset-score/shared-asset-score.module';

@NgModule({
    imports: [
        CommonModule,        
        FormsModule,

        RouterModule,

        TagRoutingModule,

        //primeng
        TableModule,
        OverlayPanelModule,
                
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
        TagViewModule,
        TilesModule,
        
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,        
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedAssetScoreModule,
    ],
    declarations: [
        TagComponent,
        TagItemComponent
    ],
    providers: [

    ]
})
export class TagModule { }