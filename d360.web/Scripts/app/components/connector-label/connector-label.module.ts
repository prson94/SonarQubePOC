import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';



import { RouterModule } from '@angular/router';

import { CoreModule } from '../shared/core.module';
import { D3SSharedModule } from '../shared/shared.module';
import { PipesModule } from '../../pipes/pipes.module';
import { TilesModule } from '../shared/tiles/tiles.module';
import { SharedDeleteFormModule } from '../shared/delete.form';
import { SharedGridPagingInfoModule } from '../shared/grid-paging-info.component';
import { SharedDynamicGridEditorModule } from '../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../shared/objectdetails/shared-object-details.module';

import { TableModule } from 'primeng/table';
import { OverlayPanelModule } from 'primeng/overlaypanel';
import { TagViewModule } from '../shared/tags/d3s-tag-view.module';
import { ConnectorLabelRoutingModule } from './connector-label.routes';
import { ConnectorLabelComponent } from './connector-label.component';
import { ConnectorLabelItemComponent } from './connector-label-item.component';
import { ConnectorLabelFormModule } from '../sidebar/connector-labels/connector-label-form.module';
import { SiteModalModule } from '../shared/modal/gov-modal.module';
import { WhereUsedModule } from '../shared/where-used/where-used.module';
import { SharedAssetScoreModule } from '../shared/asset-score/shared-asset-score.module';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,

        RouterModule,

        ConnectorLabelRoutingModule,

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
        ConnectorLabelFormModule,
        SiteModalModule,
        WhereUsedModule
    ],
    declarations: [
        ConnectorLabelComponent,
        ConnectorLabelItemComponent
    ],
    providers: [
        
    ]
})
export class ConnectorLabelModule { }