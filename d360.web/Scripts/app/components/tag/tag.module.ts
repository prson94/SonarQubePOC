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
import { SidePanelModule } from '../shared/sidepanel/side-panel.module';
import { DataProfileModule } from '../shared/dataprofile/dataprofile.module';
import { AssetDetailModule } from '../shared/asset-detail/asset-detail.module';
import { AssetTypeDetailModule } from '../shared/asset-type-detail/asset-type-detail.module';
import { TaggedAssetDetailModule } from '../shared/tagged-assets/tagged-assets-detail.module';
import { SemanticsModule } from '../semantic/semantics.module';

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
        SidePanelModule,
        DataProfileModule,
        AssetDetailModule,
        AssetTypeDetailModule,
        TaggedAssetDetailModule,
        SemanticsModule,
        
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