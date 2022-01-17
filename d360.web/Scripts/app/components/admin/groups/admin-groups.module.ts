import { NgModule }       from '@angular/core';
import { CommonModule }       from '@angular/common';
import { FormsModule }    from '@angular/forms';



import { CoreModule } from '../../shared/core.module';
import { TilesModule  } from '../../shared/tiles/tiles.module';
import { SharedGridPagingInfoModule } from '../../shared/grid-paging-info.component';
import { SharedDeleteFormModule } from '../../shared/delete.form';
import { SharedFieldDefinitionModule } from '../../shared/fielddefinition/shared-field-definition.module';
import { D3SSharedModule } from '../../shared/shared.module';

import { AdminGroupsComponent } from './admin-groups.component';

import { AdminGroupsRoutingModule } from './admin-groups.routes';

import { SharedModule } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DropdownModule } from 'primeng/dropdown';
import { EditorModule } from 'primeng/editor';
import { InputTextModule } from 'primeng/inputtext';

import { TableModule } from 'primeng/table';
import { ResourceMultiSelectGridModule } from '../../shared/resource-multiselect-grid.component';
import { SidePanelModule } from '../../shared/sidepanel/side-panel.module';
import { AssetDetailModule } from '../../shared/asset-detail/asset-detail.module';
import { SharedDynamicGridEditorModule } from '../../shared/dynamicgrideditor/shared-dynamic-grid-editor.module';
import { SiteModalModule } from '../../shared/modal/gov-modal.module';
import { AssetEditorModule } from '../../shared/asset-editor/asset-editor.module';
import { PropertyGroupModule } from '../../shared/controls/property-group/property-group.component';
import { PopupMenuModule } from '../../shared/controls/popup-menu/popup-menu.component';
import { SearchFieldModule } from '../../shared/controls/search-field/search-field.component';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,


        AdminGroupsRoutingModule,

        //prime
        ButtonModule,
        DropdownModule,
        EditorModule,
        InputTextModule,
        SharedModule,
        TableModule,

        //d3s        
        CoreModule,  
        D3SSharedModule,  
        ResourceMultiSelectGridModule,
        SharedDeleteFormModule,
        SharedGridPagingInfoModule,
        SharedFieldDefinitionModule,
        TilesModule,
        SidePanelModule,
        AssetDetailModule,
        SharedDynamicGridEditorModule,
        SiteModalModule,
        AssetEditorModule,
        PropertyGroupModule,
        PopupMenuModule,
        SearchFieldModule
    ],
    declarations: [
        AdminGroupsComponent
    ],
    providers: [
    ]
})
export class AdminGroupsModule { }