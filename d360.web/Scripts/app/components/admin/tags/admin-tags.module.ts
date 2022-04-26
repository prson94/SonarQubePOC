import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';

import { HTTP_INTERCEPTORS } from '@angular/common/http';

import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';

import { AdminTagsComponent } from './admin-tags.component';
import { AdminTagsConsolidateComponent } from './admin-tags-consolidate.component'

import { AdminTagsRoutingModule } from './admin-tags.routes';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { EditorModule } from 'primeng/editor';
import { TableModule } from 'primeng/table';

import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { TagUsageInfoModule } from './tags-usage-info.module';
import { AdminTagsActionModule } from './admin-tags-action.module';
import { AdvancedFiltersModule } from '../../assets-grid/advanced-filtering/advanced-filtering.module';
import { SearchFieldModule } from '../../shared/controls/search-field/search-field.component';

@NgModule({
    imports: [
        CommonModule,
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
        AdvancedFiltersModule,
        SearchFieldModule,
        AdminTagsActionModule
    ],
    declarations: [
        AdminTagsComponent,
        AdminTagsConsolidateComponent
    ],
    providers: [
        
    ]
    
})
export class AdminTagsModule { }