import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { CoreModule } from '../../shared/core.module';
import { TilesModule } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SharedObjectDetailsModule } from '../../shared/objectdetails/shared-object-details.module';
import { TabsModule } from '../../shared/tabs/tabs.module';
import { PageHeaderModule } from '../../shared/page-header/page-header.module';

import { AdminTagsComponent } from './admin-tags.component';
import { AdminTagsConsolidateComponent } from './admin-tags-consolidate.component';

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
import { TooltipModule } from 'primeng/tooltip';
import { TagsHeaderComponent } from './tags-headers/tags-header.component';
import { TagTypesPanelComponent } from './tag-types/tag-types.component';
import { SiteMenuModule } from '../../shared/menu/site-menu.module';
import { RouterModule } from '@angular/router';


@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        RouterModule,
        
        AdminTagsRoutingModule,

        //prime      
        ButtonModule,
        EditorModule,
        InputTextModule,
        SharedModule,
        TableModule,
        TooltipModule,

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
        AdminTagsActionModule,
        TabsModule,
        PageHeaderModule,
        SiteMenuModule,
    ],
    declarations: [
        AdminTagsComponent,
        AdminTagsConsolidateComponent,
        TagsHeaderComponent,
        TagTypesPanelComponent
    ],
    providers: [
        
    ]
    
})
export class AdminTagsModule { }