import { NgModule }       from '@angular/core';
import { CommonModule, DeprecatedI18NPipesModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';

import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GovernRequestInterceptor } from "../../../http-interceptors/govern-request.interceptor";
import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { AdminTagsComponent } from './admin-tags.component';
import { AdminTagsActionComponent } from './admin-tags-action.component';
import { AdminTagsConsolidateComponent } from './admin-tags-consolidate.component'
import { D3SCheckboxModule } from '../../shared/controls/gov-checkbox';


import { AdminTagsRoutingModule } from './admin-tags.routes';

import {
    ButtonModule,
    EditorModule,
    InputTextModule,
    SharedModule,
} from 'primeng/primeng';

import { TableModule } from 'primeng/table';
import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { TagUsageInfoModule } from './tags-usage-info.module';
import { AdminTagsActionModule } from './admin-tags-action.module';

@NgModule({
    imports: [CommonModule,
        DeprecatedI18NPipesModule,
        FormsModule,
        
        AdminTagsRoutingModule,

        //prime      
        ButtonModule,
        EditorModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s                
        CoreModule,                
        SharedDeleteFormModule,
        SharedDynamicGridEditorModule,
        SharedObjectDetailsModule,
        SharedGridPagingInfoModule,
        TilesModule,
        SiteModalModule,
        TagUsageInfoModule,
        D3SCheckboxModule,
        AdminTagsActionModule
    ],
    declarations: [
        AdminTagsComponent,
        AdminTagsConsolidateComponent
    ],
    providers: [
        {
            provide: HTTP_INTERCEPTORS,
            useClass: GovernRequestInterceptor,
            multi: true
        },
    ]
    
})
export class AdminTagsModule { }