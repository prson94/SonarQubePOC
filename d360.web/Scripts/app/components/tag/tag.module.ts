import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../http-interceptors/govern-request.interceptor";

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
import { SharedObjectGovernanceModule } from '../shared/objectgovernance/shared-object-governance.module';
import { TagComponent } from './tag.component';
import { TagItemComponent } from './tag-item.component';
import { TagRoutingModule } from './tag.routes';
import { TagViewModule } from '../shared/tags/d3s-tag-view';
import { OverlayPanelModule } from 'primeng/primeng';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        HttpClientModule,
        RouterModule,

        TagRoutingModule,

        //primeng
        TableModule,
        OverlayPanelModule,
                
        //d3s
        CoreModule,
        D3SSharedModule,
        PipesModule,
        TilesModule,
        TagViewModule,
        
        SharedGridPagingInfoModule,
        SharedDeleteFormModule,        
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedObjectGovernanceModule,
    ],
    declarations: [
        TagComponent,
        TagItemComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true },
    ]
})
export class TagModule { }